using UnityEngine;

/// IK that outputs delta lengths (meters, 0 at neutral) and writes them to prismatic ArticulationBodies.
/// Math matches your MATLAB: R = Rz(yaw)*Ry(pitch)*Rx(roll).
public class Large_StewartIK_DeltaAB : MonoBehaviour
{
    [Header("Pose (Base frame A)")]
    public float surge;   // +X (m)
    public float sway;    // +Y (m)
    public float heave;   // +Z (m)
    public float roll;    // about +X (deg if anglesAreDegrees)
    public float pitch;   // about +Y
    public float yaw;     // about +Z
    public bool anglesAreDegrees = true;

    [Header("Geometry for the LARGE platform (meters)")]
    // Base anchors in base frame A
    public Vector3[] p_AibC = new Vector3[6]
    {
        new Vector3( 1.35616f, -2.12033f, 0.25f),   // A1
        new Vector3( 1.15818f, -2.23463f, 0.25f),   // A2
        new Vector3(-2.51434f, -0.11430f, 0.25f),   // A3
        new Vector3(-2.51343f,  0.11430f, 0.25f),   // A4
        new Vector3( 1.15818f,  2.23463f, 0.25f),   // A5
        new Vector3( 1.35616f,  2.12033f, 0.25f),   // A6
    };

    // Top anchors in top frame B
    public Vector3[] p_BitC = new Vector3[6]
    {
        new Vector3( 2.11178f, -0.11430f, 0.00002f),  // B1
        new Vector3(-0.95610f, -1.88787f, 0.00002f),  // B2
        new Vector3(-1.15487f, -1.77170f, 0.00002f),  // B3
        new Vector3(-1.15487f,  1.77170f, 0.00002f),  // B4
        new Vector3(-0.95690f,  1.88600f, 0.00002f),  // B5
        new Vector3( 2.11178f,  0.11430f, 0.00002f),  // B6
    };

    [Header("Actuators (prismatic ArticulationBodies; X axis = stroke)")]
    public ArticulationBody[] cylinders = new ArticulationBody[6];
    [Tooltip("Software stroke limits (Δ from neutral), meters.")]
    public Vector2[] deltaLimits = new Vector2[6] {
        new Vector2(-3f, 3f), new Vector2(-3f, 3f), new Vector2(0f, 3f),
        new Vector2(-3f, 3f), new Vector2(-3f, 3f), new Vector2(-3f, 3f)
    };

    [Header("Speed limiting")]
    public bool limitSpeed = true;
    [Tooltip("Fallback speed cap (m/s).")]
    public float defaultMaxSpeed = 0.40f;
    [Tooltip("Per-leg speed caps (m/s). 0 uses default.")]
    public float[] maxSpeed = new float[6] { 0.40f, 0.40f, 0.40f, 0.40f, 0.40f, 0.40f };

    [Header("Neutral mode")]
    [Tooltip("If true, use MATLAB's single Lmin for all legs; if false, capture L0 at Start.")]
    public bool useMatlabLmin = false;
    [Tooltip("MATLAB Lmin")]
    public float Lmin = 2.68687f;

    [Header("Outputs (read-only)")]
    public float[] delta = new float[6];  // ΔLᵢ = Lᵢ − Lᵢ⁰
    public float[] L0 = new float[6];     // neutral absolute lengths

    [Header("Options")]
    public bool computeNeutralOnStart = true;  // ignored when useMatlabLmin = true
    public bool writeToCylinders = true;

    // ---------- Demo (optional) ----------
    [Header("Demo (optional)")]
    public bool demoEnabled = false;
    public float demoTimeScale = 1f;
    public Vector3 demoAmpXYZ = new Vector3(0f, 0f, 2f);
    public Vector3 demoFreqXYZ = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 demoPhaseXYZ = Vector3.zero;
    public Vector3 demoAmpRPY = new Vector3(0f, 0f, 0f);
    public Vector3 demoFreqRPY = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 demoPhaseRPY = new Vector3(0f, 90f, 180f);

    float demoTime;

    void Start()
    {
        if (useMatlabLmin)
        {
            for (int i = 0; i < 6; i++) L0[i] = Lmin;   // mirror MATLAB convention
        }
        else if (computeNeutralOnStart)
        {
            // Capture neutral lengths with zero pose
            ComputeLengths(0f, 0f, 0f, 0f, 0f, 0f, L0);
        }
    }

    public void realTimeInput(float surgeRT, float swayRT, float heaveRT,
                              float rollRT, float pitchRT, float yawRT)
    {
        if (!demoEnabled)
        {
            surge = surgeRT; sway = swayRT; heave = heaveRT;
            roll = rollRT;   pitch = pitchRT; yaw = yawRT;
        }
    }

