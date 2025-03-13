using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    class TrajectoryGenerator
    {
        public List<(int, double[])> trajectory;

        public TrajectoryGenerator(int xbotId, double[] startPostion, double[] tagetPostion, int numPoints, string movement)
        {
            trajectory = new List<(int, double[])>();
            GenerateTrajectory(xbotId, startPostion, tagetPostion, numPoints, movement);
        }
        private void GenerateTrajectory(int xbotId, double[] startPostion, double[] tagetPostion, int numPoints, string movement)
        {
            switch (movement)
            {
                case "XY":
                    for (int i = 1; i <= numPoints / 2; i++)
                    {
                        double x = startPostion[0] + (tagetPostion[0] - startPostion[0]) * i / (numPoints / 2);
                        double[] point = { x, startPostion[1] };
                        trajectory.Add((xbotId, point));
                    }
                    for (int i = 0; i <= numPoints / 2; i++)
                    {
                        double y = startPostion[1] + (tagetPostion[1] - startPostion[1]) * i / (numPoints / 2);
                        double[] point = { tagetPostion[0], y };
                        trajectory.Add((xbotId, point));
                    }

                    break;
                case "YX":
                    // Step 1: Move in Y direction first
                    for (int i = 0; i <= numPoints / 2; i++)
                    {
                        double y = startPostion[1] + (tagetPostion[1] - startPostion[1]) * i / (numPoints / 2);
                        double[] point = { startPostion[0], y };
                        trajectory.Add((xbotId, point));
                    }
                    // Step 2: Move in X direction
                    for (int i = 1; i <= numPoints / 2; i++)
                    {
                        double x = startPostion[0] + (tagetPostion[0] - startPostion[0]) * i / (numPoints / 2);
                        double[] point = { x, tagetPostion[1] };
                        trajectory.Add((xbotId, point));
                    }
                    break;

            }

            


        }
        public void PrintTrajectory()
        {
            foreach (var point in trajectory)
            {
                Console.WriteLine($"Label: {point.Item1}");
                // Coordinate entry
                Console.WriteLine($"({point.Item2[0]:F3}, {point.Item2[1]:F3})");
            }
        }
    }
}
