using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Rigidbody))]
public class RoamingAI : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float rotationSpeed = 10f;
    public float stoppingDistance = 1f;
    public float minWanderDistance = 5f;
    public float maxWanderDistance = 15f;
    public float minWaitTime = 1f;
    public float maxWaitTime = 4f;

    [Header("Awareness")]
    public float fleeDetectionRange = 10f;
    public float fovAwareRange = 18f;
    public float fovAngle = 120f;
    public float fleeBoostDuration = 1f;
    public float panicDuration = 3f;

    [Header("Death")]
    public Material deathMaterial;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private bool isFleeing;
    private bool isMoving;
    private float waitTimer;
    private bool hasDestination;
    private Transform[] enemyCache;
    private float enemyRefreshTimer;
    private float fleeStartTime;
    private Transform currentFleeTarget;
    private Vector3 fleeDirection;
    private bool isInPanic;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        agent.speed = walkSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false;
        agent.acceleration = 15f;
        agent.autoBraking = false;
    }

    void Update()
    {
        if (!agent.isOnNavMesh || !enabled) return;

        if (enemyRefreshTimer <= 0)
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag("Enemy");
            enemyCache = new Transform[objs.Length];
            for (int i = 0; i < objs.Length; i++)
                enemyCache[i] = objs[i].transform;
            enemyRefreshTimer = 0.3f;
        }
        enemyRefreshTimer -= Time.deltaTime;

        Transform nearestEnemy = FindNearestEnemy();
        bool enemyInRange = nearestEnemy != null && Vector3.Distance(transform.position, nearestEnemy.position) <= fleeDetectionRange;

        if (enemyInRange && !isFleeing)
        {
            isFleeing = true;
            fleeStartTime = Time.time;
            isInPanic = true;
            currentFleeTarget = nearestEnemy;
            fleeDirection = (transform.position - nearestEnemy.position).normalized;
            waitTimer = 0;
            hasDestination = false;
        }

        if (isFleeing)
        {
            if (nearestEnemy != null && Vector3.Distance(transform.position, nearestEnemy.position) <= fleeDetectionRange)
            {
                currentFleeTarget = nearestEnemy;
                fleeDirection = (transform.position - nearestEnemy.position).normalized;
                FleeFrom(nearestEnemy);
            }
            else if (Time.time - fleeStartTime > fleeBoostDuration)
            {
                isFleeing = false;
                isInPanic = false;
                hasDestination = false;
            }
        }

        if (Time.time - fleeStartTime > panicDuration)
            isInPanic = false;

        float currentSpeed = isInPanic ? runSpeed * 1.3f : (isFleeing ? runSpeed : walkSpeed);
        agent.speed = currentSpeed;

        if (!isFleeing)
        {
            if (!hasDestination)
            {
                SetRandomDestination();
                hasDestination = true;
                isMoving = true;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (isMoving)
                {
                    isMoving = false;
                    waitTimer = Random.Range(minWaitTime, maxWaitTime);
                }
                if (waitTimer > 0)
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0)
                    {
                        SetRandomDestination();
                        isMoving = true;
                        waitTimer = 0;
                    }
                }
            }
            else
            {
                isMoving = true;
            }
        }
        else
        {
            isMoving = true;
        }

        Vector3 desired = agent.desiredVelocity;
        float speed = new Vector3(desired.x, 0, desired.z).magnitude;
        bool moving = isMoving && speed > 0.1f;

        if (moving)
        {
            Vector3 dir = new Vector3(desired.x, 0, desired.z).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
        }
    }

    Transform FindNearestEnemy()
    {
        Transform nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var enemy in enemyCache)
        {
            if (enemy == null) continue;
            float d = Vector3.Distance(transform.position, enemy.position);
            bool canSense = d <= fleeDetectionRange || CanSeeEnemy(enemy, d);
            if (!canSense) continue;
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = enemy;
            }
        }
        return nearest;
    }

    bool CanSeeEnemy(Transform enemy, float dist)
    {
        if (dist > fovAwareRange) return false;

        Vector3 dir = (enemy.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > fovAngle * 0.5f)
            return false;

        return HasLineOfSight(enemy);
    }

    bool HasLineOfSight(Transform target)
    {
        Vector3 dir = (target.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, target.position);
        int mask = ~LayerMask.GetMask("Npc", "Enemy");
        return !Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, dist, mask);
    }

    void FleeFrom(Transform enemy)
    {
        Vector3 dir = (transform.position - enemy.position).normalized;
        Vector3 fleePoint = transform.position + dir * fleeDetectionRange * 1.5f;

        fleePoint += new Vector3(dir.z, 0, -dir.x) * Random.Range(-3f, 3f);

        if (NavMesh.SamplePosition(fleePoint, out NavMeshHit hit, fleeDetectionRange * 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void SetRandomDestination()
    {
        Vector3 dir = Random.insideUnitSphere * maxWanderDistance + transform.position;
        if (NavMesh.SamplePosition(dir, out NavMeshHit hit, maxWanderDistance, NavMesh.AllAreas)
            && Vector3.Distance(transform.position, hit.position) >= minWanderDistance)
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            waitTimer = 0.1f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawWireSphere(transform.position, fleeDetectionRange);

        Gizmos.color = new Color(1, 0.8f, 0, 0.12f);
        float fovRadius = fovAwareRange;
        Vector3 fwd = transform.forward * fovRadius;
        Vector3 right = Quaternion.Euler(0, fovAngle * 0.5f, 0) * fwd;
        Vector3 left = Quaternion.Euler(0, -fovAngle * 0.5f, 0) * fwd;

        Gizmos.DrawRay(transform.position, right);
        Gizmos.DrawRay(transform.position, left);

        int rays = 12;
        Vector3 prev = right;
        for (int i = 1; i <= rays; i++)
        {
            float a = Mathf.Lerp(-fovAngle * 0.5f, fovAngle * 0.5f, (float)i / rays);
            Vector3 dir = Quaternion.Euler(0, a, 0) * transform.forward * fovRadius;
            Gizmos.DrawLine(transform.position, transform.position + dir);
            Gizmos.DrawLine(transform.position + prev, transform.position + dir);
            prev = dir;
        }
    }

    public void Die()
    {
        if (deathMaterial != null)
        {
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
                r.material = deathMaterial;
        }

        agent.enabled = false;
        rb.isKinematic = false;
        enabled = false;
    }
}