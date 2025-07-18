﻿
using PathPlaningNode;
using PMCLIB;
using System.ComponentModel.Design;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Transactions;
using VT2_Aseptic_Production_Demonstrator;
using static PathPlanPMC.PathPlanPMCNode;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;

//For the PathPlanPMCNode the PMCLib needs to be included in the project and the MQTTnet (version 4.3.7.1207) Package needs to be installed


namespace PathPlanPMC
{
    class PathPlanPMCNode
    {
        //PlanerMotor Library
        private static XBotCommands _xbotCommand = new XBotCommands();
        private static SystemCommands _systemCommands = new SystemCommands();
        //Connection To the PMC
        private connection_handler connectionHandler = new connection_handler();


        private MQTTSubscriber mqttSubscriber = null!;
        private MQTTPublisher mqttPublisher = null!;
        private Pathfinding pathfinder;
        //Dictionarys
        private Dictionary<string, Action<string, string>> topicHandlers = null!;
        private Dictionary<int, string> XbotStates = new Dictionary<int, string>
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
        private Dictionary<int, string> ShuttleStates = new();
       
        private Dictionary<int, double[]> lastPublishedPositions = new();
        private Dictionary<int, string> lastPublishedStates = new();
        private Dictionary<int, int> lastPublishedStatesID = new();
        private Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();
        private CancellationTokenSource runTrajectoryCancellationTokenSource;
        private Dictionary<int, Task> runningTasks = new(); // Track running tasks for each xbotID
        private Dictionary<int, CancellationTokenSource> taskCancellationTokens = new(); // Track cancellation tokens for each xbotID
        private Dictionary<int, CancellationTokenSource> rotationCancellationTokens = new(); // Manage rotation cancellation tokens per xbot
        //private Dictionary<int, double[]> Stations = new(); //Key is the StationdId, value is the position
        private Dictionary<int, string> CommandUuid = new();
        private Dictionary<int, string> CurrentCommand = new();
        private Dictionary<int, string> CurrentTask = new();
        private Dictionary<int, bool> RotationLock = new();
        private Dictionary<int, double[]> targetPositions = new();
        private Dictionary<int, double[]> positions = new();
        private Dictionary<int, int> xbotStateStationID = new();
        //private Dictionary<string, List<double[]>> StationCordinate = new();
        private Dictionary<string, Dictionary<string, double[]>> StationCoordinates = new();
        private Dictionary<string, int> StationsID = new();


        //UNS Prefix
        string UNSPrefix = "AAU/Fibigerstræde/Building14/FillingLine/Planar/";

        //MQTT Broker settings
        string brokerIP = "172.20.66.135";
        //string brokerIP = "localhost";
        int port = 1883;

        //Pathpalner Grid Settings
        private int xbotSize = 12;
        private int width = 98;
        private int height = 98;
        private Pathfinding.grid gridGlobal;

        //PathPlanning Lock and Task
        private readonly object pathPlannerLock = new();
        private Task? pathPlannerTask = null;
        private CancellationTokenSource? pathPlannerCts = null;


        private Dictionary<int, (string, bool, bool, bool)> CommandState = new();
        private Dictionary<int, CancellationTokenSource> commandCancellationTokens = new();

        int[] xbotsID;
       
        //Lists
        private List<(int, double[], double[])> xBotID_From_To = new();






        #region messagePayloads
        // Represents the structure of messages sent to and from the xbot, including position, target position, command, sub-command, and state messages.
        private class PositionMessage
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

        private class CommandMessage
        {
            public string CommandUuid { get; set; } = string.Empty;
            public string Command { get; set; } = string.Empty;
            public string TimeStamp { get; set; } = string.Empty;
        }

        private class SubCommandMessage
        {
            public string CommandUuid { get; set; } = string.Empty;
            public string Task { get; set; } = string.Empty;
            public string TimeStamp { get; set; } = string.Empty;
        }

        private class xbotStateMessage
        {
            public string Name { get; set; } = string.Empty;

            public string State { get; set; } = string.Empty;

            public int? StationID { get; set; }

            public string TimeStamp { get; set; } = string.Empty;
        }

        public class StationMessage
        {
            public List<Station> Stations { get; set; } = new();
        }

        public class Station
        {
            public string Name { get; set; } = string.Empty;
            public int StationID { get; set; }
            public string? RequiredTool { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtraProperties { get; set; } = new();
        }


        #endregion


        #region Initialize

        public PathPlanPMCNode()
        {
            CONNECTIONSTATUS status = connectionHandler.ConnectAndGainMastership();
            while(status != 0)
            {
                break;
            }

            Console.WriteLine(status);


            InitializeMqttPublisher();
            InitializeTopicHandlers();
            InitializeMqttSubscriber();

            
            

            Task.Run(async () =>
            {
                await WaitForMqttConnectionAsync();
                gridGlobal = new Pathfinding.grid(width, height, xbotSize);
                pathfinder = new Pathfinding();
                
            });


            PublishXbotIDAsync();
        }

        private async Task WaitForMqttConnectionAsync()
        {
            while (!mqttPublisher.IsConnected || !mqttSubscriber.IsConnected)
            {
                Console.WriteLine("Waiting for MQTT connection...");
                await Task.Delay(500); // Wait for 500ms before checking again
            }
            Console.WriteLine("MQTT connection established.");
        }

