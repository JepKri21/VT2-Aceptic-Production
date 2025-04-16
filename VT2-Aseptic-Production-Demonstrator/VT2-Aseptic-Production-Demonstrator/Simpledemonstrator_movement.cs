using PMCLIB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Collections.Specialized.BitVector32;

namespace VT2_Aseptic_Production_Demonstrator
{
    internal class realtime_movement_test
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //this class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.
        private static XBotCommands _xbotCommand = new XBotCommands();

        int selector = 2;


        private static MotionsFunctions MF = new MotionsFunctions();

        private static Station_Class Filling = new Station_Class();
        private static Station_Class Stoppering = new Station_Class();
        private static Station_Class Vision = new Station_Class();

        private static Queue_Class PhysicalEndQueue = new Queue_Class();

        private static ShuttleClass Shuttle1 = new ShuttleClass();
        private static ShuttleClass Shuttle2 = new ShuttleClass();
        private static ShuttleClass Shuttle3 = new ShuttleClass();
        private static ShuttleClass Shuttle4 = new ShuttleClass();


        //REMEMBER TO ADD YOUR SHUTTLE HERE
        ShuttleClass[] allShuttles = { Shuttle1, Shuttle2, Shuttle3 };

        Station_Class[] allStations = { Filling, Stoppering, Vision };

        Queue_Class[] allQueues = { PhysicalEndQueue};

        public int setSelectorOne()
        {
            return selector;
        }

