using UnityEngine;

/// <summary>
/// Procedural motions for a 6-leg Stewart platform using ArticulationBody xDrive targets.
/// No runtime input required; everything is controlled from the Inspector.
/// </summary>
public class SmallStewartMovement : MonoBehaviour
{
    public enum Program
    {
        HeaveSine = 1,   // all legs in phase (up/down)
        TiltPitch,       // front/back tilt oscillation
        TiltRoll,        // left/right tilt oscillation
        OrbitingTilt,    // tilt axis spins around (wobble)
        SwirlWave,       // travelling wave around the ring
        YawTwist,        // alternating up/down legs
        Figure8          // lissajous tilt path
    }

    [Header("Actuators (6)")]
    [SerializeField] ArticulationBody[] cylinders = new ArticulationBody[6];

    [Header("Target range (xDrive target, in YOUR units)")]
    [Tooltip("Minimum xDrive.target allowed (mm for prismatic, degrees for revolute).")]
    [SerializeField] float minTarget = 0f;
    [Tooltip("Maximum xDrive.target allowed (mm for prismatic, degrees for revolute).")]
    [SerializeField] float maxTarget = 100f;

    [Header("Motion selection")]
    [SerializeField] Program program = Program.OrbitingTilt;
    [Tooltip("Base motion frequency in cycles per second.")]
    [SerializeField] float frequency = 0.5f;
    [Tooltip("Which leg is 'forward' (affects tilt axes).")]
    [SerializeField, Range(0,5)] int forwardIndex = 0;
    [Tooltip("If your indexing is reversed, flip this.")]
    [SerializeField] bool clockwiseOrder = true;

    [Header("Amplitudes (as fraction of half-range)")]
    [SerializeField, Range(0f, 1f)] float heaveAmp   = 0.40f;
    [SerializeField, Range(0f, 1f)] float tiltAmp    = 0.50f;
    [SerializeField, Range(0f, 1f)] float swirlAmp   = 0.35f;
    [SerializeField, Range(0f, 1f)] float twistAmp   = 0.30f;

    [Header("Extras")]
    [Tooltip("How fast the tilt axis spins for OrbitingTilt / Figure8 (Hz).")]
    [SerializeField] float orbitTiltHz = 0.25f;
    [Tooltip("Small per-leg Perlin noise to keep motion lively (units fraction).")]
    [SerializeField, Range(0f, 0.2f)] float noise = 0.02f;
    [Tooltip("Drive in FixedUpdate (recommended for Articulation).")]
    [SerializeField] bool useFixedUpdate = true;

    float[] angles; // leg azimuths around the ring (radians)

    void Awake()
    {
        if (cylinders == null || cylinders.Length != 6)
            Debug.LogWarning("Expected exactly 6 cylinders.");
        BuildAngles();
    }

    void OnValidate()
    {
        if (maxTarget < minTarget) maxTarget = minTarget;
        BuildAngles();
    }

    void BuildAngles()
    {
        angles = new float[6];
        for (int i = 0; i < 6; i++)
        {
            int idx = (i - forwardIndex + 6) % 6;        // rotate so index 'forwardIndex' is angle 0
            if (!clockwiseOrder) idx = (6 - idx) % 6;    // reverse direction if needed
            angles[i] = idx * Mathf.PI * 2f / 6f;        // even spacing
        }
    }

    void Update()
    {
        if (!useFixedUpdate) Step(Time.time, Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (useFixedUpdate) Step(Time.time, Time.fixedDeltaTime);
    }

    void Step(float time, float dt)
    {
        if (cylinders == null) return;

        float center = 0.5f * (minTarget + maxTarget);
        float halfRange = 0.5f * Mathf.Max(0f, maxTarget - minTarget);

        // Per-leg targets
        for (int i = 0; i < cylinders.Length && i < 6; i++)
        {
            float theta = angles[i];
            float val = center;

            float w = 2f * Mathf.PI * frequency;       // base angular speed
            float phi = 2f * Mathf.PI * orbitTiltHz * time;

            switch (program)
            {
                case Program.HeaveSine:
                    val += heaveAmp * halfRange * Mathf.Sin(w * time);
                    break;

                case Program.TiltPitch:
                    // front/back = sin(theta)
                    val += tiltAmp * halfRange * Mathf.Sin(theta) * Mathf.Sin(w * time);
                    break;

                case Program.TiltRoll:
                    // left/right = cos(theta)
                    val += tiltAmp * halfRange * Mathf.Cos(theta) * Mathf.Sin(w * time);
                    break;

                case Program.OrbitingTilt:
                    // tilt axis rotates: cos(theta - phi)
                    val += tiltAmp * halfRange * Mathf.Cos(theta - phi);
                    // optional subtle breathing
                    val += 0.3f * heaveAmp * halfRange * Mathf.Sin(w * time);
                    break;

                case Program.SwirlWave:
                    // travelling wave around ring
                    val += swirlAmp * halfRange * Mathf.Sin(theta + w * time);
                    break;

                case Program.YawTwist:
                    // alternating legs (~sin(3θ))
                    val += twistAmp * halfRange * Mathf.Sin(3f * theta) * Mathf.Sin(w * time);
                    break;

                case Program.Figure8:
                    // two perpendicular tilts with different rates
                    float a = Mathf.Cos(theta - phi);
                    float b = Mathf.Sin(theta + phi);
                    val += tiltAmp * halfRange * (0.7f * a * Mathf.Sin(w * time)
                                                + 0.7f * b * Mathf.Sin(2f * w * time));
                    break;
            }

            if (noise > 0f)
            {
                float n = Mathf.PerlinNoise(i * 0.73f, time * 0.5f) - 0.5f;
                val += (2f * noise) * halfRange * n;
            }

            val = Mathf.Clamp(val, minTarget, maxTarget);
            ApplyTarget(cylinders[i], val);
        }
    }

    static void ApplyTarget(ArticulationBody body, float target)
    {
        if (!body) return;
        var d = body.xDrive;
        d.target = target;
        body.xDrive = d;
    }
}
