using PMC_Connection_Node;
using PMCLIB;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using VT2_Aseptic_Production_Demonstrator;


namespace PMC
{
    class PMC_Connection_Node
    {
        private static MotionsFunctions motionsFunctions = new MotionsFunctions();
        private static XBotCommands _xbotCommand = new XBotCommands();
        private connection_handler connectionHandler = new connection_handler();
        private Dictionary<string, Action<string, string>> topicHandlers;
        private MQTTSubscriber mqttSubscriber;
        private MQTTPublisher mqttPublisher;
        string brokerIP = "localhost";
        int port = 1883;
        int[] xbotsID;
        Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();

        private PMC_Connection_Node()
        {
            InitializeTopicHandlers();
            CONNECTIONSTATUS status = connectionHandler.ConnectAndGainMastership();
            Console.WriteLine(status);
            InitializeMqttSubscriber();
            InitializeMqttPublisher();
            PublishTargetPositionsAsync();
            PublishXbotIDAsync();
            PublishPostionsAsync();
        }
        #region MQTT Initialize
        private async void InitializeMqttSubscriber()
        {
            mqttSubscriber = new MQTTSubscriber(brokerIP, port);
            mqttSubscriber.MessageReceived += messageHandler;
            await mqttSubscriber.StartAsync();
            Console.WriteLine("Connected to MQTT broker.");
            // Subscribe to each specific topic in the topicHandlers dictionary
            foreach (var topic in topicHandlers.Keys)
            {
                await mqttSubscriber.SubscribeAsync(topic);
            }
        }
        private void InitializeTopicHandlers()
        {
            topicHandlers = new Dictionary<string, Action<string, string>>
            {
            { "AAU/Fiberstæde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/Trajectory", GetTrajectories },
            { "AAU/Fiberstæde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", HandleStatus }
            };
        }

        private async void InitializeMqttPublisher()
        {
            mqttPublisher = new MQTTPublisher(brokerIP, port);
            await mqttPublisher.StartAsync();
        }
        #endregion

        #region MQTT Publishers
        public async Task PublishTargetPositionsAsync()
        {
            double[] targetPosition1 = { 0.660, 0.400 };
            double[] targetPosition2 = { 0.120, 0.120 };
            double[] targetPosition3 = { 0.600, 0.520 };
            double[] targetPosition4 = { 0.200, 0.500 };

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
                await mqttPublisher.PublishMessageAsync($"AAU/Fiberstæde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{targetPosition.Key}/TargetPosition", message);
                Console.WriteLine($"Published target position for xbot {targetPosition.Key}: {string.Join(", ", targetPosition.Value)}");
            }
        }
        private async void PublishPostionsAsync()
        {
            foreach (var xbot in xbotsID)
            {
                XBotStatus status = _xbotCommand.GetXbotStatus(xbot);
                double[] position = status.FeedbackPositionSI;
                position = position.Select(p => Math.Round(p, 3)).ToArray();
                double[] positionXY = { position[0], position[1] };

                //Console.WriteLine($"StartPostion for xbot {xbot} is ({positionXY[0]:F3}, {positionXY[1]:F3})");
                PublishPositionAsync(xbot, positionXY);
            }
        }

        private async void PublishPostionsINFAsync()
        {
            while (true)
            {
                foreach (var xbot in xbotsID)
                {
                    XBotStatus status = _xbotCommand.GetXbotStatus(xbot);
                    double[] position = status.FeedbackPositionSI;
                    position = position.Select(p => Math.Round(p, 3)).ToArray();
                    double[] positionXY = { position[0], position[1] };

                    //Console.WriteLine($"StartPostion for xbot {xbot} is ({positionXY[0]:F3}, {positionXY[1]:F3})");
                    PublishPositionAsync(xbot, positionXY);
                }
                await Task.Delay(1000);
            }
        }
        public async Task PublishPositionAsync(int xbotId, double[] position)
        {
            var message = JsonSerializer.Serialize(position);
            await mqttPublisher.PublishMessageAsync($"AAU/Fiberstæde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{xbotId}/Position", message);
            //Console.WriteLine($"Published position for xbot {xbotId}: {string.Join(", ", position)}");
        }

        public async Task PublishXbotIDAsync()
        {
            XBotIDs xBotIDs = _xbotCommand.GetXBotIDS();
            xbotsID = xBotIDs.XBotIDsArray;
            Console.WriteLine("XBot IDs: " + string.Join(", ", xbotsID));
            var message = JsonSerializer.Serialize(xbotsID);
            await mqttPublisher.PublishMessageAsync($"AAU/Fiberstæde/Building14/FillingLine/Stations/Acopos6D/Xbots/IDs", message);
        }

        #endregion






