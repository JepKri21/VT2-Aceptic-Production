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

        struct MotionStatusStruct
        {
            bool isBufferBlocked;
            int bufferCount;
            int firstCmdLabel;
            int lastCmdLabel;
            int shuttleID;
        }

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


        //REMEMBER TO ADD YOUR SHUTTLE HERE
        ShuttleClass[] allShuttles = { Shuttle1, Shuttle2, Shuttle3};


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
            Console.WriteLine("3    buffer test");

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {

                case '0':
                    selector = 1;
                    break;

                case '1':
                    //Starting the initial position process--------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    int[] initialXbots = { 1, 2, 3}; //You could make this dynamic so that you don't have to specify how many you are using.
                    double[] initalX = { 0.600, 0.360, 0.120, 0.120 };
                    double[] initialY = { 0.840, 0.840, 0.840, 0.600 };

                    //Drive to initial positions
                    _xbotCommand.AutoDrivingMotionSI(initialXbots.Length, ASYNCOPTIONS.MOVEALL, initialXbots, initalX, initialY);

                    for (int i = 0; i<= initialXbots.Length; i++)
                    {
                        MF.RotateMotion(0, i, 0, "CW");
                    }

                    //Motion Functions of the physical filling queue-----------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int[]> physicalFillingQueuePos = (ids) =>
                    {
                        double[] fillingQueuePosX = { 0.420, 0.300, 0.180 }; //0.540
                        double[] fillingQueuePosY = { 0.900, 0.900, 0.900 }; //0.900


                        for (int i = 0; i < ids.Length; i++)
                        {
                            MF.LinarMotion(0, ids[i], fillingQueuePosX[i], fillingQueuePosY[i], "YX");
                        }
                    };

                    //Motion Functions of the physical stoppering queue--------------------------------------------------
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

                    //Motion Functions of the physical vision queue------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int[]> physicalVisionQueuePos = (ids) =>
                    {
                        double[] visionQueuePosX = { 0.300, 0.300, 0.300, 0.420 };
                        double[] visionQueuePosY = { 0.300, 0.180, 0.060, 0.060 };


                        for (int i = 0; i < ids.Length; i++)
                        {
                            MF.LinarMotion(0, ids[i], visionQueuePosX[i], visionQueuePosY[i], "XY");
                        }
                    };

                    //Motion Functions of the physical End queue---------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int[]> physicalEndQueuePos = (ids) =>
                    {
                        double[] endQueuePosX = { 0.060, 0.060, 0.060, 0.060 };
                        double[] endQueuePosY = { 0.780, 0.660, 0.540, 0.420 };


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
                        MF.LinarMotion(0, id, 0.660, 0.880, "YX");
                        MF.Stationairy(3, id);
                        MF.LinarMotion(0, id, 0.660, 0.860, "XY");
                        MF.Stationairy(3, id);
                        MF.LinarMotion(0, id, 0.660, 0.840, "XY");
                        MF.Stationairy(3, id);
                        MF.LinarMotion(0, id, 0.660, 0.820, "XY");
                        MF.Stationairy(3, id);
                        MF.LinarMotion(0, id, 0.660, 0.800, "XY");
                        MF.Stationairy(3, id);
                        MF.LinarMotion(0, id, 0.660, 0.660, "YX");


                    };

                    //Motion Functions of the stoppering line------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------

                    Action<int> stopperingMovements = (id) =>
                    {
                        MF.LinarMotion(0, id, 0.660, 0.160, "YX");
                        MF.Stationairy(5, id);
                        MF.LinarMotion(0, id, 0.660, 0.140, "XY");
                        MF.Stationairy(5, id);
                        MF.LinarMotion(0, id, 0.660, 0.120, "XY");
                        MF.Stationairy(5, id);
                        MF.LinarMotion(0, id, 0.660, 0.100, "XY");
                        MF.Stationairy(5, id);
                        MF.LinarMotion(0, id, 0.660, 0.080, "XY");
                        MF.Stationairy(5, id);
                        MF.LinarMotion(0, id, 0.540, 0.060, "YX");
                    };

                    //Motion Functions of the vision line----------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------

                    Action<int> visionMovements = (id) =>
                    {
                        MF.LinarMotion(0, id, 0.120, 0.120, "YX");
                        MF.Stationairy(1, id);
                        MF.RotateMotion(0, id, 180, "CW");
                        MF.Stationairy(1, id);
                        MF.RotateMotion(0, id, 0, "CW");
                        MF.LinarMotion(0, id, 0.060, 0.180, "XY");

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

                    string[] tasksArray = { "filling","stoppering", "vision", "filling", "stoppering", "vision", "filling", "stoppering", "vision", "done" };
                     

                    foreach (ShuttleClass shuttle in allShuttles)
                    {
                        shuttle.insertTaskArray(tasksArray);

                    }

                    selector = 2;
                    break;


                case '2':

                    while (true)
                    {
                        Console.WriteLine("----------------------------------------------------------------------------");

                        //---------------------- First we update the queues-----------------------------------------------------------------------
                        //------------------------------------------------------------------------------------------------------------------------
                        PhysicalFillingQueue.updatingQueue();
                        PhysicalStopperingQueue.updatingQueue();
                        PhysicalVisionQueue.updatingQueue();
                        PhysicalEndQueue.updatingQueue();

                        //Updating stations
                        Filling.updateStationStatus();
                        Stoppering.updateStationStatus();
                        Vision.updateStationStatus();

                        //---------------------- Second we check if a shuttle is idle, then we check the tasks of the shuttles---------------------
                        //------------------------------------------------------------------------------------------------------------------------
                        foreach (ShuttleClass shuttle in allShuttles)
                        {
                            Console.Write("Shuttle ");
                            Console.Write(shuttle.shuttleID);
                            Console.Write(" has these tasks ");
                            Console.WriteLine(string.Join(", ", shuttle.Tasks));
                            

                            if (shuttle.bufferMotionCount == 0) //If the shuttle doesn't have any movements in its buffer
                                                                // then we want to look at that shuttles tasks, else we don't want to do naything, yet
                            {
                                
                                if (shuttle.Tasks.Count > 0) //If the shuttle has any tasks
                                                             // then we check what task is next for the shuttle
                                {
                                    

                                    switch (shuttle.Tasks[0]) //Checking what the shuttle task is
                                    {
                                        

                                        case "filling": // If the first task of the shuttle is filling

                                            Filling.addIDToList(shuttle.shuttleID); //add the shuttle ID to the queue of the station (it will not be added if it is already there)

                                            if (Filling.stationStatus() == false) //If filling is NOT running
                                            {
                                                PhysicalFillingQueue.removeIDFromList(shuttle.shuttleID); //Here we should remove it from the queue that the shuttle is in, not always the filling queue
                                                Filling.runMovement(); //We then give the shuttle the movements AND remove it from the station queue
                                                shuttle.replaceFirstTask("runningFilling"); //Then we replace the filling task with a running filling status
                                                Filling.updateStationStatus();
                                            }

                                            else if (Filling.stationStatus() == true) //If filling IS running
                                            {
                                                PhysicalFillingQueue.addIDToList(shuttle.shuttleID); //Then add it to the physical filling queue
                                            }

                                            break;

                                        case "stoppering":

                                            Stoppering.addIDToList(shuttle.shuttleID); //add the shuttle ID to the queue of the station (it will not be added if it is already there)

                                            if (Stoppering.stationStatus() == false) //If filling is NOT running
                                            {
                                                PhysicalStopperingQueue.removeIDFromList(shuttle.shuttleID); //Here we should remove it from the queue that the shuttle is in, not always the filling queue
                                                Stoppering.runMovement(); //We then give the shuttle the movements AND remove it from the station queue
                                                shuttle.replaceFirstTask("runningStoppering"); //Then we replace the filling task with a running filling status
                                                Stoppering.updateStationStatus();
                                            }

                                            else if (Stoppering.stationStatus() == true) //If filling IS running
                                            {
                                                PhysicalStopperingQueue.addIDToList(shuttle.shuttleID); //Then add it to the physical filling queue
                                            }

                                            break;

                                        case "vision":

                                            Vision.addIDToList(shuttle.shuttleID); //add the shuttle ID to the queue of the station (it will not be added if it is already there)

                                            if (Vision.stationStatus() == false) //If filling is NOT running
                                            {
                                                PhysicalVisionQueue.removeIDFromList(shuttle.shuttleID); //Here we should remove it from the queue that the shuttle is in, not always the filling queue
                                                Vision.runMovement(); //We then give the shuttle the movements AND remove it from the station queue
                                                shuttle.replaceFirstTask("runningVision"); //Then we replace the filling task with a running filling status
                                                Vision.updateStationStatus();
                                            }

                                            else if (Vision.stationStatus() == true) //If filling IS running
                                            {
                                                PhysicalVisionQueue.addIDToList(shuttle.shuttleID); //Then add it to the physical filling queue
                                            }

                                            break;

                                        

                                        case "runningFilling": //Because we are still in the if-statement of idle shuttles, we can do this here

                                            
                                            Filling.currentID = 0;
                                            shuttle.removeTask("runningFilling"); //Remove the task from the shuttle
                                            Filling.updateStationStatus(); //We update the station status because the shuttle is idle

                                            break;

                                        case "runningStoppering":
                                            Stoppering.currentID = 0;
                                            shuttle.removeTask("runningStoppering"); //Remove the task from the shuttle
                                            Stoppering.updateStationStatus(); //We update the station status because the shuttle is idle

                                            break;

                                        case "runningVision":

                                            Vision.currentID = 0;
                                            shuttle.removeTask("runningVision"); //Remove the task from the shuttle
                                            Vision.updateStationStatus(); //We update the station status because the shuttle is idle

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

                    int xbotID = 1;  // Replace with your actual XBot ID

                    MF.LinarMotion(0, xbotID, 0.660, 0.780, "XY");
                    MF.LinarMotion(0, xbotID, 0.060, 0.780, "XY");
                    MF.LinarMotion(0, xbotID, 0.660, 0.780, "XY");
                    MF.LinarMotion(0, xbotID, 0.060, 0.780, "XY");
                    MF.LinarMotion(0, xbotID, 0.660, 0.780, "XY");
                    MF.LinarMotion(0, xbotID, 0.060, 0.780, "XY");
                    MF.LinarMotion(0, xbotID, 0.660, 0.780, "XY");
                    MF.LinarMotion(0, xbotID, 0.060, 0.780, "XY");
                    MF.LinarMotion(0, xbotID, 0.660, 0.780, "XY");
                    MF.LinarMotion(0, xbotID, 0.060, 0.780, "XY");

                    



                    while (true)
                    {
                        MotionBufferReturn bufferReturn = _xbotCommand.MotionBufferControl(xbotID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
                        int count = bufferReturn.motionBufferStatus.bufferedMotionCount;

                        Console.Write(count);
                        Console.WriteLine(" left in the buffer");
                    }
                    


                    break; 
                

            }


        }
        



    }
}
