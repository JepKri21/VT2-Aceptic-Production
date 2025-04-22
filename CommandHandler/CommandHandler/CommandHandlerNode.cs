using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;




namespace CommandHandlerNode
{
    class CommandHandler
    {

        //----------- AT SOME POINT WE SHOULD MAKE IT DETECT THE NUMBER OF STATIONS BASED ON WHEN NEW NAMES OF STATIONS ARE GIVEN----
        private static StationClass Filling = new StationClass();
        private static StationClass Stoppering = new StationClass();
        private static StationClass Vision = new StationClass();

        private static QueueClass PhysicalEndQueue = new QueueClass();

        StationClass[] allStations = { Filling, Stoppering, Vision };

        QueueClass[] allQueues = { PhysicalEndQueue };

        private List<ShuttleClass> allShuttles = new List<ShuttleClass>();

        private MQTTSubscriber mqttSubscriber;
        private MQTTPublisher mqttPublisher;

        //string brokerIP = "localhost";
        string brokerIP = "172.20.66.135";
        int port = 1883;
        private Dictionary<string, Action<string, string>> topicHandlers;


        public CommandHandler()
        {
            //Motion Functions of the filling line---------------------------------------------------------------
            //---------------------------------------------------------------------------------------------------

            double[] fillingAction = [0.660, 0.840];

            //Motion Functions of the stoppering line------------------------------------------------------------
            //---------------------------------------------------------------------------------------------------

            double[] stopperingAction = [0.660, 0.120];

            //Motion Functions of the vision line----------------------------------------------------------------
            //---------------------------------------------------------------------------------------------------

            double[] visionAction = [0.120, 0.120];

            //Passing positions to the queues------------------------------------------------------
            //---------------------------------------------------------------------------------------------------
            double[] fillingQueuePosX = { 0.420, 0.300, 0.180 };
            double[] fillingQueuePosY = { 0.900, 0.9, 0.9 };


            double[] stopperingQueuePosX = { 0.660, 0.660, 0.660 };
            double[] stopperingQueuePosY = { 0.360, 0.480, 0.600 };


            double[] visionQueuePosX = { 0.300, 0.420, 0.300 };
            double[] visionQueuePosY = { 0.300, 0.300, 0.060 };


            double[] endQueuePosX = { 0.060, 0.060, 0.060, 0.060 };
            double[] endQueuePosY = { 0.780, 0.660, 0.540, 0.420 };


            //Passing the motion functions to their classes and positions to the queues------------------------------------------------------
            //---------------------------------------------------------------------------------------------------
            Filling.stationTaskName = "FillingStation";         //We could add a publisher on the individual ESP32's to create a station based 
            Stoppering.stationTaskName = "StopperingStation";   //Might be possible on this website: randomnerdtutorials.com/esp32-ota-over-the-air-vs-code 
            Vision.stationTaskName = "VisionStation";           //to update the position of the station if we move it.
            PhysicalEndQueue.queueName = "EndQueue";

            Filling.passingStationAction(fillingAction);
            Stoppering.passingStationAction(stopperingAction);
            Vision.passingStationAction(visionAction);

            Filling.passingStationQueuePositions(fillingQueuePosX, fillingQueuePosY);
            Stoppering.passingStationQueuePositions(stopperingQueuePosX, stopperingQueuePosY);
            Vision.passingStationQueuePositions(visionQueuePosX, visionQueuePosY);

            PhysicalEndQueue.passingStationQueuePositions(endQueuePosX, endQueuePosY);

            InitializeTopicHandlers();
            InitializeMqttSubscriber();
            InitializeMqttPublisher();

            //Make a function that calls the IDs from the MQTT broker
        }

