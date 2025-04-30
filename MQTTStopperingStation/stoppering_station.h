#ifndef STOPPERIN_STATION_H
#define STOPPERIN_STATION_H

#include <Arduino.h>
#include <PubSubClient.h>
#include <Stepper.h>

extern PubSubClient client;

// #define ENCA 23
// #define ENCB 22
// #define PWM 21
// #define IN1 16
// #define IN2 17

#define ENA 18
#define IN1 17
#define IN2 16

#define ENB 41
#define IN3 39
#define IN4 40

#define ENCA 48
#define ENCB 47

// Stepper motor setup
const int stepsPerRevolution = 2048;
extern Stepper myStepper;

extern volatile int pos;
extern bool stopperingRunning;

extern unsigned long timedCounter;
extern unsigned long prevTime;
extern int difPos;
extern int stepsDif;
extern int speed;
extern unsigned long startTime;


void readEncoder() {
  int b = digitalRead(ENCB);
  if (b > 0) pos++;
  else pos--;
}

void StopperingStop() {
  digitalWrite(IN1, LOW);
  digitalWrite(IN2, HIGH);
  analogWrite(ENA, speed);
  startTime = millis();

  while (pos > 0) {
    if (millis() - startTime >= 8000) {
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/StationStatus", "motion_error_up");
      break;
    }

  }

  digitalWrite(IN1, LOW);
  digitalWrite(IN2, LOW);
  analogWrite(ENA, 0);

  client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus", "finished");
  
  // Stepper to beginning
  Serial.println("Rotating counterclockwise...");
  myStepper.step(-stepsPerRevolution); // 1 full revolution counterclockwise
  delay(1000);
  
  // Drive LA to init pos.
  digitalWrite(IN1, LOW);
  digitalWrite(IN2, HIGH);
  delay(1000);
  digitalWrite(IN1, LOW);
  digitalWrite(IN2, LOW);
  delay(5);
  

}

void StopperingRunning() {
  int target = 12000;
  digitalWrite(IN1, HIGH);
  digitalWrite(IN2, LOW);
  analogWrite(ENA, speed);
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
        analogWrite(ENA, 0);
        client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/StationStatus", "We_Hit_Something"); 
        
      }
    }

    client.loop();
    if (millis() - startTime >= 8000) {
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/StationStatus", "motion_error_down");
      break;
    }

  }
  Serial.println("Rotating clockwise...");
  myStepper.step(stepsPerRevolution); // 1 full revolution clockwise
  delay(1000);

  // Linear Actuator
  digitalWrite(IN1, HIGH);
  digitalWrite(IN2, LOW);
  delay(1000);
  digitalWrite(IN1, LOW);
  digitalWrite(IN2, LOW);
  delay(5);
  

  StopperingStop();

}

void InitStepper(){
  myStepper.step(-2.5*stepsPerRevolution); // 1 full revolution counterclockwise
  Serial.println("Stepper Motor Initialized");
}
void InitLA(){
  digitalWrite(IN1, LOW);
  digitalWrite(IN2, HIGH);
  delay(5000);
  digitalWrite(IN1, LOW);
  digitalWrite(IN2, LOW);
  Serial.println("Linear Actuator Initialized");
}

void InitDC(){
  bool initialized = false;
  digitalWrite(IN1, HIGH);
  digitalWrite(IN2, LOW);
  analogWrite(ENA, speed);

  timedCounter = millis();
  difPos = pos;
  while (!initialized) {
    prevTime = millis();

    if(prevTime - timedCounter >= 10){
      stepsDif = difPos - pos;
      Serial.println(stepsDif);
      difPos = pos;
      timedCounter = millis();
      if (abs(stepsDif) <= 25 && millis() - startTime >= 1000){
        digitalWrite(IN1, LOW);
        digitalWrite(IN2, LOW);
        analogWrite(ENA, 0);
        client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/StationStatus", "We_Hit_Something"); 
        initialized = true;
      }
    }
  }
}


void InitStoppering(){
  pinMode(ENCA, INPUT);
  pinMode(ENCB, INPUT);
  attachInterrupt(digitalPinToInterrupt(ENCA), readEncoder, RISING);

  pinMode(ENA, OUTPUT);
  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);

  myStepper.setSpeed(15); // RPM

  InitStepper();
  InitLA();
  InitDC();

}

#endif