        public void runRealtimeMovementTest(int[] xbot_ids)
        {
            selector = 1;
            Console.Clear();
            Console.WriteLine(" Realtime Movement Test");
            Console.WriteLine("0    Return ");
            Console.WriteLine("1    Setting Initial Positions ");
            Console.WriteLine("2    Run Program");
            Console.WriteLine("3    Queue test");

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {

                case '0':
                    selector = 1;
                    break;

                case '1':
                    //Starting the initial position process--------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    //int[] initialXbots = { 1, 2, 3 }; //You could make this dynamic so that you don't have to specify how many you are using.
                    //double[] initalX = { 0.600, 0.360, 0.120, 0.120 };
                    //double[] initialY = { 0.840, 0.840, 0.840, 0.600 };
                    //
                    ////Drive to initial positions
                    //_xbotCommand.AutoDrivingMotionSI(initialXbots.Length, ASYNCOPTIONS.MOVEALL, initialXbots, initalX, initialY);
                    //
                    //for (int i = 0; i <= initialXbots.Length; i++)
                    //{
                    //    MF.RotateMotion(0, i, 0, "CW");
                    //}

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
                    double[] fillingQueuePosY = { 0.900, 0.900, 0.900 };
                    Filling.setQueueDirection = "XY";

                    double[] stopperingQueuePosX = { 0.660, 0.660, 0.660};
                    double[] stopperingQueuePosY = { 0.360, 0.480, 0.600};
                    Stoppering.setQueueDirection = "XY";

                    double[] visionQueuePosX = { 0.300, 0.420, 0.300};
                    double[] visionQueuePosY = { 0.300, 0.300, 0.060};
                    Vision.setQueueDirection = "XY";

                    double[] endQueuePosX = { 0.060, 0.060, 0.060, 0.060 };
                    double[] endQueuePosY = { 0.780, 0.660, 0.540, 0.420 };
                    PhysicalEndQueue.setQueueDirection = "XY";


                    //Passing the motion functions to their classes and positions to the queues------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Filling.stationTaskName = "Filling";
                    Stoppering.stationTaskName = "Stoppering";
                    Vision.stationTaskName = "Vision";
                    PhysicalEndQueue.queueName = "Done";

                    Filling.passingStationAction(fillingAction);
                    Stoppering.passingStationAction(stopperingAction);
                    Vision.passingStationAction(visionAction);

                    Filling.passingStationQueuePositions(fillingQueuePosX, fillingQueuePosY);
                    Stoppering.passingStationQueuePositions(stopperingQueuePosX, stopperingQueuePosY);
                    Vision.passingStationQueuePositions(visionQueuePosX, visionQueuePosY);

                    PhysicalEndQueue.passingStationQueuePositions(endQueuePosX, endQueuePosY);

                    //Adding tasks to the shuttles-------------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Shuttle1.shuttleID = 1;
                    Shuttle2.shuttleID = 2;
                    Shuttle3.shuttleID = 3;
                    Shuttle4.shuttleID = 4;

                    string[] tasksArray = { "Filling", "Stoppering", "Vision", "Done", "Filling", "Stoppering", "Vision", "Done", "Filling", "Stoppering", "Vision", "Done" };


                    foreach (ShuttleClass shuttle in allShuttles)
                    {
                        shuttle.insertTaskArray(tasksArray);

                    }

                    selector = 2;
                    break;


                case '2':
                    while (true)
                    {
                        foreach (Station_Class station in allStations)
                        {
                            station.updateStationQueue();
                        }

                        foreach (Queue_Class queue in allQueues)
                        {
                            queue.updateQueuePositions();
                        }

                        foreach (ShuttleClass shuttle in allShuttles)
                        {

                            if (shuttle.bufferMotionCount == 0 && shuttle.Tasks.Count > 0) //If their buffer is empty, meaning they are idle and they have a task to do
                            {


                                // If a task of a shuttle matches with a station task, then we can run the movements (or add to queue)
                                Station_Class matchingTaskStation = allStations.FirstOrDefault(station => station.stationTaskName == shuttle.Tasks[0]);

                                if (matchingTaskStation != null)
                                {

                                    matchingTaskStation.addIDToQueue(shuttle.shuttleID); //We add it to the queue because we know it has to do this task

                                    if (matchingTaskStation.stationOccupied == false) //Then we say that if the relevant station is not occupied, then we can:
                                    {
                                        if (shuttle.shuttleInQueue != null)
                                        {
                                            //Remove it from the queue
                                            Queue_Class inQueue = allQueues.FirstOrDefault(queue => queue.queueName == shuttle.shuttleInQueue);
                                            inQueue.removeIDFromList(shuttle.shuttleID);
                                            shuttle.shuttleInQueue = null; //I'm not sure this part works

                                        }

                                        matchingTaskStation.runMovement(matchingTaskStation.stationQueue[0]); //We run the movement of the first in the queue
                                        matchingTaskStation.stationOccupied = true; //Then we say that the station is occupied
                                        shuttle.replaceFirstTask(matchingTaskStation.runningTaskName); //Then we replace the shuttle's task with the stations running task name
                                        matchingTaskStation.removeIDFromQueue(matchingTaskStation.stationQueue[0]); //Then we remove that shuttle from the station queue
                                    }

                                }
                                else
                                {
                                    Station_Class matchingRunningTaskOfStation = allStations.FirstOrDefault(station => station.runningTaskName == shuttle.Tasks[0]);
                                    if (matchingRunningTaskOfStation != null)
                                    {
                                        matchingRunningTaskOfStation.currentID = 0; //Just saying that the shuttle of the station is 0, meaning there is no shuttle
                                        matchingRunningTaskOfStation.stationOccupied = false; //Setting the occupancy to false
                                        shuttle.removeTask(matchingRunningTaskOfStation.runningTaskName); //Removing the task name from the shuttle
                                    }
                                }
                                //Now we check if the task is a queue instead
                                Queue_Class matchingQueue = allQueues.FirstOrDefault(queue => queue.queueName == shuttle.Tasks[0]);
                                if (matchingQueue != null) //If a queue name matches with the task of a shuttle
                                {
                                    matchingQueue.addIDToList(shuttle.shuttleID); //if it is, then we add it to that queue, then we update positions above
                                    shuttle.shuttleInQueue = matchingQueue.queueName;
                                    shuttle.removeTask(matchingQueue.queueName);
                                }


                            }
                        }

                    }
                    break;

                case '3':



                    break;

            }




        }

    }
}

