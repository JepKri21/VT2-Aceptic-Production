using System;
using System.Collections.Generic;

namespace VT2_Aseptic_Production_Pathfinding
{
    class Program
    {
        static void Main()
        {
            // Opret et grid
            Grid grid = new Grid(20, 20); // Mindre grid til testformål
            grid.staticObstacles();
            Console.WriteLine("Statiske forhindringer tilføjet.");

            // Tilføj en tilfældig forhindring i midten
            grid.setObstacle(10, 10);
            grid.setObstacle(10, 11);
            grid.setObstacle(11, 10);
            grid.setObstacle(11, 11);
            Console.WriteLine("Tilføjet central forhindring.");

            // Dilater forhindringer (simulerer shuttle-størrelse)
            grid.dilateObstacles();
            Console.WriteLine("Forhindringer dilateret.");

            // Simuler shuttles på tilfældige positioner
            int shuttleCount = 3;
            Random rand = new Random();
            for (int i = 0; i < shuttleCount; i++)
            {
                int x, y;
                do
                {
                    x = rand.Next(0, grid.Width);
                    y = rand.Next(0, grid.Height);
                } while (!grid.Nodes[x, y].nodeWalkable); // Sørger for, at shuttlen ikke lander på en forhindring

                Console.WriteLine($"Shuttle {i + 1} placeret ved ({x}, {y})");
            }

            // Test GetNeighbors() funktion
            Node testNode = grid.Nodes[5, 5]; // Vælg en tilfældig node til test
            List<Node> neighbors = grid.GetNeighbors(testNode);
            Console.WriteLine($"Naboer til node ({testNode.X}, {testNode.Y}):");
            foreach (var neighbor in neighbors)
            {
                Console.WriteLine($"({neighbor.X}, {neighbor.Y})");
            }

            // Udskriv grid for at visualisere forhindringer
            PrintGrid(grid);
        }

        static void PrintGrid(Grid grid)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (!grid.Nodes[x, y].nodeWalkable)
                        Console.Write("█ "); // Forhindring
                    else
                        Console.Write(". "); // Walkable område
                }
                Console.WriteLine();
            }
        }
    }
}
