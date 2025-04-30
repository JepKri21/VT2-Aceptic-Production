#include <Arduino.h>
#include "wifi_mqtt_setup.h"
//#include "filling_station.h"
#include "stoppering_station.h"

volatile int pos = 0;
bool stopperingRunning = false;

int speed = 140;
unsigned long startTime;
unsigned long timedCounter;
unsigned long prevTime = 0;
int difPos = 0;
int stepsDif = 0;

Stepper myStepper(stepsPerRevolution, 14, 13, 12, 11);

void setup() {
  Serial.begin(115200);
  initWiFiAndMQTT();   // Fra wifi_mqtt_setup.h
  //InitFilling();
  InitStoppering();
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();

  unsigned long currentMillis = millis();
  static unsigned long previousMillis = 0;
  if (currentMillis - previousMillis >= interval) {
    previousMillis = currentMillis;
    char buffer[50];
    snprintf(buffer, sizeof(buffer), "[%.3f, %.3f]", stationPosition[0], stationPosition[1]);
    client.publish(topic_pub, buffer);
  }
}

void callback(char* topic, byte* payload, unsigned int length) {
  String message;
  for (unsigned int i = 0; i < length; i++) {
    message += (char)payload[i];
  }

  // if (String(topic) == topic_sub_Filling) {
  //   if (message == "running") {
  //     FillingRunning();
  //     Serial.println("RUNNING");
  //   } else if (message == "idle") {
  //     FillingStop();
  //     Serial.println("IDLE");uca
  //   }
  // }
  if (String(topic) == topic_sub_Stoppering) {
    if (message == "running") {
      StopperingRunning();
      stopperingRunning = true;
      Serial.println("RUNNING");
    } else if (message == "idle") {
      StopperingStop();
      stopperingRunning = false;
      Serial.println("IDLE");
    }
  }
}
