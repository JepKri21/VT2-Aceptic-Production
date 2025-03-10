using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    class Station_Class
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //this class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.
        private static XBotCommands _xbotCommand = new XBotCommands();

        public List<int> station_queue = new List<int>();  //Making a queue for the station
        public bool station_running = false; //Making a boolean that says if the station is being used or not
        public int current_ID = 0; //Making a integer of the ID that is currently being used
        public int previous_ID; //Saying that if previous ID is idle then put it into a new queue

        private Action<int>? station_functions; //Accepting a list of functions 

        
        public void add_ID_to_list(int ID_to_add)
        {
            station_queue.Add(ID_to_add);
        }
        public void remove_ID_from_list(int ID_to_remove)
        {
            station_queue.Add(ID_to_remove);
        }

        public void passing_functions(Action<int> movements)
        {
            station_functions = movements;
        }

        public bool station_status()
        {
            if (current_ID == 0)
            {
                station_running = false;
                return false;
            }
            XBotStatus status = _xbotCommand.GetXbotStatus(current_ID);
            Enum state = status.XBOTState;
            if (Convert.ToInt32(state) == 3)
            {
                station_running = false;
                return station_running;
            }
            else
            {
                station_running = true;
                return station_running;
            }
        }

        public void run_movement()
        {
            if (station_queue.Count > 0)
            {
                previous_ID = current_ID;
                current_ID = station_queue[0];
                station_queue.RemoveAt(0);
                station_functions(current_ID);
            }            
        }


    }
}
