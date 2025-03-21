using PMCLIB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VT2_Aseptic_Production_Demonstrator
{
    internal class Pathfinding
    {
        private Program program = new();
        /*
        private static SystemCommands _systemCommand = new SystemCommands();
        //This class holds the pathfinding algorithm.
        private static XBotCommands _xbotCommand = new XBotCommands();
        WaitUntilTriggerParams CMD_params = new WaitUntilTriggerParams();
        int selector = 2;
        */ // Uncomment when using the code in the actual program

        public class Node
        {
            public int X, Y;
            public bool Walkable;
            public bool Initial;
            public bool Temp;
            public bool InitialWall;
            public bool ShuttleCenter;
            public bool DilatedWall;
            public Node? Parent;
            public int gCost, hCost;
            public int fCost
            { get { return gCost + hCost; } }

            public Node(int x, int y, bool walkable, bool wall, bool dilatedWall, bool shuttleCenter, bool temp, bool initial)
            {
                X = x;
                Y = y;
                Walkable = walkable;
                InitialWall = wall;
                ShuttleCenter = shuttleCenter;
                DilatedWall = dilatedWall;
                Parent = null;
                Temp = temp;
                Initial = initial;
            }
        }

        public class Grid
        {
            public int Width, Height;
            public Node[,] grid;

            public Grid(int width, int height)
            {
                Width = width;
                Height = height;
                grid = new Node[width, height];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        grid[x, y] = new Node(x, y, true, false, false, false, false, false);
                    }
                }
            }

            public void setObstacle(int x, int y, bool initial, bool temp)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    grid[x, y].Walkable = false;
                    if (initial)
                    {
                        grid[x, y].Initial = true;
                    }
                    if (temp)
                    {
                        grid[x, y].ShuttleCenter = true;
                    }
                }
            }

            public void removeTemps()
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (grid[x, y].Temp || grid[x, y].ShuttleCenter)
                        {
                            grid[x, y].Walkable = true;
                            grid[x, y].Temp = false;
                            grid[x, y].ShuttleCenter = false;
                        }
                    }
                }
            }

            public void removeMovingShuttles(List<Node> centersToRemove)
            {
                foreach (Node node in centersToRemove)
                {
                    grid[node.X, node.Y].Walkable = true;
                    grid[node.X, node.Y].ShuttleCenter = false;
                }
            }


            public void removeObstacle(int x, int y)
            {
                const int shuttleSize = 60; // Shuttle size in mm
                for (int i = -shuttleSize; i <= shuttleSize; i++)
                {
                    for (int j = -shuttleSize; j <= shuttleSize; j++)
                    {
                        if (x+i >= 0 && x+i < Width && y+j >= 0 && y+j < Height)
                        {
                            if (!grid[x, y].Initial || !grid[x, y].DilatedWall)
                            {
                                grid[x, y].Walkable = true;
                            }
                        }
                    }
                }
            }

            public List<(int, Node)> shuttlePosition(List<int> xbot_IDs)
            {
                List<(int, Node)> startPos = new();
                foreach (int shuttleID in xbot_IDs)
                {
                    /* Uncomment when using the code in the actual program
                     * Get commucation from UNS server.
                    XBotStatus pos = _xbotCommand.GetXbotStatus(shuttleID);
                    double[] temPos = pos.FeedbackPositionSI;
                    int positionX = (int)Math.Round(temPos[0]);
                    int positionY = (int)Math.Round(temPos[1]);
                    */
                    int positionX = 10; // Temporary position
                    int positionY = 10; // Temporary position

                    grid[positionX, positionY].ShuttleCenter = true;
                    startPos.Add((shuttleID, grid[positionX, positionY]));
                }
                return startPos;
            }

            public void staticObstacles(Grid gridGlobal)
            {
                for (int i = 0; i < gridGlobal.Width; i++)
                {
                    gridGlobal.setObstacle(i, 0, true, false);
                    gridGlobal.setObstacle(i, gridGlobal.Height - 1, true, false);
                }
                for (int i = 0; i < gridGlobal.Height; i++)
                {
                    gridGlobal.setObstacle(0, i, true, false);
                    gridGlobal.setObstacle(gridGlobal.Width - 1, i, true, false);
                }
                //Husk at sætte static obstacles for hvad der kommer til at være i midten.
            }


            public void dilateGrid()
            {
                const int shuttleSize = 60; // Shuttle size in mm
                int rows = Height;
                int cols = Width;
                for (int i = 0; i < cols; i++)
                {
                    for (int j = 0; j < rows; j++)
                    {
                        if (grid[i, j].Initial || grid[i, j].ShuttleCenter)
                        {
                            for (int k = -shuttleSize; k <= shuttleSize; k++)
                            {
                                for (int l = -shuttleSize; l <= shuttleSize; l++)
                                {
                                    int ni = i + k;
                                    int nj = j + l;
                                    if (ni >= 0 && ni < cols && nj >= 0 && nj < rows && grid[ni, nj].Walkable)
                                    {
                                        grid[ni, nj].Walkable = false;
                                        if (grid[i, j].Initial)
                                        {
                                            grid[ni, nj].DilatedWall = true;
                                        }
                                        if (grid[i, j].ShuttleCenter)
                                        {
                                            grid[ni, nj].Temp = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            public List<Node> GetNeighbors(Node node)
            {
                List<Node> neighbors = new List<Node>();

                int[,] shuttleDirections = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } }; // 4-way movement

                for (int i = 0; i < 4; i++)
                {
                    int nx = node.X + shuttleDirections[i, 0];
                    int ny = node.Y + shuttleDirections[i, 1];


                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && grid[nx, ny].Walkable && !grid[nx, ny].DilatedWall && !grid[nx, ny].Initial)
                    {
                        neighbors.Add(grid[nx, ny]);
                    }
                }
                return neighbors;
            }
        }

        public class AStar
        {
            public List<Node>? findPath(Grid grid, Node startNode, Node goalNode)
            {
                PriorityQueue<Node, int> openList = new PriorityQueue<Node, int>();
                HashSet<Node> closedList = new HashSet<Node>();
                Dictionary<Node, int> costSoFar = new Dictionary<Node, int>();

                openList.Enqueue(startNode, 0);
                costSoFar[startNode] = 0;

                while (openList.Count > 0)
                {
                    Node currentNode = openList.Dequeue();
                    if (currentNode == goalNode)
                    {
                        return reconstructPath(goalNode);
                    }
                    closedList.Add(currentNode);

                    foreach (Node neighbor in grid.GetNeighbors(currentNode))
                    {
                        if (closedList.Contains(neighbor)) continue;

                        int newG = costSoFar[currentNode] + 1;

                        if (!costSoFar.ContainsKey(neighbor) || newG < costSoFar[neighbor])
                        {
                            costSoFar[neighbor] = newG;
                            neighbor.gCost = newG;
                            neighbor.hCost = heuristic(neighbor, goalNode);
                            neighbor.Parent = currentNode;
                            openList.Enqueue(neighbor, neighbor.fCost);
                        }
                    }
                }
                return null; // No path found
            }
            private static List<Node> reconstructPath(Node? node)
            {
                List<Node> path = new List<Node>();
                while (node != null)
                {
                    path.Add(node);
                    node = node.Parent;
                }
                path.Reverse();
                return path;
            }

            private static int heuristic(Node a, Node b) // Manhattan Distance
            {
                return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
            }
            public void CBS()
            {
                // CBS algorithm
                Dictionary<int, List<Node>> constraints = new Dictionary<int, List<Node>>();
            }

            public void gridInitializer(int width, int height)
            {
                Grid gridGlobal = new(width, height);  // Create Grid
                gridGlobal.staticObstacles(gridGlobal); // Set initial obstacles
            }

            public Dictionary<int, List<double[]>> runPathfinder(List<int> xbot_IDs, List<(int, double, double)> ID_X_Y_END, Grid grid)
            {
                Grid gridGlobal = grid;
                List<(int, Node)> moving_Xbot_IDs = ID_X_Y_END.Select(item => (item.Item1, gridGlobal.grid[(int)(item.Item2 * 1000), (int)(item.Item3 * 1000)])).ToList();

                List<(int, Node)> startPos = gridGlobal.shuttlePosition(xbot_IDs); // Set shuttle position
                List<(int, Node)> movingStartPos = gridGlobal.shuttlePosition(moving_Xbot_IDs.Select(item => item.Item1).ToList()); // Set moving shuttle position
                gridGlobal.removeMovingShuttles(movingStartPos.Select(item => item.Item2).ToList()); // Remove moving shuttle's centers

                gridGlobal.dilateGrid(); // Dilate the grid
                bool conflictExists = true;
                Dictionary<int, (List<Node>, List<int>)> paths = new();
                Dictionary<int, Node> movingStartDict = movingStartPos.ToDictionary(item => item.Item1, item => item.Item2);
                Dictionary<int, Node> endNodeDict = moving_Xbot_IDs.ToDictionary(item => item.Item1, item => item.Item2);

                while (conflictExists)
                {
                    paths.Clear();

                    foreach (int shuttleID in moving_Xbot_IDs.Select(item => item.Item1).ToList())
                    {
                        AStar aStar = new();
                        List<Node>? path = aStar.findPath(gridGlobal, movingStartDict[shuttleID], endNodeDict[shuttleID]);

                        if (path != null)
                        {
                            paths[shuttleID] = (path, Enumerable.Range(0, path.Count).ToList());
                        }
                        else
                        {
                            Console.WriteLine($"No path available for shuttleID: {shuttleID}");
                        }
                    }

                    // Check for conflicts
                    List<(int, int, Node, int)> conflicts = ConflictSearcher(paths);
                    if (conflicts.Count == 0)
                    {
                        conflictExists = false;
                        gridGlobal.removeTemps();
                    }
                    else
                    {
                        foreach (var conflict in conflicts)
                        {
                            int shuttleID1 = conflict.Item1;
                            int shuttleID2 = conflict.Item2;
                            Node node = conflict.Item3;
                            int step = conflict.Item4;
                            gridGlobal.setObstacle(node.X, node.Y, false, true);
                            gridGlobal.dilateGrid();
                            Console.WriteLine($"Conflict at step {step} for shuttle {shuttleID1} and {shuttleID2} in node {node}");
                        }
                    }
                }
                return paths.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Item1.Select(node => new double[] { node.X / 1000.0, node.Y / 1000.0 }).ToList()
                );
            }
            public List<(int, int, Node, int)> ConflictSearcher(Dictionary<int, (List<Node>, List<int>)> paths)
            {
                List<(int, int, Node, int)> conflicts = new();

                foreach (var path1 in paths)
                {
                    foreach (var path2 in paths)
                    {
                        if (path1.Key >= path2.Key) continue; // Avoid duplicate comparisons

                        int minSteps = Math.Min(path1.Value.Item1.Count, path2.Value.Item1.Count);
                        for (int t = 0; t < minSteps; t++)
                        {
                            // Check direct conflict (same position, same time)
                            if (path1.Value.Item1[t] == path2.Value.Item1[t])
                            {
                                conflicts.Add((path1.Key, path2.Key, path1.Value.Item1[t], path1.Value.Item2[t]));
                            }

                            // Check swap conflict (A moves to B’s previous position & B moves to A’s previous position)
                            if (t > 0) // Swaps can only happen from step 1 onward
                            {
                                if (path1.Value.Item1[t] == path2.Value.Item1[t - 1] &&
                                    path2.Value.Item1[t] == path1.Value.Item1[t - 1])
                                {
                                    conflicts.Add((path1.Key, path2.Key, path1.Value.Item1[t], path1.Value.Item2[t]));
                                }
                            }
                        }
                    }
                }
                return conflicts;
            }
        }
    }
}