        // Initializes the topicHandlers dictionary with MQTT topic patterns and their corresponding handler methods.
        // Each entry maps a topic pattern (with wildcards) to a method that will process messages for that topic.
        private void InitializeTopicHandlers()
        {
            topicHandlers = new Dictionary<string, Action<string, string>>
                {
                    // Example handlers for position and state topics (currently commented out):
                    //{ UNSPrefix + "+/DATA/TargetPosition", getTargetPostion },
                    //{ UNSPrefix + "+/DATA/Position", getPostion },

                    //{UNSPrefix + "+/DATA/State", getXbotState },
                    //{ UNSPrefix + "PathPlan/CMD", HandlePathPlanStatus },

                    // Handler for sub-commands sent to specific Xbots
                    { UNSPrefix + "+/CMD/SubCMD", HandleSubCMD},
                    // Handler for main commands sent to specific Xbots
                    { UNSPrefix + "+/CMD", HandleCMD },
                    // Handler for station configuration messages
                    { "AAU/Fibigerstræde/Building14/FillingLine/Configuration/DATA/Planar/Stations", HandleStationSetup}
                };
        }
        

        private async void InitializeMqttSubscriber()
        {
            mqttSubscriber = new MQTTSubscriber(brokerIP, port);
            mqttSubscriber.MessageReceived += messageHandler;
            await mqttSubscriber.StartAsync();
            Console.WriteLine("Connected to MQTT broker.");
            foreach (var topic in topicHandlers.Keys)
            {
                await mqttSubscriber.SubscribeAsync(topic);
            }
        }

        private async void InitializeMqttPublisher()
        {
            mqttPublisher = new MQTTPublisher(brokerIP, port);
            await mqttPublisher.StartAsync();
        }
        #endregion

