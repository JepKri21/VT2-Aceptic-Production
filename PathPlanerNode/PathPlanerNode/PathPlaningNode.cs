using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace PathPlaningNode
{
   class PathPlaningNode
    {
        private MQTTSubscriber mqttSubscriber;
        private MQTTPublisher mqttPublisher;
        string brokerIP = "localhost";
        int port = 1883;
        int[] xbotsID;
        Dictionary<int, List<double[]>> trajectories = new Dictionary<int, List<double[]>>();

        public PathPlaningNode()
        {
            InitializeMqttSubscriber();
            InitializeMqttPublisher();
        }






        public static async Task Main(string[] args) // Change return type to Task
        {

            PathPlaningNode client = new PathPlaningNode();


            //Thread thread1 = new Thread(new ThreadStart(client.SendPostionsINF));


            //thread1.Start();
            await Task.Delay(-1); // Keep the main thread alive
        }
    }

}