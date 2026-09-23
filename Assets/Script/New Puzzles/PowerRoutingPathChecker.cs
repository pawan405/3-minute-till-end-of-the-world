//using UnityEngine;
//using System.Collections.Generic;

//public class PowerRoutingPathChecker : MonoBehaviour
//{
//    [Header("Grid")]
//    [SerializeField] private int rows = 6;
//    [SerializeField] private int columns = 6;

//    [Header("Grid Parent")]
//    [SerializeField] private Transform gridParent;

//    [Header("Source & Receiver")]
//    [SerializeField] private int sourceRow = 0;
//    [SerializeField] private int sourceColumn = 0;

//    [SerializeField] private int receiverRow = 5;
//    [SerializeField] private int receiverColumn = 5;

//    public bool CheckPath()
//    {
//        PowerRoutingNode[,] nodes = GetGridNodes();

//        if (nodes == null)
//        {
//            return false;
//        }

//        bool[,] visited = new bool[rows, columns];

//        Queue<Vector2Int> queue = new Queue<Vector2Int>();

//        Vector2Int source =
//            new Vector2Int(sourceRow, sourceColumn);

//        Vector2Int receiver =
//            new Vector2Int(receiverRow, receiverColumn);

//        queue.Enqueue(source);
//        visited[sourceRow, sourceColumn] = true;

//        while (queue.Count > 0)
//        {
//            Vector2Int current = queue.Dequeue();

//            // Receiver reached
//            if (current == receiver)
//            {
//                return true;
//            }

//            PowerRoutingNode currentNode =
//                nodes[current.x, current.y];

//            // UP
//            if (currentNode.HasConnectionUp())
//            {
//                TryAddNeighbour(
//                    current,
//                    new Vector2Int(current.x - 1, current.y),
//                    true,
//                    nodes,
//                    visited,
//                    queue
//                );
//            }

//            // RIGHT
//            if (currentNode.HasConnectionRight())
//            {
//                TryAddNeighbour(
//                    current,
//                    new Vector2Int(current.x, current.y + 1),
//                    false,
//                    nodes,
//                    visited,
//                    queue
//                );
//            }

//            // DOWN
//            if (currentNode.HasConnectionDown())
//            {
//                TryAddNeighbour(
//                    current,
//                    new Vector2Int(current.x + 1, current.y),
//                    true,
//                    nodes,
//                    visited,
//                    queue
//                );
//            }

//            // LEFT
//            if (currentNode.HasConnectionLeft())
//            {
//                TryAddNeighbour(
//                    current,
//                    new Vector2Int(current.x, current.y - 1),
//                    false,
//                    nodes,
//                    visited,
//                    queue
//                );
//            }
//        }

//        return false;
//    }

//    private void TryAddNeighbour(
//        Vector2Int current,
//        Vector2Int neighbour,
//        bool vertical,
//        PowerRoutingNode[,] nodes,
//        bool[,] visited,
//        Queue<Vector2Int> queue)
//    {
//        // Outside grid
//        if (neighbour.x < 0 ||
//            neighbour.x >= rows ||
//            neighbour.y < 0 ||
//            neighbour.y >= columns)
//        {
//            return;
//        }

//        // Already checked
//        if (visited[neighbour.x, neighbour.y])
//        {
//            return;
//        }

//        PowerRoutingNode neighbourNode =
//            nodes[neighbour.x, neighbour.y];

//        bool connectsBack = false;

//        if (vertical)
//        {
//            // Current UP → neighbour must connect DOWN
//            if (neighbour.x < current.x)
//            {
//                connectsBack =
//                    neighbourNode.HasConnectionDown();
//            }
//            // Current DOWN → neighbour must connect UP
//            else
//            {
//                connectsBack =
//                    neighbourNode.HasConnectionUp();
//            }
//        }
//        else
//        {
//            // Current LEFT → neighbour must connect RIGHT
//            if (neighbour.y < current.y)
//            {
//                connectsBack =
//                    neighbourNode.HasConnectionRight();
//            }
//            // Current RIGHT → neighbour must connect LEFT
//            else
//            {
//                connectsBack =
//                    neighbourNode.HasConnectionLeft();
//            }
//        }

//        if (!connectsBack)
//        {
//            return;
//        }

//        visited[neighbour.x, neighbour.y] = true;
//        queue.Enqueue(neighbour);
//    }

//    private PowerRoutingNode[,] GetGridNodes()
//    {
//        if (gridParent == null)
//        {
//            Debug.LogError(
//                "PowerRoutingPathChecker: Grid Parent is missing!"
//            );

//            return null;
//        }

//        PowerRoutingNode[,] nodes =
//            new PowerRoutingNode[rows, columns];

//        if (gridParent.childCount < rows * columns)
//        {
//            Debug.LogError(
//                "Not enough nodes in the grid!"
//            );

//            return null;
//        }

//        for (int row = 0; row < rows; row++)
//        {
//            for (int column = 0; column < columns; column++)
//            {
//                int index =
//                    row * columns + column;