        #region Message Handlers
        // Handles incoming MQTT messages by matching the topic to a registered handler.
        // Iterates through the topicHandlers dictionary and invokes the corresponding handler if a match is found.
        // If no handler matches, logs the unhandled topic.
        private void messageHandler(string topic, string message)
            {
            
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

            // Determines if a given MQTT topic matches a pattern with wildcards.
            // The pattern may contain '+' as a wildcard for a single topic segment.
            // Returns true if the topic matches the pattern, false otherwise.
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

        private async void HandleStationSetup(string topic, string message)
        {
            

            try
            {
                var stationMessage = JsonSerializer.Deserialize<StationMessage>(message)
                    ?? throw new InvalidOperationException("StationMessage is null");

                Console.WriteLine($"[Debug] Deserialized StationMessage with {stationMessage.Stations.Count} stations.");

                foreach (var station in stationMessage.Stations)
                {
                    //Console.WriteLine($"[Debug] Station: Name={station.Name}, StationID={station.StationID}, RequiredTool={station.RequiredTool}");
                    var positions = new Dictionary<string, double[]>();
                    StationsID[station.Name] = station.StationID;
                    foreach (var kvp in station.ExtraProperties)
                    {
                        //Console.WriteLine($"[Debug] ExtraProperty Key: {kvp.Key}, ValueKind: {kvp.Value.ValueKind}");
                        if (kvp.Value.ValueKind == JsonValueKind.Array)
                        {
                            var arr = kvp.Value.EnumerateArray()
                                .Select(el => el.GetDouble())
                                .ToArray();
                            positions[kvp.Key] = arr;
                        }
                        // Print out each sub key and its value
                        Console.Write($"[Station: {station.Name}] SubKey: {kvp.Key} = ");
                        if (kvp.Value.ValueKind == JsonValueKind.Array)
                        {
                            var arr = kvp.Value.EnumerateArray().Select(el => el.GetDouble()).ToArray();
                            Console.WriteLine($"[{string.Join(", ", arr)}]");
                        }
                        else if (kvp.Value.ValueKind == JsonValueKind.String)
                        {
                            Console.WriteLine(kvp.Value.GetString());
                        }
                        else if (kvp.Value.ValueKind == JsonValueKind.Number)
                        {
                            Console.WriteLine(kvp.Value.GetDouble());
                        }
                        else
                        {
                            Console.WriteLine(kvp.Value.ToString());
                        }
                    }

                    StationCoordinates[station.Name] = positions;
                    Console.WriteLine($"[Debug] Stored positions for station '{station.Name}': {positions.Count} entries.");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] {ex.Message}");
            }
        }

        private int GetStationIdForPosition(double[] position)
        {
            foreach (var station in StationCoordinates)
            {
                foreach (var kvp in station.Value)
                {
                    if (kvp.Key.StartsWith("Approach", StringComparison.OrdinalIgnoreCase))
                        continue;

                    double[] storedPosition = kvp.Value;
                    if (storedPosition.Length >= 2 &&
                        Math.Abs(position[0] - storedPosition[0]) < 0.001 &&
                        Math.Abs(position[1] - storedPosition[1]) < 0.001)
                    {
                        // Found a matching station, return its ID
                        if (StationsID.TryGetValue(station.Key, out int stationId))
                            return stationId;
                    }
                }
            }
            return 0; // Not at any station
        }

        private async void HandleCMD(string topic, string message)
        {
            Console.WriteLine($"[MessageReceived] Topic: {topic}, Message: {message}");
            string[] segments = topic.Split('/');
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot")) ?? throw new InvalidOperationException("xbot segment not found");
            int xbotID = int.Parse(xbotSegment.Substring(4));
            if (string.IsNullOrWhiteSpace(message) || message == "None")
            {
                Console.WriteLine("[DEBUG] Received empty or null command message. Ignoring.");
                CommandUuid[xbotID] = null;
                CurrentCommand[xbotID] = null;
                return;
            }
            // Deserialize the message into a structured object
            var commandMessage = JsonSerializer.Deserialize<CommandMessage>(message);

            if (commandMessage == null)
            {
                throw new InvalidOperationException("Command message is null or invalid.");
            }

            /*// Cancel the existing command execution if it exists
            if (commandCancellationTokens.ContainsKey(xbotID))
            {
                Console.WriteLine($"[DEBUG] Cancelling existing command for xbotID {xbotID}.");
                commandCancellationTokens[xbotID].Cancel();
                commandCancellationTokens[xbotID].Dispose();
                commandCancellationTokens.Remove(xbotID);
                CommandState.Remove(xbotID);
            }
            */
            // Update the command state and start the new command
            CommandUuid[xbotID] = commandMessage.CommandUuid;
            CurrentCommand[xbotID] = commandMessage.Command;
            Console.WriteLine($"[MessageReceived] CommandUuid: {CommandUuid[xbotID]}, From: {commandMessage.CommandUuid}");
            Console.WriteLine($"[MessageReceived] Command: {CurrentCommand[xbotID]}, From: {commandMessage.Command}");
        }
        // Handles incoming SubCMD messages for a specific xbot, parses the command, and dispatches the appropriate action.
        private async void HandleSubCMD(string topic, string message)
        {
            Console.WriteLine($"Received message on topic {topic}: {message}");
            // Extract the xbotID from the topic string (expects "Xbot<ID>" in the topic)
            string[] segments = topic.Split('/');
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot")) ?? throw new InvalidOperationException("xbot segment not found");
            int xbotID = int.Parse(xbotSegment.Substring(4));

            // Deserialize the incoming message into a SubCommandMessage object
            var subCommandMessage = JsonSerializer.Deserialize<SubCommandMessage>(message);
            // Update the current task for this xbotID
            CurrentTask[xbotID] = subCommandMessage.Task;
            if (subCommandMessage == null)
            {
                throw new InvalidOperationException("Command message is null or invalid.");
            }

            // Handle the subcommand based on its Task value
            switch (subCommandMessage.Task)
            {
                case "None":
                    // No action required for "None" task
                    break;
                case "Levitate":
                    // Send levitate command to the xbot
                    Levitate(xbotID);
                    break;
                case "Land":
                    // Send land command to the xbot
                    Land(xbotID);
                    break;
                case "Rotation":
                    // Prevent concurrent rotation commands for the same xbot
                    if (RotationLock[xbotID])
                    {
                        Console.WriteLine($"[DEBUG] Rotation for xbotID {xbotID} is already in progress. Ignoring new command.");
                        return;
                    }
                    RotationLock[xbotID] = true; // Lock the rotation for this xbotID
                    Rotation(xbotID);
                    break;
                default:
                    // Cancel any existing movement command for this xbotID
                    if (commandCancellationTokens.ContainsKey(xbotID))
                    {
                        Console.WriteLine($"[DEBUG] Cancelling existing command for xbotID {xbotID}.");
                        commandCancellationTokens[xbotID].Cancel();
                        commandCancellationTokens[xbotID].Dispose();
                        commandCancellationTokens.Remove(xbotID);
                        CommandState.Remove(xbotID);
                    }
                    // If the command and station/task are valid, start a new movement task
                    if (
                        CurrentCommand.ContainsKey(xbotID) &&
                        StationCoordinates.ContainsKey(CurrentCommand[xbotID]) &&
                        StationCoordinates[CurrentCommand[xbotID]].ContainsKey(subCommandMessage.Task))
                    {
                        var cancellationTokenSource = new CancellationTokenSource();
                        commandCancellationTokens[xbotID] = cancellationTokenSource;

                        // Start the movement task asynchronously
                        Task.Run(() => ExecuteMovementTask(xbotID, subCommandMessage.Task, cancellationTokenSource.Token), cancellationTokenSource.Token);
                    }
                    break;
            }
        }

            
            
        
        #endregion

        #region Publish Data

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
                    string xbotState = XbotStates.ContainsKey(xbotStateID) ? XbotStates[xbotStateID] : "Unknown";
                    ShuttleStates[xbot] = xbotState; // Update ShuttleStates dictionary
                    // Round the position values
                    position = position.Select(p => Math.Round(p, 3)).ToArray();

                    // Retrieve the CommandUuid for the xbot
                    string commandUuid = CommandUuid.ContainsKey(xbot) ? CommandUuid[xbot] : null;

                    // Convert the last three entries of the position from radians to degrees
                    for (int i = position.Length - 3; i < position.Length; i++)
                    {
                        position[i] = Math.Round(position[i] * (180 / Math.PI), 3);
                    }

                    // Update the positions dictionary
                    positions[xbot] = position;                    

                    // Get the current timestamp
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    // Check if position has changed
                    if (!lastPublishedPositions.ContainsKey(xbot) ||
                        !position.SequenceEqual(lastPublishedPositions[xbot]))
                    {
                        lastPublishedPositions[xbot] = position;

                        // Determine if xbot is at a station
                        int stationId = 0;
                        if (positions.ContainsKey(xbot) && IsCurrentPositionAtStation(xbot, positions[xbot]))
                        {
                            if (CurrentCommand.ContainsKey(xbot) && StationsID.ContainsKey(CurrentCommand[xbot]))
                                stationId = StationsID[CurrentCommand[xbot]];
                        }
                        xbotStateStationID[xbot] = stationId;
                        lastPublishedStatesID[xbot] = stationId;


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




                    // Check if state has changed
                    if ((!lastPublishedStates.ContainsKey(xbot) ||
                        lastPublishedStates[xbot] != xbotState) || (!lastPublishedStatesID.ContainsKey(xbot) || lastPublishedStatesID[xbot] != xbotStateStationID.GetValueOrDefault(xbot)))
                    {
                        lastPublishedStates[xbot] = xbotState;

                        // Determine stationId for current position
                        int stationId = 0;
                        if (positions.ContainsKey(xbot))
                        {
                            // Try to use CurrentCommand if available, else search all stations
                            if (CurrentCommand.ContainsKey(xbot) && StationsID.ContainsKey(CurrentCommand[xbot]) &&
                                IsCurrentPositionAtStation(xbot, positions[xbot]))
                            {
                                stationId = StationsID[CurrentCommand[xbot]];
                            }
                            else
                            {
                                stationId = GetStationIdForPosition(positions[xbot]);
                            }
                        }
                        xbotStateStationID[xbot] = stationId;
                        lastPublishedStatesID[xbot] = stationId;

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

            foreach (var xbotID in xbotsID)
            {                
                RotationLock[xbotID] = false; // Initialize RotationLock
                
            }
        }


        


        #endregion

        #region PathPlaning
        
        private void ExecutePathPlanner()
        {
            lock (pathPlannerLock)
            {
                // Cancel and await previous task if running
                if (pathPlannerTask != null && !pathPlannerTask.IsCompleted)
                {
                    Console.WriteLine("[DEBUG] Cancelling previous path planner task.");
                    pathPlannerCts?.Cancel(); // Signal cancellation to the previous task

                    try
                    {
                        // Wait for the previous task to finish (avoid deadlock by not blocking UI thread)
                        pathPlannerTask.Wait(500); 
                    }
                    catch (AggregateException ex)
                    {
                        // Log any exceptions thrown by the previous task
                        foreach (var inner in ex.InnerExceptions)
                            Console.WriteLine($"[ERROR] Exception while waiting for previous path planner task: {inner.Message}");
                    }
                    finally
                    {
                        // Clean up resources for the previous task
                        pathPlannerCts?.Dispose();
                        pathPlannerCts = null;
                        pathPlannerTask = null;
                    }

                    // Reset trajectories for a fresh planning run
                    trajectories = new Dictionary<int, List<double[]>>();
                }

                // Create a new cancellation token and start a new path planner task
                pathPlannerCts = new CancellationTokenSource();
                pathPlannerTask = Task.Run(() => RunExecutePathPlannerAsync(pathPlannerCts.Token), pathPlannerCts.Token);
            }
        }
        // Executes the path planning logic for all xbots in xBotID_From_To list
        private async Task RunExecutePathPlannerAsync(CancellationToken token)
        {
            try
            {
                Console.WriteLine("[Debug] Running Path Planner");
                Console.WriteLine($"{xBotID_From_To.Count}");

                // Update the 'from' position for each xbot to its current position
                for (int i = 0; i < xBotID_From_To.Count; i++)
                {
                    if (token.IsCancellationRequested) return;

                    var (xbotID, from, to) = xBotID_From_To[i];

                    // Defensive: Ensure positions contains xbotID
                    if (!positions.ContainsKey(xbotID))
                    {
                        Console.WriteLine($"[ERROR] positions does not contain xbotID {xbotID}. Skipping.");
                        continue;
                    }
                    // Always use the latest position as 'from'
                    from = positions[xbotID];

                    xBotID_From_To[i] = (xbotID, from, to);
                    // If 'to' is null or empty, set it to the current position
                    if (to == null || to.Length == 0)
                    {
                        if (positions.ContainsKey(xbotID))
                        {
                            xBotID_From_To[i] = (xbotID, from, from);
                        }
                        else
                        {
                            Console.WriteLine($"Error: Current position for xbotID {xbotID} is not available.");
                            continue;
                        }
                    }
                    // Debug print for each entry
                    Console.WriteLine("xBotID_From_To contents:");
                    string fromStr = from != null ? $"[{string.Join(", ", from)}]" : "null";
                    string toStr = to != null ? $"[{string.Join(", ", to)}]" : "null";
                    Console.WriteLine($"xbotID: {xbotID}, from: {fromStr}, to: {toStr}");
                }

                // Run the path planner and group the results by xbotID
                trajectories = pathfinder.pathPlanRunner(gridGlobal, xBotID_From_To, xbotSize)
                               .GroupBy(item => item.Item1)
                               .ToDictionary(group => group.Key, group => group.Last().Item2);

                // For each trajectory, handle target orientation and publish the trajectory
                foreach (var trajectory in trajectories)
                {
                    if (token.IsCancellationRequested) return;

                    int xbotID = trajectory.Key;

                    // If a target orientation is specified and the current orientation is close, add the target position to the trajectory
                    if (targetPositions.ContainsKey(xbotID))
                    {
                        var targetPosition = targetPositions[xbotID];
                        if (positions.ContainsKey(xbotID))
                        {
                            var currentPosition = positions[xbotID];
                            // If orientation (Rz) is within 1 degree, add the target position
                            if (Math.Abs(currentPosition[5] - targetPosition[5]) < 1)
                            {
                                trajectory.Value.Add(targetPosition);
                            }
                        }
                    }

                    // Prepare and publish the trajectory message
                    string commandUuid = CommandUuid.ContainsKey(xbotID) ? CommandUuid[xbotID] : null;
                    string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                    var trajectoryMessage = new
                    {
                        CommandUuid = commandUuid,
                        Trajectory = trajectory.Value.Select(t => new double[] { t[0], t[1] }).ToList(),
                        TimeStamp = timestamp
                    };

                    string serializedMessage = JsonSerializer.Serialize(trajectoryMessage);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbotID}/DATA/Trajectory", serializedMessage);

                    // If a subcommand is set, ensure the last point matches the station position
                    if (CurrentCommand.ContainsKey(xbotID) &&
                        CurrentTask.ContainsKey(xbotID) &&
                        !string.IsNullOrEmpty(CurrentTask[xbotID]))
                    {
                        var currentCmd = CurrentCommand[xbotID];
                        var currentTask = CurrentTask[xbotID];
                        if (StationCoordinates.ContainsKey(currentCmd) && StationCoordinates[currentCmd].ContainsKey(currentTask))
                        {
                            var stationPos = StationCoordinates[currentCmd][currentTask];
                            if (stationPos.Length >= 2)
                            {
                                // Only add if not already the last point
                                var lastPoint = trajectory.Value.LastOrDefault();
                                if (lastPoint == null ||
                                    Math.Abs(lastPoint[0] - stationPos[0]) > 1e-6 ||
                                    Math.Abs(lastPoint[1] - stationPos[1]) > 1e-6)
                                {
                                    trajectory.Value.Add(new double[] { stationPos[0], stationPos[1] });
                                }
                            }
                        }
                    }
                }

                // Start executing the planned trajectories
                ExecuteTrajectory();

                // Optionally: Clear all target positions here if needed
                //ClearAllTargetPositions();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[DEBUG] Path planner task was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in path planner: {ex.Message}");
            }            
        }

        #endregion

        #region Command execution
        private async void Levitate(int xbotID)
        {
            Console.WriteLine($"[DEBUG] Sending levitate command to xbotID {xbotID}.");
            // Send the Levitate command to the xbot
            _xbotCommand.LevitationCommand(xbotID, LEVITATEOPTIONS.LEVITATE);

            // Prepare a "None" subcommand message to clear the current subcommand
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var nullCommand = new
            {
                CommandUuid = "None",
                Task = "None",
                TimeStamp = timestamp
            };
            string serializedMessage = JsonSerializer.Serialize(nullCommand);

            // Publish the "None" subcommand to the MQTT topic for this xbot
            await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbotID}/CMD/SubCMD", serializedMessage);
        }
        // Sends a land command to the specified xbotID and publishes a "None" subcommand to MQTT
        private async void Land(int xbotID)
        {
            Console.WriteLine($"[DEBUG] Sending levitate command to xbotID {xbotID}.");
            // Send the land command to the xbot
            _xbotCommand.LevitationCommand(xbotID, LEVITATEOPTIONS.LAND);

            // Prepare a "None" subcommand message to clear the current subcommand
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var nullCommand = new
            {
                CommandUuid = "None",
                Task = "None",
                TimeStamp = timestamp
            };
            string serializedMessage = JsonSerializer.Serialize(nullCommand);

            // Publish the "None" subcommand to the MQTT topic for this xbot
            await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbotID}/CMD/SubCMD", serializedMessage);
        }

