#ifndef FILLING_STATION_H
#define FILLING_STATION_H

#include <Arduino.h>
#include <PubSubClient.h>

extern PubSubClient client;

#define BUTTON_PIN_BOTTOM 36
#define BUTTON_PIN_TOP 39

#define enB 19
#define in3 18
#define in4 5

int speed = 140;

unsigned long startTime;

void FillingStop() {
  digitalWrite(in3, HIGH);
  digitalWrite(in4, LOW);
  analogWrite(enB, speed);
  startTime = millis();

  while (digitalRead(BUTTON_PIN_TOP) == 0) {
    if (millis() - startTime >= 8000) {
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus", "motion_error_up");
      break;
    }
  }

  digitalWrite(in3, LOW);
  digitalWrite(in4, LOW);
  analogWrite(enB, 0);

  client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus", "finished");
}

void FillingRunning() {
  digitalWrite(in3, LOW);
  digitalWrite(in4, HIGH);
  analogWrite(enB, speed);
  startTime = millis();

  while (digitalRead(BUTTON_PIN_BOTTOM) == 0) {
    if (millis() - startTime >= 8000) {
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus", "motion_error_down");
      break;
    }
  }

  FillingStop();
}

void InitFilling(){
  pinMode(enB, OUTPUT);
  pinMode(in3, OUTPUT);
  pinMode(in4, OUTPUT);
  pinMode(BUTTON_PIN_BOTTOM, INPUT_PULLUP);
  pinMode(BUTTON_PIN_TOP, INPUT_PULLUP);

  analogWrite(enB, 0);
  FillingStop();

}


#endif