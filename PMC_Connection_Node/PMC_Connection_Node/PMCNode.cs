using MQTTnet;
using PMC_Connection_Node;
using PMCLIB;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
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
        //string brokerIP = "172.20.66.135";
        string brokerIP = "localhost";
        int port = 1883;
        int[] xbotsID;
        Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();
        private CancellationTokenSource runTrajectoryCancellationTokenSource;

        private PMC_Connection_Node()
        {
            InitializeTopicHandlers();
            InitializeMqttSubscriber();
            InitializeMqttPublisher();
            
            CONNECTIONSTATUS status = connectionHandler.ConnectAndGainMastership();
            Console.WriteLine(status);
            
            
            PublishXbotIDAsync();
            PublishTargetPositionsAsync();

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
            { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/Trajectory", GetTrajectories },
            { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", HandleStatus },
            { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Stop", HandleStop }
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
            double[] targetPosition1 = { 0.6, 0.78 };
            double[] targetPosition2 = { 0.120, 0.9 };
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
                await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{targetPosition.Key}/TargetPosition", message);
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
            await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{xbotId}/Position", message);
            //Console.WriteLine($"Published position for xbot {xbotId}: {string.Join(", ", position)}");
        }

        public async Task PublishXbotIDAsync()
        {
            XBotIDs xBotIDs = _xbotCommand.GetXBotIDS();
            xbotsID = xBotIDs.XBotIDsArray;
            Console.WriteLine("XBot IDs: " + string.Join(", ", xbotsID));
            var message = JsonSerializer.Serialize(xbotsID);
            await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/IDs", message, true);
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
                int[] xbot = { 1, 2 };
                

                double[] xpostions = { 0.12, 0.6 };

                double[] ypostions = { 0.78, 0.9 };

                _xbotCommand.AutoDrivingMotionSI(2, ASYNCOPTIONS.MOVEALL, xbot, xpostions, ypostions);
                
                

            }
            if (message == "SendPostions")
            {
                PublishPostionsAsync();
            }
            if (message == "runPathPlanner")
            {
                
            }
            else
            {
                PrintTrajectories();
            }
        }

        private void HandleStop(string topic, string message)
        {
            Console.WriteLine("Stop message received. Stopping RunTrajectory...");
            runTrajectoryCancellationTokenSource?.Cancel();
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
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot"));
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

        

        public async void RunTrajectory()
        {
            runTrajectoryCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = runTrajectoryCancellationTokenSource.Token;

            List<Task> tasks = new List<Task>();

            foreach (var xbotID in trajectories.Keys)
            {
                if (trajectories.ContainsKey(xbotID) && trajectories[xbotID].Count > 0)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            // Add the first motion to the buffer
                            double[] point = trajectories[xbotID][0];
                            motionsFunctions.LinarMotion(0, xbotID, point[0], point[1], "D");

                            for (int i = 1; i < trajectories[xbotID].Count; i++)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Console.WriteLine($"RunTrajectory canceled for xbotID {xbotID}");
                                    return; // Exit the loop if cancellation is requested
                                }

                                MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                int bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;

                                XBotStatus status = _xbotCommand.GetXbotStatus(xbotID);
                                double[] position = status.FeedbackPositionSI;
                                position = position.Select(p => Math.Round(p, 3)).ToArray();

                                await PublishPositionAsync(xbotID, position);

                                // Wait until there is only 1 motion left in the buffer
                                while (bufferCount > 1)
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        Console.WriteLine($"RunTrajectory canceled for xbotID {xbotID}");
                                        return; // Exit the loop if cancellation is requested
                                    }

                                    BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                    bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;
                                }

                                // Handle trajectory points (existing logic)
                                double[] currentPoint = trajectories[xbotID][i - 1];
                                double[] nextPoint = trajectories[xbotID][i];
                                double deltaX = nextPoint[0] - currentPoint[0];
                                double deltaY = nextPoint[1] - currentPoint[1];
                                double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                                double baseVelocity = 0.1;
                                double adjustedVelocity = (distance > 1.0) ? baseVelocity * Math.Sqrt(2) : baseVelocity;

                                if (i < trajectories[xbotID].Count - 1)
                                {
                                    double[] nextNextPoint = trajectories[xbotID][i + 1];
                                    double[] nextDirectionVector = { nextNextPoint[0] - nextPoint[0], nextNextPoint[1] - nextPoint[1] };

                                    if (Math.Sign(deltaX) != Math.Sign(nextDirectionVector[0]) || Math.Sign(deltaY) != Math.Sign(nextDirectionVector[1]))
                                    {
                                        Console.WriteLine($"Direction change detected at point {i + 1} for xbotID {xbotID}");
                                        _xbotCommand.LinearMotionSI(0, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.DIRECT, nextPoint[0], nextPoint[1], 0, adjustedVelocity, 0.2);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Maintaining direction for xbotID {xbotID} at point {i}");
                                        _xbotCommand.LinearMotionSI(0, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.DIRECT, nextPoint[0], nextPoint[1], adjustedVelocity, adjustedVelocity, 0.2);
                                    }
                                }
                                else
                                {
                                    // Handle the last point explicitly
                                    Console.WriteLine($"Adding last point to buffer for xbotID {xbotID}: {string.Join(", ", nextPoint.Select(p => Math.Round(p, 3)))}");
                                    _xbotCommand.LinearMotionSI(0, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.DIRECT, nextPoint[0], nextPoint[1], 0, adjustedVelocity, 0.2);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in RunTrajectory for xbotID {xbotID}: {ex.Message}");
                        }
                        finally
                        {
                            lock (trajectories)
                            {
                                trajectories.Remove(xbotID);
                            }
                        }
                    }));
                }
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("RunTrajectory operation was canceled.");
            }
            finally
            {
                runTrajectoryCancellationTokenSource?.Dispose();
                runTrajectoryCancellationTokenSource = new CancellationTokenSource(); // Replace null assignment
            }
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