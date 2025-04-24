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
        private MQTTSubscriber mqttSubscriber = null!;
        private MQTTPublisher mqttPublisher = null!;
        private Dictionary<string, Action<string, string>> topicHandlers = null!;
        private int[] xbotsID = null!;
        private Dictionary<int, List<double[]>> trajectories = new();
        private Dictionary<int, double[]> targetPositions = new();
        private Dictionary<int, double[]> positions = new();
        string brokerIP = "localhost";
        //string brokerIP = "172.20.66.135";
        int port = 1883;
        private int xbotSize = 12;
        private int width = 72;
        private int height = 96;
        private Pathfinding.grid gridGlobal;
        private Pathfinding pathfinder;
        private List<(int, double[], double[])> xBotID_From_To = new();
        private double[] TargetPositionStation;

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
                    { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/TargetPosition", getTargetPostion },
                    { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/Position", getPostion },
                    { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", HandleStatus },
                    { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/SubCMD", HandleSubCMD}
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

        #region GetTargetAndPostion
        public async void getTargetPostion(string topic, string message)
        {
            Console.WriteLine($"Received target position message on topic {topic}: {message}");

            await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Stop", "Stop");

            var targetPosition = JsonSerializer.Deserialize<double[]>(message) ?? throw new InvalidOperationException("TargetPosition is null");

            if (targetPosition.Length != 3)
            {
                throw new InvalidOperationException("TargetPosition must contain exactly 3 values: x, y, and RZ.");
            }
            double thresholdValue = 1;
            string[] segments = topic.Split('/');
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot")) ?? throw new InvalidOperationException("xbot segment not found");
            int xbotID = int.Parse(xbotSegment.Substring(4));
            targetPositions[xbotID] = targetPosition;
            if (positions.ContainsKey(xbotID))
            {
                var currentPosition = positions[xbotID];
                // Updated condition to include a threshold value for inconsistencies              

                if (Math.Abs(currentPosition[2] - targetPosition[2]) < thresholdValue)
                {
                        Console.WriteLine($"Rz value difference is less than the threshold for xbotID {xbotID}.");
                        Console.WriteLine($"Updating target position for xbotID {xbotID}: {string.Join(", ", targetPosition)}");

                        UpdateFromAndTo(xbotID);
                        //await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", "runPathPlanner");
                }                
                else
                {
                    // Ensure the rotation target is included if applicable
                    RotateCommand(xbotID);
                    await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", "runPathPlanner");
                    await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{xbotID}/SubCMD", "Rotate");
                }
            }

            
        }

        private void RotateCommand(int xbotID)
        {
            Console.WriteLine("Sending rotate command to xbot...");

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

            double[] targetPositionsRotation = FindClosestCenter(positions[xbotID], flywayCenters);
            double[]? currentPosition = positions.ContainsKey(xbotID) ? positions[xbotID] : null;
            // Update xBotID_From_To with the rotation target

            double[]? currentXY = currentPosition != null ? new double[] { currentPosition[0], currentPosition[1] } : null;


            // Add the rotation target to xBotID_From_To           

            
            var existingEntry = xBotID_From_To.FirstOrDefault(entry => entry.Item1 == xbotID);

            if (existingEntry != default)
            {
                double[] updatedFrom = currentPosition ?? existingEntry.Item2;
                double[] updatedTo = targetPositionsRotation ?? existingEntry.Item3;
                xBotID_From_To.Remove(existingEntry);
                xBotID_From_To.Add((xbotID, updatedFrom, updatedTo));
            }
            else
            {
                xBotID_From_To.Add((xbotID, currentPosition ?? Array.Empty<double>(), targetPositionsRotation ?? Array.Empty<double>()));
            }
            //PrintXBotIDFromTo();

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

        public void getPostion(string topic, string message)
        {
            //Console.WriteLine($"Received message on topic {topic}: {message}");

            try
            {
                var position = JsonSerializer.Deserialize<double[]>(message) ?? throw new InvalidOperationException("Position is null");
                if (position.Length < 2)
                {
                    throw new InvalidOperationException("Position must contain at least 2 values: x and y.");
                }

                string[] segments = topic.Split('/');
                string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot", StringComparison.OrdinalIgnoreCase))
                                     ?? throw new InvalidOperationException("xbot segment not found");
                int xbotId = int.Parse(xbotSegment.Substring(4));

                //Console.WriteLine($"Updating position for xbotID {xbotId}: {string.Join(", ", position)}");

                positions[xbotId] = position;

                UpdateFromAndTo(xbotId);
            }
            catch (Exception ex)    
            {
                Console.WriteLine($"Error processing position message: {ex.Message}");
            }
        }

        private async void UpdateFromAndTo(int xbotID)
        {
            
            //Console.WriteLine($"Checking UpdateFromAndTo for xbotID {xbotID}");
            //Console.WriteLine($"positions contains: {string.Join(", ", positions.Keys)}");
            //Console.WriteLine($"targetPostions contains: {string.Join(", ", targetPositions.Keys)}");

            double[]? currentPosition = positions.ContainsKey(xbotID) ? positions[xbotID] : null;
            double[]? targetPosition = targetPositions.ContainsKey(xbotID) ? targetPositions[xbotID] : null;

            double[]? currentXY = currentPosition != null ? new double[] { currentPosition[0], currentPosition[1] } : null;
            double[]? targetXY = targetPosition != null ? new double[] { targetPosition[0], targetPosition[1] } : null;

            var existingEntry = xBotID_From_To.FirstOrDefault(entry => entry.Item1 == xbotID);

            if (existingEntry != default)
            {
                double[] updatedFrom = currentXY ?? existingEntry.Item2;
                double[] updatedTo = targetXY ?? existingEntry.Item3;

                xBotID_From_To.Remove(existingEntry);
                xBotID_From_To.Add((xbotID, updatedFrom, updatedTo));
            }
            else
            {
                xBotID_From_To.Add((xbotID, currentXY ?? Array.Empty<double>(), targetXY ?? Array.Empty<double>()));
            }

            

            
        }

        public void PrintXBotIDFromTo()
        {
            Console.WriteLine($"positions contains: {string.Join(", ", positions.Keys)}");
            Console.WriteLine($"targetPostions contains: {string.Join(", ", targetPositions.Keys)}");
            Console.WriteLine("Current xBotID_From_To Entries:");
            foreach (var entry in xBotID_From_To)
            {
                string from = entry.Item2 != null ? $"[{string.Join(", ", entry.Item2)}]" : "null";
                string to = entry.Item3 != null ? $"[{string.Join(", ", entry.Item3)}]" : "null";
                Console.WriteLine($"xbotID: {entry.Item1}, From: {from}, To: {to}");
            }
        }
        #endregion

        #region MessageHandler
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
            if (message == "runPathPlanner")
            {
                Console.WriteLine("Running Path Planner");
                //PrintXBotIDFromTo();
                Console.WriteLine($"{xBotID_From_To.Count}");
                for (int i = 0; i < xBotID_From_To.Count; i++)
                {
                    var (xbotID, from, to) = xBotID_From_To[i];

                    if (to == null || to.Length == 0)
                    {
                        if (positions.ContainsKey(xbotID))
                        {
                            xBotID_From_To[i] = (xbotID, from, from);
                        }
                        else
                        {
                            Console.WriteLine($"Error: Current position for xbotID {xbotID} is not available.");
                            return;
                        }
                    }
                }
                //PrintXBotIDFromTo();

                trajectories = pathfinder.pathPlanRunner(gridGlobal, xBotID_From_To, xbotSize)
                    .ToDictionary(item => item.Item1, item => item.Item2);

                foreach (var trajectory in trajectories)
                {
                    int xbotId = trajectory.Key;
                    //gridGlobal.SaveWalkablePointsToFile(xbotId, "walkable_points.txt");
                    if (targetPositions.ContainsKey(xbotId))
                    {
                        var targetPosition = targetPositions[xbotId];
                        if (positions.ContainsKey(xbotId))
                        {
                            var currentPosition = positions[xbotId];

                            // Check if the Rz values match
                            if (Math.Abs(currentPosition[2] - targetPosition[2]) < 1) // Threshold for Rz comparison
                            {
                                // Append the target position as the last point in the trajectory
                                trajectory.Value.Add(targetPosition);
                            }
                            else
                            {
                                Console.WriteLine($"Rz values do not match for xbotID {xbotId}. Target position will not be added.");
                            }
                        }
                    }

                    var trajectoryMessage = JsonSerializer.Serialize(trajectory.Value.Select(t => new double[] { t[0], t[1] }).ToList());
                    await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{xbotId}/Trajectory", trajectoryMessage);
                }

                // Add a delay before sending the "ready" message
                await Task.Delay(500); // Delay for 1 second
                await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", "ready");
            }
        }

        private async void HandleSubCMD(string topic, string message)
        {
            Console.WriteLine($"Received message on topic {topic}: {message}");

            if (message == "RotationDone")
            {
                Console.WriteLine("I am here");
                //await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Stop", "Stop");
                string[] segments = topic.Split('/');
                string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot")) ?? throw new InvalidOperationException("xbot segment not found");
                int xbotID = int.Parse(xbotSegment.Substring(4));
                Console.WriteLine($"Checking UpdateFromAndTo for xbotID {xbotID}");
                Console.WriteLine($"{targetPositions[xbotID]}");

                await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", "runPathPlanner");
            }
        }
        #endregion

        public static async Task Main(string[] args)
        {
            PathPlaningNode client = new PathPlaningNode();
            await Task.Delay(-1);
        }
        
    }

}