using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject npc1Prefab;
    public GameObject npc2Prefab;
    public GameObject enemyPrefab;

    [Header("Counts")]
    public int npcCount = 10;

    [Header("Spawn Area")]
    public float spreadWidth = 30f;
    public float spreadDepth = 30f;
    public bool randomizeRotation = true;

    void Start()
    {
        for (int i = 0; i < npcCount; i++)
        {
            GameObject prefab = Random.value < 0.5f ? npc1Prefab : npc2Prefab;
            SpawnOne(prefab, "Npc");
        }

        if (enemyPrefab != null)
            SpawnOne(enemyPrefab, "Enemy");
        else
            Debug.LogError("NPCSpawner: No enemyPrefab assigned.");
    }

    void SpawnOne(GameObject prefab, string tag)
    {
        if (prefab == null) return;

        Vector3 pos = FindValidSpawnPosition();
        if (pos == Vector3.zero) return;

        Quaternion rot = randomizeRotation ? Quaternion.Euler(0, Random.Range(0f, 360f), 0) : Quaternion.identity;
        GameObject obj = Instantiate(prefab, pos, rot, transform);
        obj.tag = tag;
    }

    Vector3 FindValidSpawnPosition()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            float x = Random.Range(-spreadWidth * 0.5f, spreadWidth * 0.5f);
            float z = Random.Range(-spreadDepth * 0.5f, spreadDepth * 0.5f);
            Vector3 samplePos = transform.position + new Vector3(x, 100f, z);

            if (NavMesh.SamplePosition(samplePos, out NavMeshHit hit, 200f, NavMesh.AllAreas))
                return hit.position;
        }
        return Vector3.zero;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = transform.position;
        float hw = spreadWidth * 0.5f;
        float hd = spreadDepth * 0.5f;

        Vector3 bl = c + new Vector3(-hw, 0, -hd);
        Vector3 br = c + new Vector3(hw, 0, -hd);
        Vector3 tl = c + new Vector3(-hw, 0, hd);
        Vector3 tr = c + new Vector3(hw, 0, hd);

        Gizmos.color = new Color(0, 1f, 0.5f, 0.1f);
        Gizmos.DrawCube(c, new Vector3(spreadWidth, 0.1f, spreadDepth));

        Gizmos.color = new Color(0, 1f, 0.5f, 0.6f);
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);

        Gizmos.color = new Color(0, 1f, 0.5f, 0.8f);
        int gizmoCount = Mathf.Min(npcCount, 20);
        for (int i = 0; i < gizmoCount; i++)
        {
            float x = ((i + 0.5f) / gizmoCount) * spreadWidth - hw;
            float z = ((i * 7f + 3f) % gizmoCount) / gizmoCount * spreadDepth - hd;
            Vector3 gizmoPos = c + new Vector3(x, 0, z);

            if (NavMesh.SamplePosition(gizmoPos + Vector3.up * 100f, out NavMeshHit hit, 200f, NavMesh.AllAreas))
                gizmoPos = hit.position;

            Gizmos.DrawSphere(gizmoPos, 0.4f);
        }
    }
}
