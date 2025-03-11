using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    class Shuttle_Class
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //this class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.
        private static XBotCommands _xbotCommand = new XBotCommands();

        private int id;
        private int status;
        private List<string> tasks = new List<string>();
        private List<double> position = new List<double>() {0,0,0,0,0,0};


        //----------------------------------Give ID to Shuttle----------------------------------------
        //--------------------------------------------------------------------------------------------
        public int ID
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
                update_status();
                return status; 
            }

        }

        public void update_status()
        {
            XBotStatus state = _xbotCommand.GetXbotStatus(ID);
            status = Convert.ToInt32(state.XBOTState);
        }

        //-----------------------------------------Position of the Shuttle----------------------------
        //--------------------------------------------------------------------------------------------
        public List<double> Position
        {
           get 
           {
               update_position();
               return position; 
           }
        }

        private void update_position()
        {
            XBotStatus pos = _xbotCommand.GetXbotStatus(ID);
            double[] temp_pos = pos.FeedbackPositionSI;

            position[0] = temp_pos[0];
            position[1] = temp_pos[1];
            position[2] = temp_pos[2];
            position[3] = temp_pos[3];
            position[4] = temp_pos[4];
            position[5] = temp_pos[5];

        }

        //----------------------------------------Shuttle Tasks---------------------------------------
        //--------------------------------------------------------------------------------------------

        public List<string> Tasks
        {
            get { return tasks; }
        }

        public void insert_task(int index, string task)
        {
            tasks.Insert(index, task);
        }

        public void remove_task(string task)
        {
            tasks.Remove(task);
        }

        public void clear_all_tasks()
        {
            tasks.Clear();
        }


    }
}
