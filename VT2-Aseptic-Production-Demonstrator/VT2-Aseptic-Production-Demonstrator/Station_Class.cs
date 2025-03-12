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

        public List<int> stationQueue = new List<int>();  //Making a queue for the station
        private bool stationRunning = false; //Making a boolean that says if the station is being used or not
        private int currentID = 0; //Making a integer of the ID that is currently being used
        
        private Action<int>? stationFunctions; //Accepting a list of functions 

        
        public void addIDToList(int ID_to_add)
        {
            stationQueue.Add(ID_to_add);
        }
        public void removeIDFromList(int ID_to_remove)
        {
            stationQueue.Add(ID_to_remove);
        }

        public void passingFunctions(Action<int> movements)
        {
            stationFunctions = movements;
        }

        public void updateStationStatus()
        {
            XBotStatus status = _xbotCommand.GetXbotStatus(currentID);
            Enum state = status.XBOTState;
            if (Convert.ToInt32(state) == 3)
            {
                stationRunning = false;
            }
            else
            {
                stationRunning = true;
            }
        }

        public bool stationStatus()
        {
            return stationRunning;
        }

        public void runMovement()
        {
            if (stationQueue.Count > 0)
            {
                currentID = stationQueue[0];
                stationQueue.RemoveAt(0);
                stationFunctions(currentID);
            }            
        }


    }
}
