using UnityEngine;

public class PowerRoutingPuzzle : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int rows = 6;
    [SerializeField] private int columns = 6;

    [Header("References")]
    [SerializeField] private GameObject powerRoutingNodePrefab;
    [SerializeField] private Transform gridParent;

    private PowerRoutingNode[,] nodes;

    private bool[,] up;
    private bool[,] down;
    private bool[,] left;
    private bool[,] right;

    private void Start()
    {
        GeneratePuzzle();

        DebugSolution();
    }

    private void GeneratePuzzle()
    {
        CreateGrid();

        CreateEmptyConnections();

        CreateGuaranteedPath();

        CreateNodeTypes();

        //ScramblePuzzle();
    }

    // ==================================================
    // CREATE GRID
    // ==================================================

    private void CreateGrid()
    {
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Destroy(gridParent.GetChild(i).gameObject);
        }

        nodes = new PowerRoutingNode[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                GameObject nodeObject =
                    Instantiate(
                        powerRoutingNodePrefab,
                        gridParent
                    );

                nodeObject.name =
                    $"PowerNode_{row}_{column}";

                nodes[row, column] =
                    nodeObject.GetComponent<PowerRoutingNode>();
            }
        }
    }

    // ==================================================
    // CONNECTION ARRAYS
    // ==================================================

    private void CreateEmptyConnections()
    {
        up = new bool[rows, columns];
        down = new bool[rows, columns];
        left = new bool[rows, columns];
        right = new bool[rows, columns];
    }

    // ==================================================
    // GUARANTEED PATH
    // ==================================================

    private void CreateGuaranteedPath()
    {
        int row = 0;
        int column = 0;

        while (row < rows - 1 ||
               column < columns - 1)
        {
            bool moveRight;

            if (row == rows - 1)
            {
                moveRight = true;
            }
            else if (column == columns - 1)
            {
                moveRight = false;
            }
            else
            {
                moveRight = Random.value > 0.5f;
            }

            if (moveRight)
            {
                right[row, column] = true;

                left[row, column + 1] = true;

                column++;
            }
            else
            {
                down[row, column] = true;

                up[row + 1, column] = true;

                row++;
            }
        }
    }

    // ==================================================
    // CONVERT CONNECTIONS → NODE TYPES
    // ==================================================

    private void CreateNodeTypes()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int connectionCount = 0;

                if (up[row, column])
                    connectionCount++;

                if (down[row, column])
                    connectionCount++;

                if (left[row, column])
                    connectionCount++;

                if (right[row, column])
                    connectionCount++;

                PowerRoutingNode.NodeType type;

                switch (connectionCount)
                {
                    case 1:
                        type =
                            PowerRoutingNode.NodeType.DeadEnd;
                        break;

                    case 2:

                        bool straight =
                            (up[row, column] &&
                             down[row, column]) ||
                            (left[row, column] &&
                             right[row, column]);

                        type = straight
                            ? PowerRoutingNode.NodeType.Straight
                            : PowerRoutingNode.NodeType.Corner;

                        break;

                    case 3:
                        type =
                            PowerRoutingNode.NodeType.TJunction;
                        break;

                    case 4:
                        type =
                            PowerRoutingNode.NodeType.Cross;
                        break;

                    default:
                        type =
                            PowerRoutingNode.NodeType.DeadEnd;
                        break;
                }

                nodes[row, column].SetNodeType(type);
            }
        }
    }

    // ==================================================
    // SCRAMBLE
    // ==================================================

    //private void ScramblePuzzle()
    //{
    //    for (int row = 0; row < rows; row++)
    //    {
    //        for (int column = 0; column < columns; column++)
    //        {
    //            int rotations =
    //                Random.Range(0, 4);

    //            for (int i = 0; i < rotations; i++)
    //            {
    //                nodes[row, column].RotateNode();
    //            }
    //        }
    //    }
    //}
    private void DebugSolution()
    {
        Debug.Log("===== GENERATED SOLUTION =====");

        for (int row = 0; row < rows; row++)
        {
            string line = "";

            for (int column = 0; column < columns; column++)
            {
                string connections = "";

                if (up[row, column])
                    connections += "U";

                if (right[row, column])
                    connections += "R";

                if (down[row, column])
                    connections += "D";

                if (left[row, column])
                    connections += "L";

                line += $"[{connections}] ";
            }

            Debug.Log(line);
        }
    }
}