    void FixedUpdate()
    {
        // Optional demo motion
        if (demoEnabled)
        {
            demoTime += Time.fixedDeltaTime * demoTimeScale;
            const float TAU = 6.28318530718f;

            surge = demoAmpXYZ.x * Mathf.Sin(TAU * demoFreqXYZ.x * demoTime + Mathf.Deg2Rad * demoPhaseXYZ.x);
            sway  = demoAmpXYZ.y * Mathf.Sin(TAU * demoFreqXYZ.y * demoTime + Mathf.Deg2Rad * demoPhaseXYZ.y);
            heave = demoAmpXYZ.z * Mathf.Sin(TAU * demoFreqXYZ.z * demoTime + Mathf.Deg2Rad * demoPhaseXYZ.z);

            float rD = demoAmpRPY.x * Mathf.Sin(TAU * demoFreqRPY.x * demoTime + Mathf.Deg2Rad * demoPhaseRPY.x);
            float pD = demoAmpRPY.y * Mathf.Sin(TAU * demoFreqRPY.y * demoTime + Mathf.Deg2Rad * demoPhaseRPY.y);
            float yD = demoAmpRPY.z * Mathf.Sin(TAU * demoFreqRPY.z * demoTime + Mathf.Deg2Rad * demoPhaseRPY.z);

            if (anglesAreDegrees) { roll = rD; pitch = pD; yaw = yD; }
            else { roll = rD * Mathf.Deg2Rad; pitch = pD * Mathf.Deg2Rad; yaw = yD * Mathf.Deg2Rad; }
        }

        // IK → absolute lengths
        float[] L = new float[6];
        ComputeLengths(surge, sway, heave, roll, pitch, yaw, L);

        // Write with speed limiting & bounds
        float dt = Time.fixedDeltaTime;
        for (int i = 0; i < 6; i++)
        {
            delta[i] = L[i] - L0[i];

            if (i < deltaLimits.Length)
                delta[i] = Mathf.Clamp(delta[i], deltaLimits[i].x, deltaLimits[i].y);

            if (writeToCylinders && i < cylinders.Length && cylinders[i])
            {
                float desired = delta[i];
                float outputTarget = desired;

                if (limitSpeed)
                {
                    float cur = (float)cylinders[i].jointPosition[0];
                    float cap = (i < maxSpeed.Length && maxSpeed[i] > 0f) ? maxSpeed[i] : defaultMaxSpeed;
                    float step = cap * dt;

                    // Optional soft landing near hard limits (if you set them on the drive)
                    float lower = cylinders[i].xDrive.lowerLimit;
                    float upper = cylinders[i].xDrive.upperLimit;
                    const float softBand = 0.03f; // 3 cm
                    float distToLower = Mathf.Max(0f, cur - lower);
                    float distToUpper = Mathf.Max(0f, upper - cur);
                    float minDist = Mathf.Min(distToLower, distToUpper);
                    float softScale = Mathf.Clamp01(minDist / softBand);

                    step *= softScale;
                    outputTarget = Mathf.MoveTowards(cur, desired, step);
                }

                var d = cylinders[i].xDrive;
                d.target = outputTarget;
                cylinders[i].xDrive = d;
            }
        }
    }

    // --- Core math (ported from MATLAB) ---
    void ComputeLengths(float s, float w, float h, float r, float p, float y, float[] outL)
    {
        if (anglesAreDegrees) { r *= Mathf.Deg2Rad; p *= Mathf.Deg2Rad; y *= Mathf.Deg2Rad; }

        float cr = Mathf.Cos(r), sr = Mathf.Sin(r);
        float cp = Mathf.Cos(p), sp = Mathf.Sin(p);
        float cy = Mathf.Cos(y), sy = Mathf.Sin(y);

        // Rz(yaw)*Ry(pitch)*Rx(roll)
        float r00 = cy * cp;
        float r01 = cy * sp * sr - sy * cr;
        float r02 = cy * sp * cr + sy * sr;
        float r10 = sy * cp;
        float r11 = sy * sp * sr + cy * cr;
        float r12 = sy * sp * cr - cy * sr;
        float r20 = -sp;
        float r21 =  cp * sr;
        float r22 =  cp * cr;

        Vector3 p_tb = new Vector3(s, w, h);

        for (int i = 0; i < 6; i++)
        {
            Vector3 t = p_BitC[i];
            Vector3 Rt = new Vector3(
                r00 * t.x + r01 * t.y + r02 * t.z,
                r10 * t.x + r11 * t.y + r12 * t.z,
                r20 * t.x + r21 * t.y + r22 * t.z
            );
            Vector3 Pi = p_tb + Rt;          // top anchor in Base frame
            Vector3 diff = Pi - p_AibC[i];   // leg vector
            outL[i] = Mathf.Sqrt(diff.x*diff.x + diff.y*diff.y + diff.z*diff.z);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < 6; i++) Gizmos.DrawSphere(p_AibC[i], 0.025f);

        float[] Ltmp = new float[6];
        ComputeLengths(surge, sway, heave, roll, pitch, yaw, Ltmp);

        // draw transformed top anchors/legs
        float r = anglesAreDegrees ? roll * Mathf.Deg2Rad : roll;
        float p = anglesAreDegrees ? pitch * Mathf.Deg2Rad : pitch;
        float y = anglesAreDegrees ? yaw * Mathf.Deg2Rad : yaw;

        float cr = Mathf.Cos(r), sr = Mathf.Sin(r);
        float cp = Mathf.Cos(p), sp = Mathf.Sin(p);
        float cy = Mathf.Cos(y), sy = Mathf.Sin(y);

        float r00 = cy * cp; float r01 = cy * sp * sr - sy * cr; float r02 = cy * sp * cr + sy * sr;
        float r10 = sy * cp; float r11 = sy * sp * sr + cy * cr; float r12 = sy * sp * cr - cy * sr;
        float r20 = -sp;     float r21 =  cp * sr;               float r22 =  cp * cr;

        Vector3 p_tb = new Vector3(surge, sway, heave);
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 6; i++)
        {
            Vector3 t = p_BitC[i];
            Vector3 Pi = new Vector3(r00 * t.x + r01 * t.y + r02 * t.z,
                                     r10 * t.x + r11 * t.y + r12 * t.z,
                                     r20 * t.x + r21 * t.y + r22 * t.z) + p_tb;
            Gizmos.DrawSphere(Pi, 0.025f);
            Gizmos.DrawLine(p_AibC[i], Pi);
            Debug.DrawLine(p_AibC[i], Pi);
        }
    }
#endif
}
