using System.Numerics;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

class VerletNode
{
    public Vector2 CurrentPosition;
    public Vector2 PreviousPosition;

    public VerletNode(Vector2 position)
    {
        this.CurrentPosition = position;
        this.PreviousPosition = position;
    }
}

enum ColliderType
{
    Circle,
    Box,
    None,
}

class CollisionInfo
{
    public int id;
    public ColliderType colliderType;
    public Vector2 colliderSize;
    public Vector2 position;
    public Vector2 scale;
    public Matrix4x4 worldToLocalMatrix;
    public Matrix4x4 localToWorldMatrix;
    public int numCollisions;

    public int[] collidingNodes;

    public CollisionInfo(int maxCollisions)
    {
        this.id = -1;
        this.colliderType = ColliderType.None;
        this.colliderSize = Vector2.zero;
        this.position = Vector2.zero;
        this.scale = Vector2.zero;
        this.worldToLocalMatrix = Matrix4x4.zero;
        this.localToWorldMatrix = Matrix4x4.zero;
        this.numCollisions = 0;
        collidingNodes = new int[maxCollisions];
    }
}

public class VerletRopeOptimized : MonoBehaviour
{
    // Maximum number of colliders hitting the rope at once
    private const int MAX_ROPE_COLLISIONS = 32;

    // Size of the collider buffer, also the maximum number of colliders that a single node can touch at once
    private const int COLLIDER_BUFFER_SIZE = 8;

    // Maximum number of points to render on the rope, used to limit the number of line segments drawn for performance reasons  
    private const int MAX_RENDER_POINTS = 256;

    // Number of triangles present in each line segment between nodes.
    // 4 = 2 per quad, one quad for the line and one for the start cap.
    private const int TRIANGLES_PER_NODE = 4;

    // Number of vertices present in each line segment between nodes.
    // 8 = 2 quads.
    private const int VERTICES_PER_NODE = 8;

    [Header("Rope Settings")]
    [Min(2)]
    [SerializeField]
    [Tooltip("Total number of nodes in the rope.")]
    private int totalNodes = 200;

    [SerializeField]
    [Tooltip("Number of iterations to run the physics simulation each frame.")]
    private int iterations = 80;

    [SerializeField]
    [Tooltip("Distance between each node in the rope.")]
    private float nodeDistance = 0.1f;

    [SerializeField]
    [Tooltip("Width of the rope when rendered.")]
    private float drawWidth = 0.025f;

    [Header("Rope Physics Settings")]

    [Min(0.001f)]
    [SerializeField]
    [Tooltip("Time step for the physics simulation. Lower values will result in a more stable simulation but may require more iterations.")]
    private float stepTime = 0.01f;

    [SerializeField]
    [Tooltip("Maximum time step for the physics simulation.")]
    private float maxStep = 0.1f;

    [SerializeField]
    [Tooltip("Gravity force applied to the rope.")]
    private Vector2 gravity = new Vector2(0, -20f);

    [SerializeField]
    [Tooltip("Collision radius for the rope nodes.")]
    private float collisionRadius = 0.5f;

    [SerializeField]
    [Tooltip("Damping factor for the rope nodes.")]
    private float dampingFactor = 0.99f;

    private VerletNode[] nodes;
    private float m_timeAccumulator;
    private CollisionInfo[] collisionInfos;
    private int numCollisions;
    private bool shouldSnapshotCollision;

    private Camera cam;
    private Material material;
    private Collider2D[] colliderBuffer;
    private Vector4[] renderPositions;


