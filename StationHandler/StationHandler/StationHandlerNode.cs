
using System.Text.Json;

namespace StationHandlerNode
{

    class StationHandler
    {

        private MQTTSubscriber mqttSubscriber;
        private MQTTPublisher mqttPublisher;

        string brokerIP = "localhost";
        //string brokerIP = "172.20.66.135";
        int port = 1883;
        private Dictionary<string, Action<string, string>> topicHandlers;


        Dictionary<string, string> allStationStatuses = new Dictionary<string, string>();

        private void InitializeTopicHandlers()
        {
            topicHandlers = new Dictionary<string, Action<string, string>>
            {
                { "AAU/Fiberstræde/Building14/FillingLine/Stations/+/StationStatus", updateStationStatus},
                
            };
        }

        private async void InitializeMqttSubscriber()
        {
            mqttSubscriber = new MQTTSubscriber(brokerIP, port);
            mqttSubscriber.MessageReceived += messageHandler;
            await mqttSubscriber.StartAsync();

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
        public StationHandler()
        {


            InitializeTopicHandlers();
            InitializeMqttSubscriber();
            InitializeMqttPublisher();
        }




        private void updateStationStatus(string topic, string message)
        {
            string[] segments = topic.Split('/');
            //Check for a topic that ends with station (assuming that it is either FillingStation, StopperingStation or VisionStation
            string stationSegment = segments.FirstOrDefault(s => s.EndsWith("Station"));

            if (allStationStatuses.ContainsKey(stationSegment))
            {
                allStationStatuses[stationSegment] = message;  // Update value
                Console.WriteLine($"Updated {stationSegment} with status {message}");
            }
            else
            {
                allStationStatuses.Add(stationSegment, message);  // Add new entry
                Console.WriteLine($"Added {stationSegment} with status {message}");
            }

        }


        //Read Xbot positions
        //The xbot position that matches the station position (Which we should get from the ESP maybe?)
        //Make the ESP send the station coordiantes
        //Make sure the ESP DOESN'T send "idle" back. This program is meant to do that now

        public static async Task Main(string[] args) // Change return type to Task
        {

            StationHandler client = new StationHandler();
            
            await Task.Delay(-1); // Keep the main thread alive
        }

    }
}