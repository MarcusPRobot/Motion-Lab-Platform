using UnityEngine;

/// IK that outputs delta lengths (0 at neutral) and writes them to prismatic ArticulationBodies.
/// Math is a direct port of your MATLAB: R = Rz(yaw)*Ry(pitch)*Rx(roll).
public class StewartIK_DeltaAB : MonoBehaviour
{
    [Header("Pose (Base frame A)")]
    public float surge;   // +X (m)
    public float sway;    // +Y (m)
    public float heave;   // +Z (m)
    public float roll;    // about +X (deg if anglesAreDegrees)
    public float pitch;   // about +Y
    public float yaw;     // about +Z
    public bool anglesAreDegrees = true;

    [Header("Geometry (same as MATLAB, meters)")]
    public Vector3[] p_AibC = new Vector3[6]
    {
        new Vector3( 0.70416f, -1.02102f, 0.11258f),
        new Vector3( 0.53215f, -1.12033f, 0.11258f),
        new Vector3(-1.23631f, -0.09931f, 0.11258f),
        new Vector3(-1.23631f,  0.09931f, 0.11258f),
        new Vector3( 0.53215f,  1.12033f, 0.11258f),
        new Vector3( 0.70416f,  1.02102f, 0.11258f),
    };
    public Vector3[] p_BitC = new Vector3[6]
    {
        new Vector3( 0.95000f, -0.09000f, 0f),
        new Vector3(-0.39706f, -0.86772f, 0f),
        new Vector3(-0.55294f, -0.77772f, 0f),
        new Vector3(-0.55294f,  0.77772f, 0f),
        new Vector3(-0.39706f,  0.86772f, 0f),
        new Vector3( 0.95000f,  0.09000f, 0f),
    };

    [Header("Actuators (prismatic ArticulationBodies; X axis = stroke)")]
    public ArticulationBody[] cylinders = new ArticulationBody[6];
    [Tooltip("Optional per-leg limits for safety (meters delta from neutral).")]
    public Vector2[] deltaLimits = new Vector2[6] {
        new Vector2(-2, 2), new Vector2(-2, 2), new Vector2(-2, 2),
        new Vector2(-2, 2), new Vector2(-2, 2), new Vector2(-2, 2)
    };

    [Header("Speed limiting")]
    public bool limitSpeed = true;
    [Tooltip("Fallback speed cap (m/s) used when a leg's entry is 0 or missing.")]
    public float defaultMaxSpeed = 0.40f;
    [Tooltip("Per-leg speed caps (m/s). Size 6; 0 uses defaultMaxSpeed.")]
    public float[] maxSpeed = new float[6] { 0.40f, 0.40f, 0.40f, 0.40f, 0.40f, 0.40f };

    [Header("Outputs (read-only)")]
    public float[] delta = new float[6];  // ΔLᵢ = Lᵢ − Lᵢ⁰
    public float[] L0 = new float[6];     // neutral absolute lengths, auto-computed on Start

    [Header("Options")]
    public bool computeNeutralOnStart = true;  // capture L0 from pose (0,0,0,0,0,0) at Start
    public bool writeToCylinders = true;

    // ---------- Sinusoidal demo driver ----------
    [Header("Demo (optional)")]
    public bool demoEnabled = false;
    public float demoTimeScale = 1f;
    public Vector3 demoAmpXYZ = new Vector3(0f, 0f, 0f);
    public Vector3 demoFreqXYZ = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 demoPhaseXYZ = Vector3.zero;
    public Vector3 demoAmpRPY = new Vector3(0f, 0f, 0f);
    public Vector3 demoFreqRPY = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 demoPhaseRPY = new Vector3(0f, 90f, 180f);

