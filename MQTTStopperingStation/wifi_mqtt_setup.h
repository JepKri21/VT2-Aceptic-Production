#ifndef WIFI_MQTT_SETUP_H
#define WIFI_MQTT_SETUP_H

#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>

extern WiFiClient espClient;
extern PubSubClient client; //Why do we call an external here when we create this class below?

// WiFi og MQTT data
const char* ssid = "smart_production_WIFI";
const char* pass = "aau smart production lab";
const char* mqtt_serv = "172.20.66.135";

const char* topic_pub = "AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/StationPosition";
const char* topic_sub_Stoppering = "AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/StationStatus";



WiFiClient espClient;
PubSubClient client(espClient); //RIGHT HERE

// Forward declarations
void callback(char*, byte*, unsigned int);



void reconnect() {
  while (!client.connected()) {
    String clientId = "ESP32Client-" + String(random(0xffff), HEX);
    StaticJsonDocument<100> doc;
    doc["clientId"] = clientId;
    char output[100];
    Serial.println("Trying to establish an MQTT connection");

    if (client.connect(clientId.c_str())) {
      Serial.println("Have have established an MQTT connection");
      doc["message"] = "Success";
      serializeJson(doc, output);
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/MQTTConnection", output);
      client.subscribe(topic_sub_Stoppering);
    } else {
      delay(5000);
    }
  }
}

void initWiFiAndMQTT() {
  WiFi.begin(ssid, pass);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("WiFi Connected");

  client.setServer(mqtt_serv, 1883);
  client.setCallback(callback);
  reconnect();
}


#endif