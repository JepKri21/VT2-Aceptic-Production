﻿using MQTTnet;
using PMC_Connection_Node;
using PMCLIB;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        private Dictionary<int, double[]> targetPositions = new();
        private Dictionary<int, double[]> positions = new();
        private Dictionary<int, int> xbotStateStationID = new();

        private Dictionary<int, bool> RotationLock = new Dictionary<int, bool>
        {
            {1, false },
            {2, false },
            {3, false },
            {4, false },
            {5, false },
            {6, false },
            {7, false },
            {8, false },
        };
        
        private Dictionary<int, double[]> Station = new(); //Key is the StationdId, value is the position
        private Dictionary<int, string> CommandUuid = new();
        string brokerIP = "172.20.66.135";
        //string brokerIP = "localhost";
        int port = 1883;
        int[] xbotsID;
        Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();
        private CancellationTokenSource runTrajectoryCancellationTokenSource;
        private Dictionary<int, Task> runningTasks = new(); // Track running tasks for each xbotID
        private Dictionary<int, CancellationTokenSource> taskCancellationTokens = new(); // Track cancellation tokens for each xbotID
        private Dictionary<int, CancellationTokenSource> rotationCancellationTokens = new(); // Manage rotation cancellation tokens per xbot
        string UNSPrefix = "AAU/Fibigerstræde/Building14/FillingLine/Planar/";
        
        private Dictionary<int, string> states = new Dictionary<int, string>
        {
            { 0, "Undetected" },
            { 1, "Discovering" },
            { 2, "Execute" },            
            { 3, "Idle" },
            { 4, "Stopped" },
            { 5, "Executing" },
            { 6, "Executing" },
            { 7, "Stopping" },
            { 8, "Held" },
            { 9, "Held" },
            { 10, "Stopped" },
            { 14, "Error" }
        };

        private Dictionary<int, double[]> lastPublishedPositions = new();
        private Dictionary<int, string> lastPublishedStates = new();

        private const double OrientationTolerance = 0.01;

        private class TargetPositionMessage
        {
            public string CommandUuid { get; set; } = string.Empty;
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double Rx { get; set; }
            public double Ry { get; set; }
            public double Rz { get; set; }
            public string TimeStamp { get; set; } = string.Empty;
        }

        private class StationMessage
        {
            public List<Station> Stations { get; set; } = new();

            public class Station
            {
                public string Name { get; set; } = string.Empty;
                public int StationId { get; set; }
                public double[] ApproachPosition { get; set; } = Array.Empty<double>();
                public double[] ProcessPosition { get; set; } = Array.Empty<double>();
            }
        }

        private class CommandMessage
        {
            public string CommandUuid { get; set; } = string.Empty;
            public string Command { get; set; } = string.Empty;
            public string TimeStamp { get; set; } = string.Empty;
        }

        private class TrajectoryMessage
        {
            public string CommandUuid { get; set; } = string.Empty;

            

            public string TimeStamp { get; set; } = string.Empty;

            [JsonPropertyName("Trajectory")] // Map the JSON property "Trajectory" to this property
            public List<double[]> TrajectoryPoints { get; set; } = new List<double[]>();

        }

        private PMC_Connection_Node()
        {
            InitializeTopicHandlers();
            InitializeMqttSubscriber();
            InitializeMqttPublisher();

            CONNECTIONSTATUS status = connectionHandler.ConnectAndGainMastership();
            Console.WriteLine(status);

           


            PublishXbotIDAsync();
            //PublishTargetPositionsAsync();
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
            { UNSPrefix + "+/DATA/Trajectory", GetTrajectories },
            { UNSPrefix + "+/DATA/TargetPosition", getTargetPosition },
            { UNSPrefix + "+/CMD/SubCMD", HandlerSubCMD },
            { UNSPrefix + "PathPlan/CMD", HandleStatus },
            { "AAU/Fibigerstræde/Building14/FillingLine/Configuration/DATA/Planar/Staions", HandleStations}

            };
        }

        private async void InitializeMqttPublisher()
        {
            mqttPublisher = new MQTTPublisher(brokerIP, port);
            await mqttPublisher.StartAsync();
        }
        #endregion

        #region MQTT Publishers
        /*
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
        */
        

        



        public async void PublishPostionsINFAsync()
        {
            while (true)
            {
                foreach (var xbot in xbotsID)
                {
                    // Get the xbot status and position
                    XBotStatus status = _xbotCommand.GetXbotStatus(xbot);
                    double[] position = status.FeedbackPositionSI;
                    int xbotStateID = (int)status.XBOTState;
                    string xbotState = states.ContainsKey(xbotStateID) ? states[xbotStateID] : "Unknown";

                    // Round the position values
                    position = position.Select(p => Math.Round(p, 3)).ToArray();

                    // Retrieve the CommandUuid for the xbot
                    string commandUuid = CommandUuid.ContainsKey(xbot) ? CommandUuid[xbot] : null;

                    // Update the positions dictionary
                    positions[xbot] = position;

                    // Convert the last three entries of the position from radians to degrees
                    for (int i = position.Length - 3; i < position.Length; i++)
                    {
                        position[i] = Math.Round(position[i] * (180 / Math.PI), 3);
                    }

                    // Get the current timestamp
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    // Check if position has changed
                    if (!lastPublishedPositions.ContainsKey(xbot) ||
                        !position.SequenceEqual(lastPublishedPositions[xbot]))
                    {
                        lastPublishedPositions[xbot] = position;

                        // Create a message object with position and timestamp
                        var positionMessage = new
                        {
                            CommandUuid = commandUuid,
                            X = position[0],
                            Y = position[1],
                            Z = position[2],
                            Rx = position[3],
                            Ry = position[4],
                            Rz = position[5],
                            TimeStamp = timestamp
                        };

                        // Serialize and publish the position message
                        string serializedPositionMessage = JsonSerializer.Serialize(positionMessage);
                        await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/DATA/Position", serializedPositionMessage, retain: true);
                    }
                    // Find the key in Station where the value matches the current position
                    var matchingStation = Station.FirstOrDefault(station =>
                        station.Value.Take(2).Zip(new double[] { position[0], position[1] },
                                                 (stationValue, targetValue) => Math.Abs(stationValue - targetValue) < 0.001).All(isClose => isClose));

                    if (matchingStation.Key != 0) // If the current position matches a station position
                    {
                        xbotStateStationID[xbot] = matchingStation.Key;
                    }
                    else // If the current position does not match any station position
                    {
                        xbotStateStationID[xbot] = 0;
                    }
                    // Check if state has changed
                    if (!lastPublishedStates.ContainsKey(xbot) ||
                        lastPublishedStates[xbot] != xbotState)
                    {
                        lastPublishedStates[xbot] = xbotState;

                        if (!xbotStateStationID.ContainsKey(xbot))
                        {
                            xbotStateStationID[xbot] = 0;
                        }

                        // Create a message object with state and timestamp
                        var stateMessage = new
                        {
                            CommandUuid = commandUuid,
                            State = xbotState,
                            StationId = xbotStateStationID[xbot],
                            TimeStamp = timestamp
                        };

                        // Serialize and publish the state message
                        string serializedStateMessage = JsonSerializer.Serialize(stateMessage);
                        await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/DATA/State", serializedStateMessage, retain: true);
                    }
                }
                await Task.Delay(500);
            }
        }
        
        public async Task PublishXbotIDAsync()
        {
            XBotIDs xBotIDs = _xbotCommand.GetXBotIDS();
            xbotsID = xBotIDs.XBotIDsArray;
            Console.WriteLine("XBot IDs: " + string.Join(", ", xbotsID));
            var message = JsonSerializer.Serialize(new { XBotIDs = xbotsID, TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
            await mqttPublisher.PublishMessageAsync(UNSPrefix + $"DATA/IDs", message, true);
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

        private  void HandleCMD(string topic, string message)
        {

            
            string[] segments = topic.Split('/');
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot", StringComparison.OrdinalIgnoreCase))
                                 ?? throw new InvalidOperationException("xbot segment not found");
            int xbotID = int.Parse(xbotSegment.Substring(4));

            //var commandMessage = JsonSerializer.Deserialize<CommandMessage>(message);
            /*
            if (commandMessage == null)
            {
                throw new InvalidOperationException("Command message is null or invalid.");
            }
            */
            //CommandUuid[xbotID] = commandMessage.CommandUuid;
            //Console.WriteLine($"{commandMessage.CommandUuid}");
        }


        private void HandleStations(string topic, string message)
        {
            

            var stationMessage = JsonSerializer.Deserialize<StationMessage>(message);
            if (stationMessage == null)
            {
                Console.WriteLine("[Error] Station message is null or invalid.");
                throw new InvalidOperationException("Station message is null or invalid.");
            }

            Console.WriteLine($"[Debug] Deserialized StationMessage contains {stationMessage.Stations.Count} stations.");

            foreach (var station in stationMessage.Stations)
            {
                
                Station[station.StationId] = station.ProcessPosition;
            }

            Console.WriteLine("[Debug] Station dictionary updated successfully.");
        }

        private async void HandleStatus(string topic, string message)
        {

            if (message == "ready")
            {
                Console.WriteLine("Xbots is ready");

                ExecuteTrajectory();


            }
            if (message == "home")
            {
                int[] xbot = { 1, 2, 3, 4, 5 };


                double[] xpostions = { 0.12, 0.6, 0.6, 0.36, 0.84};

                double[] ypostions = { 0.6, 0.9,  0.12, 0.12, 0.84};

                _xbotCommand.AutoDrivingMotionSI(5, ASYNCOPTIONS.MOVEALL, xbot, xpostions, ypostions);



            }            
            if (message == "Stop")
            {
                //Console.Clear();
                Console.WriteLine("[Debug] Xbots ordred to stop");
                StopRunTrajectory();
            }
            else
            {
                //PrintTrajectories();
            }
        }

        private void StopRunTrajectory()
        {
            Console.WriteLine("[StopDebug] Stop message received. Stopping RunTrajectory...");
            foreach (var xbot in xbotsID)
            {
                // Cancel the running task for the xbot if it exists
                if (taskCancellationTokens.ContainsKey(xbot))
                {
                    try
                    {
                        taskCancellationTokens[xbot].Cancel();
                        Console.WriteLine($"[StopDebug] Cancellation token triggered for xbot {xbot}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StopDebug] Error cancelling task for xbot {xbot}: {ex.Message}");
                    }
                    finally
                    {
                        taskCancellationTokens[xbot].Dispose();
                        taskCancellationTokens.Remove(xbot);
                        Console.WriteLine($"[StopDebug] Disposed and removed cancellation token for xbot {xbot}");
                    }
                }

                lock (trajectories)
                {
                    if (trajectories.ContainsKey(xbot))
                    {
                        trajectories.Remove(xbot);
                        Console.WriteLine($"[StopDebug] Removed trajectory for xbot {xbot}");
                    }
                }

                // Clear motion buffer and stop motion for each xbot
                if (RotationLock[xbot] == true)
                {
                    try
                    {
                        MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbot, MOTIONBUFFEROPTIONS.CLEARBUFFER);
                        Console.WriteLine($"[StopDebug] Motion buffer cleared for xbot {xbot}, Status: {BufferStatus}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StopDebug] Error clearing motion buffer for xbot {xbot}: {ex.Message}");
                    }

                    try
                    {
                        _xbotCommand.StopMotion(xbot);
                        Console.WriteLine($"[StopDebug] Motion stopped for xbot {xbot}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StopDebug] Error stopping motion for xbot {xbot}: {ex.Message}");
                    }
                }
                else if (!RotationLock.ContainsKey(xbot))
                {
                    
                }
                
                
            }
        }

        private void HandlerSubCMD(string topic, string message)
        {
            string[] segments = topic.Split('/');
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot", StringComparison.OrdinalIgnoreCase))
                                 ?? throw new InvalidOperationException("xbot segment not found");
            int xbotID = int.Parse(xbotSegment.Substring(4));

            if (message == "Rotate")
            {
                RotationLock[xbotID] = true;
                Rotation(xbotID);
            }
            if (message == "Land")
            {
                Land(xbotID);
            }
            if (message == "Levitate")
            {
                Levitate(xbotID);
            }
        }


        private void Land(int xbotID)
        {
            _xbotCommand.LevitationCommand(xbotID, LEVITATEOPTIONS.LAND);
        }
            
        private void Levitate(int xbotID)
        {
            _xbotCommand.LevitationCommand(xbotID, LEVITATEOPTIONS.LEVITATE);
        }

        private double[] FindClosestCenter(double[] currentPosition, List<double[]> centers)
        {
            if (currentPosition == null || currentPosition.Length < 2)
            {
                throw new ArgumentException("Current position must contain at least 2 values: x and y.");
            }

            double[] closestCenter = centers[0];
            double minDistance = double.MaxValue;

            foreach (var center in centers)
            {
                double distance = Math.Sqrt(Math.Pow(center[0] - currentPosition[0], 2) + Math.Pow(center[1] - currentPosition[1], 2));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestCenter = center;
                }
            }

            return closestCenter;
        }

       

        // Utility method to count consecutive identical points in a trajectory (exact match, both X and Y)
        private int CountConsecutiveIdenticalTrajectoryPoints(List<double[]> trajectory, double tolerance = 0.0)
        {
            if (trajectory == null || trajectory.Count < 2)
                return 0;

            int count = 0;
            for (int i = 1; i < trajectory.Count; i++)
            {
                bool same =
                    Math.Abs(trajectory[i][0] - trajectory[i - 1][0]) <= tolerance &&
                    Math.Abs(trajectory[i][1] - trajectory[i - 1][1]) <= tolerance;
                if (same)
                    count++;
            }
            return count;
        }

        public void PrintTrajectories()
        {
            // Debugging: Log the content of the trajectories dictionary
            Console.WriteLine("Debug: Current state of trajectories dictionary:");
            foreach (var kvp in trajectories)
            {
                Console.WriteLine($"xbotId: {kvp.Key}, Trajectory Points Count: {kvp.Value.Count}");
            }

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

        // Add this utility method to filter trajectory points to only move "forward" in Y (or X) direction
        private List<double[]> FilterForwardTrajectory(List<double[]> trajectory, int axis = 1)
        {
            if (trajectory == null || trajectory.Count < 2)
                return trajectory;

            List<double[]> filtered = new List<double[]>();
            filtered.Add(trajectory[0]);
            double lastValue = trajectory[0][axis];

            for (int i = 1; i < trajectory.Count; i++)
            {
                double currentValue = trajectory[i][axis];
                // Only add points that are strictly forward (greater than last value)
                if (currentValue > lastValue)
                {
                    filtered.Add(trajectory[i]);
                    lastValue = currentValue;
                }
                // Optionally, allow strictly decreasing by changing the comparison
                // if (currentValue < lastValue) { ... }
            }
            return filtered;
        }

        public async void GetTrajectories(string topic, string message)
        {
            try 
            {
                // Find the segment that starts with "xbot" and extract the numeric part
                string[] segments = topic.Split('/');
                string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot"));
                if (xbotSegment != null)
                {
                    int xbotId = int.Parse(xbotSegment.Substring(4)); // Extract the numeric part after "xbot"
                    //CountTrajectories[xbotId]++;
                    // Deserialize the message into a TrajectoryMessage object
                    var trajectoryMessage = JsonSerializer.Deserialize<TrajectoryMessage>(message);
                    if (trajectoryMessage == null)
                    {
                        Console.WriteLine($"Invalid trajectory message received for xbot {xbotId}");
                        return;
                    }

                    // Debugging: Log the deserialized trajectoryMessage object
                    Console.WriteLine($"Debug: Deserialized trajectoryMessage for xbot {xbotId}:");
                    //Console.WriteLine($"CommandUuid: {trajectoryMessage.CommandUuid}");
                    //Console.WriteLine($"TimeStamp: {trajectoryMessage.TimeStamp}");
                    /*Console.WriteLine($"TrajectoryPoints Count: {trajectoryMessage.TrajectoryPoints?.Count ?? 0}");
                    if (trajectoryMessage.TrajectoryPoints != null)
                    {
                        foreach (var point in trajectoryMessage.TrajectoryPoints)
                        {
                            Console.WriteLine($"Point: ({point[0]}, {point[1]})");
                        }
                    }
                    */
                    // Update the trajectories dictionary
                    trajectories[xbotId] = trajectoryMessage.TrajectoryPoints ?? new List<double[]>();

                    // Debugging: Log the updated state of the trajectories dictionary
                    Console.WriteLine("Debug: Updated state of the trajectories dictionary:");
                    foreach (var kvp in trajectories)
                    {
                        Console.WriteLine($"xbotId: {kvp.Key}, Trajectory Points Count: {kvp.Value.Count}");
                    }

                    // Print the received trajectory
                    PrintTrajectories();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing trajectory message: {ex.Message}");
            }
        }

        public void getTargetPosition(string topic, string message)
        {
            //Console.WriteLine($"Received message on topic {topic}: {message}");

            try
            {
                var targetPositionMessage = JsonSerializer.Deserialize<TargetPositionMessage>(message) ?? throw new InvalidOperationException("Position is null");
                if (targetPositionMessage == null)
                {
                    throw new InvalidOperationException("Target Position is null");
                }
                
                double[] targetPosition = new double[]
                {
                    targetPositionMessage.X,
                    targetPositionMessage.Y,
                    targetPositionMessage.Z,
                    targetPositionMessage.Rx,
                    targetPositionMessage.Ry,
                    targetPositionMessage.Rz
                };
                string[] segments = topic.Split('/');
                string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot", StringComparison.OrdinalIgnoreCase))
                                     ?? throw new InvalidOperationException("xbot segment not found");
                int xbotId = int.Parse(xbotSegment.Substring(4));

                //Console.WriteLine($"Updating position for xbotID {xbotId}: {string.Join(", ", targetPosition)}");
                CommandUuid[xbotId] = targetPositionMessage.CommandUuid;
                targetPositions[xbotId] = targetPosition;



            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing position message: {ex.Message}");
            }
        }


        /*
        public async void GetTrajectories(string topic, string message)
        {
            try 
            {
                // Find the segment that starts with "xbot" and extract the numeric part
                string[] segments = topic.Split('/');
                string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot"));
                if (xbotSegment != null)
                {
                    int xbotId = int.Parse(xbotSegment.Substring(4)); // Extract the numeric part after "xbot"

                    // Deserialize the message into a TrajectoryMessage object
                    var trajectoryMessage = JsonSerializer.Deserialize<TrajectoryMessage>(message);
                    if (trajectoryMessage == null || trajectoryMessage.TrajectoryPoints == null)
                    {
                        Console.WriteLine($"Invalid trajectory message received for xbot {xbotId}");
                        return;
                    }

                    // Log the CommandUuid and TimeStamp for debugging
                    Console.WriteLine($"Received trajectory for xbot {xbotId} with CommandUuid: {trajectoryMessage.CommandUuid} and TimeStamp: {trajectoryMessage.TimeStamp}");

                    // Update the trajectories dictionary
                    trajectories[xbotId] = trajectoryMessage.TrajectoryPoints;

                    // Print the received trajectory
                    PrintTrajectories();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing trajectory message: {ex.Message}");
            }
        }
        */

        public async void ExecuteTrajectory()
        {
            Console.WriteLine("Executing trajectory...");
            runTrajectoryCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = runTrajectoryCancellationTokenSource.Token;

            List<Task> tasks = new List<Task>();

            foreach (var xbotID in trajectories.Keys)
            {
                // Ensure only one task per xbotID
                if (runningTasks.ContainsKey(xbotID) && runningTasks[xbotID] != null && !runningTasks[xbotID].IsCompleted)
                {
                    Console.WriteLine($"[Info] Task for xbotID {xbotID} is already running. Skipping new task.");
                    continue;
                }
                //&& trajectories[xbotID].Count > 0
                if (trajectories.ContainsKey(xbotID) )
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            
                            if (!trajectories.ContainsKey(xbotID))
                            {
                                Console.WriteLine($"[Error] Key {xbotID} not found in trajectories dictionary.");
                                return;
                            }
                            if (trajectories[xbotID].Count < 2)
                            {
                                Console.WriteLine($"[Warning] Trajectory for xbot {xbotID} has less than 2 points. Skipping execution.");
                                return;
                            }
                            xbotStateStationID[xbotID] = 0;
                            int identicalCount = CountConsecutiveIdenticalTrajectoryPoints(trajectories[xbotID], 0.0);
                            Console.WriteLine($"Identical points in trajectory for xbot {xbotID}: {identicalCount}");

                            int skipAhead = 0;
                            // Fix: Loop to Count-1 so i+1 is always valid
                            for (int i = 0; i < trajectories[xbotID].Count - 1; i += skipAhead == 0 ? 1 : skipAhead)
                            {
                                skipAhead = 0; // reset
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Console.WriteLine($"RunTrajectory canceled for xbotID {xbotID}");
                                    return;
                                }

                                MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                int bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;

                                while (bufferCount > 1)
                                {
                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        Console.WriteLine($"RunTrajectory canceled for xbotID {xbotID}");
                                        return;
                                    }

                                    BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                    bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;
                                }

                                double[] currentPoint = trajectories[xbotID][i];
                                double[] nextPoint = trajectories[xbotID][i + 1];
                                double deltaX = nextPoint[0] - currentPoint[0];
                                double deltaY = nextPoint[1] - currentPoint[1];
                                double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                                double baseVelocity = 0.1;
                                double adjustedVelocity = (distance > 1.0) ? baseVelocity * Math.Sqrt(2) : baseVelocity;

                                if (i < trajectories[xbotID].Count - 2)
                                {
                                    double[] nextNextPoint = trajectories[xbotID][i + 2];
                                    double[] nextDirectionVector = { nextNextPoint[0] - nextPoint[0], nextNextPoint[1] - nextPoint[1] };
                                    WaitUntilTriggerParams time_params = new WaitUntilTriggerParams();
                                    time_params.delaySecs = 0.18 * identicalCount - 1;

                                    if (i == 1  && nextPoint[0] == nextNextPoint[0] && nextPoint[1] == nextNextPoint[1])
                                    {
                                        Console.WriteLine($"xbot{xbotID} is waiting");
                                        _xbotCommand.WaitUntil(0, xbotID, TRIGGERSOURCE.TIME_DELAY, time_params);
                                        skipAhead = identicalCount - 1;
                                        Console.WriteLine($"[Debug] Skipping ahead by {identicalCount - 1} points at index {i}");
                                        continue;
                                    }
                                    else
                                    {
                                        if (Math.Sign(deltaX) != Math.Sign(nextDirectionVector[0]) || Math.Sign(deltaY) != Math.Sign(nextDirectionVector[1]))
                                        {
                                            _xbotCommand.LinearMotionSI(0, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.DIRECT, nextPoint[0], nextPoint[1], 0, adjustedVelocity, 0.5);
                                        }
                                        else
                                        {
                                            _xbotCommand.LinearMotionSI(0, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.DIRECT, nextPoint[0], nextPoint[1], adjustedVelocity, adjustedVelocity, 0.5);
                                        }
                                    }
                                }
                                else
                                {
                                    // This is the second last point, so handle the last move here
                                    _xbotCommand.LinearMotionSI(0, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.DIRECT, nextPoint[0], nextPoint[1], 0, adjustedVelocity, 0.5);

                                    if (nextPoint[0] == targetPositions[xbotID][0] && nextPoint[1] == targetPositions[xbotID][1])
                                    {
                                        var matchingStation = Station.FirstOrDefault(station =>
                                            station.Value.Take(2).Zip(new double[] { targetPositions[xbotID][0], targetPositions[xbotID][1] },
                                                                     (stationValue, targetValue) => Math.Abs(stationValue - targetValue) < 0.001).All(isClose => isClose));

                                        if (matchingStation.Key != 0)
                                        {
                                            xbotStateStationID[xbotID] = matchingStation.Key;
                                        }
                                    }
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
                    });
                    runningTasks[xbotID] = task;
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



        

        


        private void Rotation(int xbotID)
        {
            Console.WriteLine($"Entered Rotate for xbotID: {xbotID}");

            // Cancel any existing rotation task for this xbotID
            if (rotationCancellationTokens.ContainsKey(xbotID))
            {
                try
                {
                    rotationCancellationTokens[xbotID].Cancel();
                    rotationCancellationTokens[xbotID].Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Rotation] Error cancelling previous rotation for xbotID {xbotID}: {ex.Message}");
                }
                finally
                {
                    rotationCancellationTokens.Remove(xbotID);
                }
            }

            // Create a new cancellation token for this rotation
            var cts = new CancellationTokenSource();
            rotationCancellationTokens[xbotID] = cts;
            var cancellationToken = cts.Token;

            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"[Debug] Task started for xbotID: {xbotID}");

                    List<double[]> flywayCenters = new()
                    {
                        new double[] {0.12, 0.12},
                        new double[] {0.12, 0.36},
                        new double[] {0.12, 0.6},
                        new double[] {0.12, 0.84},
                        new double[] {0.36, 0.12},
                        new double[] {0.36, 0.36},
                        new double[] {0.36, 0.84},
                        new double[] {0.6, 0.12},
                        new double[] {0.6, 0.36},
                        new double[] {0.6, 0.6},
                        new double[] {0.6, 0.84}
                    };

                    // Ensure positions dictionary is updated
                    if (!positions.ContainsKey(xbotID))
                    {
                        Console.WriteLine($"[Error] positions dictionary does not contain xbotID: {xbotID}");
                        return;
                    }

                    double[] targetPosition = targetPositions[xbotID];

                    _xbotCommand.RotaryMotionP2P(0, xbotID, ROTATIONMODE.NO_ANGLE_WRAP, targetPosition[5] * (Math.PI / 180), 1, 0.5, POSITIONMODE.ABSOLUTE);

                    double lastOrientation = positions[xbotID][5];
                    int unchangedOrientationCount = 0;
                    bool retried = false;

                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine($"[Rotation] Task cancelled for xbotID {xbotID}");
                            break;
                        }

                        XBotStatus status = _xbotCommand.GetXbotStatus(xbotID);
                        int xbotState = (int)status.XBOTState;
                        double currentOrientation = positions[xbotID][5];
                        double targetOrientation = targetPositions[xbotID][5];
                        double adjustedDifference = Math.Min(Math.Abs(currentOrientation - targetOrientation), 360 - Math.Abs(currentOrientation - targetOrientation));

                        Console.WriteLine($"[Debug] xbotState: {xbotState}, Current orientation: {currentOrientation}, Target orientation: {targetOrientation}, Difference: {adjustedDifference}");

                        MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                        int bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;

                        Console.WriteLine($"xbot{xbotID} buffer count is {bufferCount}");

                        if (Math.Abs(currentOrientation - lastOrientation) < 0.01)
                        {
                            unchangedOrientationCount++;
                        }
                        else
                        {
                            unchangedOrientationCount = 0;
                        }
                        lastOrientation = currentOrientation;

                        if (unchangedOrientationCount >= 3 && !retried)
                        {
                            Console.WriteLine($"[Debug] Orientation unchanged for 3 iterations for xbotID {xbotID}, calling Rotation again.");
                            retried = true; // Prevent infinite recursion
                            Rotation(xbotID);
                            break;
                        }

                        if (xbotState == 3 && adjustedDifference <= 0.1)
                        {
                            Console.WriteLine($"[Debug] Rotation completed for xbotID: {xbotID}");
                            await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbotID}/CMD/SubCMD", "RotationDone");
                            RotationLock[xbotID] = false;
                            break;
                        }

                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Exception in Rotation task for xbotID {xbotID}: {ex.Message}");
                }
                finally
                {
                    // Clean up the cancellation token after task ends
                    if (rotationCancellationTokens.ContainsKey(xbotID))
                    {
                        rotationCancellationTokens[xbotID].Dispose();
                        rotationCancellationTokens.Remove(xbotID);
                    }
                }
            }, cancellationToken);
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