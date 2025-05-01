#ifndef STOPPERIN_STATION_H 
#define STOPPERIN_STATION_H

extern PubSubClient client;

//Stepper goes counter clockwise at Positive steps and speed of 10
//Stepper goes clockwise at positive steps and speed of 10
//I found out why it doesn't print anything, we have to choose the port again after uploading

void readEncoder() {
  int b = digitalRead(DC_encoder_ENCB);
  if (b > 0) pos++;
  else pos--;
}
/*
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

*/

void InitStepper(){
  const int stepsPerRevolution = 2048;

  pinMode(stepIN1 ,OUTPUT);
  pinMode(stepIN2 ,OUTPUT);
  pinMode(stepIN3 ,OUTPUT);
  pinMode(stepIN4 ,OUTPUT);

  Stepper myStepper(stepsPerRevolution, stepIN1, stepIN2, stepIN3, stepIN4);

  myStepper.setSpeed(10); // RPM
  delay(10); //Delay for good measure

  myStepper.step(-2.5*stepsPerRevolution); // 2.5 full revolution counterclockwise
  Serial.println("Stepper Motor Initialized");
}



void InitLA(){ //Initializing the linear actuator

  int speed = 140;

  //Setting pins as outputs
  pinMode(LA_ENA, OUTPUT);
  pinMode(LA_IN1, OUTPUT);
  pinMode(LA_IN2, OUTPUT);
  delay(10); //Delay for good measure

  //Starting the Linear Actuator going backwards until it reaches the backstop
  digitalWrite(LA_IN1, LOW);
  digitalWrite(LA_IN2, HIGH);
  analogWrite(LA_ENA, speed);

  delay(5000); //Adjust for the maximum time it takes to move from fully extended to no extension

  //Stopping the Linear Actuator
  digitalWrite(LA_IN1, LOW);
  digitalWrite(LA_IN2, LOW);
  Serial.println("Linear Actuator Initialized");
}



void InitDC(){

  int speed = 140; //Speed of the motor, adjust as needed
  
  //The rest of these are used to stop the motor when it gets resistance
  unsigned long startTime;
  unsigned long timedCounter;
  unsigned long prevTime = 0;
  int difPos = 0;
  int stepsDif = 0;

  StaticJsonDocument<100> doc;

  //Boolean used to stop the motor when it has reached the top
  bool initialized = false; 

  //Setting the pin modes for the Encoder and then attaching an interrupt
  pinMode(DC_encoder_ENCA, INPUT);
  pinMode(DC_encoder_ENCB, INPUT);
  attachInterrupt(digitalPinToInterrupt(DC_encoder_ENCA), readEncoder, RISING);

  //Setting the pin modes for the DC motor controller pins
  pinMode(DC_ENB, OUTPUT);
  pinMode(DC_IN3, OUTPUT);
  pinMode(DC_IN4, OUTPUT);
  delay(10); //Delay for good measure

  //Starting the DC motor movement
  digitalWrite(DC_IN3, HIGH);
  digitalWrite(DC_IN4, LOW);
  analogWrite(DC_ENB, speed);

  startTime = millis(); //Used to make sure that we only check 1 second after it has started moving (could maybe be a problem)
  timedCounter = millis(); //Used to make sure that we only check the motor position difference every 10 milliseconds
  difPos = pos; //Checking to see what the first position is so that we can compare it

  while (!initialized) {

    prevTime = millis(); //Checking what the previous time was and comparing it to the timedCounter

    if(prevTime - timedCounter >= 10){ //If 10 milliseconds have passed since we last checked the speed of the motor we will check it again

      stepsDif = difPos - pos; //Calculating the difference in steps or the speed of the motor in the last 10 milliseconds
      //Serial.println(stepsDif); //Used to check what the normal speed of the motor is 

      //Resetting these two to be used again
      difPos = pos;
      timedCounter = millis();

      if (abs(stepsDif) <= 18 && millis() - startTime >= 1000){ //If the speed of the motor is lower than a constant AND at least 1 seconds has passed since we started the motor
        digitalWrite(DC_IN3, LOW); //Then we stop the motor
        digitalWrite(DC_IN4, LOW);

        analogWrite(DC_ENB, 0); //Setting the speed to 0, maybe we don't have to do this, just always have a constant speed
        delay(2000);

        Serial.println("We have to reconnect");
        reconnect();
        
        delay(1000);
        Serial.println("Should publish station status now");
        doc["message"] = "We Reached The Top";
        char initialized_done_output[100];
        serializeJson(doc, initialized_done_output);
        client.publish(topic_sub_Stoppering, initialized_done_output); 
        initialized = true; //Used to stop the while-loop
        //We probably also have to reset "pos" right here to set this point as 0 on the encoders
      }
    }
  }
}


void InitStoppering(){ //These functions could have speeds to give
  InitStepper();
  InitLA();
  InitDC();

}

#endif