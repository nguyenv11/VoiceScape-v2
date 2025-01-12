using UnityEngine;

public class BuddhaPickupMaterial : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Material materialInstance;

    private void Awake()
    {
        // Get the renderer
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogError("No MeshRenderer found!");
                return;
            }
        }

        // Create a material instance and set up for transparency
        if (meshRenderer.sharedMaterial != null)
        {
            materialInstance = new Material(meshRenderer.sharedMaterial);
            
            // Configure material for transparency
            materialInstance.SetFloat("_Surface", 1); // 0 = opaque, 1 = transparent
            materialInstance.SetFloat("_Blend", 0);   // 0 = alpha, 1 = premultiply
            
            // Set blend mode
            materialInstance.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            materialInstance.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            
            // Enable transparency keywords
            materialInstance.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            materialInstance.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            
            // Set render queue for transparency
            materialInstance.renderQueue = 3000;
            
            meshRenderer.material = materialInstance;
            Debug.Log($"Created transparent material instance for {gameObject.name}. Shader: {materialInstance.shader.name}");
        }
    }

    public void SetColor(Color color)
    {
        if (materialInstance != null)
        {
            Debug.Log($"Setting material color to {color} on {gameObject.name}");
            materialInstance.SetColor("_BaseColor", color);  // URP uses _BaseColor instead of color
            materialInstance.SetColor("_Color", color);      // Backup in case shader variant uses _Color
        }
        else
        {
            Debug.LogError("No material instance!");
        }
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}