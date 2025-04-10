using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTTnet.Client; 
using System.Threading.Tasks;
using MQTTnet.Protocol;
using MQTTnet.Formatter;

namespace VT2_Aseptic_Production_Demonstrator
{
    class MQTTSubscriber
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _options;
        private readonly string _topic;

        // Define the MessageReceived event
        public event Action<string, string> MessageReceived;


        public MQTTSubscriber(string broker, int port, string topic)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            _topic = topic;

            _options = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port)
                .WithCleanSession()
                .WithProtocolVersion(MqttProtocolVersion.V500)  // Set to MQTT v5.0
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += MessageReceivedHandler;
            _mqttClient.ConnectedAsync += ConnectedHandler;
            _mqttClient.DisconnectedAsync += DisconnectedHandler;
        }

        public async Task StartAsync()
        {
            await _mqttClient.ConnectAsync(_options);
        }

        public async Task StopAsync()
        {
            await _mqttClient.DisconnectAsync();
        }

        public async Task SubscribeAsync(string topic)
        {
            await _mqttClient.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);
            Console.WriteLine($"Subscribed to topic: {topic}");
        }

        private async Task MessageReceivedHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            Console.WriteLine($"Received message on topic '{topic}': {payload}");

            // Raise the MessageReceived event
            MessageReceived?.Invoke(topic, payload);
        }

        private async Task ConnectedHandler(MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Connected to MQTT broker.");
            await _mqttClient.SubscribeAsync(_topic, MqttQualityOfServiceLevel.AtLeastOnce);
            Console.WriteLine($"Subscribed to topic: {_topic}");
        }

        private async Task DisconnectedHandler(MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnected from MQTT broker. Reconnecting in 5 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _mqttClient.ConnectAsync(_options);
        }
    }
}