        private async void messageHandler(string topic, string message)
        {
            Console.WriteLine($"Received message on topic {topic}: {message}");
            try
            {
                foreach (var handler in topicHandlers)
                {
                    if (TopicMatches(handler.Key, topic))
                    {
                        handler.Value(topic, message);
                        return;
                    }
                }
                Console.WriteLine($"Unhandled topic: {topic}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize message: {message}");
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
        private bool TopicMatches(string pattern, string topic)
        {
            var patternSegments = pattern.Split('/');
            var topicSegments = topic.Split('/');

            if (patternSegments.Length != topicSegments.Length)
                return false;

            for (int i = 0; i < patternSegments.Length; i++)
            {
                if (patternSegments[i] == "+")
                    continue;

                if (patternSegments[i] != topicSegments[i])
                    return false;
            }

            return true;
        }

        private async void HandleStatus(string topic, string message)
        {

            if (message == "ready")
            {
                Console.WriteLine("Xbots is ready");

                RunTrajectory();


            }
            if (message == "home")
            {
                double[] startPostion1 = { 0.060, 0.060 };

                double[] startPostion2 = { 0.600, 0.400 };

                double[] startPostion3 = { 0.360, 0.360 };

                double[] startPostion4 = { 0.200, 0.200 };


                motionsFunctions.LinarMotion(0, 1, startPostion1[0], startPostion1[1], "yx");
                motionsFunctions.LinarMotion(0, 2, startPostion2[0], startPostion2[1], "xy");
                motionsFunctions.LinarMotion(0, 3, startPostion3[0], startPostion3[1], "yx");
                motionsFunctions.LinarMotion(0, 4, startPostion4[0], startPostion4[1], "xy");

            }
            if (message == "SendPostions")
            {
                PublishPostionsAsync();
            }
            if (message == "runPathPlanner")
            {
                /*trajectories = pathfinder.pathPlanRunner(gridGlobal, xBotID_From_To, xbotSize);

                foreach (var trajectory in trajectories)
                {
                    Console.WriteLine($"Trajectory for xbot{trajectory.Key}: {trajectory.Value}");
                    var trajectoryMessage = JsonSerializer.Serialize(trajectory.Value.Select(t => new { x = t[0], y = t[1] }).ToList());
                    await mqttPublisher.PublishMessageAsync($"Acopos6D/xbots/xbot{trajectory.Key}/trajectory", trajectoryMessage);
                    Console.WriteLine($"Published trajectory for xbot {trajectory.Key}: {trajectoryMessage}");
                }

                */
            }
            else
            {
                PrintTrajectories();
            }
        }

        

        public void PrintTrajectories()
        {
            foreach (var kvp in trajectories)
            {
                int xbotId = kvp.Key;
                List<double[]> trajectory = kvp.Value;

                Console.WriteLine($"Trajectory for xbot {xbotId}:");
                foreach (var point in trajectory)
                {
                    Console.WriteLine($"({point[0]:F3}, {point[1]:F3})");
                }
            }
            
        }
        


        


        public async void GetTrajectories(string topic, string message )
        {
            // Find the segment that starts with "xbot" and extract the numeric part
            string[] segments = topic.Split('/');
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("xbot"));
            if (xbotSegment != null)
            {
                int xbotId = int.Parse(xbotSegment.Substring(4)); // Extract the numeric part after "xbot"
                var traj = JsonSerializer.Deserialize<double[][]>(message);
                List<double[]> trajectory = traj.ToList();
                Console.WriteLine($"Trajectory for xbot {xbotId}:");
                foreach (var point in trajectory)
                {
                    Console.WriteLine($"({point[0]:F3}, {point[1]:F3})");
                }
                trajectories[xbotId] = trajectory;
            }
        }

        /*
        public async void RunTrajectory()
        {
            int maxLength = trajectories.Values.Max(t => t.Count);

            List<Task> tasks = new List<Task>();

            foreach (var xbotID in trajectories.Keys)
            {
                if (trajectories.ContainsKey(xbotID) && trajectories[xbotID].Count > 0)
                {
                    tasks.Add(Task.Run(() =>
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


                        }
                        lock (trajectories)
                        {
                            trajectories.Remove(xbotID);
                        }

                    }));
                }
            }

            //Task.WaitAll(tasks.ToArray());
        }
        */

        public async void RunTrajectory()
        {
            int maxLength = trajectories.Values.Max(t => t.Count);

            List<Task> tasks = new List<Task>();

            foreach (var xbotID in trajectories.Keys)
            {
                if (trajectories.ContainsKey(xbotID) && trajectories[xbotID].Count > 0)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        // Add the first two motions to the buffer
                        double[] point = trajectories[xbotID][0];
                        double[] nextPoint = trajectories[xbotID].Count > 1 ? trajectories[xbotID][1] : null;
                        motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");
                        if (nextPoint != null)
                        {
                            motionsFunctions.LinarMotion(0, xbotID, nextPoint[0], nextPoint[1], "D");
                        }

                        for (int i = 1; i < trajectories[xbotID].Count - 1; i++)
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

                            // Calculate direction vector
                            double[] currentPoint = trajectories[xbotID][i];
                            double[] nextPointDirection = trajectories[xbotID][i + 1];
                            double[] directionVector = { nextPoint[0] - currentPoint[0], nextPoint[1] - currentPoint[1] };

                            // Check if the direction is consistent
                            if (i < trajectories[xbotID].Count - 2)
                            {
                                double[] nextNextPoint = trajectories[xbotID][i + 2];
                                double[] nextDirectionVector = { nextNextPoint[0] - nextPointDirection[0], nextNextPoint[1] - nextPointDirection[1] };
                                if (Math.Sign(directionVector[0]) != Math.Sign(nextDirectionVector[0]) || Math.Sign(directionVector[1]) != Math.Sign(nextDirectionVector[1]))
                                {
                                    Console.WriteLine($"Direction change detected at point {i + 1} for xbotID {xbotID}");
                                    motionsFunctions.LinarMotion(0, xbotID, nextPointDirection[0], nextPointDirection[1], "D");
                                }
                            }

                            // Add the next motion to the buffer
                            Console.WriteLine($"Adding point {i + 1} to buffer for xbotID {xbotID}: {string.Join(", ", nextPoint.Select(p => Math.Round(p, 3)))}");
                            
                        }
                        lock (trajectories)
                        {
                            trajectories.Remove(xbotID);
                        }
                    }));
                }
            }

            //Task.WaitAll(tasks.ToArray());
        }



        public static async Task Main(string[] args) // Change return type to Task
        {
            
            PMC_Connection_Node client = new PMC_Connection_Node();


            Thread thread1 = new Thread(new ThreadStart(client.PublishPostionsINFAsync));

            
            thread1.Start();
            await Task.Delay(-1); // Keep the main thread alive
        }
    }
    

    
}