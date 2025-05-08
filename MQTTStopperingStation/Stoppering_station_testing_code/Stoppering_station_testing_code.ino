#include <ESP32Servo.h>
#include <Arduino.h>

#define servoPWM 2

#define buttonPin 4

#define DC_ENB 41
#define DC_IN3 39
#define DC_IN4 40

#define LA_ENA 18
#define LA_IN1 17
#define LA_IN2 16

#define DC_encoder_ENCA 48
#define DC_encoder_ENCB 47

Servo myservo;

/*
volatile int pos = 0;

void readEncoder() {
  int b = digitalRead(DC_encoder_ENCB);
  if (b > 0) pos++;
  else pos--;
}
*/
void setup() {
  Serial.begin(9600);
  delay(500);
  Serial.println("Initializing Servo");
  InitServo();
  Serial.println("Servo Initialized");
  delay(1000);
  Serial.println("Initializing Linear Actuator");
  InitLA();
  Serial.println("Linear Actuator Initialized");
  Serial.println("Initializing DC Motor");
  InitDC();
  Serial.println("DC Motor Initialized");
  delay(5000);

  //runStoppering();
}

void InitLA(){ //Initializing the linear actuator

  int speed = 140;

  //Setting pins as outputs
  pinMode(LA_ENA, OUTPUT);
  pinMode(LA_IN1, OUTPUT);
  pinMode(LA_IN2, OUTPUT);
  delay(10); //Delay for good measure

  //Starting the Linear Actuator going up until it reaches the backstop
  digitalWrite(LA_IN1, HIGH);
  digitalWrite(LA_IN2, LOW);
  analogWrite(LA_ENA, speed);

  delay(6500); //Adjust for the maximum time it takes to move from fully extended to no extension

  //Stopping the Linear Actuator
  digitalWrite(LA_IN1, LOW);
  digitalWrite(LA_IN2, LOW);
}


void InitDC(){

  int speed = 200; //Speed of the motor, adjust as needed
  
  pinMode(buttonPin, INPUT_PULLUP); // Enable internal pull-up resistor

  //Setting the pin modes for the Encoder and then attaching an interrupt
  //pinMode(DC_encoder_ENCA, INPUT);
  //pinMode(DC_encoder_ENCB, INPUT);
  //attachInterrupt(digitalPinToInterrupt(DC_encoder_ENCA), readEncoder, RISING);

  //Setting the pin modes for the DC motor controller pins
  pinMode(DC_ENB, OUTPUT);
  pinMode(DC_IN3, OUTPUT);
  pinMode(DC_IN4, OUTPUT);
  delay(10); //Delay for good measure

  //Starting the DC motor movement downward
  digitalWrite(DC_IN3, LOW);
  digitalWrite(DC_IN4, HIGH);
  analogWrite(DC_ENB, speed);

  // Wait for button press (active LOW)
  while (digitalRead(buttonPin) == LOW) {
  // Do nothing, just wait
  }
  //Then we have reached the bottom, so now we can move back up some amount

  digitalWrite(DC_IN3, HIGH);
  digitalWrite(DC_IN4, LOW);

  delay(1500);

  digitalWrite(DC_IN3, LOW);
  digitalWrite(DC_IN4, LOW);


}



void runStoppering()
{
  digitalWrite(DC_IN3, LOW); //Move DC motor down
  digitalWrite(DC_IN4, HIGH);
  digitalWrite(DC_ENB, 200);

  while (digitalRead(buttonPin) == LOW) { //Wait for button
  // Do nothing, just wait
  }

  digitalWrite(DC_IN3, LOW); //Stop DC motor
  digitalWrite(DC_IN4, LOW);
 

  delay(500);
  
  int pos = 0;
  for(pos = 40; pos <= 180; pos++)
  {
    myservo.write(pos);
    delay(15);
  }
  delay(750);
  for(pos = 180; pos >= 40; pos--)
  {
    myservo.write(pos);
    delay(15);
  }

  digitalWrite(LA_IN1, LOW); //Move linear actuator down
  digitalWrite(LA_IN2, HIGH);
  digitalWrite(LA_ENA, 140);

  delay(10000);

  digitalWrite(LA_IN1, HIGH); //Move linear actuator up
  digitalWrite(LA_IN2, LOW);

  delay(6500);

  digitalWrite(LA_IN1, LOW); //Stop linear actuator
  digitalWrite(LA_IN2, LOW);

  digitalWrite(DC_IN3, HIGH); //Move DC motor up
  digitalWrite(DC_IN4, LOW);
  digitalWrite(DC_ENB, 200);
  delay(1500);
  
  digitalWrite(DC_IN3, LOW); //Stop DC motor 
  digitalWrite(DC_IN4, LOW);
}

void loop() {

  
}


void InitServo()
{
  int startPos = 40;
  myservo.attach(servoPWM);
  //myservo.attach(servoPWM, 500, 2400, 0);
  delay(500);
  myservo.write(startPos);

}