        private void InitializeTopicHandlers()
        {
            topicHandlers = new Dictionary<string, Action<string, string>>
            {
                { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/IDs", getIDsReturnShuttleClasses},
                { "AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/+/Command",  getCommand},
                { "AAU/Fiberstræde/Building14/FillingLine/Stations/+/StationStatus",  updateStationOccupancy}

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




        public async void getCommand(string topic, string message)
        {

            if (allShuttles == null || allShuttles.Count == 0)
            {
                // Exit the function early
                Console.WriteLine("Trying to update the commands,but there are no IDs in the CommandHandler");
                return;
            }

            var command = JsonSerializer.Deserialize<string>(message);
            //var command = message;
            // Split the topic into segments
            string[] segments = topic.Split('/');
            // Find the segment that starts with "xbot" and extract the numeric part
            string xbotSegment = segments.LastOrDefault(s => s.StartsWith("Xbot")) ?? throw new InvalidOperationException("Xbot segment not found");
            int xbotId = int.Parse(xbotSegment.Substring(4)); // Extract the numeric part after "xbot"

            bool shuttleFound = false;

            //Turn the Command in to the shuttle class with the correct ID
            foreach (ShuttleClass shuttle in allShuttles)
            {
                if (shuttle.shuttleID == xbotId)
                {
                    shuttle.addSingleTask(command);
                    Console.Write("Task added to");
                    Console.WriteLine(shuttle.shuttleID);
                    shuttleFound = true;
                    break;
                }
            }

            if (shuttleFound)
            {
                Console.WriteLine("Now gonna run the command checker");
                commandHandlingCheck();
                //Console.WriteLine("Now publishing runPathPlanner");
                //await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", "runPathPlanner");
            }
            else
            {
                Console.WriteLine("No shuttle with that ID was found in the CommandHandler");
            }
            

            //Console.WriteLine("Now publishing runPathPlanner");
            //await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", "runPathPlanner");


        }

        public async void getIDsReturnShuttleClasses(string topic, string message)
        {
            //Get the IDs and create the shuttle classes
            int[] newIDs = JsonSerializer.Deserialize<int[]>(message);


            //Check if any of the shuttle IDs are already created
            if (allShuttles.Count > 0)
            {
                foreach( ShuttleClass shuttle in allShuttles)
                {
                    for (int i = 0; i < newIDs.Length; i++)
                    {
                        if (newIDs[i] == shuttle.shuttleID)
                        {
                            Console.WriteLine($"Shuttle with ID{newIDs[i]} already exists");
                            newIDs[i] = 0;

                        }
                    }
                }
                
            }


            foreach (int num in newIDs)
            {
                if (num > 0)
                {
                    var instance = new ShuttleClass();
                    instance.shuttleID = num;
                    allShuttles.Add(instance);
                    Console.WriteLine($"Shuttle with ID {num} has been created");
                }
                

            }



            Console.WriteLine("IDs have now been checked and shuttle classes have been created");
            Console.WriteLine(string.Join(", ", allShuttles));

            foreach (ShuttleClass shuttle in allShuttles)
            {
                Console.Write(shuttle.shuttleID);
            }

        }

        public async void updateStationOccupancy(string topic, string message)
        {

            if (allShuttles == null || allShuttles.Count == 0)
            {
                // Exit the function early
                Console.WriteLine("Trying to update the station occupancy, but there are no IDs in the CommandHandler");
                return;
            }

            //If the topic equal a name of one of the stations, then we change the occupancy
            // Split the topic into segments
            string[] segments = topic.Split('/');
            //Check for a topic that ends with station (assuming that it is either FillingStation, StopperingStation or VisionStation
            string stationSegment = segments.FirstOrDefault(s => s.EndsWith("Station"));

            bool stationFound = false;

            foreach (StationClass station in allStations)
            {
                if (station.stationTaskName == stationSegment)
                {
                    station.stationStatus = message;
                    stationFound = true;
                    break;
                }
            }

            if (stationFound)
            {
                commandHandlingCheck();
                //Console.WriteLine("Now publishing runPathPlanner");
                //await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/PathPlan/Status", "runPathPlanner");
            }
            else
            {
                Console.WriteLine("No station with that name was found in the CommandHandler");
            }
            

            

        }


        private void commandHandlingCheck()
        //The problem with this one is that if you add multiple tasks at the same time, then you don't have a trigger 
        // for starting the next task when the first is done
        {


            foreach (ShuttleClass shuttle in allShuttles)
            {
                //Console.Write($"Shuttle {shuttle.shuttleID}");
                //Console.WriteLine(string.Join(", ", shuttle.Tasks));


                if (shuttle.Tasks.Count > 0)
                //If their buffer is empty, meaning they are idle and they have a task to do
                //If we don't rely on the buffer count here, then I have to assume that the decision tree will take the buffer into consideration
                {


                    // If a task of a shuttle matches with a station task, then we can run the movements (or add to queue)
                    StationClass matchingTaskStation = allStations.FirstOrDefault(station => station.stationTaskName == shuttle.Tasks[0]);

                    if (matchingTaskStation != null)
                    {

                        matchingTaskStation.addIDToQueue(shuttle.shuttleID); //We add it to the queue because we know it has to do this task

                        if (matchingTaskStation.stationStatus == "idle") //Then we say that if the relevant station is not occupied, then we can:
                        {
                            if (shuttle.shuttleInQueue != null) //IF it is in a queue
                            {
                                //Remove it from the queue
                                QueueClass inQueue = allQueues.FirstOrDefault(queue => queue.queueName == shuttle.shuttleInQueue);
                                inQueue.removeIDFromList(shuttle.shuttleID);
                                shuttle.shuttleInQueue = null; //I'm not sure this part works

                            }
                            //If it is not in a queue, then we can just run the movement of the station
                            matchingTaskStation.runMovement(matchingTaskStation.stationQueue[0]); //We run the movement of the first in the queue
                            //matchingTaskStation.stationOccupied = true; //Then we say that the station is occupied (I think the station itself should tell us)
                            matchingTaskStation.stationStatus = "_running";
                            //shuttle.replaceFirstTask(matchingTaskStation.runningTaskName); //Then we replace the shuttle's task with the stations running task name
                            shuttle.removeTask(matchingTaskStation.stationTaskName); //We should have sent the coordinates now, so we can just remove the task
                            matchingTaskStation.removeIDFromQueue(matchingTaskStation.stationQueue[0]); //Then we remove that shuttle from the station queue
                        }

                    }
                    /*
                    else
                    {
                        StationClass matchingRunningTaskOfStation = allStations.FirstOrDefault(station => station.runningTaskName == shuttle.Tasks[0]);
                        if (matchingRunningTaskOfStation != null)
                        {
                            matchingRunningTaskOfStation.currentID = 0; //Just saying that the shuttle of the station is 0, meaning there is no shuttle
                            //matchingRunningTaskOfStation.stationOccupied = false; //Setting the occupancy to false //Station should do that itself
                            shuttle.removeTask(matchingRunningTaskOfStation.runningTaskName); //Removing the task name from the shuttle

                            //INSTEAD OF HAVING THE RUNNING TASK, PROBABLY JUST REMOVE THE TASK IMMEDIATELY 
                        }
                    }
                    */

                    if (shuttle.Tasks.Count > 0)
                    {
                        //Now we check if the task is a queue instead
                        QueueClass matchingQueue = allQueues.FirstOrDefault(queue => queue.queueName == shuttle.Tasks[0]);
                        if (matchingQueue != null) //If a queue name matches with the task of a shuttle
                        {
                            matchingQueue.addIDToList(shuttle.shuttleID); //if it is, then we add it to that queue, then we update positions above
                            shuttle.shuttleInQueue = matchingQueue.queueName;
                            shuttle.removeTask(matchingQueue.queueName);
                        }
                    }



                }
            }

            foreach (StationClass station in allStations)
            {
                station.updateStationQueue();
            }

            foreach (QueueClass queue in allQueues)
            {
                queue.updateQueuePositions();
            }
        }



        public static async Task Main(string[] args) // Change return type to Task
        {

            CommandHandler client = new CommandHandler();
            //Do I just add the command function that checks all the commands?

            await Task.Delay(-1); // Keep the main thread alive
        }

    }
}