    public void changeAMP(float val, int id)
    {
        if      (id == 0) demoAmpXYZ.x = val;
        else if (id == 1) demoAmpXYZ.y = val;
        else if (id == 2) demoAmpXYZ.z = val;
        else if (id == 3) demoAmpRPY.x = val;
        else if (id == 4) demoAmpRPY.y = val;
        else if (id == 5) demoAmpRPY.z = val;
    }
    public void changeFreq(float val, int id)
    {
        if      (id == 0) demoFreqXYZ.x = val;
        else if (id == 1) demoFreqXYZ.y = val;
        else if (id == 2) demoFreqXYZ.z = val;
        else if (id == 3) demoFreqRPY.x = val;
        else if (id == 4) demoFreqRPY.y = val;
        else if (id == 5) demoFreqRPY.z = val;
    }

    float demoTime;

    void Start()
    {
        if (computeNeutralOnStart)
            ComputeLengths(0f, 0f, 0f, 0f, 0f, 0f, L0);
    }

    public void realTimeInput(float surgeRT, float swayRT, float heaveRT, float rollRT, float pitchRT, float yawRT)
    {
        if (!demoEnabled)
        {
            surge = surgeRT;
            sway  = swayRT;
            heave = heaveRT;
            roll  = rollRT;
            pitch = pitchRT;
            yaw   = yawRT;
        }
    }

    void FixedUpdate()
    {
        // Demo drive (optional)
        if (demoEnabled)
        {
            demoTime += Time.fixedDeltaTime * demoTimeScale;
            const float TAU = 6.28318530718f;

            surge = demoAmpXYZ.x * Mathf.Sin(TAU * demoFreqXYZ.x * demoTime + Mathf.Deg2Rad * demoPhaseXYZ.x);
            sway  = demoAmpXYZ.y * Mathf.Sin(TAU * demoFreqXYZ.y * demoTime + Mathf.Deg2Rad * demoPhaseXYZ.y);
            heave = demoAmpXYZ.z * Mathf.Sin(TAU * demoFreqXYZ.z * demoTime + Mathf.Deg2Rad * demoPhaseXYZ.z) + 1.1f;

            float rollDemo  = demoAmpRPY.x * Mathf.Sin(TAU * demoFreqRPY.x * demoTime + Mathf.Deg2Rad * demoPhaseRPY.x);
            float pitchDemo = demoAmpRPY.y * Mathf.Sin(TAU * demoFreqRPY.y * demoTime + Mathf.Deg2Rad * demoPhaseRPY.y);
            float yawDemo   = demoAmpRPY.z * Mathf.Sin(TAU * demoFreqRPY.z * demoTime + Mathf.Deg2Rad * demoPhaseRPY.z);

            if (anglesAreDegrees) { roll = rollDemo; pitch = pitchDemo; yaw = yawDemo; }
            else { roll = rollDemo * Mathf.Deg2Rad; pitch = pitchDemo * Mathf.Deg2Rad; yaw = yawDemo * Mathf.Deg2Rad; }
        }

        // IK → absolute lengths
        float[] L = new float[6];
        ComputeLengths(surge, sway, heave, roll, pitch, yaw, L);

        // Write with optional speed limiting
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
                    float cur = (float)cylinders[i].jointPosition[0];      // current prismatic pos (m)
                    float cap = (i < maxSpeed.Length && maxSpeed[i] > 0f) ? maxSpeed[i] : defaultMaxSpeed;
                    float step = cap * dt;
                    outputTarget = Mathf.MoveTowards(cur, desired, step);  // rate-limited
                }

                var d = cylinders[i].xDrive;   // struct copy
                d.target = outputTarget;
                cylinders[i].xDrive = d;       // assign back
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
        Gizmos.color = Color.cyan; for (int i = 0; i < 6; i++) Gizmos.DrawSphere(p_AibC[i], 0.02f);
        float[] Ltmp = new float[6];
        ComputeLengths(surge, sway, heave, roll, pitch, yaw, Ltmp);

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
            Vector3 Pi = new Vector3(r00*t.x + r01*t.y + r02*t.z,
                                     r10*t.x + r11*t.y + r12*t.z,
                                     r20*t.x + r21*t.y + r22*t.z) + p_tb;
            Gizmos.DrawSphere(Pi, 0.02f);
            Gizmos.DrawLine(p_AibC[i], Pi);
        }
    }
#endif
}
