#include <Arduino.h>
#include <PubSubClient.h>

#define servoPWM 12

#define LA_ENA 18
#define LA_IN1 17
#define LA_IN2 16

#define DC_ENB 41
#define DC_IN3 39
#define DC_IN4 40

#define DC_encoder_ENCA 48
#define DC_encoder_ENCB 47

volatile int pos = 0;
bool stopperingRunning = false; //What do we use this for, we don't use it to check for anything?

unsigned long interval = 5000;
double stationPosition[] = {0.120, 0.600};

//If we only call these after we have defined everything, then we can easily use them in the other headers without doing anything.
#include "wifi_mqtt_setup.h"
#include "stoppering_station.h"





void setup() {
  Serial.begin(115200);
  initWiFiAndMQTT();   // Fra wifi_mqtt_setup.h
  delay(1000);
  InitStoppering();


  delay(1000);
}

void loop() {
  if (!client.connected()) {
    Serial.println("We have to reconnect");
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
      //StopperingRunning();
      stopperingRunning = true;
      Serial.println("RUNNING");
    } else if (message == "idle") {
      //StopperingStop();
      stopperingRunning = false;
      Serial.println("IDLE");
    }
  }
}
