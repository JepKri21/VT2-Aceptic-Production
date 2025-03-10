using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    class Queue_Class
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //this class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.
        private static XBotCommands _xbotCommand = new XBotCommands();

        public List<int> physical_queue = new List<int>();  //Making a queue for the station
        public int max_queue_size;

        private Action<int[]>? queue_positions; //Accepting a list of functions 

        //Maybe add a function that checks if it is full or not and return it
        public void define_size(int size)
        {
            max_queue_size = size;
        }

        public void add_ID_to_list(int ID_to_add)
        {
            if (physical_queue.Contains(ID_to_add))
            {
                Console.WriteLine($"ID {ID_to_add} is already in the queue.");
                return;  // Exit the function early
            }

            if (physical_queue.Count < max_queue_size)
            {
                physical_queue.Add(ID_to_add);
            }
            else
            {
                Console.WriteLine("Physicla queue is full, unable to add");
            }
            
        }

        public void remove_ID_from_list(int ID_to_remove)
        {
            if (physical_queue.Count > 0)
            {
                physical_queue.Remove(ID_to_remove);
            }
            else
            {
                Console.WriteLine("No shuttle to remove");
            }
            
        }

        public void passing_functions(Action<int[]> positions)
        {
            queue_positions = positions;
        }

        public void updating_queue()
        {
            if (physical_queue.Count > 0 && queue_positions != null)
            {
                queue_positions(physical_queue.ToArray()); // Pass all IDs in the queue
                
            }
            else
            {
                Console.WriteLine("No positions function assigned or queue is empty.");
            }
        }

    }
}