        // Handles the rotation command for a specific xbotID
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

            // Start the rotation task asynchronously
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine($"[Debug] Task started for xbotID: {xbotID}");                  

                    // Ensure positions dictionary is updated
                    if (!positions.ContainsKey(xbotID))
                    {
                        Console.WriteLine($"[Error] positions dictionary does not contain xbotID: {xbotID}");
                        return;
                    }

                    // Get the target position for rotation
                    double[] targetPosition = targetPositions[xbotID];

                    // Send the rotary motion command to the xbot (convert degrees to radians)
                    _xbotCommand.RotaryMotionP2P(0, xbotID, ROTATIONMODE.NO_ANGLE_WRAP, targetPosition[5] * (Math.PI / 180), 1, 0.5, POSITIONMODE.ABSOLUTE);

                    double lastOrientation = positions[xbotID][5];
                    int unchangedOrientationCount = 0;
                    bool retried = false;

                    // Monitor the rotation progress
                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine($"[Rotation] Task cancelled for xbotID {xbotID}");
                            break;
                        }

                        // Get the current xbot status and orientation
                        XBotStatus status = _xbotCommand.GetXbotStatus(xbotID);
                        int xbotState = (int)status.XBOTState;
                        double currentOrientation = positions[xbotID][5];
                        double targetOrientation = targetPosition[5];
                        // Calculate the minimal difference between current and target orientation (handle wrap-around)
                        double adjustedDifference = Math.Min(Math.Abs(currentOrientation - targetOrientation), 360 - Math.Abs(currentOrientation - targetOrientation));

