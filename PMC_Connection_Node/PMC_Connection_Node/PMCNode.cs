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
        private MQTTSubscriber mqttSubscriber;
        private MQTTPublisher mqttPublisher;
        string brokerIP = "localhost";
        int port = 1883;
        int[] xbotsID;
        Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();

        public PMC_Connection_Node()
        {

            CONNECTIONSTATUS status = connectionHandler.ConnectAndGainMastership();
            Console.WriteLine(status);
            

            InitializeMqttSubscriber();
            InitializeMqttPublisher();
            XBotIDs xBotIDs = _xbotCommand.GetXBotIDS();
            xbotsID = xBotIDs.XBotIDsArray;
            SendPostions();
        }

        private async void InitializeMqttSubscriber()
        {
            mqttSubscriber = new MQTTSubscriber(brokerIP, 1883, "Acopos6D/#");
            mqttSubscriber.MessageReceived += messageHandler;
            await mqttSubscriber.StartAsync();
        }

        private async void InitializeMqttPublisher()
        {
            mqttPublisher = new MQTTPublisher(brokerIP, port);
            await mqttPublisher.StartAsync();
        }

        private async void SendPostions()
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
        public async void messageHandler(string topic, string message)
        {
            Console.WriteLine($"Received message on topic {topic}: {message}");
            try
            {
                //var targetPosition = JsonSerializer.Deserialize<double[]>(message);
                string[] segments = topic.Split('/');
                Console.WriteLine($"Received messages topics is: {segments[2]}");
                switch ( segments[2])
                {
                    case "status":
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
                            SendPostions();
                        }
                        else
                        {
                            Console.WriteLine("Xbot is not ready");
                            PrintTrajectories();
                        }
                        break;
                    default:
                        switch (segments[3]) // segments[2] is the third segment of the topic can be changed to the desired topic
                        {
                            case "targetPosition":
                                Console.WriteLine($"Recived Target Postions");
                                break;
                            case "trajectory":
                                GetTrajectories(segments, message);
                                

                                RunTrajectory();
                                break;
                            case "position":

                                break;
                            default:
                                Console.WriteLine($"Unhandled topic: {topic}");
                                break;
                        }
                        break;
                }
                // Handle different topics
               
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize message: {message}");
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }


        public async Task PublishPositionAsync(int xbotId, double[] position)
        {
            var message = JsonSerializer.Serialize(position);
            await mqttPublisher.PublishMessageAsync($"Acopos6D/xbots/xbot{xbotId}/position", message);
            Console.WriteLine($"Published position for xbot {xbotId}: {string.Join(", ", position)}");
        }


        public async void GetTrajectories(string[] segments, string message )
        {
            // Find the segment that starts with "xbot" and extract the numeric part
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

                            _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                        }
                        lock (trajectories)
                        {
                            trajectories.Remove(xbotID);
                        }

                    }));
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        public static async Task Main(string[] args) // Change return type to Task
        {
            
            PMC_Connection_Node client = new PMC_Connection_Node();


            //Thread thread1 = new Thread(new ThreadStart(client.Run));
            //thread1.Start();
            await Task.Delay(-1); // Keep the main thread alive
        }
    }
    

    
}