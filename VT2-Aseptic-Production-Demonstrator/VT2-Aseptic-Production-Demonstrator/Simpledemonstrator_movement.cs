using PMCLIB;
using System;
using System.Collections;
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

        //Creating lists for the station queues
        public List<int> filling_queue = new List<int>() {1,2,3,4,1,2,3,4};

        //Creating lists for the physical queues on the table
        public List<int> physical_filling_queue = new List<int>();

        public bool filling_running = false;

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
                if (Convert.ToInt32(state1) == 3 & Convert.ToInt32(state2) == 3 & Convert.ToInt32(state3) == 3 & Convert.ToInt32(state4) == 3)
                {
                    finished = true;
                    break;
                }
            }

        }

        public void updating_filling_queue(string action, int? ID = null)
        {
            double[] queue_posX = {0.420, 0.420, 0.300, 0.300};
            double[] queue_posY = {0.780, 0.900, 0.900 ,0.780 };

            if (action == "remove")
            {
                physical_filling_queue.Remove(ID.Value);
            }
            else if (action == "add")
            {
                physical_filling_queue.Add(ID.Value);
            }
            else if (action == "update")
            {
                //Here we update the physical positions in the queue
                for (int i = 0; i < physical_filling_queue.Count; i++)
                {
                    _xbotCommand.LinearMotionSI(0, physical_filling_queue[i], POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.YTHENX, queue_posX[i], queue_posY[i], 0, 0.5, 2);
                }
            }
            else return;
                
        }

        public void Filling_occupied(int XID)
        {
            if (!filling_running)
            {
                _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.660, 0.060, 0, 0.5, 2);
                _xbotCommand.LinearMotionSI(0, XID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, 0.060, 0.780, 0, 0.5, 2);
                filling_running = true;
                

            }
            else if (filling_running)
            {
                XBotStatus status = _xbotCommand.GetXbotStatus(XID);
                Enum state = status.XBOTState;
                if (state.ToString() == "IDLE")
                {
                    filling_running = false;
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

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {

                case '0':
                    selector = 1;
                    break;

                case '1':

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
                            filling_queue.RemoveAt(0); //Remove the specific ID from the filling_queue aswell
                            updating_filling_queue("add", current_filling); //Inserting the shuttle into the filling queue again, just as a test
                        }
                        else
                        {
                            Filling_occupied(current_filling); //Just checking if this ID is still moving
                        }

                    }

                    
                    


                    selector = 2;

                    break;
            }

        }
        



    }
}
