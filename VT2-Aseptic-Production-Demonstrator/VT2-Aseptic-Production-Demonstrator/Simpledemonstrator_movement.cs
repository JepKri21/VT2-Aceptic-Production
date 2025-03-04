using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
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

            // Collect Autobots
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
                if (state1.ToString() == "IDLE" & state2.ToString() == "IDLE" & state3.ToString() == "IDLE" & state4.ToString() == "IDLE")
                {
                    finished = true;
                    break;
                }
            }

        }
        public bool Filling_occupied(int XID, bool run_cmd)
        {
            if (!run_cmd)
            {
                _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.660, 0.060, 0, 0.5, 2);
                _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.060, 0.780, 0, 0.5, 2);
                run_cmd = true;
                Console.WriteLine("Filling begun");
                return run_cmd;
            }
            else if (run_cmd)
            {
                XBotStatus status = _xbotCommand.GetXbotStatus(XID);
                Enum state = status.XBOTState;
                if (state.ToString() == "IDLE")
                {
                    run_cmd = false;
                    return run_cmd;
                }
            }
            return run_cmd; // Ensure all code paths return a value
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

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {

                case '0':
                    selector = 1;
                    break;

                case '1':

                    initial_pos(xbot_ids);
                    int current_filling = 0;
                    bool run_cmd = false;

                    Filling_occupied(xbot_ids[0], run_cmd);
                    


                    selector = 2;

                    break;
            }

        }
        



    }
}