    private void Awake()
    {
        if (totalNodes > MAX_RENDER_POINTS)
        {
            Debug.LogError("Total nodes is more than MAX_RENDER_POINTS, so won't be able to render the entire rope.");
        }

        nodes = new VerletNode[totalNodes];
        collisionInfos = new CollisionInfo[MAX_ROPE_COLLISIONS];

        for (int i = 0; i < collisionInfos.Length; i++)
        {
            collisionInfos[i] = new CollisionInfo(totalNodes);
        }

        // Buffer for OverlapCircleNonAlloc.
        colliderBuffer = new Collider2D[COLLIDER_BUFFER_SIZE];
        renderPositions = new Vector4[totalNodes];

        Vector2 pos = transform.position;
        for (int i = 0; i < totalNodes; i++)
        {
            nodes[i] = new VerletNode(pos);
            renderPositions[i] = new Vector4(pos.x, pos.y, 1, 1);
            pos.y -= nodeDistance;
        }

        // Create a mesh for rendering the rope. We only need to do this once since the number of vertices and triangles won't change -- we just update the vertex positions each frame.
        Mesh mesh = new Mesh();
        {
            Vector3[] vertices = new Vector3[totalNodes * VERTICES_PER_NODE];
            int[] triangles = new int[totalNodes * TRIANGLES_PER_NODE * 3];

            for (int i = 0; i < totalNodes; i++)
            {
                // 4 triangles per node, 3 indices per triangle.
                int idx = i * TRIANGLES_PER_NODE * 3;
                // 8 vertices per node.
                int vIdx = i * VERTICES_PER_NODE;

                // Unity uses a CLOCKWISE WINDING ORDER -- clockwise tri indices are facing the camera.
                // Rect between segments.
                triangles[idx + 0] = vIdx; // v1 top
                triangles[idx + 1] = vIdx + 1; // v2 bottom
                triangles[idx + 2] = vIdx + 2; // v1 bottom
                triangles[idx + 3] = vIdx; // v1 top
                triangles[idx + 4] = vIdx + 3; // v2 top
                triangles[idx + 5] = vIdx + 1; // v2 bottom

                // End cap quad
                triangles[idx + 6] = vIdx + 4; // tl
                triangles[idx + 7] = vIdx + 7; // br
                triangles[idx + 8] = vIdx + 6; // bl
                triangles[idx + 9] = vIdx + 4; // tl
                triangles[idx + 10] = vIdx + 5; // tr
                triangles[idx + 11] = vIdx + 7; // br
            }

            // We only really care about the number of vertices, not what they actually are -- the positions aren't used.
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            // Since we pretty much want the rope to always render (it's always going to be on screen if it's active), we
            // just set the bounds super large to avoid recalculating the bounds when the rope changes.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
        }
        GetComponent<MeshFilter>().mesh = mesh;
        material = GetComponent<MeshRenderer>().material;
        material.SetFloat("_Width", drawWidth);
    }

    void Start()
    {

    }

    private void FixedUpdate()
    {
        shouldSnapshotCollision = true;
    }

    void Update()
    {
        if (shouldSnapshotCollision)
        {
            SnapshotCollision();
        }

        // Fixed timestep.
        m_timeAccumulator += Time.deltaTime;
        m_timeAccumulator = Mathf.Min(m_timeAccumulator, maxStep);
        while (m_timeAccumulator >= stepTime)
        {
            Simulate();

            for (int i = 0; i < iterations; i++)
            {
                ApplyConstraints();
                AdjustCollisions();
            }

            m_timeAccumulator -= stepTime;
        }
    }

