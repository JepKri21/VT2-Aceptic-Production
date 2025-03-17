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

        WaitUntilTriggerParams CMD_params = new WaitUntilTriggerParams();
        int selector = 3;

        Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();

        public int setSelectorOne()
        {
            return selector;
        }

        public void runListBasedMotion()
        {
            //Console.Clear();
            Console.WriteLine(" List based motions structure");
            Console.WriteLine("0    Return ");
            Console.WriteLine("1    Run Code ");
            Console.WriteLine("2    Print Trajectories");

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {
                case '0':
                    selector = 1;
                    break;

                case '1':
                    // Generate trajectories form the start postions to the target postions
                    double[] startPostion1 = { 0.120, 0.120 };
                    double[] targetPostion1 = { 0.600, 0.840 };
                    double[] startPostion2 = { 0.600, 0.120 };
                    double[] targetPostion2 = { 0.360, 0.360 };

                    _xbotCommand.MotionBufferControl(1, MOTIONBUFFEROPTIONS.CLEARBUFFER);
                    _xbotCommand.MotionBufferControl(2, MOTIONBUFFEROPTIONS.CLEARBUFFER);

                    motionsFunctions.LinarMotion(0, 1, startPostion1[0], startPostion1[1], "xy");
                    motionsFunctions.LinarMotion(0, 2, startPostion2[0], startPostion2[1], "xy");

                    TrajectoryGenerator traj1 = new TrajectoryGenerator(1, startPostion1, targetPostion1, 20, "YX");
                    //Console.WriteLine("Trajectory 1:");
                    traj1.PrintTrajectory();
                    trajectories[traj1.trajectory.First().Item1] = traj1.trajectory.Select(t => t.Item2).ToList();

                    TrajectoryGenerator traj2 = new TrajectoryGenerator(2, startPostion2, targetPostion2, 30, "XY");
                    //Console.WriteLine("\n Trajectory 2:");
                    traj2.PrintTrajectory();
                    trajectories[traj2.trajectory.First().Item1] = traj2.trajectory.Select(t => t.Item2).ToList();

                    break;

                case '2':
                    int[] xbotIDs2 = { 1 };
                    int maxLength = trajectories.Values.Max(t => t.Count);

                    for (int i = 0; i < maxLength; i++)
                    {
                        foreach (var xbotID in xbotIDs2)
                        {
                            if (trajectories.ContainsKey(xbotID) && i < trajectories[xbotID].Count)
                            {
                                double[] point = trajectories[xbotID][i];
                                double[] nextPoint = i + 1 < trajectories[xbotID].Count ? trajectories[xbotID][i + 1] : null;
                                double[] currentPos = new double[2];
                                while (Math.Round(currentPos[0], 3) != Math.Round(point[0], 3) || Math.Round(currentPos[1], 3) != Math.Round(point[1], 3))
                                {
                                    XBotStatus status = _xbotCommand.GetXbotStatus(xbotID);
                                    currentPos = status.FeedbackPositionSI;

                                    Console.WriteLine($"Current Position for xbotID {xbotID}: {string.Join(", ", currentPos.Select(p => Math.Round(p, 3)))}");
                                    motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");
                                    if (nextPoint != null)
                                    {
                                        motionsFunctions.LinarMotion(0, xbotID, nextPoint[0], nextPoint[1], "D");
                                    }
                                }
                                Console.WriteLine($"Entry {i + 1} for xbotID {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");
                            }
                        }
                    }

                    break;
                
                case '3':

                    int[] xbotIDs3 = { 1 ,2 };
                    int maxLength3 = trajectories.Values.Max(t => t.Count);
                    double tolerance = 0.005; // Tolerance for position check

                    for (int i = 0; i < maxLength3; i++)
                    {
                        foreach (var xbotID in xbotIDs3)
                        {
                            if (trajectories.ContainsKey(xbotID) && trajectories[xbotID].Count > 0)
                            {
                                double[] currentPos = new double[2];
                                double[] point = trajectories[xbotID].First();
                                double[] nextPoint = trajectories[xbotID].Count > 1 ? trajectories[xbotID][1] : null;

                                Console.WriteLine($"The next point is {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");
                                while (Math.Abs(currentPos[0] - point[0]) > tolerance || Math.Abs(currentPos[1] - point[1]) > tolerance)
                                {
                                    XBotStatus status = _xbotCommand.GetXbotStatus(xbotID);
                                    currentPos = status.FeedbackPositionSI;
                                    Console.WriteLine($"Going to {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");
                                    Console.WriteLine($"Current Position for xbotID {xbotID}: {string.Join(", ", currentPos.Select(p => Math.Round(p, 3)))}");
                                    motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");

                                    // Add a small delay to give the xbot time to move
                                    System.Threading.Thread.Sleep(100);
                                }
                                Console.WriteLine($"Reached point for xbotID {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");

                                if (nextPoint != null)
                                {
                                    trajectories[xbotID].RemoveAt(0);
                                }
                            }
                        }
                    }
                    break;

                case '4':
                    int[] xbotIDs4 = { 1,2 };
                    int maxLength4 = trajectories.Values.Max(t => t.Count);

                    foreach (var xbotID in xbotIDs4)
                    {
                        if (trajectories.ContainsKey(xbotID) && trajectories[xbotID].Count > 0)
                        {
                            // Add the first two motions to the buffer
                            double[] point = trajectories[xbotID][0];
                            double[] nextPoint = trajectories[xbotID].Count > 1 ? trajectories[xbotID][1] : null;
                            Console.WriteLine($"Adding first point to buffer for xbotID {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");
                            motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");
                            if (nextPoint != null)
                            {
                                Console.WriteLine($"Adding second point to buffer for xbotID {xbotID}: {string.Join(", ", nextPoint.Select(p => Math.Round(p, 3)))}");
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
                        }
                    }
                    break;

                case '5':
                    int[] xbotIDs5 = { 1, 2 };
                    int maxLength5 = trajectories.Values.Max(t => t.Count);
                    int bufferThreshold = 2; // Set a threshold for the buffer count

                    foreach (var xbotID in xbotIDs5)
                    {
                        if (trajectories.ContainsKey(xbotID) && trajectories[xbotID].Count > 0)
                        {
                            // Add the first two motions to the buffer
                            double[] point = trajectories[xbotID][0];
                            double[] nextPoint = trajectories[xbotID].Count > 1 ? trajectories[xbotID][1] : null;
                            Console.WriteLine($"Adding first point to buffer for xbotID {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");
                            motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");
                            if (nextPoint != null)
                            {
                                Console.WriteLine($"Adding second point to buffer for xbotID {xbotID}: {string.Join(", ", nextPoint.Select(p => Math.Round(p, 3)))}");
                                motionsFunctions.LinarMotion(0, xbotID, nextPoint[0], nextPoint[1], "D");
                            }
                        }
                    }

                    for (int i = 2; i < maxLength5; i++)
                    {
                        List<Task> tasks = new List<Task>();

                        foreach (var xbotID in xbotIDs5)
                        {
                            if (trajectories.ContainsKey(xbotID) && i < trajectories[xbotID].Count)
                            {
                                tasks.Add(Task.Run(() =>
                                {
                                    while (true)
                                    {
                                        MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                        int bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;

                                        // Print the buffer count
                                        Console.WriteLine($"Buffer count for xbotID {xbotID}: {bufferCount}");

                                        // Add the next motion to the buffer if the buffer count drops below the threshold
                                        if (bufferCount < bufferThreshold)
                                        {
                                            double[] point = trajectories[xbotID][i];
                                            Console.WriteLine($"Adding point {i + 1} to buffer for xbotID {xbotID}: {string.Join(", ", point.Select(p => Math.Round(p, 3)))}");
                                            motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");

                                            _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                            break;
                                        }

                                        // Add a small delay to avoid busy-waiting
                                        System.Threading.Thread.Sleep(50);
                                    }
                                }));
                            }
                        }

                        Task.WaitAll(tasks.ToArray());
                    }
                    break;



                case '6':
                    int[] xbotIDs6 = { 1, 2 };
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
