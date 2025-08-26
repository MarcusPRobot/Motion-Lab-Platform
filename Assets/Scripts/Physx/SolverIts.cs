using UnityEngine;

public class PhysxDefaults : MonoBehaviour
{
    [SerializeField] int defaultPosIters = 32;   // global baseline
    [SerializeField] int defaultVelIters = 16;
    [SerializeField] bool useTGS = true;         // Temporal Gauss-Seidel is stiffer
    [SerializeField] float fixedTimestep = 0.005f; // 100 Hz (try 0.005 for 200 Hz)

    void Awake()
    {
        Physics.defaultSolverIterations = defaultPosIters;
        Physics.defaultSolverVelocityIterations = defaultVelIters;
        if (useTGS) Physics.defaultSolverType = SolverType.TemporalGaussSeidel;
        Time.fixedDeltaTime = fixedTimestep;
    }
}