                        Console.WriteLine($"[Debug] xbotState: {xbotState}, Current orientation: {currentOrientation}, Target orientation: {targetOrientation}, Difference: {adjustedDifference}");

                        // Release the motion buffer and get the buffer count
                        MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                        int bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;

                        Console.WriteLine($"xbot{xbotID} buffer count is {bufferCount}");

                        // Check if the orientation has not changed significantly
                        if (Math.Abs(currentOrientation - lastOrientation) < 0.01)
                        {
                            unchangedOrientationCount++;
                        }
                        else
                        {
                            unchangedOrientationCount = 0;
                        }
                        lastOrientation = currentOrientation;

                        // If orientation hasn't changed for 3 iterations, retry rotation once
                        if (unchangedOrientationCount >= 3 && !retried)
                        {
                            Console.WriteLine($"[Debug] Orientation unchanged for 3 iterations for xbotID {xbotID}, calling Rotation again.");
                            retried = true; // Prevent infinite recursion
                            Rotation(xbotID);
                            break;
                        }

                        // If xbot is idle and orientation is close enough to target, finish rotation
                        if (xbotState == 3 && adjustedDifference <= 0.1)
                        {
                            Console.WriteLine($"[Debug] Rotation completed for xbotID: {xbotID}");
                            RotationLock[xbotID] = false;
                            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                            var nullCommand = new
                            {
                                CommandUuid = "None",
                                Task = "None",
                                TimeStamp = timestamp
                            };
                            string serializedMessage = JsonSerializer.Serialize(nullCommand);

                            // Publish a "None" subcommand to indicate rotation is done
                            await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbotID}/CMD/SubCMD", serializedMessage);
                            break;
                        }

                        // Wait before next check
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


        // Executes a movement task for a specific xbotID to a target station/task, with cancellation support.
        public async Task ExecuteMovementTask(int xbotID, string Task, CancellationToken cancellationToken)
        {
            bool PositionReached = false;
            bool CommandFinished = false;
            xbotStateStationID[xbotID] = 0;
            try
            {
                // Main loop for executing the movement task
                while (!CommandFinished)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // Get the target position for the current command and task
                    double[] PositionTarget = StationCoordinates[CurrentCommand[xbotID]][Task];
                    targetPositions[xbotID] = PositionTarget;

                    Console.WriteLine($"[DEBUG] Starting {Task} under {CurrentCommand[xbotID]} for xbotID: {xbotID}");
                    // Step 1: Move to the approach position
                    if (!PositionReached)
                    {
                        Console.WriteLine($"[DEBUG] Current position of xbotID {xbotID}: {string.Join(", ", positions[xbotID])}");
                        Console.WriteLine($"[DEBUG] Target station position of xbotID {xbotID}: {string.Join(", ", StationCoordinates[CurrentCommand[xbotID]][Task])}");

                        // If the current position does not match the target, plan a path
                        if (positions[xbotID][0] != StationCoordinates[CurrentCommand[xbotID]][Task][0] || positions[xbotID][1] != StationCoordinates[CurrentCommand[xbotID]][Task][1])
                        {
                            Console.WriteLine($"[DEBUG] Moving xbotID {xbotID} to approach position: {string.Join(", ", StationCoordinates[CurrentCommand[xbotID]][Task])}");

                            // Check if an entry for this xbotID already exists in the movement list
                            var existingEntry = xBotID_From_To.FirstOrDefault(entry => entry.Item1 == xbotID);

                            if (existingEntry != default)
                            {
                                // Update the existing entry with the latest position and target
                                Console.WriteLine($"[DEBUG] Updating existing entry for xbotID {xbotID} in xBotID_From_To to Approach target");
                                double[] updatedFrom = positions[xbotID] ?? existingEntry.Item2;
                                double[] updatedTo = PositionTarget ?? existingEntry.Item3;

                                xBotID_From_To.Remove(existingEntry);
                                xBotID_From_To.Add((xbotID, updatedFrom, updatedTo));
                                Console.WriteLine($"[DEBUG] Updated entry for xbotID {xbotID}: From: [{string.Join(", ", updatedFrom)}], To: [{string.Join(", ", updatedTo)}]");
                            }
                            else
                            {
                                // Add a new entry for this xbotID
                                Console.WriteLine($"[DEBUG] Adding new entry for xbotID {xbotID} in xBotID_From_To");

                                xBotID_From_To.Add((xbotID, positions[xbotID] ?? Array.Empty<double>(), PositionTarget ?? Array.Empty<double>()));
                                Console.WriteLine($"[DEBUG] Added entry for xbotID {xbotID}: From: [{string.Join(", ", positions[xbotID] ?? Array.Empty<double>())}], To: [{string.Join(", ", PositionTarget ?? Array.Empty<double>())}]");
                            }

                            // Stop any running trajectory execution
                            StopRunTrajectory();

                            // Wait until all xbots are idle, executing, or stopped before planning
                            while (true)
                            {
                                lock (XbotStates)
                                {
                                    if (ShuttleStates.Count > 0 && (ShuttleStates.Values.All(state => state.Equals("Idle", StringComparison.OrdinalIgnoreCase) || state.Equals("Execute", StringComparison.OrdinalIgnoreCase) || state.Equals("Stopped", StringComparison.OrdinalIgnoreCase))))
                                    {
                                        Console.WriteLine("[DEBUG] All xbots are Idle or Execute. Breaking loop.");
                                        break;
                                    }
                                }
                                Thread.Sleep(100); // Poll every 100ms  
                            }

                            // Start the path planner
                            ExecutePathPlanner();

                            // Wait until the xbot reaches the approach position
                            DateTime startTime = DateTime.Now;
                            int rerunCounter = 0; // Counter to track reruns
                            var lastPosition = positions[xbotID];
                            while (positions[xbotID][0] != StationCoordinates[CurrentCommand[xbotID]][Task][0] || positions[xbotID][1] != StationCoordinates[CurrentCommand[xbotID]][Task][1])
                            {
                                Thread.Sleep(100);
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Console.WriteLine($"[DEBUG] {Task} execution for xbotID {xbotID} was cancelled (inside approach loop).");
                                    return;
                                }
                                var currentPosition = positions[xbotID];
                                // If the xbot has not moved for 2 seconds and is idle, rerun the path planner (up to 2 times)
                                if ((DateTime.Now - startTime).TotalSeconds >= 2 && ShuttleStates.ContainsKey(xbotID) && ShuttleStates[xbotID].Equals("Idle", StringComparison.OrdinalIgnoreCase) && currentPosition == lastPosition)
                                {
                                    rerunCounter++; // Increment the rerun counter

                                    if (rerunCounter >= 2)
                                    {
                                        // If rerun limit reached, cancel and restart the movement task
                                        Console.WriteLine($"[DEBUG] xbotID {xbotID} has reached the maximum rerun count of 3. Cancelling thread.");
                                        commandCancellationTokens[xbotID].Cancel();
                                        commandCancellationTokens[xbotID].Dispose();
                                        commandCancellationTokens.Remove(xbotID);
                                        var cancellationTokenSource = new CancellationTokenSource();
                                        commandCancellationTokens[xbotID] = cancellationTokenSource;
                                        ExecuteMovementTask(xbotID, Task, commandCancellationTokens[xbotID].Token);

                                        return; // Exit this thread and run again  
                                    }

                                    Console.WriteLine($"[DEBUG] xbotID {xbotID} has been waiting for 5 seconds without position change. Re-running PathPlanner. Rerun count: {rerunCounter}");
                                    StopRunTrajectory();

                                    // Wait for all xbots to be idle before rerunning the planner
                                    Console.WriteLine("[DEBUG] Waiting for all xbots to be idle...");
                                    while (true)
                                    {
                                        lock (ShuttleStates)
                                        {
                                            if (ShuttleStates.Count > 0 && (ShuttleStates.Values.All(state => state.Equals("Idle", StringComparison.OrdinalIgnoreCase) || state.Equals("Execute", StringComparison.OrdinalIgnoreCase))))
                                            {
                                                break;
                                            }
                                        }
                                        Thread.Sleep(100); // Poll every 100ms  
                                        StopRunTrajectory();
                                    }
                                    Console.WriteLine("[DEBUG] All xbots are now idle.");

                                    ExecutePathPlanner();
                                    startTime = DateTime.Now; // Reset the timer
                                }

                                lastPosition = currentPosition;
                            }
                        }

                        PositionReached = true; // Set to true once completed
                        Console.WriteLine($"[DEBUG] Approach position reached for xbotID {xbotID}.");
                    }

                    // Prepare and send a "None" subcommand to clear the current subcommand
                    string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    var nullCommand = new
                    {
                        CommandUuid = "None",
                        Task = "None",
                        TimeStamp = timestamp
                    };
                    string serializedMessage = JsonSerializer.Serialize(nullCommand);

                    CommandFinished = true;
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbotID}/CMD/SubCMD", serializedMessage); // Sends an empty message
                    Console.WriteLine($"[Debug] StationID for command:{CurrentCommand[xbotID]} is {StationsID[CurrentCommand[xbotID]]}");

                    // Check if the xbot is located at a station and update its state
                    bool LocatedAdStation = IsCurrentPositionAtStation(xbotID, positions[xbotID]);

                    if (LocatedAdStation == true)
                    {
                        xbotStateStationID[xbotID] = StationsID[CurrentCommand[xbotID]];
                    }

                    Thread.Sleep(100);
                }
                // Remove the command UUID if it exists for this xbot
                if (CommandUuid.ContainsKey(xbotID))
                {
                    CommandUuid.Remove(xbotID);
                }
                // If Task is "Storage", remove xbotID from all relevant dictionaries and lists
                if (Task.Equals("Storage", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove from all relevant dictionaries
                    CommandUuid.Remove(xbotID);
                    CurrentCommand.Remove(xbotID);
                    CurrentTask.Remove(xbotID);
                    RotationLock.Remove(xbotID);
                    targetPositions.Remove(xbotID);
                    positions.Remove(xbotID);
                    xbotStateStationID.Remove(xbotID);
                    lastPublishedPositions.Remove(xbotID);
                    lastPublishedStates.Remove(xbotID);
                    lastPublishedStatesID.Remove(xbotID);
                    ShuttleStates.Remove(xbotID);
                    trajectories.Remove(xbotID);
                    commandCancellationTokens.Remove(xbotID);
                    taskCancellationTokens.Remove(xbotID);
                    rotationCancellationTokens.Remove(xbotID);
                    runningTasks.Remove(xbotID);
                    // Remove from xBotID_From_To
                    xBotID_From_To.RemoveAll(entry => entry.Item1 == xbotID);
                    // Optionally, publish a message or log
                    Console.WriteLine($"[INFO] xbotID {xbotID} removed from all directories due to Storage task.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error in ApproachCommandExecution for xbotID {xbotID}: {ex.Message}");
            }
        }


        #endregion

        #region Trajectory execution
        // Executes the planned trajectories for all xbots in the 'trajectories' dictionary.
        // Each xbot gets its own task for trajectory execution.
        public async void ExecuteTrajectory()
        {
            Console.WriteLine("Executing trajectory...");
            // Create a new cancellation token for this run
            runTrajectoryCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = runTrajectoryCancellationTokenSource.Token;

            List<Task> tasks = new List<Task>();

            // Iterate over all xbots with planned trajectories
            foreach (var xbotID in trajectories.Keys)
            {
                // Ensure only one task per xbotID is running at a time
                if (runningTasks.ContainsKey(xbotID) && runningTasks[xbotID] != null && !runningTasks[xbotID].IsCompleted)
                {
                    Console.WriteLine($"[Info] Task for xbotID {xbotID} is already running. Skipping new task.");
                    continue;
                }
                // Only execute if a trajectory exists for this xbot
                if (trajectories.ContainsKey(xbotID))
                {
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            // Defensive: Check trajectory still exists
                            if (!trajectories.ContainsKey(xbotID))
                            {
                                Console.WriteLine($"[Error] Key {xbotID} not found in trajectories dictionary.");
                                return;
                            }
                            // If there is only one point, make a linear motion to that point
                            if (trajectories[xbotID].Count == 1)
                            {
                                double[] point = trajectories[xbotID][0];
                                double baseVelocity = 0.1;
                                _xbotCommand.LinearMotionSI(0, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.DIRECT, point[0], point[1], 0, baseVelocity, 0.5);
                                return;
                            }
                            // If less than 2 points, skip execution
                            if (trajectories[xbotID].Count < 2)
                            {
                                Console.WriteLine($"[Warning] Trajectory for xbot {xbotID} has less than 2 points. Skipping execution.");
                                return;
                            }
                            // Reset station ID for this xbot
                            xbotStateStationID[xbotID] = 0;
                            // Count consecutive identical points in the trajectory (for wait logic)
                            int identicalCount = CountConsecutiveIdenticalTrajectoryPoints(trajectories[xbotID], 0.0);
                            Console.WriteLine($"Identical points in trajectory for xbot {xbotID}: {identicalCount}");

                            int skipAhead = 0;
                            // Loop through trajectory points, sending motion commands
                            // Loop to Count-1 so i+1 is always valid
                            for (int i = 0; i < trajectories[xbotID].Count - 1; i += skipAhead == 0 ? 1 : skipAhead)
                            {
                                skipAhead = 0; // reset skipAhead
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    Console.WriteLine($"RunTrajectory canceled for xbotID {xbotID}");
                                    return;
                                }

                                // Release buffer and check buffer count before sending new motion
                                MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                                int bufferCount = BufferStatus.motionBufferStatus.bufferedMotionCount;

                                // Wait until buffer is ready (<=1)
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

                                // Get current and next trajectory points
                                double[] currentPoint = trajectories[xbotID][i];
                                double[] nextPoint = trajectories[xbotID][i + 1];
                                double deltaX = nextPoint[0] - currentPoint[0];
                                double deltaY = nextPoint[1] - currentPoint[1];
                                double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                                double baseVelocity = 0.1;
                                // Increase velocity for longer moves
                                double adjustedVelocity = (distance > 1.0) ? baseVelocity * Math.Sqrt(2) : baseVelocity;

                                // If not at the last segment, check for wait or direction change
                                if (i < trajectories[xbotID].Count - 2)
                                {
                                    double[] nextNextPoint = trajectories[xbotID][i + 2];
                                    double[] nextDirectionVector = { nextNextPoint[0] - nextPoint[0], nextNextPoint[1] - nextPoint[1] };
                                    WaitUntilTriggerParams time_params = new WaitUntilTriggerParams();
                                    time_params.delaySecs = 0.18 * identicalCount - 1;

                                    // If the next two points are identical, insert a wait command and skip ahead
                                    if (i == 1 && nextPoint[0] == nextNextPoint[0] && nextPoint[1] == nextNextPoint[1])
                                    {
                                        Console.WriteLine($"xbot{xbotID} is waiting");
                                        _xbotCommand.WaitUntil(0, xbotID, TRIGGERSOURCE.TIME_DELAY, time_params);
                                        skipAhead = identicalCount - 1;
                                        Console.WriteLine($"[Debug] Skipping ahead by {identicalCount - 1} points at index {i}");
                                        continue;
                                    }
                                    else
                                    {
                                        // If direction changes, use a different velocity profile
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

                                    // Optionally: Check if the last point matches the target position
                                    if (nextPoint[0] == targetPositions[xbotID][0] && nextPoint[1] == targetPositions[xbotID][1])
                                    {
                                        // Optionally update station state here
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
                            // Remove trajectory for this xbot after execution
                            lock (trajectories)
                            {
                                trajectories.Remove(xbotID);
                            }
                        }
                    });
                    // Track the running task for this xbot
                    runningTasks[xbotID] = task;
                }
            }

            try
            {
                // Wait for all trajectory tasks to complete
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("RunTrajectory operation was canceled.");
            }
            finally
            {
                // Dispose of the cancellation token source and create a new one for future runs
                runTrajectoryCancellationTokenSource?.Dispose();
                runTrajectoryCancellationTokenSource = new CancellationTokenSource(); // Replace null assignment
            }
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

        private bool IsCurrentPositionAtStation(int xbotID, double[] currentPosition)
        {
            if (!CurrentCommand.ContainsKey(xbotID) || !StationCoordinates.ContainsKey(CurrentCommand[xbotID]))
                return false;

            var positions = StationCoordinates[CurrentCommand[xbotID]];
            foreach (var kvp in positions)
            {
                // Skip any key that starts with "Approach" (case-insensitive)
                if (kvp.Key.StartsWith("Approach", StringComparison.OrdinalIgnoreCase))
                    continue;

                double[] storedPosition = kvp.Value;
                // Compare only X and Y (or all elements if needed)
                bool isAtStation = storedPosition.Length >= 2 &&
                    Math.Abs(currentPosition[0] - storedPosition[0]) < 0.001 &&
                    Math.Abs(currentPosition[1] - storedPosition[1]) < 0.001;

                // Print the result for each entry
                //Console.WriteLine($"[IsCurrentPositionAtStation] Checking key '{kvp.Key}': " +
                //    $"Current=({currentPosition[0]:F3}, {currentPosition[1]:F3}), " +
                //    $"Stored=({storedPosition[0]:F3}, {storedPosition[1]:F3}) => Result: {isAtStation}");

                if (isAtStation)
                {
                    return true;
                }
            }
            return false;
        }

        // Stops all running trajectory tasks for each xbot, cancels their tokens, clears trajectories, and stops motion.
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
                        taskCancellationTokens[xbot].Cancel(); // Signal cancellation
                        Console.WriteLine($"[StopDebug] Cancellation token triggered for xbot {xbot}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StopDebug] Error cancelling task for xbot {xbot}: {ex.Message}");
                    }
                    finally
                    {
                        taskCancellationTokens[xbot].Dispose(); // Clean up resources
                        taskCancellationTokens.Remove(xbot);
                        Console.WriteLine($"[StopDebug] Disposed and removed cancellation token for xbot {xbot}");
                    }
                }

                // Remove the trajectory for this xbot if it exists
                lock (trajectories)
                {
                    if (trajectories.ContainsKey(xbot))
                    {
                        trajectories.Remove(xbot);
                        Console.WriteLine($"[StopDebug] Removed trajectory for xbot {xbot}");
                    }
                }

                // Clear motion buffer and stop motion for each xbot if rotation is locked
                if (RotationLock[xbot] == true)
                {
                    try
                    {
                        // Clear the motion buffer for this xbot
                        MotionBufferReturn BufferStatus = _xbotCommand.MotionBufferControl(xbot, MOTIONBUFFEROPTIONS.CLEARBUFFER);
                        Console.WriteLine($"[StopDebug] Motion buffer cleared for xbot {xbot}, Status: {BufferStatus}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[StopDebug] Error clearing motion buffer for xbot {xbot}: {ex.Message}");
                    }

                    try
                    {
                        // Stop the motion for this xbot
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
                    // No action needed if RotationLock does not contain the xbot
                }
            }
        }

        #endregion

        public static async Task Main(string[] args)
        {
            PathPlanPMCNode client = new PathPlanPMCNode();

            Thread thread1 = new Thread(new ThreadStart(client.PublishPostionsINFAsync));

            thread1.Start();
            await Task.Delay(-1);

        }
        // Add this method to your PathPlanPMCNode class to print the values from StationCoordinates
        public void PrintStationCoordinates()
        {
            Console.WriteLine("Station Coordinates:");
            foreach (var station in StationCoordinates)
            {
                Console.WriteLine($"Station: {station.Key}");
                foreach (var coord in station.Value)
                {
                    string values = string.Join(", ", coord.Value.Select(v => v.ToString("F3")));
                    Console.WriteLine($"  {coord.Key}: [{values}]");
                }
            }
        }
    }    
 }