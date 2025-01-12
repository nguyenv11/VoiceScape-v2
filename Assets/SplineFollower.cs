using UnityEngine;
using UnityEngine.Splines;

public class SplineFollower : MonoBehaviour
{
    [Header("Spline Settings")]
    // Reference to the SplineContainer with the spline data.
    public SplineContainer splineContainer;
    
    [Header("Movement Settings")]
    // Duration (in seconds) for the object to travel the complete spline.
    public float duration = 5f;

    private float elapsedTime = 0f;

    void Update()
    {
        // Check if we have a valid spline.
        if (splineContainer == null || splineContainer.Spline == null)
        {
            Debug.LogWarning("SplineContainer or its Spline is not assigned.");
            return;
        }
        
        // Update elapsed time and normalize it to a t value in [0, 1]
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / duration);

        // Evaluate the spline position at normalized parameter t
        // The EvaluatePosition method returns the position along the spline.
        Vector3 position = splineContainer.Spline.EvaluatePosition(t);
        transform.position = position;

        // Optionally, update rotation based on the spline's tangent.
        // EvaluateTangent returns the direction along the spline.
        Vector3 tangent = splineContainer.Spline.EvaluateTangent(t);
        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent);
        }

        // (Optional) Reset or loop when reaching the end.
        if (t >= 1f)
        {
            // For example, you can reset elapsedTime to 0 to loop the movement.
            // elapsedTime = 0f;
            // Or you could simply stop updating.
        }
    }
}