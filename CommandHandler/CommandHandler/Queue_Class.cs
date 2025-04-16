using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Formatter;
using System.Text.Json;

namespace CommandHandlerNode
{
    class QueueClass
    {

        public List<int> physicalQueue = new List<int>();  //Making a queue list
        private string queueName_string;

        private double[] queuePosX; //Accepting an array of x positions
        private double[] queuePosY; //Accepting an array of y positions

        public int queueSize; //Size of the queue is dependent on how many queue positions are given

        private MQTTPublisher mqttPublisher;
        string brokerIP = "localhost";
        //string brokerIP = "172.20.66.135";
        int port = 1883;

        public QueueClass()
        {
            InitializeMqttPublisher();
        }

        private async void InitializeMqttPublisher()
        {
            mqttPublisher = new MQTTPublisher(brokerIP, port);
            await mqttPublisher.StartAsync();
        }

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


        //----------------------------------Updating Queue--------------------------------------------
        //--------------------------------------------------------------------------------------------

        public async void updateQueuePositions() //When we get a bit further, this should instead just send coordinates to the path planner for each shuttle
        {
            if (physicalQueue.Count > 0)
            {
                for (int i = 0; i < physicalQueue.Count; i++)
                {
                    double[] queuePOS = [queuePosX[i], queuePosY[i]];
                    var message = JsonSerializer.Serialize(queuePOS);
                    Console.WriteLine($"Sending target position {queuePOS} to Xbot{physicalQueue[i]} ");
                    await mqttPublisher.PublishMessageAsync($"AAU/Fiberstræde/Building14/FillingLine/Stations/Acopos6D/Xbots/Xbot{physicalQueue[i]}/TargetPosition", message);
                }
            }
            else
            {
                return;
            }

        }


    }
}
