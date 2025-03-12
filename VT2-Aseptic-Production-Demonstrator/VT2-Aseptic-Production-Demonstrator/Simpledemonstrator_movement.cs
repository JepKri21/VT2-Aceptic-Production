using PMCLIB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VT2_Aseptic_Production_Demonstrator
{
    internal class realtime_movement_test
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //this class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.
        private static XBotCommands _xbotCommand = new XBotCommands();
        WaitUntilTriggerParams CMD_params = new WaitUntilTriggerParams();


        int selector = 2;

        
        private static MotionsFunctions MF = new MotionsFunctions();

        private static Station_Class Filling = new Station_Class();
        private static Station_Class Stoppering = new Station_Class();
        private static Station_Class Vision = new Station_Class();

        private static Queue_Class PhysicalFillingQueue = new Queue_Class();
        private static Queue_Class PhysicalStopperingQueue = new Queue_Class();
        private static Queue_Class PhysicalVisionQueue = new Queue_Class();
        private static Queue_Class PhysicalEndQueue = new Queue_Class();

        private static ShuttleClass Shuttle1 = new ShuttleClass();
        private static ShuttleClass Shuttle2= new ShuttleClass();
        private static ShuttleClass Shuttle3 = new ShuttleClass();
        private static ShuttleClass Shuttle4 = new ShuttleClass();

        ShuttleClass[] allShuttles = { Shuttle1, Shuttle2, Shuttle3, Shuttle4 };


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
            Console.WriteLine("3    ");

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {

                case '0':
                    selector = 1;
                    break;

                case '1':
                    //Starting the initial position process--------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    int[] initialXbots = { 1, 2, 3, 4 };
                    double[] initalX = { 0.600, 0.360, 0.120, 0.120 };
                    double[] initialY = { 0.840, 0.840, 0.840, 0.600 };

                    //Drive to initial positions
                    _xbotCommand.AutoDrivingMotionSI(4, ASYNCOPTIONS.MOVEALL, initialXbots, initalX, initialY);

                    //Motion Functions of the physical filling queue-----------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int[]> physicalFillingQueuePos = (ids) =>
                    {
                        double[] fillingQueuePosX = { 0.540, 0.420, 0.300, 0.180 };
                        double[] fillingQueuePosY = { 0.900, 0.900, 0.900, 0.900 };


                        for (int i = 0; i < ids.Length; i++)
                        {
                            MF.LinarMotion(0, ids[i], fillingQueuePosX[i], fillingQueuePosY[i], "YX");
                        }
                    };

                    //Motion Functions of the physical temp queue--------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int[]> physicalStopperingQueuePos = (ids) =>
                    {
                        double[] stopperingQueuePosX = { 0.660, 0.660, 0.660, 0.660 };
                        double[] stopperingQueuePosY = { 0.300, 0.420, 0.540, 0.660 };


                        for (int i = 0; i < ids.Length; i++)
                        {
                            MF.LinarMotion(0, ids[i], stopperingQueuePosX[i], stopperingQueuePosY[i], "YX");
                        }
                    };

                    //Motion Functions of the physical temp queue--------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int[]> physicalVisionQueuePos = (ids) =>
                    {
                        double[] visionQueuePosX = { 0.180, 0.300, 0.420, 0.540 };
                        double[] visionQueuePosY = { 0.300, 0.300, 0.300, 0.300 };


                        for (int i = 0; i < ids.Length; i++)
                        {
                            MF.LinarMotion(0, ids[i], visionQueuePosX[i], visionQueuePosY[i], "YX");
                        }
                    };

                    //Motion Functions of the physical temp queue--------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int[]> physicalEndQueuePos = (ids) =>
                    {
                        double[] endQueuePosX = { 0.060, 0.060, 0.060, 0.060 };
                        double[] endQueuePosY = { 0.420, 0.540, 0.660, 0.780 };


                        for (int i = 0; i < ids.Length; i++)
                        {
                            MF.LinarMotion(0, ids[i], endQueuePosX[i], endQueuePosY[i], "YX");
                        }
                    };

                    //Motion Functions of the filling line---------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int> fillingMovements = (id) =>
                    {
                        //Moving through the filling steps
                        MF.LinarMotion(0, id, 0.660, 0.880, "XY");
                        MF.Stationairy(1, id);
                        MF.LinarMotion(0, id, 0.660, 0.860, "XY");
                        MF.Stationairy(1, id);
                        MF.LinarMotion(0, id, 0.660, 0.840, "XY");
                        MF.Stationairy(1, id);
                        MF.LinarMotion(0, id, 0.660, 0.820, "XY");
                        MF.Stationairy(1, id);
                        MF.LinarMotion(0, id, 0.660, 0.800, "XY");
                        MF.Stationairy(1, id);


                    };

                    //Motion Functions of the stoppering line------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------

                    Action<int> stopperingMovements = (id) =>
                    {
                        MF.LinarMotion(0, id, 0.660, 0.160, "XY");
                        MF.Stationairy(2, id);
                        MF.LinarMotion(0, id, 0.660, 0.140, "XY");
                        MF.Stationairy(2, id);
                        MF.LinarMotion(0, id, 0.660, 0.120, "XY");
                        MF.Stationairy(2, id);
                        MF.LinarMotion(0, id, 0.660, 0.100, "XY");
                        MF.Stationairy(2, id);
                        MF.LinarMotion(0, id, 0.660, 0.080, "XY");
                        MF.Stationairy(2, id);
                    };

                    //Motion Functions of the vision line----------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------

                    Action<int> visionMovements = (id) =>
                    {
                        MF.LinarMotion(0, id, 0.120, 0.120, "YX");
                        MF.Stationairy(2, id);
                        MF.RotateMotion(0, id, 180, "CW");
                        MF.Stationairy(2, id);

                    };


                    //Passing positions to the queues------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    PhysicalFillingQueue.passingFunctions(physicalFillingQueuePos);
                    PhysicalFillingQueue.defineSize(4);

                    PhysicalStopperingQueue.passingFunctions(physicalStopperingQueuePos);
                    PhysicalStopperingQueue.defineSize(4);

                    PhysicalVisionQueue.passingFunctions(physicalVisionQueuePos);
                    PhysicalVisionQueue.defineSize(4);

                    PhysicalEndQueue.passingFunctions(physicalEndQueuePos);
                    PhysicalEndQueue.defineSize(4);

                    //Passing the motion functions to their classes------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Filling.passingFunctions(fillingMovements);
                    Stoppering.passingFunctions(stopperingMovements);
                    Vision.passingFunctions(visionMovements);

                    //Adding tasks to the shuttles-------------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Shuttle1.shuttleID = 1;
                    Shuttle2.shuttleID = 2;
                    Shuttle3.shuttleID = 3;
                    Shuttle4.shuttleID = 4;

                    string[] tasksArray = { "filling", "done" };
                     

                    foreach (ShuttleClass shuttle in allShuttles)
                    {
                        shuttle.insertTaskArray(tasksArray);

                    }

                    selector = 2;
                    break;


                case '2':

                    while (true)
                    {

                        //---------------------- First we update the queues-----------------------------------------------------------------------
                        //------------------------------------------------------------------------------------------------------------------------
                        PhysicalFillingQueue.updatingQueue();
                        PhysicalStopperingQueue.updatingQueue();
                        PhysicalVisionQueue.updatingQueue();
                        PhysicalEndQueue.updatingQueue();
                        
                        //---------------------- Second we check if a shuttle is idle, then we check the tasks of the shuttles---------------------
                        //------------------------------------------------------------------------------------------------------------------------
                        foreach (ShuttleClass shuttle in allShuttles)
                        {
                            if (shuttle.Status == shuttle.shuttleIdle) 
                                //If the shuttle is moving, aka not idle, then we don't want to do anything to it, other than check for errors
                            {
                                if (shuttle.Tasks.Count > 0) //If the shuttle has any tasks
                                {

                                    switch (shuttle.Tasks[0]) //Checking what the first task is.
                                    {

                                        case "filling": // If the first task of the shuttle is filling

                                            Console.WriteLine(Filling.stationStatus());

                                            if (Filling.stationStatus() == false) //filling is NOT running, so we start it

                                            {


                                                PhysicalFillingQueue.removeIDFromList(shuttle.shuttleID); //Here we should add it so that it removes it from the queue that the shuttle is in, not always the filling queue
                                                Filling.addIDToList(shuttle.shuttleID); //add the shuttle ID to the queue of the station
                                                Filling.runMovement(); //We then give the shuttle the movements AND remove it from the station queue
                                                shuttle.replaceFirstTask("runningFilling"); //Then we replace the filling task with a running filling status
                                                Filling.updateStationStatus();

                                            }

                                            else if (Filling.stationStatus() == true) //Filling IS running, so we add this one to the queues
                                            {
                                                Filling.addIDToList(shuttle.shuttleID); // We add this ID to the filling station queue, it should only do it once since we replace the task
                                                PhysicalFillingQueue.addIDToList(shuttle.shuttleID); //AND then physical filling queue
                                                Filling.updateStationStatus();
                                            }

                                            break;

                                        case "stoppering":
                                            break;

                                        case "vision":
                                            break;

                                        case "runningFilling": //Because we are still in the if-statement of idle shuttles, we can do this here

                                            shuttle.removeTask("runningFilling"); //Remove the task from the shuttle
                                            Filling.updateStationStatus(); //We update the station station because the shuttle is idle


                                            break;

                                        case "done":

                                            PhysicalEndQueue.addIDToList(shuttle.shuttleID);
                                            shuttle.removeTask("done");

                                            break;

                                    }
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
