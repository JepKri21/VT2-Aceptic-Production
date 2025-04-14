using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
//using UnityEngine;



namespace PathPlaningNode
{
   class PathPlaningNode
    {
        private MQTTSubscriber mqttSubscriber;
        private MQTTPublisher mqttPublisher;

        private Dictionary<string, Action<string, string>> topicHandlers;
        string brokerIP = "localhost";
        int port = 1883;
        int[] xbotsID;
        Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();
        Dictionary<int, double[]> targetPostions = new Dictionary<int, double[]>();
        Dictionary<int, double[]> postions = new Dictionary<int, double[]>();
        int xbotSize = 12;
        int width = 72;
        int height = 96;
        private Pathfinding.grid gridGlobal;
        private Pathfinding pathfinder;
        List<(int, double[], double[])> xBotID_From_To = new List<(int, double[], double[])>();





        #region Initialize
        public PathPlaningNode()
        {
            InitializeTopicHandlers();
            InitializeMqttSubscriber();
            InitializeMqttPublisher();
            gridGlobal = new Pathfinding.grid(width, height, xbotSize);
            pathfinder = new Pathfinding();


        }

        private void InitializeTopicHandlers()
        {
            topicHandlers = new Dictionary<string, Action<string, string>>
            {
                { "AAU/Fiberstæde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/TargetPosition", getTargetPostion },
                { "AAU/Fiberstæde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/Position", getPostion },
                { "AAU/Fiberstæde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", HandleStatus }
            };
        }

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

        private async void InitializeMqttPublisher()
        {
            mqttPublisher = new MQTTPublisher(brokerIP, port);
            await mqttPublisher.StartAsync();
        }
        #endregion
 
        #region GetTargetAndPostion    
        public async void getTargetPostion(string topic, string message)
        {
            Console.WriteLine($"Received target position message on topic {topic}: {message}");
            //Console.WriteLine($"Received message on topic {topic}: {message}");
           
            var targetPosition = JsonSerializer.Deserialize<double[]>(message);
            // Split the topic into segments
            string[] segments = topic.Split('/');
            // Find the segment that starts with "xbot" and extract the numeric part
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot")) ?? throw new InvalidOperationException("xbot segment not found");
            int xbotId = int.Parse(xbotSegment.Substring(4)); // Extract the numeric part after "xbot"

            Console.WriteLine($"Updating target position for xbotID {xbotId}: {string.Join(", ", targetPosition)}");
            targetPostions[xbotId] = targetPosition ?? throw new InvalidOperationException("TargetPosition is null");
            UpdateFromAndTo(xbotId);
        }

        public async void getPostion(string topic, string message)
        {
            Console.WriteLine($"Received message on topic {topic}: {message}");
            
            var position = JsonSerializer.Deserialize<double[]>(message);
            // Split the topic into segments
            string[] segments = topic.Split('/');
            // Find the segment that starts with "xbot" and extract the numeric part
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot", StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException("xbot segment not found");
            int xbotId = int.Parse(xbotSegment.Substring(4)); // Extract the numeric part after "xbot"
            Console.WriteLine($"Updating position for xbotID {xbotId}: {string.Join(", ", position)}");

            postions[xbotId] = position ?? throw new InvalidOperationException("Position is null");
            UpdateFromAndTo(xbotId);
        }
       
            // Check if the list already contains a value for the given xbotID
        private async void UpdateFromAndTo(int xbotID)
        {
            Console.WriteLine($"Checking UpdateFromAndTo for xbotID {xbotID}");
            Console.WriteLine($"postions contains: {string.Join(", ", postions.Keys)}");
            Console.WriteLine($"targetPostions contains: {string.Join(", ", targetPostions.Keys)}");

            if (postions.ContainsKey(xbotID) && targetPostions.ContainsKey(xbotID))
            {
                var existingEntry = xBotID_From_To.FirstOrDefault(entry => entry.Item1 == xbotID);

                if (existingEntry != default)
                {
                    xBotID_From_To.Remove(existingEntry);
                }
                xBotID_From_To.Add((xbotID, postions[xbotID], targetPostions[xbotID]));
                PrintXBotIDFromTo();
            }
            else
            {
                Console.WriteLine($"XbotID {xbotID} not found in postions or targetPostions dictionaries.");
            }
        }
        
        public void PrintXBotIDFromTo()
        {
            foreach (var entry in xBotID_From_To)
            {
                Console.WriteLine($"xbotID: {entry.Item1}, From: [{string.Join(", ", entry.Item2)}], To: [{string.Join(", ", entry.Item3)}]");
            }
        }
        #endregion

        #region MessageHandler
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



            }
            if (message == "home")
            {

            }
            if (message == "SendPostions")
            {

            }
            if (message == "runPathPlanner")
            {
                Console.WriteLine("Running Path Planner");
                PrintGridSize();
                trajectories = pathfinder.pathPlanRunner(gridGlobal, xBotID_From_To, xbotSize)
                    .ToDictionary(item => item.Item1, item => item.Item2);
                
                foreach (var trajectory in trajectories)
                {
                    Console.WriteLine($"Trajectory for xbot{trajectory.Key}: {trajectory.Value}");
                    var trajectoryMessage = JsonSerializer.Serialize(trajectory.Value.Select(t => new double[] { t[0], t[1] }).ToList());
                    await mqttPublisher.PublishMessageAsync($"AAU/Fiberstæde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{trajectory.Key}/Trajectory", trajectoryMessage);
                    Console.WriteLine($"Published trajectory for xbot {trajectory.Key}: {trajectoryMessage}");
                }
                

            }
            else
            {

            }
        }
        private void PrintGridSize()
        {
            Console.WriteLine($"Grid Size - Width: {gridGlobal.width}, Height: {gridGlobal.height}");
        }


        #endregion





        public static async Task Main(string[] args) // Change return type to Task
        {

            PathPlaningNode client = new PathPlaningNode();


            //Thread thread1 = new Thread(new ThreadStart(client.SendPostionsINF));


            //thread1.Start();
            await Task.Delay(-1); // Keep the main thread alive
        }
    }

}