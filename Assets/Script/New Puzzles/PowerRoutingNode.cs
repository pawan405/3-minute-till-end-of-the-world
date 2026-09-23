using UnityEngine;

public class PowerRoutingNode : MonoBehaviour
{
    public enum NodeType
    {
        Straight,
        Corner,
        TJunction,
        Cross,
        DeadEnd
    }

    [Header("Node Settings")]
    [SerializeField] private NodeType nodeType = NodeType.Straight;

    [Header("Connection Visuals")]
    [SerializeField] private GameObject up;
    [SerializeField] private GameObject down;
    [SerializeField] private GameObject left;
    [SerializeField] private GameObject right;
    [SerializeField] private GameObject center;

    [Header("Rotation")]
    [SerializeField] private float rotationAmount = 90f;

    // Current rotation: 0, 1, 2, 3
    private int rotationSteps = 0;

    private void Start()
    {
        UpdateVisual();
    }

    private void OnValidate()
    {
        UpdateVisual();
    }

    public void RotateNode()
    {
        // Rotate visually anticlockwise
        transform.Rotate(0f, 0f, -rotationAmount);

        rotationSteps++;

        if (rotationSteps >= 4)
        {
            rotationSteps = 0;
        }
    }

    public void SetNodeType(NodeType type)
    {
        nodeType = type;

        rotationSteps = 0;
        transform.localRotation = Quaternion.identity;

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (up == null || down == null || left == null ||
            right == null || center == null)
        {
            return;
        }

        up.SetActive(false);
        down.SetActive(false);
        left.SetActive(false);
        right.SetActive(false);

        center.SetActive(true);

        // Base orientation only.
        // Actual rotation is handled by the parent transform.

        switch (nodeType)
        {
            case NodeType.Straight:
                up.SetActive(true);
                down.SetActive(true);
                break;

            case NodeType.Corner:
                up.SetActive(true);
                right.SetActive(true);
                break;

            case NodeType.TJunction:
                up.SetActive(true);
                left.SetActive(true);
                right.SetActive(true);
                break;

            case NodeType.Cross:
                up.SetActive(true);
                down.SetActive(true);
                left.SetActive(true);
                right.SetActive(true);
                break;

            case NodeType.DeadEnd:
                up.SetActive(true);
                break;
        }
    }

    // -------------------------
    // CONNECTION CHECKS
    // -------------------------

    public bool HasConnectionUp()
    {
        return GetConnection(0);
    }

    public bool HasConnectionRight()
    {
        return GetConnection(1);
    }

    public bool HasConnectionDown()
    {
        return GetConnection(2);
    }

    public bool HasConnectionLeft()
    {
        return GetConnection(3);
    }

    private bool GetConnection(int direction)
    {
        bool[] baseConnections = GetBaseConnections();

        // Clockwise rotation:
        // 0 = Up
        // 1 = Right
        // 2 = Down
        // 3 = Left
        //
        // A base connection moves clockwise by rotationSteps.
        // Therefore convert the world direction back to base direction.

        int baseDirection = (direction - rotationSteps + 4) % 4;

        return baseConnections[baseDirection];
    }
    //private bool GetConnection(int direction)
    //{
    //    bool[] baseConnections = GetBaseConnections();

    //    // Node rotates clockwise,
    //    // so convert world direction back to base direction.
    //    int baseDirection =
    //        (direction - rotationSteps + 4) % 4;

    //    return baseConnections[baseDirection];
    //}

    private bool[] GetBaseConnections()
    {
        bool[] connections = new bool[4];

        // Order:
        // 0 = Up
        // 1 = Right
        // 2 = Down
        // 3 = Left

        switch (nodeType)
        {
            case NodeType.Straight:
                connections[0] = true;
                connections[2] = true;
                break;

            case NodeType.Corner:
                connections[0] = true;
                connections[1] = true;
                break;

            case NodeType.TJunction:
                connections[0] = true;
                connections[1] = true;
                connections[3] = true;
                break;

            case NodeType.Cross:
                connections[0] = true;
                connections[1] = true;
                connections[2] = true;
                connections[3] = true;
                break;

            case NodeType.DeadEnd:
                connections[0] = true;
                break;
        }

        return connections;
    }
}