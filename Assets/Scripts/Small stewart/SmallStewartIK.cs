using UnityEngine;

public class StewartPlatformController : MonoBehaviour
{

    [SerializeField] bool manualControl = false; 
    [SerializeField] private Transform desiredPlatformPose; // Desired 6DOF pose of the platform
    [SerializeField] private ArticulationBody[] cylinders = new ArticulationBody[6]; // The 6 actuators

    [SerializeField] private Transform[] cylinderBases = new Transform[6]; // These never move

    [SerializeField] private Transform[] platformAttachmentPoints = new Transform[6];

    private float Lmin = 1.45f;
    void Update()
    {
        if(manualControl)
            UpdateCylinders();
    }

    void UpdateCylinders()
    {
        for (int i = 0; i < 6; i++)
        {
            // Get base point in world space

            // Vector from base to platform
            float length = Vector3.Distance(platformAttachmentPoints[i].position, cylinderBases[i].position);
            Debug.Log($"Cylinder {i} length: {length}");

            // Calculate extension beyond Lmin
            float extension = length - Lmin;

            // Apply to ArticulationBody's xDrive 
            ArticulationDrive drive = cylinders[i].xDrive;
            drive.target = extension;
            cylinders[i].xDrive = drive;
        }
    }
}
