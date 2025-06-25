using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        private double[] stationAction; //Accepting a sinlge position 

        private double[] queuePosX; //Accepting an array of x positions
        private double[] queuePosY; //Accepting an array of y positions
        private string queueDirection = "D";

        public int queueSize; //Size of the queue is dependent on how many queue positions are given

        private MQTTPublisher mqttPublisher;
        string brokerIP = "localhost";
        int port = 1883;


        public Station_Class()
        {
            InitializeMqttPublisher();
        }

        private async void InitializeMqttPublisher()
        {
            mqttPublisher = new MQTTPublisher(brokerIP, port);
            await mqttPublisher.StartAsync();
        }

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
        public void passingStationAction(double[] action)
        {
            stationAction = action;
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

        public async void updateStationQueue() //When we get a bit further, this should instead just send coordinates to the path planner for each shuttle
        {
            if (stationQueue.Count > 0)
            {
                for (int i = 0; i < stationQueue.Count; i++)
                {
                    double[] queuePOS = [queuePosX[i], queuePosY[i]];
                    var message = JsonSerializer.Serialize(queuePOS);
                    await mqttPublisher.PublishMessageAsync($"Acopos6D/xbots/xbot{stationQueue[i]}/targetPosition", message);

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

        public async void runMovement(int id)
        {
            if (stationQueue.Count > 0)
            {
                //Publish a target position to the xbot with the relevant ID

                var message = JsonSerializer.Serialize(stationAction);
                await mqttPublisher.PublishMessageAsync($"Acopos6D/xbots/xbot{id}/targetPosition", message);
            }            
        }


        public static async Task Main(string[] args) // Change return type to Task
        {

            Station_Class client = new Station_Class();

            await Task.Delay(-1); // Keep the main thread alive
        }


    }
}