    private void Simulate()
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            VerletNode node = nodes[i];
            Vector2 temp = node.CurrentPosition;
            Vector2 velocity = (node.CurrentPosition - node.PreviousPosition) * dampingFactor;
            node.CurrentPosition += velocity + gravity * stepTime * stepTime;
            node.PreviousPosition = temp;
        }
    }

    private void ApplyConstraints()
    {
        for (int i = 0; i < nodes.Length - 1; i++)
        {
            VerletNode node1 = nodes[i];
            VerletNode node2 = nodes[i + 1];

            // First node follows the mouse, for debugging.
            if (i == 0 && Input.GetMouseButton(0))
            {
                node1.CurrentPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            // Current distance between rope nodes.
            float diffX = node1.CurrentPosition.x - node2.CurrentPosition.x;
            float diffY = node1.CurrentPosition.y - node2.CurrentPosition.y;
            Vector2 delta = new Vector2(diffX, diffY);
            float dist = Vector2.Distance(node1.CurrentPosition, node2.CurrentPosition);
            float difference = 0;
            // Guard against divide by 0.
            if (dist > 0)
            {
                difference = (nodeDistance - dist) / dist;
            }

            Vector2 translate = delta * (.5f * difference);

            node1.CurrentPosition += translate;
            node2.CurrentPosition -= translate;
        }
    }

    private void SnapshotCollision()
    {
        // Profiler.BeginSample("Snapshot");

        numCollisions = 0;
        // Loop through each node and get collisions within a radius.
        for (int i = 0; i < nodes.Length; i++)
        {
            int collisions =
                Physics2D.OverlapCircleNonAlloc(nodes[i].CurrentPosition, collisionRadius, colliderBuffer);

            for (int j = 0; j < collisions; j++)
            {
                Collider2D collider = colliderBuffer[j];
                int id = collider.GetInstanceID();

                // Check if we already have this collider recorded -- if so, we just need to add another collision to it, otherwise we need to add a new one.
                int idx = -1;
                for (int k = 0; k < numCollisions; k++)
                {
                    if (collisionInfos[k].id == id)
                    {
                        idx = k;
                        break;
                    }
                }

                // If we didn't have the collider, we need to add it.
                if (idx < 0)
                {
                    // Record all the data we need to use into our classes.
                    CollisionInfo ci = collisionInfos[numCollisions];
                    ci.id = id;
                    ci.worldToLocalMatrix = collider.transform.worldToLocalMatrix;
                    ci.localToWorldMatrix = collider.transform.localToWorldMatrix;
                    ci.scale.x = ci.localToWorldMatrix.GetColumn(0).magnitude;
                    ci.scale.y = ci.localToWorldMatrix.GetColumn(1).magnitude;
                    ci.position = collider.transform.position;
                    ci.numCollisions = 1; // 1 collision, this one.
                    ci.collidingNodes[0] = i;

                    switch (collider)
                    {
                        case CircleCollider2D c:
                            ci.colliderType = ColliderType.Circle;
                            ci.colliderSize.x = ci.colliderSize.y = c.radius;
                            break;
                        case BoxCollider2D b:
                            ci.colliderType = ColliderType.Box;
                            ci.colliderSize = b.size;
                            break;
                        default:
                            ci.colliderType = ColliderType.None;
                            break;
                    }

                    numCollisions++;
                    if (numCollisions >= MAX_ROPE_COLLISIONS)
                    {
                        // Profiler.EndSample();
                        return;
                    }

                    // If we found the collider, then we just have to increment the collisions and add our node.
                }
                else
                {
                    CollisionInfo ci = collisionInfos[idx];
                    if (ci.numCollisions >= totalNodes)
                    {
                        continue;
                    }

                    ci.collidingNodes[ci.numCollisions++] = i;
                }
            }
        }

        shouldSnapshotCollision = false;

        // Profiler.EndSample();
    }

    private void LateUpdate()
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            renderPositions[i].w = 1;
            Vector2 pos = nodes[i].CurrentPosition;
            renderPositions[i].x = pos.x;
            renderPositions[i].y = pos.y;
            renderPositions[i].z = 1;
        }

        material.SetVectorArray("_Points", renderPositions);
    }

    private void AdjustCollisions()
    {
        // Profiler.BeginSample("Collision");

        for (int i = 0; i < numCollisions; i++)
        {
            CollisionInfo ci = collisionInfos[i];

            switch (ci.colliderType)
            {
                case ColliderType.Circle:
                    {
                        float radius = ci.colliderSize.x * Mathf.Max(ci.scale.x, ci.scale.y);

                        for (int j = 0; j < ci.numCollisions; j++)
                        {
                            VerletNode node = nodes[ci.collidingNodes[j]];
                            float distance = Vector2.Distance(ci.position, node.CurrentPosition);

                            // Early out if we're not colliding.
                            if (distance - radius > 0)
                            {
                                continue;
                            }

                            // Push point outside of the circle
                            Vector2 dir = (node.CurrentPosition - ci.position).normalized;
                            Vector2 hitPos = ci.position + dir * radius;
                            node.CurrentPosition = hitPos;
                        }
                    }
                    break;

                case ColliderType.Box:
                    {
                        for (int j = 0; j < ci.numCollisions; j++)
                        {
                            VerletNode node = nodes[ci.collidingNodes[j]];
                            Vector2 localPoint = ci.worldToLocalMatrix.MultiplyPoint(node.CurrentPosition);

                            // If distance from center is more than box "radius", then we can't be colliding.
                            Vector2 half = ci.colliderSize * .5f;
                            Vector2 scalar = ci.scale;
                            float dx = localPoint.x;
                            float px = half.x - Mathf.Abs(dx);
                            if (px <= 0)
                            {
                                continue;
                            }

                            float dy = localPoint.y;
                            float py = half.y - Mathf.Abs(dy);
                            if (py <= 0)
                            {
                                continue;
                            }

                            // Need to multiply distance by scale or we'll mess up on scaled box corners.
                            if (px * scalar.x < py * scalar.y)
                            {
                                float sx = Mathf.Sign(dx);
                                localPoint.x = half.x * sx;
                            }
                            else
                            {
                                float sy = Mathf.Sign(dy);
                                localPoint.y = half.y * sy;
                            }

                            Vector2 hitPos = ci.localToWorldMatrix.MultiplyPoint(localPoint);
                            node.CurrentPosition = hitPos;
                        }
                    }
                    break;
            }
        }


        // Profiler.EndSample();
    }
}
