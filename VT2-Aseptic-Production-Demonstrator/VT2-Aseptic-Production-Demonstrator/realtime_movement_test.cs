using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VT2_Aseptic_Production_Demonstrator
{
    internal class realtime_movement_test
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //this class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.
        private static XBotCommands _xbotCommand = new XBotCommands();
        WaitUntilTriggerParams CMD_params = new WaitUntilTriggerParams();
        int selector = 2;

        public int setSelectorOne()
        {
            return selector;
        }

        //----------------------------------------------------------------------
        public double[] GetPosition(int XID)
        {
            XBotStatus status = _xbotCommand.GetXbotStatus(XID);
            double[] position = status.FeedbackPositionSI;
            return position;
        }
        //----------------------------------------------------------------------
        public int GetStatus(int XID)
        {
            XBotStatus status = _xbotCommand.GetXbotStatus(XID);
            Enum state = status.XBOTState;
            return Convert.ToInt32(state);
        }
        //----------------------------------------------------------------------
        public bool HasStoppedMoving(params int[] XIDs)
        {
            while (true) // Infinite loop that exits once all IDs have stopped
            {
                if (XIDs.All(ID => GetStatus(ID) == 3))
                {
                    return true; // All IDs have status 3, return true
                }
            }
        }

        //----------------------------------------------------------------------

        public bool Filling(int XID, int idle_pos)
        {
            WaitUntilTriggerParams filling_params = new WaitUntilTriggerParams();
            filling_params.delaySecs = 1;

            //Do the filling motions
            _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.660, 0.720, 0, 1, 1);
            _xbotCommand.WaitUntil(0, XID, TRIGGERSOURCE.TIME_DELAY, filling_params);
            _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.660, 0.710, 0, 1, 1);
            _xbotCommand.WaitUntil(0, XID, TRIGGERSOURCE.TIME_DELAY, filling_params);
            _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.660, 0.700, 0, 1, 1);
            _xbotCommand.WaitUntil(0, XID, TRIGGERSOURCE.TIME_DELAY, filling_params);
            _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.660, 0.690, 0, 1, 1);
            _xbotCommand.WaitUntil(0, XID, TRIGGERSOURCE.TIME_DELAY, filling_params);
            _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.660, 0.680, 0, 1, 1);
            _xbotCommand.WaitUntil(0, XID, TRIGGERSOURCE.TIME_DELAY, filling_params);

            //Move away from the filling station
            if (idle_pos == 0)
            {
                _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.YTHENX, 0.300, 0.300, 0, 1, 1);
            }
            else if (idle_pos == 1)
            {
                _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.YTHENX, 0.300, 0.420, 0, 1, 1);
            }
            else if (idle_pos == 2)
            {
                _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.YTHENX, 0.420, 0.300, 0, 1, 1);
            }

            int[] ID_array = { XID };
            if (HasStoppedMoving(ID_array))
            {
                //something here
            }


            
                return true;
        }




        public void runRealtimeMovementTest(int[] xbot_ids)
        {
            selector = 1;
            Console.Clear();
            Console.WriteLine(" Realtime Movement Test");
            Console.WriteLine("0    Return ");
            Console.WriteLine("1    Run Code ");

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {

                case '0':
                    selector = 1;
                    break;

                case '1':


                    //double max_start_speed = 0.1;
                    //double max_start_accelleration = 0.1;
                    //double[] start_positionsx = { 0.180, 0.060, 0.060 };
                    //double[] start_positionsy = { 0.900, 0.900, 0.780 };
                    //int[] start_IDs = { 4, 5, 6 };
                    //
                    //Enum error = _xbotCommand.AutoDrivingMotionSIWithSpeed(3, ASYNCOPTIONS.MOVEALl_UNLABELED, max_start_speed, max_start_accelleration, start_IDs, start_positionsx, start_positionsy);
                    //
                    ////Enum error = _xbotCommand.AutoDrivingMotionSI(2, ASYNCOPTIONS.MOVEALl_UNLABELED, start_IDs, start_positionsx, start_positionsy);
                    //
                    //
                    //while (true)
                    //{
                    //    Console.WriteLine(error);
                    //}

                    //--------------------------------Initial positions-------------------------------------------

                    _xbotCommand.LinearMotionSI(1, xbot_ids[0], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.180, 0.900, 0, 1, 1);

                    WaitUntilTriggerParams shuttle2_waitcmd = new WaitUntilTriggerParams();
                    shuttle2_waitcmd.CmdLabelTriggerType = TRIGGERCMDLABELTYPE.CMD_FINISH;
                    shuttle2_waitcmd.triggerXbotID = xbot_ids[0];
                    shuttle2_waitcmd.triggerCmdLabel = 1;

                    _xbotCommand.WaitUntil(0, xbot_ids[1], TRIGGERSOURCE.CMD_LABEL, shuttle2_waitcmd);
                    _xbotCommand.LinearMotionSI(2, xbot_ids[1], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.060, 0.900, 0, 1, 1);
                    
                    WaitUntilTriggerParams shuttle3_waitcmd = new WaitUntilTriggerParams();
                    shuttle3_waitcmd.CmdLabelTriggerType = TRIGGERCMDLABELTYPE.CMD_FINISH;
                    shuttle3_waitcmd.triggerXbotID = xbot_ids[1];
                    shuttle3_waitcmd.triggerCmdLabel = 2;

                    _xbotCommand.WaitUntil(0, xbot_ids[2], TRIGGERSOURCE.CMD_LABEL, shuttle3_waitcmd);
                    _xbotCommand.LinearMotionSI(3, xbot_ids[2], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.060, 0.780, 0, 1, 1);


                    //----------------------------------------------------------------------------------------------
                    //-----------------------------------------Full process-----------------------------------------

                    bool filling_available = true;
                    int free_spot = 0;
                    int i = 0;

                    while (true)
                    {
                        
                    
                        if (filling_available)
                        {
                            filling_available = false;
                            Filling(xbot_ids[i],free_spot);
                            free_spot++;
                            free_spot = free_spot % 3;
                            i++;
                            i = i % 3;
                            if (HasStoppedMoving(xbot_ids[i]))
                            {
                                filling_available = true;
                            }
                        }
                    }


                    selector = 2;

                    break;
            }


        }
        



    }
}
