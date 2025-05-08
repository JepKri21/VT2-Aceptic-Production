#include <Arduino.h>
#include "wifi_mqtt_setup.h"
#include "stoppering_station.h"

void setup() {
  Serial.begin(115200);
  initWiFiAndMQTT();   // Fra wifi_mqtt_setup.h
  InitStoppering();
  initializeTime();
  reconnect();
  SendMQTTMessage("null", "Idle", topic_pub_status);
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();
}

void callback(char* topic, byte* payload, unsigned int length) {
  String message;
  for (unsigned int i = 0; i < length; i++) {
    message += (char)payload[i];
  }

  if (String(topic) == topic_sub_Stoppering_Cmd) {

    readMessage(message);
    StopperingRunning();
  }
}