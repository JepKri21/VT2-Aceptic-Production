// See https://aka.ms/new-console-template for more information

using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;


namespace VT2_Aseptic_Production_Demonstrator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string broker = "localhost";  // Use your broker's address (e.g., "localhost" or "192.168.1.100")
            int port = 1883;  // Default Mosquitto port
            string topic = "test/topic";  // Topic to publish and subscribe to

            // Create and start the MqttSubscriber (to receive messages)
            var subscriber = new MQTTSubscriber(broker, port, topic);
            await subscriber.StartAsync();

            // Create and start the MqttPublisher (to send messages)
            var publisher = new MQTTPublisher(broker, port, topic);
            await publisher.StartAsync();

            // Publish a few messages
            await publisher.PublishMessageAsync("Hello, MQTT! This is a test message.");
            await publisher.PublishMessageAsync("Second message to the topic.");

            // Allow time for subscriber to receive messages
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            // Stop both publisher and subscriber
            await subscriber.StopAsync();
            await publisher.StopAsync();
        }
    }
}


