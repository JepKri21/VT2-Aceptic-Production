#ifndef WIFI_MQTT_SETUP_H
#define WIFI_MQTT_SETUP_H

#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>

extern WiFiClient espClient;
extern PubSubClient client;

// WiFi og MQTT data
const char* ssid = "smart_production_WIFI";
const char* pass = "aau smart production lab";
const char* mqtt_serv = "172.20.66.135";

const char* topic_pub = "AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationPosition";
const char* topic_sub_Filling = "AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus";
const char* topic_sub_Stoppering = "AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/StationStatus";

unsigned long interval = 5000;
double stationPosition[] = {0.660, 0.840};

WiFiClient espClient;
PubSubClient client(espClient);

// Forward declarations
void callback(char*, byte*, unsigned int);

void initWiFiAndMQTT() {
  WiFi.begin(ssid, pass);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("WiFi Connected");

  client.setServer(mqtt_serv, 1883);
  client.setCallback(callback);
}

void reconnect() {
  while (!client.connected()) {
    String clientId = "ESP32Client-" + String(random(0xffff), HEX);
    StaticJsonDocument<100> doc;
    doc["clientId"] = clientId;
    doc["message"] = "Trying";
    char output[100];
    serializeJson(doc, output);
    client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/MQTTConnection", output);

    if (client.connect(clientId.c_str())) {
      doc["message"] = "Success";
      serializeJson(doc, output);
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/MQTTConnection", output);
      client.subscribe(topic_sub_Filling);
      client.subscribe(topic_sub_Stoppering);
    } else {
      delay(5000);
    }
  }
}

#endif