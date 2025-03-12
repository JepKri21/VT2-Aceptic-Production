using System;
using System.Collections.Generic;

namespace VT2_Aseptic_Production_Pathfinding
{
    class Program
    {
        static void Main()
        {
            // Opret et mindre grid til test
            Grid grid = new Grid(20, 20);
            grid.staticObstacles();
            Console.WriteLine("Statiske forhindringer tilføjet.");

            // Tilføj en central forhindring
            grid.setObstacle(10, 10);
            grid.setObstacle(10, 11);
            grid.setObstacle(11, 10);
            grid.setObstacle(11, 11);
            Console.WriteLine("Tilføjet central forhindring.");

            // Dilater forhindringer
            grid.dilateObstacles();
            Console.WriteLine("Forhindringer dilateret.");

            // Simuler shuttles på tilfældige positioner
            int shuttleCount = 3;
            Random rand = new Random();
            List<Node> shuttles = new List<Node>();

            for (int i = 0; i < shuttleCount; i++)
            {
                int x, y;
                do
                {
                    x = rand.Next(0, grid.Width);
                    y = rand.Next(0, grid.Height);
                } while (!grid.Nodes[x, y].nodeWalkable);

                shuttles.Add(grid.Nodes[x, y]);
                Console.WriteLine($"Shuttle {i + 1} placeret ved ({x}, {y})");
            }

            // Test GetNeighbors()
            Node testNode = grid.Nodes[5, 5];
            List<Node> neighbors = grid.GetNeighbors(testNode);
            Console.WriteLine($"Naboer til node ({testNode.X}, {testNode.Y}):");
            foreach (var neighbor in neighbors)
            {
                Console.WriteLine($"({neighbor.X}, {neighbor.Y})");
            }

            // Udskriv grid
            PrintGrid(grid, shuttles);
        }

        static void PrintGrid(Grid grid, List<Node> shuttles)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (!grid.Nodes[x, y].nodeWalkable)
                        Console.Write("█ "); // Forhindring
                    else if (shuttles.Exists(s => s.X == x && s.Y == y))
                        Console.Write("S "); // Shuttle
                    else
                        Console.Write(". "); // Walkable område
                }
                Console.WriteLine();
            }
        }
    }

    public class Node
    {
        public int X, Y;
        public bool nodeWalkable;
        public Node nodeParent;
        public int gCost, hCost;
        public int fCost => gCost + hCost;

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
                    Nodes[x, y] = new Node(x, y, true);
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

        public void staticObstacles()
        {
            for (int i = 0; i < Width; i++)
            {
                setObstacle(i, 0);
                setObstacle(i, Height - 1);
            }
            for (int i = 0; i < Height; i++)
            {
                setObstacle(0, i);
                setObstacle(Width - 1, i);
            }
        }

        public void dilateObstacles()
        {
            int shuttleSize = 2; // Reduceret for testformål
            Grid gridCopy = this.ShallowCopy();

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    if (!gridCopy.Nodes[i, j].nodeWalkable)
                    {
                        for (int k = -shuttleSize; k <= shuttleSize; k++)
                        {
                            for (int l = -shuttleSize; l <= shuttleSize; l++)
                            {
                                int ni = i + k;
                                int nj = j + l;
                                if (ni >= 0 && ni < Width && nj >= 0 && nj < Height)
                                {
                                    Nodes[ni, nj].nodeWalkable = false;
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
            int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

            foreach (var dir in directions)
            {
                int nx = node.X + dir[0];
                int ny = node.Y + dir[1];

                if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && Nodes[nx, ny].nodeWalkable)
                {
                    neighbors.Add(Nodes[nx, ny]);
                }
            }
            return neighbors;
        }
    }
}
