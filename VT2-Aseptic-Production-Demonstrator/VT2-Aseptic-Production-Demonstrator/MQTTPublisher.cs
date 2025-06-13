using MQTTnet.Client;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using MQTTnet.Formatter;

namespace VT2_Aseptic_Production_Demonstrator
{
    class MQTTPublisher
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _options;
        private readonly string _topic;

        public MQTTPublisher(string broker, int port)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();            

            // Setup MQTT connection options
            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)  // Connect to Mosquitto Broker (localhost or your broker IP)
                .WithCleanSession()  // Set clean session to true (start fresh on each connect)
                .WithProtocolVersion(MqttProtocolVersion.V500)  // Set to MQTT v5.0
                .Build();

            // Event handler for when the client is connected
            _mqttClient.ConnectedAsync += ConnectedHandler;
            _mqttClient.DisconnectedAsync += DisconnectedHandler;
        }

        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        public async Task StartAsync()
        {
            // Connect to the broker
            await _mqttClient.ConnectAsync(_options);
        }

        public async Task StopAsync()
        {
            // Disconnect from the broker
            await _mqttClient.DisconnectAsync();
        }

        private async Task ConnectedHandler(MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Connected to MQTT broker.");
        }

        private async Task DisconnectedHandler(MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnected from MQTT broker. Reconnecting in 5 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _mqttClient.ConnectAsync(_options);
        }

        public async Task PublishMessageAsync(string topic, string message)
        {
            if (_mqttClient.IsConnected)
            {
                // Create the MQTT message
                var mqttMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(message)  // Set the message payload
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)  // Set QoS to Exactly Once
                    .WithRetainFlag()  // Optionally retain the message
                    .Build();

                // Publish the message to the broker
                await _mqttClient.PublishAsync(mqttMessage);
                //Console.WriteLine($"Message published to topic '{topic}': {message}");
            }
            else
            {
                Console.WriteLine("MQTT client is not connected. Cannot publish message.");
            }
        }
    }
}