//                nodes[row, column] =
//                    gridParent
//                    .GetChild(index)
//                    .GetComponent<PowerRoutingNode>();
//            }
//        }

//        return nodes;
//    }

//    private void Update()
//    {
//        if (Input.GetKeyDown(KeyCode.P))
//        {
//            bool solved = CheckPath();

//            Debug.Log("POWER ROUTING PATH: " + solved);
//        }
//    }
//}
using UnityEngine;
using System.Collections.Generic;

public class PowerRoutingPathChecker : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int rows = 6;
    [SerializeField] private int columns = 6;

    [Header("Grid Parent")]
    [SerializeField] private Transform gridParent;

    [Header("Source & Receiver")]
    [SerializeField] private int sourceRow = 0;
    [SerializeField] private int sourceColumn = 0;

    [SerializeField] private int receiverRow = 5;
    [SerializeField] private int receiverColumn = 5;

    public bool CheckPath()
    {
        PowerRoutingNode[,] nodes = GetGridNodes();

        if (nodes == null)
            return false;

        bool[,] visited = new bool[rows, columns];

        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        Vector2Int source =
            new Vector2Int(sourceRow, sourceColumn);

        Vector2Int receiver =
            new Vector2Int(receiverRow, receiverColumn);

        queue.Enqueue(source);

        visited[source.x, source.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == receiver)
            {
                Debug.Log("SOURCE → RECEIVER CONNECTED!");
                return true;
            }

            PowerRoutingNode node =
                nodes[current.x, current.y];

            // =========================
            // UP
            // =========================

            if (node.HasConnectionUp())
            {
                CheckNeighbour(
                    current,
                    new Vector2Int(current.x - 1, current.y),
                    Direction.Up,
                    nodes,
                    visited,
                    queue
                );
            }

            // =========================
            // RIGHT
            // =========================

            if (node.HasConnectionRight())
            {
                CheckNeighbour(
                    current,
                    new Vector2Int(current.x, current.y + 1),
                    Direction.Right,
                    nodes,
                    visited,
                    queue
                );
            }

            // =========================
            // DOWN
            // =========================

            if (node.HasConnectionDown())
            {
                CheckNeighbour(
                    current,
                    new Vector2Int(current.x + 1, current.y),
                    Direction.Down,
                    nodes,
                    visited,
                    queue
                );
            }

            // =========================
            // LEFT
            // =========================

            if (node.HasConnectionLeft())
            {
                CheckNeighbour(
                    current,
                    new Vector2Int(current.x, current.y - 1),
                    Direction.Left,
                    nodes,
                    visited,
                    queue
                );
            }
        }

        Debug.Log("SOURCE → RECEIVER NOT CONNECTED");

        return false;
    }

    private enum Direction
    {
        Up,
        Right,
        Down,
        Left
    }

    private void CheckNeighbour(
        Vector2Int current,
        Vector2Int neighbour,
        Direction direction,
        PowerRoutingNode[,] nodes,
        bool[,] visited,
        Queue<Vector2Int> queue)
    {
        // Outside grid
        if (neighbour.x < 0 ||
            neighbour.x >= rows ||
            neighbour.y < 0 ||
            neighbour.y >= columns)
        {
            return;
        }

        // Already visited
        if (visited[neighbour.x, neighbour.y])
        {
            return;
        }

        PowerRoutingNode neighbourNode =
            nodes[neighbour.x, neighbour.y];

        bool connectsBack = false;

        switch (direction)
        {
            case Direction.Up:
                connectsBack =
                    neighbourNode.HasConnectionDown();
                break;

            case Direction.Right:
                connectsBack =
                    neighbourNode.HasConnectionLeft();
                break;

            case Direction.Down:
                connectsBack =
                    neighbourNode.HasConnectionUp();
                break;

            case Direction.Left:
                connectsBack =
                    neighbourNode.HasConnectionRight();
                break;
        }

        if (!connectsBack)
        {
            return;
        }

        visited[neighbour.x, neighbour.y] = true;

        queue.Enqueue(neighbour);
    }

    private PowerRoutingNode[,] GetGridNodes()
    {
        if (gridParent == null)
        {
            Debug.LogError(
                "PowerRoutingPathChecker: Grid Parent is missing!"
            );

            return null;
        }

        if (gridParent.childCount < rows * columns)
        {
            Debug.LogError(
                $"Expected {rows * columns} nodes, " +
                $"but found {gridParent.childCount}."
            );

            return null;
        }

        PowerRoutingNode[,] nodes =
            new PowerRoutingNode[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index =
                    row * columns + column;

                nodes[row, column] =
                    gridParent
                    .GetChild(index)
                    .GetComponent<PowerRoutingNode>();

                if (nodes[row, column] == null)
                {
                    Debug.LogError(
                        $"Missing PowerRoutingNode at " +
                        $"[{row},{column}]"
                    );

                    return null;
                }
            }
        }

        return nodes;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
          bool solved = CheckPath();

               Debug.Log("POWER ROUTING PATH: " + solved);
        }
    }
}