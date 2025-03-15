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
        public int currentID = 0; //Making a integer of the ID that is currently being used
        private int bufferCount = 0;
        MotionBufferReturn bufferReturn;

        private Action<int>? stationFunctions; //Accepting a list of functions 

        
        public void addIDToList(int IDToAdd)
        {
            
            if (stationQueue.Contains(IDToAdd))
            {
                return;  // Exit the function early
            }
            else
            {
                stationQueue.Add(IDToAdd);
            }

        }
        public void removeIDFromList(int IDToRemove)
        {
            stationQueue.Remove(IDToRemove);
        }

        public void passingFunctions(Action<int> movements)
        {
            stationFunctions = movements;
        }

        public void updateStationStatus()
        {
            if (currentID == 0)
            {
                stationRunning = false;
                return;
            }
            
            bufferReturn = _xbotCommand.MotionBufferControl(currentID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
            bufferCount = bufferReturn.motionBufferStatus.bufferedMotionCount;
        
            if (bufferCount == 0)
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
                stationQueue.Remove(currentID);
                stationFunctions(currentID);
            }            
        }


    }
}
