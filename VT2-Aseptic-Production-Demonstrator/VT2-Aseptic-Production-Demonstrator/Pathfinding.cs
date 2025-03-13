using PMCLIB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VT2_Aseptic_Production_Pathfinding
{
    internal class Pathfinding
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //This class holds the pathfinding algorithm.
        private static XBotCommands _xbotCommand = new XBotCommands();
        WaitUntilTriggerParams CMD_params = new WaitUntilTriggerParams();
        int selector = 2;

        public class Node
        {
            public int X, Y;
            public bool Walkable;
            public Node Parent;
            public int gCost, hCost;
            public int fCost
            { get { return gCost + hCost; } }

            public Node(int x, int y, bool walkable)
            {
                X = x;
                Y = y;
                Walkable = walkable;
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
                        grid[x, y] = new Node(x, y, true); // Default: walkable
                    }
                }
            }

            public void setObstacle(int x, int y)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {   
                    grid[x, y].Walkable = false;
                }
            }

            public void removeObstacle(int x, int y)
            {
                for (int i = -shuttleSize; i < shuttleSize; i++)
                {
                    for (int j = -shuttleSize; j < shuttleSize; j++)
                    {
                        if (x >= 0 && x < Width && y >= 0 && y < Height)
                        {
                            grid[x, y].Walkable = true;
                        }
                    }
                }
            } 

            public void shuttlePosition(int shuttleNr)
            {
                double[,] shuttlePosition = new double[shuttleNr, 2];
                for (int shuttleID = 1; shuttleID <= shuttleNr; shuttleID++)
                {
                    XBotStatus pos = _xbotCommand.GetXbotStatus(shuttleID);
                    double[] temPos = pos.FeedbackPositionSI;
                    int positionX = (int)Math.Round(temPos[0]);
                    int positionY = (int)Math.Round(temPos[1]);

                    shuttlePosition[shuttleID - 1, 0] = positionX;
                    shuttlePosition[shuttleID - 1, 1] = positionY;
                }
            }

            Grid gridGlobal = new Grid(720, 960); // Temporary grid size
            public void staticObstacles()
            {
                for(int i = 0; i < gridGlobal.Width; i++)
                {
                    gridGlobal.setObstacle(i, 0);
                    gridGlobal.setObstacle(i, gridGlobal.Height - 1);
                }
                for(int i = 0; i < gridGlobal.Height; i++)
                {
                    gridGlobal.setObstacle(0, i);
                    gridGlobal.setObstacle(gridGlobal.Width - 1, i);
                }
                //Husk at sætte static obstacles for hvad der kommer til at være i midten.
            }

            int shuttleSize = 60; // Shuttle size in mm

            static int[,] dilateGrid(int[,] grid, int[] shuttleSize)
            {
                int h = shuttleSize[0];
                int w = shuttleSize[1];
                int rows = grid.GetLength(0);
                int cols = grid.GetLength(1);

                int[,] stucture = new int[h, w];
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        stucture[i, j] = 1;
                    }
                }
                int[,] dilatedGrid = (int[,])grid.Clone();
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        if (grid[i, j] == 1)
                        {
                            for (int k = -h / 2; k <= h / 2; k++)
                            {
                                for (int l = -w / 2; l <= w / 2; l++)
                                {
                                    int ni = i + k;
                                    int nj = j + l;
                                    if (ni >= 0 && ni < rows && nj >= 0 && nj < cols)
                                    {
                                        dilatedGrid[ni, nj] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
                return dilatedGrid;
            }

            public List<Node> GetNeighbors(Node node)
            {
                List<Node> neighbors = new List<Node>();

                

                int[,] shuttleDirections = { { 0, 1 }, { 1, 0 }, { 0, -1 }, {-1, 0 } }; // 4-way movement

                for (int i = 0; i < 4; i++)
                    {
                        int nx = node.X + shuttleDirections[i,0];
                        int ny = node.Y + shuttleDirections[i,1];
                    

                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && grid[nx, ny].Walkable)
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
    }
}
