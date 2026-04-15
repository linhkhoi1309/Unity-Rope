using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(LineRenderer))]
public class VerletRope : MonoBehaviour
{
    [Header("Rope Settings")]

    [Tooltip("Number of segments in the rope")]
    [SerializeField] private int m_numOfSegments = 50;

    [Tooltip("Length of each rope segment")]
    [SerializeField] private float m_segmentLength = 0.225f;

    [Header("Rope Physics")]

    [Tooltip("Gravity force applied to the rope")]
    [SerializeField] private Vector2 m_gravityForce = new Vector2(0f, -20f);

    [Tooltip("Damping factor to reduce rope oscillations (0 to 1)")]
    [SerializeField] private float m_dampingFactor = 0.99f;

    [Header("Rope Iterations")]

    [Tooltip("Number of iterations for constraint solving")]
    [SerializeField] private int m_numOfIterations = 50;

    [Header("Rope Collision")]

    [Tooltip("Enable collision detection for the rope segments")]
    [SerializeField] private bool m_enableCollision = false;

    private LineRenderer m_lineRenderer;
    private List<RopeSegment> m_ropeSegments = new List<RopeSegment>();
    private Vector3 m_ropeStartPoint;

    public struct RopeSegment
    {
        public Vector2 CurrentPos;
        public Vector2 PreviousPos;

        public RopeSegment(Vector2 pos)
        {
            CurrentPos = pos;
            PreviousPos = pos;
        }
    }

    private void Awake()
    {
        // Initialize rope segments
        m_lineRenderer = GetComponent<LineRenderer>();
        m_lineRenderer.positionCount = m_numOfSegments;
        m_ropeStartPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        for (int i = 0; i < m_numOfSegments; i++)
        {
            m_ropeSegments.Add(new RopeSegment(m_ropeStartPoint));
            m_ropeStartPoint.y -= m_segmentLength;
        }
    }

    void FixedUpdate()
    {
        SimulateRope();
        for (int i = 0; i < m_numOfIterations; i++)
        {
            ApplyConstraints();
        }
    }

    void Update()
    {
        RenderRope();
    }

    void SimulateRope()
    {
        // Apply Verlet integration for rope physics
        for (int i = 0; i < m_numOfSegments; i++)
        {
            RopeSegment segment = m_ropeSegments[i];
            Vector2 velocity = (segment.CurrentPos - segment.PreviousPos) * m_dampingFactor;

            segment.PreviousPos = segment.CurrentPos;
            segment.CurrentPos = segment.CurrentPos + velocity + m_gravityForce * Time.fixedDeltaTime * Time.fixedDeltaTime;
            m_ropeSegments[i] = segment;
        }
    }

    void ApplyConstraints()
    {
        // First segment is fixed to the mouse position
        RopeSegment firstSegment = m_ropeSegments[0];
        firstSegment.CurrentPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        m_ropeSegments[0] = firstSegment;

        // Enforce segment length constraints
        for (int i = 0; i < m_numOfSegments - 1; i++)
        {
            RopeSegment segmentA = m_ropeSegments[i];
            RopeSegment segmentB = m_ropeSegments[i + 1];

            Vector2 delta = segmentB.CurrentPos - segmentA.CurrentPos;
            float distance = delta.magnitude;
            float difference = distance - m_segmentLength;
            Vector2 correction = delta.normalized * difference;

            if (i != 0)
            {
                segmentA.CurrentPos += correction * 0.5f;
                segmentB.CurrentPos -= correction * 0.5f;
            }
            else
            {
                segmentB.CurrentPos -= correction;
            }

            m_ropeSegments[i] = segmentA;
            m_ropeSegments[i + 1] = segmentB;
        }
    }

    void RenderRope()
    {
        // Snapshot current rope positions (per frame) for rendering
        Vector3[] ropePositions = new Vector3[m_numOfSegments];
        for (int i = 0; i < m_numOfSegments; i++)
        {
            ropePositions[i] = m_ropeSegments[i].CurrentPos;
        }
        m_lineRenderer.SetPositions(ropePositions);
    }
}
