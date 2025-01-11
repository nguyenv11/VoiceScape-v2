using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LandmarkManager : MonoBehaviour
{
    [System.Serializable]
    public class LandmarkType
    {
        public PrimitiveType primitiveType;
        public int count = 10;
        public Vector2 scaleRange = new Vector2(1f, 3f);
        public float minSpacing = 10f;
    }

    [Header("Landmark Settings")]
    [SerializeField] private LandmarkType[] landmarkTypes;
    [SerializeField] private float spawnRadius = 100f;
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask terrainLayer;

    [Header("Performance")]
    [SerializeField] private float spawnDelay = 2f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private int spawnsPerInterval = 3;
    [SerializeField] private float updateDistance = 50f;

    [Header("Visual Settings")]
    [SerializeField] private bool useDistanceColors = true;
    [SerializeField] private Color nearColor = new Color(1f, 0.4f, 0.4f, 1f); // Warm red
    [SerializeField] private Color farColor = new Color(0.4f, 0.4f, 1f, 1f);  // Cool blue
    [SerializeField] private float cardinalMarkerScale = 4f;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private Vector3 lastSpawnPosition;
    private List<GameObject> activeLandmarks = new List<GameObject>();
    private List<GameObject> cardinalMarkers = new List<GameObject>();
    private Queue<SpawnRequest> spawnQueue = new Queue<SpawnRequest>();
    private bool isSpawning = false;

    private struct SpawnRequest
    {
        public LandmarkType type;
        public Vector3 center;
        public bool isCardinal;
        public string label;
    }

    private void Start()
    {
        Debug.Log("LandmarkManager initialized");
        
        if (player == null)
        {
            player = GameObject.Find("[BuildingBlock] Camera Rig")?.transform;
        }

        // Start delayed spawn coroutine
        StartCoroutine(DelayedInitialSpawn());
    }

    private IEnumerator DelayedInitialSpawn()
    {
        yield return new WaitForSeconds(spawnDelay);
        CreateCardinalMarkers();
        InitiateSpawnCycle(player.position);
    }

    private void CreateCardinalMarkers()
    {
        string[] cardinals = { "N", "E", "S", "W" };
        float[] angles = { 0, 90, 180, 270 };
        
        for (int i = 0; i < cardinals.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0, angles[i], 0) * Vector3.forward;
            Vector3 position = player.position + direction * (spawnRadius * 0.8f);
            
            SpawnRequest request = new SpawnRequest 
            { 
                type = landmarkTypes[0],
                center = position,
                isCardinal = true,
                label = cardinals[i]
            };
            spawnQueue.Enqueue(request);
        }
    }

    private void Update()
    {
        if (Vector3.Distance(player.position, lastSpawnPosition) > updateDistance)
        {
            InitiateSpawnCycle(player.position);
        }

        if (!isSpawning && spawnQueue.Count > 0)
        {
            StartCoroutine(ProcessSpawnQueue());
        }

        // Update cardinal markers
        UpdateCardinalMarkers();
    }

    private void UpdateCardinalMarkers()
    {
        foreach (var marker in cardinalMarkers)
        {
            if (marker != null)
            {
                // Keep cardinal markers at fixed radius from player
                Vector3 directionFromPlayer = (marker.transform.position - player.position).normalized;
                Vector3 targetPos = player.position + directionFromPlayer * (spawnRadius * 0.8f);
                targetPos.y = marker.transform.position.y; // Maintain current height
                marker.transform.position = Vector3.Lerp(marker.transform.position, targetPos, Time.deltaTime);
            }
        }
    }

    private void InitiateSpawnCycle(Vector3 center)
    {
        lastSpawnPosition = center;
        spawnQueue.Clear();

        // Regular landmarks
        foreach (var type in landmarkTypes)
        {
            for (int i = 0; i < type.count; i++)
            {
                spawnQueue.Enqueue(new SpawnRequest { type = type, center = center });
            }
        }
    }

    private IEnumerator ProcessSpawnQueue()
    {
        isSpawning = true;

        while (spawnQueue.Count > 0)
        {
            for (int i = 0; i < spawnsPerInterval && spawnQueue.Count > 0; i++)
            {
                var request = spawnQueue.Dequeue();
                if (request.isCardinal)
                {
                    CreateCardinalMarker(request);
                }
                else
                {
                    TrySpawnLandmark(request.type, request.center);
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
        CleanupDistantLandmarks();
    }

    private void CreateCardinalMarker(SpawnRequest request)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.transform.position = request.center + Vector3.up * 2f;
        marker.transform.localScale = Vector3.one * cardinalMarkerScale;

        // Create text label
        GameObject label = new GameObject($"Label_{request.label}");
        label.transform.parent = marker.transform;
        
        // Create bright, emissive material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        mat.color = Color.white;
        marker.GetComponent<Renderer>().material = mat;

        marker.transform.parent = transform;
        cardinalMarkers.Add(marker);
    }

    private void TrySpawnLandmark(LandmarkType type, Vector3 center)
    {
        for (int attempts = 0; attempts < 5; attempts++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = center + new Vector3(randomCircle.x, 100f, randomCircle.y);

            if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 200f, terrainLayer))
            {
                if (IsPositionValid(hit.point, type.minSpacing))
                {
                    CreateLandmark(type, hit.point);
                    return;
                }
            }
        }
    }

    private bool IsPositionValid(Vector3 position, float minSpacing)
    {
        foreach (var landmark in activeLandmarks)
        {
            if (landmark != null && Vector3.Distance(position, landmark.transform.position) < minSpacing)
            {
                return false;
            }
        }
        return true;
    }

    private void CreateLandmark(LandmarkType type, Vector3 position)
    {
        GameObject landmark = GameObject.CreatePrimitive(type.primitiveType);
        
        // Calculate distance-based properties
        float distanceFromPlayer = Vector3.Distance(player.position, position);
        float distanceRatio = distanceFromPlayer / spawnRadius;
        
        // Height scales with distance
        float heightScale = Mathf.Lerp(type.scaleRange.x, type.scaleRange.y, distanceRatio);
        Vector3 scale = new Vector3(1f, heightScale, 1f) * 2f;
        landmark.transform.localScale = scale;

        // Position with slight elevation based on distance
        float elevationBoost = distanceRatio * 2f;
        landmark.transform.position = position + Vector3.up * (2f + elevationBoost);
        landmark.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        // Create material with distance-based color
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        if (useDistanceColors)
        {
            mat.color = Color.Lerp(nearColor, farColor, distanceRatio);
        }
        else
        {
            mat.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.8f, 1f);
        }
        landmark.GetComponent<Renderer>().material = mat;

        landmark.transform.parent = transform;
        activeLandmarks.Add(landmark);
    }

    private void CleanupDistantLandmarks()
    {
        float cleanupDistance = spawnRadius * 2f;
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var landmark in activeLandmarks)
        {
            if (landmark != null && Vector3.Distance(player.position, landmark.transform.position) > cleanupDistance)
            {
                toRemove.Add(landmark);
            }
        }

        foreach (var landmark in toRemove)
        {
            activeLandmarks.Remove(landmark);
            Destroy(landmark);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        // Draw spawn radius
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(player.position, spawnRadius);

        // Draw cleanup radius
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawWireSphere(player.position, spawnRadius * 2f);

        // Draw cardinal directions
        if (player != null)
        {
            Gizmos.color = Color.white;
            Vector3[] directions = {
                Vector3.forward, Vector3.right,
                Vector3.back, Vector3.left
            };
            foreach (var dir in directions)
            {
                Gizmos.DrawLine(
                    player.position,
                    player.position + dir * (spawnRadius * 0.8f)
                );
            }
        }
    }
}