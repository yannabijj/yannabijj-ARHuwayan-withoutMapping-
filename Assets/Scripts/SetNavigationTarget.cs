using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SetNavigationTarget : MonoBehaviour
{
    [SerializeField]
    private Camera topDownCamera; // Top-down camera
    [SerializeField]
    private GameObject navTargetObject; // Target object for navigation

    private NavMeshPath path; // Stores the calculated navigation path
    private LineRenderer line; // LineRenderer to display the navigation line
    private bool isNavigating = false; // Tracks if navigation is active
    private float destinationThreshold = 1.0f; // Distance threshold for detecting arrival at destination

    private void Start()
    {
        path = new NavMeshPath();
        line = GetComponent<LineRenderer>();
        line.enabled = false; // Disable the LineRenderer initially
        Debug.Log("SetNavigationTarget initialized. LineRenderer disabled at start.");
    }

    private void Update()
    {
        if (isNavigating)
        {
            UpdateNavigationLine(); // Continuously update the navigation line as the player moves
        }
    }

    public void UpdateTargetPosition(Vector3 targetPosition)
    {
        Debug.Log("Updating target position to: " + targetPosition);

        // Set the navigation target object's position
        navTargetObject.transform.position = targetPosition;

        // Calculate the path from the current position to the target
        bool pathFound = NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, path);

        if (pathFound && path.corners.Length > 0)
        {
            Debug.Log($"Path successfully calculated with {path.corners.Length} corners.");
            isNavigating = true; // Enable navigation
            line.enabled = true; // Enable the LineRenderer
            DrawPath(); // Draw the navigation line
            StartCoroutine(CheckIfDestinationReached(targetPosition)); // Monitor for arrival at the destination
        }
        else
        {
            Debug.LogError("Path calculation failed or no valid path found.");
            ClearNavigationLine(); // Clear the line if no path is found
        }
    }

    private void UpdateNavigationLine()
    {
        if (path.corners.Length > 0)
        {
            // Recalculate the path dynamically as the player moves
            bool pathFound = NavMesh.CalculatePath(transform.position, navTargetObject.transform.position, NavMesh.AllAreas, path);

            if (pathFound && path.corners.Length > 0)
            {
                line.positionCount = path.corners.Length;
                line.SetPositions(path.corners);
                Debug.Log("Navigation line updated.");
            }
            else
            {
                Debug.LogWarning("Path recalculation failed during navigation.");
                ClearNavigationLine();
            }
        }
    }

    private void DrawPath()
    {
        if (path.corners.Length > 0)
        {
            line.positionCount = path.corners.Length; // Set the number of positions in the LineRenderer
            line.SetPositions(path.corners); // Set the positions from the path corners
            Debug.Log("Navigation line drawn.");
        }
    }

    private IEnumerator CheckIfDestinationReached(Vector3 targetPosition)
    {
        while (isNavigating)
        {
            // Check the distance between the current position and the target
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance <= destinationThreshold)
            {
                Debug.Log("Destination reached. Clearing navigation line.");
                ClearNavigationLine();
                isNavigating = false; // Stop navigation
                yield break;
            }
            yield return new WaitForSeconds(0.1f); // Check periodically
        }
    }

    private void ClearNavigationLine()
    {
        if (line != null)
        {
            line.positionCount = 0; // Reset the LineRenderer
            line.enabled = false; // Disable the LineRenderer
            Debug.Log("Navigation line cleared.");
        }
    }
}
