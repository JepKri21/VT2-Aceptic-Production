using PMCLIB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{




    class ListBasedMotionStructure : MotionsFunctions
    {
        private static MotionsFunctions motionsFunctions = new MotionsFunctions();
        private static XBotCommands _xbotCommand = new XBotCommands();
        int selector = 3;
        private static Pathfinding.AStar pathfinding = new Pathfinding.AStar();

        Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();

        public int setSelectorOne()
        {
            return selector;
        }

        public void runListBasedMotion(int[] xbotIDs)
        {
            //Console.Clear();
            Console.WriteLine(" List based motions structure");
            Console.WriteLine("0    Return ");
            Console.WriteLine("1    Generate Trajectories ");
            Console.WriteLine("2    ");

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {
                case '0':
                    selector = 1;
                    break;

                case '1':
                    // Generate trajectories form the start postions to the target postions
                    double[] startPostion1 = { 0.060, 0.060 };
                    double[] targetPostion1 = { 0.660, 0.400 };
                    double[] startPostion2 = { 0.600, 0.400 };
                    double[] targetPostion2 = { 0.120, 0.120 };
                    double[] startPostion3 = { 0.360, 0.360 };
                    double[] targetPostion3 = { 0.600, 0.520 };
                    double[] startPostion4 = { 0.200, 0.200 };
                    double[] targetPostion4 = { 0.200, 0.600 };

                    List<(int, double, double)> targetPositions = new List<(int, double, double)>
                    {
                        (1, targetPostion1[0], targetPostion1[1]),
                        (2, targetPostion2[0], targetPostion2[1]),
                        (3, targetPostion3[0], targetPostion3[1]),
                        (4, targetPostion4[0], targetPostion4[1])
                    };
                    List<int> xbotIDer = new List<int>
                    {
                        1,2,3,4
                    };
                    Pathfinding.Grid grid = pathfinding.gridInitializer(720, 960);
                    trajectories = pathfinding.runPathfinder(xbotIDer, targetPositions, grid);

                    /*
                    motionsFunctions.LinarMotion(0, 1, startPostion1[0], startPostion1[1], "xy");
                    motionsFunctions.LinarMotion(0, 2, startPostion2[0], startPostion2[1], "xy");
                    motionsFunctions.LinarMotion(0, 3, startPostion3[0], startPostion3[1], "yx");
                    motionsFunctions.LinarMotion(0, 4, startPostion4[0], startPostion4[1], "xy");
                    /*
                    TrajectoryGenerator traj1 = new TrajectoryGenerator(1, startPostion1, targetPostion1, 20, "XY");
                    Console.WriteLine("Trajectory 1:");
                    traj1.PrintTrajectory();
                    trajectories[traj1.trajectory.First().Item1] = traj1.trajectory.Select(t => t.Item2).ToList();

                    TrajectoryGenerator traj2 = new TrajectoryGenerator(2, startPostion2, targetPostion2, 30, "XY");
                    Console.WriteLine("\n Trajectory 2:");
                    traj2.PrintTrajectory();
                    trajectories[traj2.trajectory.First().Item1] = traj2.trajectory.Select(t => t.Item2).ToList();

                    TrajectoryGenerator traj3 = new TrajectoryGenerator(3, startPostion3, targetPostion3, 30, "YX");
                    Console.WriteLine("\n Trajectory 2:");
                    traj3.PrintTrajectory();
                    trajectories[traj3.trajectory.First().Item1] = traj3.trajectory.Select(t => t.Item2).ToList();

                    TrajectoryGenerator traj4 = new TrajectoryGenerator(4, startPostion4, targetPostion4, 20, "YX");
                    Console.WriteLine("Trajectory 1:");
                    traj1.PrintTrajectory();
                    trajectories[traj4.trajectory.First().Item1] = traj4.trajectory.Select(t => t.Item2).ToList();
                    */
                    break;

                case '6':
                    int[] xbotIDs6 = { 1, 2, 3, 4 };
                    int maxLength6 = trajectories.Values.Max(t => t.Count);

                    List<Task> tasks6 = new List<Task>();

                    foreach (var xbotID in xbotIDs6)
                    {
                        if (trajectories.ContainsKey(xbotID) && trajectories[xbotID].Count > 0)
                        {
                            tasks6.Add(Task.Run(() =>
                            {
                                // Add the first two motions to the buffer
                                double[] point = trajectories[xbotID][0];
                                double[] nextPoint = trajectories[xbotID].Count > 1 ? trajectories[xbotID][1] : null;
                                motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");
                                if (nextPoint != null)
                                {
                                    motionsFunctions.LinarMotion(0, xbotID, nextPoint[0], nextPoint[1], "D");
                                }

                                for (int i = 2; i < trajectories[xbotID].Count; i++)
                                {
                                    MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                    int bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;

                                    // Print the initial buffer count
                                    Console.WriteLine($"Initial buffer count for xbotID {xbotID}: {bufferCount}");

                                    // Wait until there is only 1 motion left in the buffer
                                    while (bufferCount > 1)
                                    {
                                        BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                        bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;

                                        // Print the buffer count during the wait
                                        Console.WriteLine($"Buffer count for xbotID {xbotID} during wait: {bufferCount}");
                                    }

                                    // Add the next motion to the buffer
                                    point = trajectories[xbotID][i];
                                    Console.WriteLine($"Adding point {i + 1} to buffer for xbotID {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");
                                    motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");

                                    _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                }
                            }));
                        }
                    }

                    Task.WaitAll(tasks6.ToArray());
                    break;

                case '7':
                    runPathPlan(xbotIDs, trajectories);
                    break;
            }
        }


        public void runPathPlan(int[] xbotIDs, Dictionary<int, List<double[]>> pathPlan)
        {
            //int maxLength = trajectories.Values.Max(t => t.Count);

            List<Task> tasks = new List<Task>();

            foreach (var xbotID in xbotIDs)
            {
                if (pathPlan.ContainsKey(xbotID) && pathPlan[xbotID].Count > 0)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        // Add the first two motions to the buffer
                        double[] point = pathPlan[xbotID][0];
                        double[] nextPoint = pathPlan[xbotID].Count > 1 ? pathPlan[xbotID][1] : null;// if there is no next point, set it to null
                        //Console.WriteLine($"Adding first point to buffer for xbotID {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");
                        motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");
                        if (nextPoint != null)
                        {
                            //Console.WriteLine($"Adding second point to buffer for xbotID {xbotID}: {string.Join(", ", nextPoint.Select(p => Math.Round(p, 3)))}");
                            motionsFunctions.LinarMotion(0, xbotID, nextPoint[0], nextPoint[1], "D");
                        }

                        for (int i = 2; i < trajectories[xbotID].Count; i++)
                        {
                            MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                            int bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount; // get the buffer count

                            // Print the initial buffer count
                            //Console.WriteLine($"Initial buffer count for xbotID {xbotID}: {bufferCount}");

                            // Wait until there is only 1 motion left in the buffer
                            while (bufferCount > 1)
                            {
                                BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;

                                // Print the buffer count during the wait
                                //Console.WriteLine($"Buffer count for xbotID {xbotID} during wait: {bufferCount}");
                            }

                            // Add the next motion to the buffer
                            point = trajectories[xbotID][i];
                            //Console.WriteLine($"Adding point {i + 1} to buffer for xbotID {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");
                            motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");

                            
                        }
                    }));
                }
            }

            //Task.WaitAll(tasks.ToArray()); //Make sure all tasks are finished before continuing 
        }

    }
    
}
