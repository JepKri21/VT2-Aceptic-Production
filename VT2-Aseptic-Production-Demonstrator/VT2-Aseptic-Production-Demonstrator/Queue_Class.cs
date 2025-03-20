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
        private static MotionsFunctions MF = new MotionsFunctions();

        public List<int> physicalQueue = new List<int>();  //Making a queue list
        private string queueName_string;

        private double[] queuePosX; //Accepting an array of x positions
        private double[] queuePosY; //Accepting an array of y positions
        private string queueDirection = "D"; //Direction to move to the queue, direct is set a deafult

        public int queueSize; //Size of the queue is dependent on how many queue positions are given

        //----------------------------------Give Name to Queue----------------------------------------
        //--------------------------------------------------------------------------------------------
        public string queueName
        {
            get { return queueName_string; }

            set
            {
                queueName_string = value;
            }

        }

        //----------------------------------Functions for Adding and removing to Queue----------------
        //--------------------------------------------------------------------------------------------

        public void addIDToList(int IDToAdd)
        {
            if (physicalQueue.Contains(IDToAdd))
            {
                //Console.WriteLine($"ID {IDToAdd} is already in the queue.");
                return;  // Exit the function early
            }

            if (physicalQueue.Count < queueSize)
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

        //----------------------------------Set Queue Positions and Direction-------------------------
        //--------------------------------------------------------------------------------------------

        public void passingStationQueuePositions(double[] queuePositionsX, double[] queuePositionsY)
        {
            queuePosX = queuePositionsX;
            queuePosY = queuePositionsY;
            queueSize = queuePositionsX.Length;
        }

        public string setQueueDirection
        {
            set { queueDirection = value; }
        }

        //----------------------------------Updating Queue--------------------------------------------
        //--------------------------------------------------------------------------------------------

        public void updateQueuePositions() //When we get a bit further, this should instead just send coordinates to the path planner for each shuttle
        {
            if (physicalQueue.Count > 0)
            {
                for (int i = 0; i < physicalQueue.Count; i++)
                {
                    MF.LinarMotion(0, physicalQueue[i], queuePosX[i], queuePosY[i], queueDirection);
                }
            }
            else
            {
                return;
            }

        }


    }
}
