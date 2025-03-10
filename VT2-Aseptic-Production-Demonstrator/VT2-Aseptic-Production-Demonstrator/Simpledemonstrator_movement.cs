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
        private static Station_Class filling_and_stoppering = new Station_Class();
        private static Queue_Class physical_filling_queue = new Queue_Class();
        private static Queue_Class temp_q = new Queue_Class();


        public void initial_pos(int[] XIDs)
        {
            // Start position
            _xbotCommand.LinearMotionSI(0, XIDs[0], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.600, 0.840, 0, 0.5, 2);
            _xbotCommand.LinearMotionSI(0, XIDs[1], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.360, 0.840, 0, 0.5, 2);
            _xbotCommand.LinearMotionSI(0, XIDs[2], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.120, 0.840, 0, 0.5, 2);
            _xbotCommand.LinearMotionSI(0, XIDs[3], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.120, 0.600, 0, 0.5, 2);

            // Turn every Autobot to 0 degrees
            int i = 0;
            foreach (int xbot_current in XIDs)
            {
                _xbotCommand.RotaryMotionP2P(0, xbot_current, ROTATIONMODE.NO_ANGLE_WRAP, 0, 1, 1, POSITIONMODE.ABSOLUTE);
                i++;
            }

            // Position in queue
            _xbotCommand.LinearMotionSI(0, XIDs[0], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.YTHENX, 0.420, 0.780, 0, 0.5, 2);
            _xbotCommand.LinearMotionSI(0, XIDs[1], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.YTHENX, 0.420, 0.900, 0, 0.5, 2);
            _xbotCommand.LinearMotionSI(0, XIDs[2], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.YTHENX, 0.300, 0.900, 0, 0.5, 2);
            _xbotCommand.LinearMotionSI(0, XIDs[3], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.YTHENX, 0.300, 0.780, 0, 0.5, 2);

            bool finished = false;
            while(!finished)
            {
                
                XBotStatus status = _xbotCommand.GetXbotStatus(XIDs[0]);
                Enum state1 = status.XBOTState;
                status = _xbotCommand.GetXbotStatus(XIDs[1]);
                Enum state2 = status.XBOTState;
                status = _xbotCommand.GetXbotStatus(XIDs[2]);
                Enum state3 = status.XBOTState;
                status = _xbotCommand.GetXbotStatus(XIDs[3]);
                Enum state4 = status.XBOTState;
                if (Convert.ToInt32(state1) == 3 & Convert.ToInt32(state2) == 3 & Convert.ToInt32(state3) == 3 & Convert.ToInt32(state4) == 3)
                {
                    finished = true;
                    break;
                }
            }

        }

       


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
            Console.WriteLine("1    Run Code ");
            Console.WriteLine("2    Setting Initial Positions");
            Console.WriteLine("3    Start new program");

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {

                case '0':
                    selector = 1;
                    break;

                case '1':
                    /*
                    //Moving to initial positions
                    initial_pos(xbot_ids);

                    //Defining varaibles
                    int current_filling = 0;
                    

                    //Adding the relevant shuttles to the filling queue
                    for (int i = 0; i < xbot_ids.Length; i++)
                    {
                        physical_filling_queue.Add(xbot_ids[i]);
                    }

                    while (true)
                    {
                        updating_filling_queue("update"); //Update the physical queue
                        if (!filling_running & filling_queue.Count > 0) //If filling is not running AND the filling_queue is not empty
                        {
                            Filling_occupied(filling_queue[0]); //Start the filling process with the ID that is first in the filling_queue
                            current_filling = filling_queue[0]; //Save that ID for checking later
                            updating_filling_queue("remove", filling_queue[0]); //Remove that specific ID from the physical_filling_queue
                            filling_queue.RemoveAt(0); //Remove the specific ID from the filling_queue aswell (Note that currently it doesn't actually remove the specific one, just the first one)
                            updating_filling_queue("add", current_filling); //Inserting the shuttle into the filling queue again, just as a test
                                                                            //(It doesn't work 100% as this means that the next filling only starts when it has gotten back into the physical queue)
                        }
                        else
                        {
                            Filling_occupied(current_filling); //Just checking if this ID is still moving
                        }

                    }

                    
                    */
                    selector = 2;
                    break;


                case '2':
                    //Starting the initial position process--------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    int[] initial_xbots = { 1, 2, 3, 4 };
                    double[] initalX = { 0.600, 0.360, 0.120, 0.120 };
                    double[] initialY = { 0.840, 0.840, 0.840, 0.600 };

                    //Drive to initial positions
                    _xbotCommand.AutoDrivingMotionSI(4, ASYNCOPTIONS.MOVEALL, initial_xbots, initalX, initialY);

                    //Motion Functions of the physical filling queue-----------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int[]> physical_filling_queue_pos = (ids) =>
                    {
                        double[] filling_queue_posX = {0.540, 0.420, 0.300, 0.180};
                        double[] filling_queue_posY = {0.900, 0.900, 0.900, 0.900 };


                        for (int i = 0; i < ids.Length; i++)
                        {
                            MF.LinarMotion(0, ids[i], filling_queue_posX[i], filling_queue_posY[i], "YX");
                        }
                    };

                    //Motion Functions of the physical temp queue--------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int[]> temp_queue_pos = (ids) =>
                    {
                        double[] temp_queue_posX = { 0.180, 0.300, 0.420, 0.540 };
                        double[] temp_queue_posY = { 0.060, 0.060, 0.060, 0.060 };


                        for (int i = 0; i < ids.Length; i++)
                        {
                            MF.LinarMotion(0, ids[i], temp_queue_posX[i], temp_queue_posY[i], "YX");
                        }
                    };

                    //Motion Functions of the filling line---------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    Action<int> filling_movements = (id) =>
                    {
                        //Moving through the filling steps
                        MF.LinarMotion(0, id, 0.660, 0.660, "XY");
                        MF.Stationairy(1, id);
                        MF.LinarMotion(0, id, 0.660, 0.640, "XY");
                        MF.Stationairy(1, id);
                        MF.LinarMotion(0, id, 0.660, 0.620, "XY");
                        MF.Stationairy(1, id);
                        MF.LinarMotion(0, id, 0.660, 0.600, "XY");
                        MF.Stationairy(1, id);
                        MF.LinarMotion(0, id, 0.660, 0.580, "XY");
                        MF.Stationairy(1, id);

                        //Now onto the stoppering steps

                        MF.LinarMotion(0, id, 0.660, 0.400, "XY");
                        MF.Stationairy(2, id);
                        MF.LinarMotion(0, id, 0.660, 0.380, "XY");
                        MF.Stationairy(2, id);
                        MF.LinarMotion(0, id, 0.660, 0.360, "XY");
                        MF.Stationairy(2, id);
                        MF.LinarMotion(0, id, 0.660, 0.340, "XY");
                        MF.Stationairy(2, id);
                        MF.LinarMotion(0, id, 0.660, 0.320, "XY");
                        MF.Stationairy(2, id);

                    };


                    //Passing the motion functions to their classes------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------
                    physical_filling_queue.passing_functions(physical_filling_queue_pos);
                    physical_filling_queue.define_size(4);

                    temp_q.passing_functions(temp_queue_pos);
                    temp_q.define_size(4);

                    filling_and_stoppering.passing_functions(filling_movements);

                    //Adding a couple things to queues-------------------------------------------------------------------
                    //---------------------------------------------------------------------------------------------------

                    for (int i = 1; i < 5; i++) //Adding the 4 shuttles to the filling queue where they will start
                    {
                        physical_filling_queue.add_ID_to_list(i);
                    }

                    for (int i = 1; i < 5; i++) //Adding the 4 ID's once to test the systam
                    {
                        filling_and_stoppering.add_ID_to_list(i);
                    }


                    selector = 2;

                    //
                    //filling_and_stoppering.add_ID_to_list(1);
                    //filling_and_stoppering.run_movement();


                    break;

                case '3':

                    //MAYBE IF YOU GAVE EACH XBOT A LIST OF THINGS IT NEEDS TO DO INSTEAD?
                    while (true)
                    {
                        physical_filling_queue.updating_queue();
                        temp_q.updating_queue();

                        if (filling_and_stoppering.station_status() == false)
                        {
                            filling_and_stoppering.run_movement();
                            physical_filling_queue.remove_ID_from_list(filling_and_stoppering.current_ID);
                        }
                        else if(filling_and_stoppering.station_status() == true)
                        {
                            XBotStatus status = _xbotCommand.GetXbotStatus(filling_and_stoppering.previous_ID);
                            Enum state = status.XBOTState;
                            if (Convert.ToInt32(state) == 3)
                            {
                                temp_q.add_ID_to_list(filling_and_stoppering.previous_ID);
                            }
                        }
                    }
                    
                    
                    break; 
                

            }


        }
        



    }
}
