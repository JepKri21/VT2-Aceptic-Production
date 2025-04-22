#define ENCA 23
#define ENCB 22
#define PWM 21
#define IN2 19
#define IN1 18


int pos = 0;

void setup(){
  Serial.begin(9600);
  pinMode(ENCA, INPUT);
  pinMode(ENCB, INPUT);
  attachInterrupt(digitalPinToInterrupt(ENCA), readEncoder, RISING);
}

void loop(){
  setMotor(1, 25, PWM, IN1, IN2);
  delay(200);
  setMotor(-1, 25, PWM, IN1, IN2);
  delay(200);
  setMotor(0, 25, PWM, IN1, IN2);
  delay(200);
  Serial.println(pos);
}

void setMotor(int dir, int pwmVal, int pwm, int in1, int in2){
  analogWrite(pwm, pwmVal);
  if(dir == 1){
    digitalWrite(in1, HIGH);
    digitalWrite(in2, LOW);
  }
  if(dir == -1){
    digitalWrite(in1, LOW);
    digitalWrite(in2, HIGH);
  }
  else{
    digitalWrite(in1, LOW);
    digitalWrite(in2, LOW);
  }

}

void readEncoder(){
  int b = digitalRead(ENCB);
  if (b > 0){
    pos++;
  }
  else{
    pos--;
  }
}
