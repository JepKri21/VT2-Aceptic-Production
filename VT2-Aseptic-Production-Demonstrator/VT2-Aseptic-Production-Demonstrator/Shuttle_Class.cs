using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    class ShuttleClass
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //this class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.
        private static XBotCommands _xbotCommand = new XBotCommands();

        private int ID;
        private int status;
        public int shuttleIdle = 3;
        public int bufferCount = 0;
        private string placedInQueue;
        MotionBufferReturn bufferReturn;
        private List<string> tasks = new List<string>();
        private List<double> position = new List<double>() {0,0,0,0,0,0};


        //----------------------------------Give ID to Shuttle----------------------------------------
        //--------------------------------------------------------------------------------------------
        public int shuttleID
        {
            get { return ID; }
            set { ID = value; }
        }

        

        //--------------------------------Status of the Shuttle---------------------------------------
        //--------------------------------------------------------------------------------------------

        public int Status
        {
            
            get 
            {
                updateStatus();
                return status; 
            }

        }

        public string shuttleInQueue
        {
            get { return placedInQueue; }
            set { placedInQueue = value; }
        }

        public void updateStatus()
        {
            XBotStatus state = _xbotCommand.GetXbotStatus(ID);
            status = Convert.ToInt32(state.XBOTState);
        }

        //--------------------------------Buffer of the Shuttle---------------------------------------
        //--------------------------------------------------------------------------------------------


        public int bufferMotionCount
        {
            get
            {
                updateBuffer();
                return bufferCount;
            }
        }
        public void updateBuffer()
        {
            bufferReturn = _xbotCommand.MotionBufferControl(ID, MOTIONBUFFEROPTIONS.RELEASEBUFFER);
            bufferCount = bufferReturn.motionBufferStatus.bufferedMotionCount;
        }
        
        //-----------------------------------------Position of the Shuttle----------------------------
        //--------------------------------------------------------------------------------------------
        public List<double> Position
        {
           get 
           {
               updatePosition();
               return position; 
           }
        }

        private void updatePosition()
        {
            XBotStatus pos = _xbotCommand.GetXbotStatus(ID);
            double[] tempPos = pos.FeedbackPositionSI;

            position[0] = tempPos[0];
            position[1] = tempPos[1];
            position[2] = tempPos[2];
            position[3] = tempPos[3];
            position[4] = tempPos[4];
            position[5] = tempPos[5];

        }

        //----------------------------------------Shuttle Tasks---------------------------------------
        //--------------------------------------------------------------------------------------------

        public List<string> Tasks
        {
            get { return tasks; }
        }

        public void insertSingleTask(int index, string task)
        {
            tasks.Insert(index, task);
        }

        public void insertTaskArray(string[] task)
        {
            tasks.AddRange(task);
        }

        public void replaceFirstTask(string task)
        {
            tasks[0] = task; 
        }

        public void removeTask(string task)
        {
            tasks.Remove(task);
        }

        public void clearAllTasks()
        {
            tasks.Clear();
        }


    }
}
