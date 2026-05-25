using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float chaseSpeed = 8f;
    public float sprintSpeed = 12f;
    public float rotationSpeed = 10f;
    public float detectionRange = 15f;
    public float fovDetectionRange = 25f;
    public float stoppingDistance = 1f;
    public float acceleration = 8f;

    [Header("Bite")]
    public float biteRange = 1.5f;
    public float biteForce = 8f;
    public float biteCooldownDuration = 1.5f;
    public float biteWindupTime = 0.3f;

    [Header("Roaming")]
    public bool enableRoaming = true;
    public float roamRadius = 10f;
    public float minRoamDelay = 2f;
    public float maxRoamDelay = 5f;
    public float returnHomeDelay = 20f;

    [Header("AI Behavior")]
    public float patrolSpeed = 3f;
    public float searchRadius = 8f;
    public float searchTime = 5f;
    public float maxChaseDistance = 999f;
    public float fovAngle = 120f;
    public float reactionTime = 0.3f;
    public float alertLevel = 0f;
    public float alertDecayRate = 2f;
    public float alertThreshold = 30f;

    [Header("Gizmos")]
    public bool showDetectionRange = true;
    public bool showRoamRange = true;
    public bool showPathLine = true;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private Vector3 homePosition;
    private Vector3 lastKnownNpcPosition;
    private float nextRoamTime;
    private float returnHomeTime;
    private bool isChasing;
    private bool isSearching;
    private bool isAttacking;
    private float biteCooldown;
    private float attackWindup;
    private Transform currentTarget;
    private float targetLostTime;
    private float graceChaseTimer;
    private float stuckTimer;
    private Vector3 lastStuckPos;
    private NavMeshPath currentPath;
    private Transform[] npcCache;
    private float npcRefreshTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        homePosition = transform.position;
        currentPath = new NavMeshPath();

        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.acceleration = 25f;
        agent.autoBraking = false;
        agent.updateRotation = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        SetRandomRoamTime();
    }

    void Update()
    {
        if (npcRefreshTimer <= 0)
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag("Npc");
            npcCache = new Transform[objs.Length];
            for (int i = 0; i < objs.Length; i++)
                npcCache[i] = objs[i].transform;
            npcRefreshTimer = 0.2f;
        }
        npcRefreshTimer -= Time.deltaTime;

        alertLevel = Mathf.Max(0, alertLevel - alertDecayRate * Time.deltaTime);

        Transform bestTarget = FindBestTarget();
        bool hasTarget = bestTarget != null;
        float dist = hasTarget ? Vector3.Distance(transform.position, bestTarget.position) : float.MaxValue;
        bool seeTarget = hasTarget && CanSeeTarget(bestTarget);
        float effectiveRange = seeTarget ? fovDetectionRange : detectionRange;
        bool inRange = hasTarget && dist <= effectiveRange && IsReachable(bestTarget);

        if (isAttacking)
        {
            HandleAttackState(currentTarget);
        }
        else if (inRange)
        {
            if (alertLevel < 100) alertLevel += 50 * Time.deltaTime;
            ChaseTarget(bestTarget, dist);
            graceChaseTimer = 3f;

            if (dist <= biteRange && Time.time >= biteCooldown)
                StartAttack(bestTarget);
        }
        else if (graceChaseTimer > 0 && currentTarget != null)
        {
            graceChaseTimer -= Time.deltaTime;
            float graceDist = Vector3.Distance(transform.position, currentTarget.position);
            if (graceDist <= maxChaseDistance)
            {
                ChaseTarget(currentTarget, graceDist);
            }
            else
            {
                currentTarget = null;
                graceChaseTimer = 0;
            }
        }
        else if (isChasing || isSearching)
        {
            SearchForNpc();
        }
        else
        {
            updateRoaming();
        }

        if (isSearching && Time.time >= returnHomeTime && Vector3.Distance(transform.position, homePosition) > 5f)
            ReturnHome();

        float speed = DetermineTargetSpeed();
        agent.speed = speed;
        agent.stoppingDistance = isChasing || isSearching || isAttacking ? 0f : stoppingDistance;
        agent.acceleration = isChasing || isSearching || isAttacking ? 50f : acceleration;

        Vector3 desired = agent.desiredVelocity;
        float vel = new Vector3(desired.x, 0, desired.z).magnitude;
        if (vel > 0.1f)
        {
            Vector3 dir = new Vector3(desired.x, 0, desired.z).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
        }

        if (agent.hasPath && vel < 0.5f && agent.remainingDistance > agent.stoppingDistance + 0.5f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 1.5f)
            {
                agent.Warp(transform.position + Vector3.up * 0.1f);
                agent.ResetPath();
                if (currentTarget != null)
                    agent.SetDestination(currentTarget.position);
                else
                    updateRoaming();
                stuckTimer = 0;
            }
        }
        else
        {
            stuckTimer = 0;
        }
    }

    float DetermineTargetSpeed()
    {
        if (isChasing && currentTarget != null)
        {
            float d = Vector3.Distance(transform.position, currentTarget.position);
            if (d < biteRange * 2f) return sprintSpeed * 1.3f;
            if (d < detectionRange * 0.8f) return sprintSpeed;
            return chaseSpeed * 1.5f;
        }
        return isSearching ? chaseSpeed : (walkSpeed + patrolSpeed) * 0.5f;
    }

    Transform FindBestTarget()
    {
        Transform best = null;
        float bestDist = float.MaxValue;
        Vector3 myPos = transform.position;

        foreach (var t in npcCache)
        {
            if (t == null) continue;
            RoamingAI ai = t.GetComponent<RoamingAI>();
            if (ai == null || !ai.enabled) continue;

            float dist = Vector3.Distance(myPos, t.position);
            if (dist > maxChaseDistance) continue;
            if (dist >= bestDist) continue;

            bestDist = dist;
            best = t;
        }
        return best;
    }

    bool IsReachable(Transform target)
    {
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path))
        {
            return path.status == NavMeshPathStatus.PathComplete;
        }
        return false;
    }

    bool CanSeeTarget(Transform target)
    {
        if (target == null) return false;
        Vector3 dir = (target.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= detectionRange) return true;

        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > fovAngle * 0.5f) return false;
        if (dist > fovDetectionRange) return false;

        int mask = ~LayerMask.GetMask("Npc", "Enemy");
        return !Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, dist, mask);
    }

    bool HasLineOfSight(Transform target)
    {
        Vector3 dir = (target.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, target.position);
        int mask = ~LayerMask.GetMask("Npc", "Enemy");
        return !Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, dist, mask);
    }

    void ChaseTarget(Transform target, float dist)
    {
        isChasing = true;
        isSearching = false;
        currentTarget = target;
        lastKnownNpcPosition = target.position;
        agent.stoppingDistance = 0f;
        agent.acceleration = 50f;
        agent.SetDestination(target.position);
        NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, currentPath);
    }

    void StartAttack(Transform npc)
    {
        isAttacking = true;
        attackWindup = Time.time + biteWindupTime;
    }

    void HandleAttackState(Transform npc)
    {
        if (npc == null)
        {
            isAttacking = false;
            return;
        }
        RoamingAI npcAI = npc.GetComponent<RoamingAI>();
        if (npcAI == null || !npcAI.enabled)
        {
            isAttacking = false;
            return;
        }

        float dist = Vector3.Distance(transform.position, npc.position);
        if (Time.time < attackWindup)
        {
            if (dist > biteRange * 1.2f) isAttacking = false;
        }
        else if (dist <= biteRange)
        {
            BiteNpc(npc);
            isAttacking = false;
        }
        else
        {
            isAttacking = false;
        }
    }

    void BiteNpc(Transform npc)
    {
        biteCooldown = Time.time + biteCooldownDuration;

        RoamingAI ai = npc.GetComponent<RoamingAI>();
        if (ai != null) ai.Die();

        Rigidbody rb = npc.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce((transform.forward + Vector3.up) * biteForce, ForceMode.Impulse);
        }

        currentTarget = null;
        graceChaseTimer = 0;

        Transform next = FindBestTarget();
        if (next != null && Vector3.Distance(transform.position, next.position) <= detectionRange * 1.2f && IsReachable(next))
        {
            ChaseTarget(next, Vector3.Distance(transform.position, next.position));
        }
        else
        {
            isChasing = false;
            isSearching = true;
            returnHomeTime = Time.time + searchTime;
        }
    }

    void SearchForNpc()
    {
        if (isChasing)
        {
            isChasing = false;
            isSearching = true;
            returnHomeTime = Time.time + searchTime;
        }

        if (Time.time >= nextRoamTime || isChasing)
        {
            Vector3 center = lastKnownNpcPosition;
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * searchRadius;
                Vector3 point = center + offset;
                if (NavMesh.SamplePosition(point, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, currentPath);
                    break;
                }
            }
            SetRandomRoamTime();
        }
    }

    void ReturnHome()
    {
        isSearching = false;
        isChasing = false;
        currentTarget = null;
        agent.SetDestination(homePosition);
        NavMesh.CalculatePath(transform.position, homePosition, NavMesh.AllAreas, currentPath);
    }

    void updateRoaming()
    {
        if (!enableRoaming) return;

        if (Time.time >= nextRoamTime || agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 dir = Random.insideUnitSphere * roamRadius + homePosition;
            if (NavMesh.SamplePosition(dir, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, currentPath);
            }
            SetRandomRoamTime();
        }
    }

    void SetRandomRoamTime()
    {
        nextRoamTime = Time.time + Random.Range(minRoamDelay, maxRoamDelay);
    }

    void OnDrawGizmosSelected()
    {
        if (showDetectionRange)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
        if (showRoamRange && enableRoaming)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, roamRadius);
        }
        if (showPathLine && currentPath != null && currentPath.corners.Length > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < currentPath.corners.Length - 1; i++)
                Gizmos.DrawLine(currentPath.corners[i], currentPath.corners[i + 1]);
        }

        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1, 0, 0, 0.15f);
        float r = fovDetectionRange;
        Vector3 fwd = transform.forward * r;
        Vector3 right = Quaternion.Euler(0, fovAngle * 0.5f, 0) * fwd;
        Vector3 left = Quaternion.Euler(0, -fovAngle * 0.5f, 0) * fwd;
        Gizmos.DrawLine(transform.position, transform.position + right);
        Gizmos.DrawLine(transform.position, transform.position + left);

        int rays = 16;
        Vector3 prev = right;
        for (int i = 1; i <= rays; i++)
        {
            float a = Mathf.Lerp(-fovAngle * 0.5f, fovAngle * 0.5f, (float)i / rays);
            Vector3 d = Quaternion.Euler(0, a, 0) * transform.forward * r;
            Gizmos.DrawLine(transform.position, transform.position + d);
            Gizmos.DrawLine(transform.position + prev, transform.position + d);
            prev = d;
        }
    }
}
