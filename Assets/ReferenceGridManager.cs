using UnityEngine;

public class ReferenceGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private float gridSpacing = 5f;
    [SerializeField] private int gridSize = 5;
    [SerializeField] private Transform player;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.3f);
    
    private GameObject[] verticalLines;
    private GameObject[] horizontalLines;
    private float updateInterval = 0.1f;
    private float nextUpdate;

    private void Start()
    {
        CreateGrid();
    }

    private void CreateGrid()
    {
        int lineCount = gridSize * 2 + 1;
        verticalLines = new GameObject[lineCount];
        horizontalLines = new GameObject[lineCount];

        for (int i = 0; i < lineCount; i++)
        {
            // Create vertical lines
            GameObject vLine = new GameObject($"VerticalLine_{i}");
            vLine.transform.parent = transform;
            LineRenderer vRenderer = vLine.AddComponent<LineRenderer>();
            SetupLineRenderer(vRenderer);
            verticalLines[i] = vLine;

            // Create horizontal lines
            GameObject hLine = new GameObject($"HorizontalLine_{i}");
            hLine.transform.parent = transform;
            LineRenderer hRenderer = hLine.AddComponent<LineRenderer>();
            SetupLineRenderer(hRenderer);
            horizontalLines[i] = hLine;
        }
    }

    private void SetupLineRenderer(LineRenderer renderer)
    {
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        renderer.material.color = lineColor;
        renderer.startWidth = lineWidth;
        renderer.endWidth = lineWidth;
        renderer.positionCount = 2;
    }

    private void Update()
    {
        if (Time.time < nextUpdate || player == null) return;
        nextUpdate = Time.time + updateInterval;

        Vector3 playerPos = player.position;
        playerPos.y = 0; // Keep reference point at ground level
        float heightOffset = 0.1f; // Slight offset to prevent z-fighting

        int lineCount = gridSize * 2 + 1;
        float startOffset = -gridSize * gridSpacing;

        // Update vertical lines
        for (int i = 0; i < lineCount; i++)
        {
            float xOffset = startOffset + (i * gridSpacing);
            Vector3 startPoint = new Vector3(playerPos.x + xOffset, 0, playerPos.z - gridSize * gridSpacing);
            Vector3 endPoint = new Vector3(playerPos.x + xOffset, 0, playerPos.z + gridSize * gridSpacing);
            
            // Get terrain heights at start and end points
            float startHeight = GetTerrainHeight(startPoint) + heightOffset;
            float endHeight = GetTerrainHeight(endPoint) + heightOffset;
            
            startPoint.y = startHeight;
            endPoint.y = endHeight;
            
            LineRenderer renderer = verticalLines[i].GetComponent<LineRenderer>();
            renderer.SetPosition(0, startPoint);
            renderer.SetPosition(1, endPoint);
        }

        // Update horizontal lines
        for (int i = 0; i < lineCount; i++)
        {
            float zOffset = startOffset + (i * gridSpacing);
            Vector3 startPoint = new Vector3(playerPos.x - gridSize * gridSpacing, 0, playerPos.z + zOffset);
            Vector3 endPoint = new Vector3(playerPos.x + gridSize * gridSpacing, 0, playerPos.z + zOffset);
            
            // Get terrain heights at start and end points
            float startHeight = GetTerrainHeight(startPoint) + heightOffset;
            float endHeight = GetTerrainHeight(endPoint) + heightOffset;
            
            startPoint.y = startHeight;
            endPoint.y = endHeight;
            
            LineRenderer renderer = horizontalLines[i].GetComponent<LineRenderer>();
            renderer.SetPosition(0, startPoint);
            renderer.SetPosition(1, endPoint);
        }
    }

    private float GetTerrainHeight(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
        {
            return hit.point.y;
        }
        return 0f;
    }
}