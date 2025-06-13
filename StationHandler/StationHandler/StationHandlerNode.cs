
using StationHandler;
using System;
using System.Text.Json;

namespace StationHandlerNode
{

    class StationHandler
    {

        private MQTTSubscriber mqttSubscriber;
        private MQTTPublisher mqttPublisher;

        //string brokerIP = "localhost";
        string brokerIP = "172.20.66.135";
        int port = 1883;
        private Dictionary<string, Action<string, string>> topicHandlers;

        //make a dictionary or list that both contains the name of the station, the status (maybe), the station position and the current Xbot


        private List<StationInformation> allStations = new List<StationInformation>();

        private List<ShuttleInformation> allShuttles = new List<ShuttleInformation>();

        private bool alreadySentStatus = false;

        private void InitializeTopicHandlers()
        {
            topicHandlers = new Dictionary<string, Action<string, string>>
            {
                { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/Position", updateXbotPositions},
                { "AAU/Fiberstræde/Building14/FillingLine/Stations/+/StationPosition", updateStationPositions},
                { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/State", updateXbotStatus},
                { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/Objective", updateXbotObjective}

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



        private void updateStationPositions(string topic, string message)
        {
            //receives a station name and position, updates the dictionary/class 
            // Don't create a new station if it matches with a name of one of the other stations
            string[] segments = topic.Split('/');// Split the topic into segments
            //Check for a topic that ends with station (assuming that it is either FillingStation, StopperingStation or VisionStation
            string stationSegment = segments.FirstOrDefault(s => s.EndsWith("Station"));


            var existingStation = allStations.FirstOrDefault(station => station._stationName == stationSegment);

            if (existingStation != null)
            {
                Console.WriteLine($"Updating existing station {existingStation._stationName} with position: {message}");
                existingStation._stationPosition = JsonSerializer.Deserialize<double[]>(message); // Update existing
            }
            else
            {
                Console.WriteLine($"Adding a new station {stationSegment} with position: {message}");
                allStations.Add(new StationInformation { _stationName = stationSegment, _stationPosition = JsonSerializer.Deserialize<double[]>(message) }); // Add new
            }

        }

        private void updateXbotPositions(string topic, string message)
        {
            string[] segments = topic.Split('/');
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot")) ?? throw new InvalidOperationException("Xbot segment not found");
            int xbotId = int.Parse(xbotSegment.Substring(4));


            var existingShuttle = allShuttles.FirstOrDefault(shuttle => shuttle._shuttleID == xbotId);

            if (existingShuttle != null)
            {
                Console.WriteLine($"Updating existing shuttle {existingShuttle._shuttleID} with position: {message}");
                double[] position = JsonSerializer.Deserialize<double[]>(message);
                existingShuttle._shuttlePosition = position.Take(2).ToArray(); // Update existing
            }
            else
            {
                Console.WriteLine($"Adding a new shuttle {xbotId} with position: {message}");
                double[] position = JsonSerializer.Deserialize<double[]>(message);
                allShuttles.Add(new ShuttleInformation { _shuttleID = xbotId, _shuttlePosition = position.Take(2).ToArray() }); // Add new
            }

            Console.WriteLine("Now checking all shuttle positions");
            checkingAllXbotPositions(topic, message);


        }

        private void updateXbotStatus(string topic, string message)
        {
            string[] segments = topic.Split('/');
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot")) ?? throw new InvalidOperationException("Xbot segment not found");
            int xbotId = int.Parse(xbotSegment.Substring(4));

            var existingShuttle = allShuttles.FirstOrDefault(shuttle => shuttle._shuttleID == xbotId);

            if (existingShuttle != null)
            {
                Console.WriteLine($"Updating existing shuttle {existingShuttle._shuttleID} with status: {message}");
                existingShuttle._shuttleStatus = JsonSerializer.Deserialize<int>(message); // Update existing
            }
            else
            {
                Console.WriteLine($"Adding a new shuttle {xbotId} with status: {message}");
                allShuttles.Add(new ShuttleInformation { _shuttleID = xbotId, _shuttleStatus = JsonSerializer.Deserialize<int>(message) }); // Add new
            }

        }

        private void updateXbotObjective(string topic, string message)
        {
            string[] segments = topic.Split('/');
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot")) ?? throw new InvalidOperationException("Xbot segment not found");
            int xbotId = int.Parse(xbotSegment.Substring(4));


            var existingShuttle = allShuttles.FirstOrDefault(shuttle => shuttle._shuttleID == xbotId);

            if (existingShuttle != null)
            {
                Console.WriteLine($"Updating existing shuttle {existingShuttle._shuttleID} with objective: {message}");
                existingShuttle._shuttleObjective = message; // Update existing
            }
            else
            {
                Console.WriteLine($"Adding a new shuttle {xbotId} with objective: {message}");
                allShuttles.Add(new ShuttleInformation { _shuttleID = xbotId, _shuttleObjective = message }); // Add new
            }


        }


        private async void checkingAllXbotPositions(string topic, string message)
        {
            if (allShuttles.Count == 0 || allStations.Count == 0)
            {
                Console.WriteLine("There are either no shuttles or no stations (or both) defined");
                allShuttles.ForEach(shuttle => Console.WriteLine(shuttle._shuttleID));
                allStations.ForEach(station => Console.WriteLine(station._stationName));
                return;
            }

            foreach (StationInformation station in allStations)
            {
                bool objectiveFlag = false;
                foreach (ShuttleInformation shuttle in allShuttles)
                {

                    //Console.WriteLine("_" + station._stationName == shuttle._shuttleObjective);
                    //Console.WriteLine(shuttle._shuttleStatus == 3);
                    //Console.WriteLine(shuttle._shuttlePosition[0] == station._stationPosition[0]);
                    //Console.WriteLine(shuttle._shuttlePosition[1] == station._stationPosition[1]);
                    //Console.WriteLine(shuttle._shuttleID != station._occupyingXbot);

                    if ("_"+station._stationName == shuttle._shuttleObjective && shuttle._shuttleStatus == 3 && shuttle._shuttlePosition[0] == station._stationPosition[0] && shuttle._shuttlePosition[1] == station._stationPosition[1] && shuttle._shuttleID != station._occupyingXbot)
                    {
                        //Maybe change the objective to something else or nothing, but also add the shuttle to the station occupancy, also publish "running"
                        station._occupyingXbot = shuttle._shuttleID;
                        shuttle._shuttleObjective = "No Objective"; 
                        await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/{station._stationName}/StationStatus", "running");
                        await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/{"Xbot"+ shuttle._shuttleID}/Objective", shuttle._shuttleObjective);
                        alreadySentStatus = false;
                        objectiveFlag = true;

                    }
                }

                bool noShuttleAtStation = !allShuttles.Any(shuttle => shuttle._shuttlePosition.SequenceEqual(station._stationPosition));

                if (objectiveFlag == false && noShuttleAtStation && alreadySentStatus == false) //AKA no shuttle has that objective //Fuck, we also have to check that no other shuttle is in the station position
                {
                    //Remove station occupancy
                    alreadySentStatus = true;
                    Console.WriteLine($"No shuttles with objectives equal to {"_"+station._stationName} AND no shuttles are at the station position: {station._stationPosition}");
                    station._occupyingXbot = 0;
                    await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/{station._stationName}/StationStatus", "idle");

                }


            }

        }


        public static async Task Main(string[] args) // Change return type to Task
        {

            StationHandler client = new StationHandler();
            
            await Task.Delay(-1); // Keep the main thread alive
        }

    }
}