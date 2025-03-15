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

        public List<int> physicalQueue = new List<int>();  //Making a queue list
        public int maxQueueSize;

        private Action<int[]>? queuePositions; //Accepting a list of functions 

        //Maybe add a function that checks if it is full or not and return it
        public void defineSize(int size)
        {
            maxQueueSize = size;
        }

        public void addIDToList(int IDToAdd)
        {
            if (physicalQueue.Contains(IDToAdd))
            {
                //Console.WriteLine($"ID {IDToAdd} is already in the queue.");
                return;  // Exit the function early
            }

            if (physicalQueue.Count < maxQueueSize)
            {
                physicalQueue.Add(IDToAdd);
            }
            else
            {
                return; //Console.WriteLine("Physicla queue is full, unable to add");
            }
            
        }

        public void removeIDFromList(int IDToRemove)
        {
            if (physicalQueue.Count > 0)
            {
                physicalQueue.Remove(IDToRemove);
            }
            else
            {
                return; //Console.WriteLine("No shuttle to remove");
            }
            
        }

        public void passingFunctions(Action<int[]> positions)
        {
            queuePositions = positions;
        }

        public void updatingQueue()
        {
            if (physicalQueue.Count > 0 && queuePositions != null)
            {
                queuePositions(physicalQueue.ToArray()); // Pass all IDs in the queue to the motions
                
            }
            else
            {
                return; //Console.WriteLine("No positions function assigned or queue is empty.");
            }
        }

    }
}
