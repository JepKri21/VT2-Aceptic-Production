using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CommandHandlerNode
{
    class StationClass //AT SOME POINT MAKE SOME IF-STATEMENTS THAT CHECK IF ALL THE CORRECT THINGS HAVE BEEN SET!!!
    {

        
        private string taskName;
        public string runningTaskName;

        private string stationOccupancy = "idle"; //Making a boolean that says if the station is being used or not
        //public int currentID = 0; //Making an integer of the ID that is currently being used

        public List<int> stationQueue = new List<int>();  //Making a queue for the station
        private double[] stationAction; //Accepting a sinlge position 

        private double[] queuePosX; //Accepting an array of x positions
        private double[] queuePosY; //Accepting an array of y positions

        public int queueSize; //Size of the queue is dependent on how many queue positions are given

        private MQTTPublisher mqttPublisher;
        string brokerIP = "localhost";
        //string brokerIP = "172.20.66.135";
        int port = 1883;


        public StationClass()
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

        

        public async void updateStationQueue() //When we get a bit further, this should instead just send coordinates to the path planner for each shuttle
        {
            if (stationQueue.Count > 0)
            {
                for (int i = 0; i < stationQueue.Count; i++)
                {
                    double[] queuePOS = [queuePosX[i], queuePosY[i], 0];
                    var message = JsonSerializer.Serialize(queuePOS);
                    Console.WriteLine($"Sending target position {queuePOS} to Xbot{stationQueue[i]} ");
                    await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{stationQueue[i]}/TargetPosition", message);

                }
            }
            else
            {
                return;
            }
            
        }


        //----------------------------------Control Station Occupancy---------------------------------
        //--------------------------------------------------------------------------------------------
        public string stationStatus
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
                Console.WriteLine($"Sending target position {message} to Xbot{id} ");
                await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{id}/TargetPosition", message);
            }            
        }

        // I don't think I should do this in here, do it in the commandhandler
        


    }
}
