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
            public bool Walkable;
            public Node Parent;

            public Node(int x, int y, bool walkable)
            {
                X = x;
                Y = y;
                Walkable = walkable;
                Parent = null;
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


            public void SetObstacle(int x, int y)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {   
                    Nodes[x, y].Walkable = false;
                }
            }

            public List<Node> GetNeighbors(Node node)
            {
                List<Node> neighbors = new List<Node>();

                

                int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, {-1, 0 } }; // 4-way movement

                for (int i = 0; i< 4;i++)
                    {
                        int nx = node.X + directions[i,0];
                        int ny = node.Y + directions[i,1];
                    

                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && Nodes[nx, ny].Walkable)
                    {
                        neighbors.Add(Nodes[nx, ny]);
                    }
                }
                return neighbors;
            }

            public class AStarPathfinder
            {
                public static List<Node> FindPath(Grid grid , Node start, Node goal)
                {
                    List<Node> openList = new List<Node>();
                    HashSet<Node> closedList = new HashSet<Node>();

                    openList.Add(start);

                    while (openList.Count > 0)
                    {
                        // Find the node with the lowest F in the open list
                        Node current = openList[0];
                        for (int i = 1; i < openList.Count; i++)
                        {
                            if (openList[i].F < current.F || )
                            {
                                current = openList[i];
                            }
                        }
                    }
                }
            }
        } 
    }
}
