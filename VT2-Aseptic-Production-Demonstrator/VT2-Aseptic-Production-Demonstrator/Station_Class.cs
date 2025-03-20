using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    class Station_Class //AT SOME POINT MAKE SOME IF-STATEMENTS THAT CHECK IF ALL THE CORRECT THINGS HAVE BEEN SET!!!
    {
        private static MotionsFunctions MF = new MotionsFunctions();
        
        private string taskName;
        public string runningTaskName;

        private bool stationOccupancy = false; //Making a boolean that says if the station is being used or not
        public int currentID = 0; //Making an integer of the ID that is currently being used

        public List<int> stationQueue = new List<int>();  //Making a queue for the station
        private Action<int>? stationFunctions; //Accepting a list of functions 

        private double[] queuePosX; //Accepting an array of x positions
        private double[] queuePosY; //Accepting an array of y positions
        private string queueDirection = "D";

        public int queueSize; //Size of the queue is dependent on how many queue positions are given


        //----------------------------------Give Name to Station--------------------------------------
        //--------------------------------------------------------------------------------------------

        public string stationTaskName
        {
            get { return taskName; }

            set
            {
                taskName = value;
                runningTaskName = "running" + value;
            }

        }

        //----------------------------------Add and Remove From Queue---------------------------------
        //--------------------------------------------------------------------------------------------

        public void addIDToQueue(int IDToAdd)
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
        public void removeIDFromQueue(int IDToRemove)
        {
            stationQueue.Remove(IDToRemove);
        }

        //----------------------------------Give Station movements and queue positions----------------
        //--------------------------------------------------------------------------------------------
        public void passingStationMovement(Action<int> movements)
        {
            stationFunctions = movements;
        }

        public void passingStationQueuePositions(double[] queuePositionsX, double[] queuePositionsY)
        {
            queuePosX = queuePositionsX;
            queuePosY = queuePositionsY;
            queueSize = queuePositionsX.Length;
        }

        //----------------------------------Updating Station Queue------------------------------------
        //--------------------------------------------------------------------------------------------

        public string setQueueDirection
        {
            set { queueDirection = value; }
        }

        public void updateStationQueue() //When we get a bit further, this should instead just send coordinates to the path planner for each shuttle
        {
            if (stationQueue.Count > 0)
            {
                for (int i = 0; i < stationQueue.Count; i++)
                {
                    MF.LinarMotion(0, stationQueue[i], queuePosX[i], queuePosY[i], queueDirection);
                }
            }
            else
            {
                return;
            }
            
        }


        //----------------------------------Control Station Occupancy---------------------------------
        //--------------------------------------------------------------------------------------------
        public bool stationOccupied
        {
            get {return stationOccupancy;}
            set { stationOccupancy = value; }
         
        }

        //----------------------------------Run the movements of the station--------------------------
        //--------------------------------------------------------------------------------------------

        public void runMovement(int id)
        {
            if (stationQueue.Count > 0)
            {
                stationFunctions(id);
            }            
        }


    }
}
