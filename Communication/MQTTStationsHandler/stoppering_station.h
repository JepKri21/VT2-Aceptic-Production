#ifndef STOPPERIN_STATION_H
#define STOPPERIN_STATION_H

#include <Arduino.h>
#include <PubSubClient.h>

extern PubSubClient client;

#define ENCA 23
#define ENCB 22
#define PWM 21
#define IN1 16
#define IN2 17

//int speed = 140;
volatile int pos = 0;
//unsigned long startTime;
unsigned long timedCounter;
unsigned long prevTime = 0;
int difPos = 0;
int stepsDif = 0;

bool stopperingRunning = false;

void readEncoder() {
  int b = digitalRead(ENCB);
  if (b > 0) pos++;
  else pos--;
}

void StopperingStop() {
  digitalWrite(IN1, LOW);
  digitalWrite(IN2, HIGH);
  analogWrite(PWM, speed);
  startTime = millis();

  while (pos > 0) {
    if (millis() - startTime >= 8000) {
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus", "motion_error_up");
      break;
    }

  }

  digitalWrite(IN1, LOW);
  digitalWrite(IN2, LOW);
  analogWrite(PWM, 0);

  client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus", "finished");
}

void StopperingRunning() {
  int target = 12000;
  digitalWrite(IN1, HIGH);
  digitalWrite(IN2, LOW);
  analogWrite(PWM, speed);
  startTime = millis();
  timedCounter = millis();
  difPos = pos;
  while (pos <= target && stopperingRunning) {
    prevTime = millis();

    if(prevTime - timedCounter >= 10){
      stepsDif = difPos - pos;
      Serial.println(stepsDif);
      difPos = pos;
      timedCounter = millis();
      if (abs(stepsDif) <= 25 && millis() - startTime >= 1000){
        digitalWrite(IN1, LOW);
        digitalWrite(IN2, LOW);
        analogWrite(PWM, 0);
        client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus", "We_Hit_Something"); 
        
      }
    }

    client.loop();
    if (millis() - startTime >= 8000) {
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus", "motion_error_down");
      break;
    }
  }

  StopperingStop();
}

void InitStoppering(){

  pinMode(ENCA, INPUT);
  pinMode(ENCB, INPUT);
  attachInterrupt(digitalPinToInterrupt(ENCA), readEncoder, RISING);

  pinMode(PWM, OUTPUT);
  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);

}

#endif