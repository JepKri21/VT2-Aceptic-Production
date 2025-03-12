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
    internal class pathfinding
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //This class holds the pathfinding algorithm.
        private static XBotCommands _xbotCommand = new XBotCommands();
        WaitUntilTriggerParams CMD_params = new WaitUntilTriggerParams();
        int selector = 2;

        public class Node
        {
            public int X, Y;
            public bool nodeWalkable;
            public Node nodeParent;
            public int gCost, hCost;
            public int fCost
            { get { return gCost + hCost; } }

            public Node(int x, int y, bool walkable)
            {
                X = x;
                Y = y;
                nodeWalkable = walkable;
                nodeParent = null;
                gCost = hCost = 0;
            }
        }

        public class Grid
        {
            public int Width, Height;
            public Node[,] Nodes;

            public Grid(int width, int height)
            {
                Width = width;
                Height = height;
                Nodes = new Node[width, height];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Nodes[x, y] = new Node(x, y, true); // Default: walkable
                    }
                }
            }

                public Grid ShallowCopy()
                {
                    return (Grid)this.MemberwiseClone();
                }

            public void setObstacle(int x, int y)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {   
                    Nodes[x, y].nodeWalkable = false;
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

            static int[,] createShuttleGrid(int[,] grid, int[] shuttleSize)
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
                int[,] expandedGrid = (int[,])grid.Clone();
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
                                        expandedGrid[ni, nj] = 1;
                                    }
                                }
                            }
                        }
                    }
                }
                return expandedGrid;
            }

            public List<Node> GetNeighbors(Node node)
            {
                List<Node> neighbors = new List<Node>();

                

                int[,] shuttleDirections = { { 0, 1 }, { 1, 0 }, { 0, -1 }, {-1, 0 } }; // 4-way movement

                for (int i = 0; i < 4; i++)
                    {
                        int nx = node.X + shuttleDirections[i,0];
                        int ny = node.Y + shuttleDirections[i,1];
                    

                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && Nodes[nx, ny].nodeWalkable)
                    {
                        neighbors.Add(Nodes[nx, ny]);
                    }
                }
                return neighbors;
            }
        } 
    }
}
