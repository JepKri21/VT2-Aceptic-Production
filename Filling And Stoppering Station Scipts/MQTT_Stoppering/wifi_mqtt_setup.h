#ifndef WIFI_MQTT_SETUP_H
#define WIFI_MQTT_SETUP_H

#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <time.h>

extern WiFiClient espClient;
extern PubSubClient client; 

// WiFi og MQTT data
const char* ssid = "smart_production_WIFI";
const char* pass = "aau smart production lab";
const char* mqtt_serv = "172.20.66.135";

const char* topic_pub_status = "AAU/Fibigerstræde/Building14/FillingLine/Stoppering/DATA/State";
const char* topic_sub_Stoppering_Cmd = "AAU/Fibigerstræde/Building14/FillingLine/Stoppering/CMD/Plunge";
const char* topic_pub_mqtt_status = "AAU/Fibigerstræde/Building14/FillingLine/Stoppering/DATA/MQTTConnection";
const char* topic_pub_cycle_time = "AAU/Fibigerstræde/Building14/FillingLine/Stoppering/DATA/CycleTime";


unsigned long interval = 5000;

String commandUuid; 

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
    char output[100];

    if (client.connect(clientId.c_str())) {
      doc["message"] = "Success";
      serializeJson(doc, output);
      client.publish(topic_pub_mqtt_status, output);
      client.subscribe(topic_sub_Stoppering_Cmd);
    } else {
      delay(5000);
    }
  }
}

void SendMQTTMessage(String commandUuid, String state, const char* topic){
  
  struct tm timeinfo;
  if (!getLocalTime(&timeinfo)) {
    Serial.println("Error");
    return;
  }

  char timestamp[25];
  strftime(timestamp, sizeof(timestamp), "%Y-%m-%d %H:%M:%S", &timeinfo);
  StaticJsonDocument<256> doc;
  doc["CommandUuid"] = commandUuid;
  doc["State"] = state;
  doc["TimeStamp"] = timestamp;

  char output[256];
  serializeJson(doc, output);

  client.publish(topic, output, true);
}

void sendCycleTime(double cycle_time, const char* topic){
  struct tm timeinfo;
  if (!getLocalTime(&timeinfo)) {
    Serial.println("Error");
    return;
  }

  char timestamp[25];
  strftime(timestamp, sizeof(timestamp), "%Y-%m-%d %H:%M:%S", &timeinfo);
  StaticJsonDocument<256> doc;
  doc["CommandUuid"] = commandUuid;
  doc["CycleTime"] = cycle_time;
  doc["TimeStamp"] = timestamp;

  char output[256];
  serializeJson(doc, output);

  client.publish(topic, output, true);
}


void readMessage(const String& jsonString){
  StaticJsonDocument<256> doc;
  deserializeJson(doc, jsonString); 
  commandUuid = doc["CommandUuid"].as<String>();

  Serial.println("=== Modtaget MQTT besked ===");
  Serial.println("CommandUuid: " + commandUuid);
  Serial.println("============================");

}

void initializeTime() {
  // Sætter dansk tid med automatisk sommertid
  configTzTime("CET-1CEST,M3.5.0/02,M10.5.0/03", "pool.ntp.org", "time.nist.gov");

  struct tm timeinfo;
  Serial.print("Venter på NTP tid");
  for (int i = 0; i < 10; i++) {
    if (getLocalTime(&timeinfo)) {
      Serial.println(" → Tid OK");
      return;
    }
    Serial.print(".");
    delay(1000);
  }
  Serial.println("\n⚠️ Kunne ikke hente tid fra NTP-server");
}


#endif