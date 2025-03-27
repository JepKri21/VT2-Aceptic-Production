using PMCLIB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{




    class ListBasedMotionStructure : MotionsFunctions
    {
        private static MotionsFunctions motionsFunctions = new MotionsFunctions();
        private static XBotCommands _xbotCommand = new XBotCommands();
        int selector = 3;
        private static Pathfinding.AStar pathfinding = new Pathfinding.AStar();
        private MQTTSubscriber mqttSubscriber;
        private MQTTPublisher mqttPublisher;
        string brokerIP = "localhost";
        int port = 1883;


        Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();

        Dictionary<int, double[]> lastKnownTargetPositions = new Dictionary<int, double[]>();

        public int setSelectorOne()
        {
            return selector;
        }


        #region MqttConnections

        public ListBasedMotionStructure()
        {
            //MqttConnections();
            InitializeMqttSubscriber();
            InitializeMqttPublisher();

        }

        private async void InitializeMqttSubscriber()
        {
            mqttSubscriber = new MQTTSubscriber(brokerIP, port, "Acopos6D/#");
            mqttSubscriber.MessageReceived += TargetPostionChange;
            await mqttSubscriber.StartAsync();
        }
        private async void InitializeMqttPublisher()
        {
            mqttPublisher = new MQTTPublisher(brokerIP, port);
            await mqttPublisher.StartAsync();
        }
        private async void TargetPostionChange(string topic, string message)
        {
            Console.WriteLine($"Received message on topic {topic}: {message}");
            try
            {
                var targetPosition = JsonSerializer.Deserialize<double[]>(message);
                // Split the topic into segments
                string[] segments = topic.Split('/');

                // Find the segment that starts with "xbot" and extract the numeric part
                string xbotSegment = segments.LastOrDefault(s => s.StartsWith("xbot"));
                if (xbotSegment != null)
                {
                    int xbotId = int.Parse(xbotSegment.Substring(4)); // Extract the numeric part after "xbot"

                    // Check if the new target position is different from the last known target position
                    if (lastKnownTargetPositions.ContainsKey(xbotId) && lastKnownTargetPositions[xbotId].SequenceEqual(targetPosition))
                    {
                        Console.WriteLine($"Target position for xbot {xbotId} has not changed.");
                        return;
                    }

                    // Update the last known target position
                    lastKnownTargetPositions[xbotId] = targetPosition;

                    // Update the target positions for the corresponding xbot
                    if (trajectories.ContainsKey(xbotId))
                    {
                        trajectories[xbotId].Add(targetPosition);
                    }
                    else
                    {
                        trajectories[xbotId] = new List<double[]> { targetPosition };
                    }

                    Console.WriteLine($"Received target position for xbot {xbotId}: {string.Join(", ", targetPosition)}");

                    // Generate a new trajectory using the retrieved target positions
                    if (trajectories.ContainsKey(xbotId))
                    {
                        var targetPositions = trajectories[xbotId];
                        if (targetPositions.Count > 0)
                        {
                            double[] startPosition = { 0.060, 0.060 }; // Example start position
                            double[] newTargetPosition = targetPositions.Last(); // Use the last received target position

                            TrajectoryGenerator traj = new TrajectoryGenerator(xbotId, startPosition, newTargetPosition, 20, "XY");
                            Console.WriteLine($"Trajectory for xbot {xbotId}:");
                            traj.PrintTrajectory();
                            trajectories[traj.trajectory.First().Item1] = traj.trajectory.Select(t => t.Item2).ToList();

                            // Serialize the trajectory and publish it to the topic
                            var trajectoryMessage = JsonSerializer.Serialize(traj.trajectory.Select(t => t.Item2).ToList());
                            await mqttPublisher.PublishMessageAsync($"Acopos6D/xbots/xbot{xbotId}/trajectory", trajectoryMessage);
                            Console.WriteLine($"Published trajectory for xbot {xbotId}: {trajectoryMessage}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No xbot topic segment found in the Broker.");
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize message: {message}");
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
        

        public async Task PublishTargetPositionsAsync()
        {
            double[] targetPosition1 = { 0.660, 0.400 };
            double[] targetPosition2 = { 0.120, 0.120 };
            double[] targetPosition3 = { 0.600, 0.520 };
            double[] targetPosition4 = { 0.200, 0.600 };

            var targetPositions = new Dictionary<int, double[]>
            {
                { 1, targetPosition1 },
                { 2, targetPosition2 },
                { 3, targetPosition3 },
                { 4, targetPosition4 }
            };

            foreach (var targetPosition in targetPositions)
            {
                var message = JsonSerializer.Serialize(targetPosition.Value);
                await mqttPublisher.PublishMessageAsync($"Acopos6D/xbots/xbot{targetPosition.Key}/targetPosition", message);
                Console.WriteLine($"Published target position for xbot {targetPosition.Key}: {string.Join(", ", targetPosition.Value)}");
            }
        }

        public async Task PublishPositionAsync(int xbotId, double[] position)
        {
            var message = JsonSerializer.Serialize(position);
            await mqttPublisher.PublishMessageAsync($"Acopos6D/xbots/xbot{xbotId}/position", message);
            Console.WriteLine($"Published position for xbot {xbotId}: {string.Join(", ", position)}");
        }
        #endregion


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
                // Generate trajectories form the start postions to the target postions

                /*
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
                
                */
                case '1':
                    // Publish the target positions
                    PublishTargetPositionsAsync().Wait();
                    double[] startPostion1 = { 0.060, 0.060 };
                    double[] targetPostion1 = { 0.660, 0.400 };
                    double[] startPostion2 = { 0.600, 0.400 };
                    double[] targetPostion2 = { 0.120, 0.120 };
                    double[] startPostion3 = { 0.360, 0.360 };
                    double[] targetPostion3 = { 0.600, 0.520 };
                    double[] startPostion4 = { 0.200, 0.200 };
                    double[] targetPostion4 = { 0.200, 0.600 };

                    motionsFunctions.LinarMotion(0, 1, startPostion1[0], startPostion1[1], "xy");
                    motionsFunctions.LinarMotion(0, 2, startPostion2[0], startPostion2[1], "xy");
                    motionsFunctions.LinarMotion(0, 3, startPostion3[0], startPostion3[1], "yx");
                    motionsFunctions.LinarMotion(0, 4, startPostion4[0], startPostion4[1], "xy");
                    break;


                case '2':
                    // Wait for a short period to allow messages to be received
                    Task.Delay(2000).Wait();

                    // Generate trajectories using the retrieved target positions
                    foreach (var xbotID in xbotIDs)
                    {
                        if (trajectories.ContainsKey(xbotID))
                        {
                            var targetPositions = trajectories[xbotID];
                            if (targetPositions.Count > 0)
                            {
                                double[] startPosition = { 0.060, 0.060 }; // Example start position
                                double[] targetPosition = targetPositions.Last(); // Use the last received target position

                                TrajectoryGenerator traj = new TrajectoryGenerator(xbotID, startPosition, targetPosition, 20, "XY");
                                Console.WriteLine($"Trajectory for xbot {xbotID}:");
                                traj.PrintTrajectory();
                                trajectories[traj.trajectory.First().Item1] = traj.trajectory.Select(t => t.Item2).ToList();
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No target positions received for xbot {xbotID}.");
                        }
                    }
                    break;

                case '6':
                    int[] xbotIDs6 = { 1, 2, 3, 4 };
                    int maxLength6 = trajectories.Values.Max(t => t.Count);

                    List<Task> tasks6 = new List<Task>();

                    foreach (var xbotID in xbotIDs6)s
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

                                    
                                    XBotStatus status = _xbotCommand.GetXbotStatus(xbotID);
                                    double[] position = status.FeedbackPositionSI;
                                    position = position.Select(p => Math.Round(p, 3)).ToArray();
                                    

                                    PublishPositionAsync(xbotID, position);

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
