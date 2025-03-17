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
            public bool InitialWall;
            public bool ShuttleCenter;
            public bool DilatedWall;
            public Node Parent;
            public int gCost, hCost;
            public int fCost
            { get { return gCost + hCost; } }

            public Node(int x, int y, bool walkable, bool wall, bool dilatedWall, bool shuttleCenter)
            {
                X = x;
                Y = y;
                Walkable = walkable;
                InitialWall = wall;
                ShuttleCenter = shuttleCenter;
                DilatedWall = dilatedWall;
                Parent = null;
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
                        grid[x, y] = new Node(x, y, true, false, false, false); 
                        //  walkable, no-lwall, no-dilatedwall, no-shuttlecenter
                    }
                }
            }

            public void setObstacle(int x, int y, bool initial)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {   
                    grid[x, y].Walkable = false;
                    if (initial)
                    {
                        grid[x, y].Initial = true;
                    }
                }
            }

            public void removeObstacle(int x, int y)
            {
                int shuttleSize = 60; // Shuttle size in mm
                for (int i = -shuttleSize; i < shuttleSize; i++)
                {
                    for (int j = -shuttleSize; j < shuttleSize; j++)
                    {
                        if (x >= 0 && x < Width && y >= 0 && y < Height)
                        {
                            if (!grid[x, y].Initial || !grid[x, y].DilatedWall)
                            {
                                grid[x, y].Walkable = true;
                            }
                        }
                    }
                }
            } 

            public void shuttlePosition(int shuttleNr)
            {
                shuttleNr = 1; // Number of shuttles // Change to actual number of shuttles
                double[,] shuttlePosition = new double[shuttleNr, 2];
                for (int shuttleID = 1; shuttleID <= shuttleNr; shuttleID++)
                {
                    /*
                    XBotStatus pos = _xbotCommand.GetXbotStatus(shuttleID);
                    double[] temPos = pos.FeedbackPositionSI;
                    int positionX = (int)Math.Round(temPos[0]);
                    int positionY = (int)Math.Round(temPos[1]);
                    */
                    int positionX = 10; // Temporary position
                    int positionY = 10; // Temporary position

                    shuttlePosition[shuttleID - 1, 0] = positionX;
                    shuttlePosition[shuttleID - 1, 1] = positionY;
                    grid[positionX, positionY].ShuttleCenter = true;
                }
            }

            public void staticObstacles(Grid gridGlobal)
            {
                for(int i = 0; i < gridGlobal.Width; i++)
                {
                    gridGlobal.setObstacle(i, 0, true);
                    gridGlobal.setObstacle(i, gridGlobal.Height - 1, true);
                }
                for(int i = 0; i < gridGlobal.Height; i++)
                {
                    gridGlobal.setObstacle(0, i, true);
                    gridGlobal.setObstacle(gridGlobal.Width - 1, i, true);
                }
                //Husk at sætte static obstacles for hvad der kommer til at være i midten.
            }


            public void dilateGrid()
            {
                int shuttleSize = 60; // Shuttle size in mm
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
                                        if (grid[ni, nj].Initial)
                                        {
                                            grid[ni, nj].DilatedWall = true;
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

                int[,] shuttleDirections = { { 0, 1 }, { 1, 0 }, { 0, -1 }, {-1, 0 } }; // 4-way movement

                for (int i = 0; i < 4; i++)
                    {
                        int nx = node.X + shuttleDirections[i,0];
                        int ny = node.Y + shuttleDirections[i,1];
                    

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
            public List<Node> findPath(Grid grid, Node startNode, Node goalNode)
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

                        int newG = costSoFar[neighbor] + 1;

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
            private static List<Node> reconstructPath(Node node)
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
        }

        public void runPathfinder(Node startNode, Node endNode)
        {
            Grid gridGlobal = new(50, 50);  // Create Grid
            gridGlobal.staticObstacles(gridGlobal); // Set initial obstacles
            gridGlobal.dilateGrid(); // Dilate the grid
            AStar aStar = new();
            List<Node> path = aStar.findPath(gridGlobal, startNode, endNode);
            if (path != null)
            {
                foreach (Node node in path)
                {
                    Console.WriteLine(node.X + " " + node.Y);
                }
            }
            else
            {
                Console.WriteLine("No path found");
            }
        }
    }
}
