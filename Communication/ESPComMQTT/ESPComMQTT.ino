#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <WiFi.h>

portMUX_TYPE mux = portMUX_INITIALIZER_UNLOCKED;
// DC-Motor Pins
// Blue is VCC 5V
// Green is GND
#define ENCA 23 // YELLOW
#define ENCB 22 // WHITE
#define PWM 21 // From Motor Driver <--- De eneste der mangler at blive connected nu :D
#define IN1 16 // From Motor Driver <--- De eneste der mangler at blive connected nu :D
#define IN2 17 // From Motor Driver <--- De eneste der mangler at blive connected nu :D


#define BUTTON_PIN_BOTTOM 36
#define BUTTON_PIN_TOP 39

#define enB 19
#define in3 18
#define in4 5

unsigned long startTime;
unsigned long endTime;
unsigned long elapsedTime;

volatile int posi = 0;
int prevT = 0;
int ePrev = 0;
int eIntegral = 0;
int pos = 0;

bool fillingRunning = false;
int state = 0;
int target = 0;

int speed = 140;

double stationPosition[] = {0.660, 0.840};

// WiFi-oplysninger AAU Smart Production
const char* ssid = "smart_production_WIFI";
const char* pass = "aau smart production lab";
const char* mqtt_serv = "172.20.66.135";

//To publish the station position
unsigned long previousMillis = 0;       // Stores the last time the task ran
const long interval = 5000;            // Interval at which to run (milliseconds)

// MQTT Topics
const char* topic_pub = "AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationPosition";
const char* topic_sub = "AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/StationStatus";

WiFiClient espClient;
PubSubClient client(espClient);

// Variabler
long lastMsg = 0;
const int ledPin = 2; // Onboard LED på ESP32 (GPIO2)

void setup() {
  delay(100);
  Serial.begin(115200);
  while(!Serial);
  

  // WiFi-forbindelse
  Serial.print("Connecting to WiFi: ");
  Serial.println(ssid);
  WiFi.begin(ssid, pass); // Kun når der er kode på nettet
  //WiFi.begin(ssid);         // Kun når der IKKE er kode på nettet
  
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  
  Serial.println("");
  Serial.print("WiFi Connected, IP: ");
  Serial.println(WiFi.localIP());

  pinMode(ledPin, OUTPUT); // Opsæt LED som output

  // MQTT-opsætning
  client.setServer(mqtt_serv, 1883);
  client.setCallback(callback);

  // Moter Driver Setup
  pinMode(enB, OUTPUT);
  pinMode(in3, OUTPUT);
  pinMode(in4, OUTPUT);
  pinMode(BUTTON_PIN_BOTTOM, INPUT_PULLUP);
  pinMode(BUTTON_PIN_TOP, INPUT_PULLUP);
  stopMotor();
  analogWrite(enB,0);

  //For Stoppering Motor
  pinMode(ENCA, INPUT);
  pinMode(ENCB, INPUT);
  attachInterrupt(digitalPinToInterrupt(ENCA), readEncoder, RISING);

  pinMode(PWM, OUTPUT);
  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);

}

void loop() {
  // Sikrer MQTT-forbindelse
  if (!client.connected()) {
    reconnect();
  }
  client.loop(); // Behandler indgående MQTT-beskeder


  //Sending station position every 5 seconds
  unsigned long currentMillis = millis();

  if (currentMillis - previousMillis >= interval) {
    // Save the last time the task ran
    previousMillis = currentMillis;

    // Your code to run every 10 seconds
    char buffer[50];
    snprintf(buffer, sizeof(buffer), "[%.3f, %.3f]", stationPosition[0], stationPosition[1]);

    client.publish(topic_pub, buffer);
  }
}

// Callback-funktion: Håndterer modtagne beskeder
void callback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message received on topic: ");
  Serial.println(topic);

  String message;
  for (unsigned int i = 0; i < length; i++) {
      message += (char)payload[i];
  }
  
  Serial.print("Message: ");
  Serial.println(message);
  Serial.println("-----------------------");

  // Tjek om beskeden er "START" eller "STOP"
  if (String(topic) == topic_sub) {
    if (message == "running") {
      Serial.println("Starting the motor / LED!");
      //digitalWrite(ledPin, HIGH);  // Tænder motor/LED
      startMotor();
      StopperingRunning();

    } 
    else if (message == "STOP") {
      Serial.println("Stopping the motor / LED!");
      //digitalWrite(ledPin, LOW);   // Slukker motor/LED
      stopMotor();
    } 
    else {
      Serial.println("Unknown command.");
    }
  }
}

// Funktion til at genoprette MQTT-forbindelsen
void reconnect() {
  while (!client.connected()) {
    //Serial.print("MQTT not Connected... Trying to connect");

    // Opret unikt klient-ID
    String clientId = "ESP32Client-";
    clientId += String(random(0xffff), HEX);

    // Send forsøg på forbindelse
    StaticJsonDocument<100> doc;
    doc["clientId"] = clientId;
    doc["message"] = "Trying";
    char output[100];
    serializeJson(doc, output);  

    client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/MQTTConnection", output);

    // Forsøg at forbinde
    if (client.connect(clientId.c_str())) {
      Serial.println("Connected Successfully!");

      // Send bekræftelse på forbindelse
      //StaticJsonDocument<100> doc;
      doc["clientId"] = clientId;
      doc["message"] = "Success";

      //char output[100];
      serializeJson(doc, output);  
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/FillingStation/MQTTConnection", output);
      Serial.println("Published connection message");

      // Subscribe til topic
      client.subscribe(topic_sub);
      Serial.print("Subscribed to topic: ");
      Serial.println(topic_sub);
      
    } else {
      Serial.print("Failed, code=");
      Serial.println(client.state());
      delay(5000); // Vent 5 sekunder og prøv igen
    }
  }
}

void startMotor(){
  digitalWrite(in3, LOW); //if it is, then we run in one direction
  digitalWrite(in4, HIGH);
  analogWrite(enB, speed);
  startTime = millis(); //Start time for going down
    
    while (digitalRead(BUTTON_PIN_BOTTOM) == 0) //While we wait for button
    {


      if (millis() - startTime >= 8000) //If it takes more than 4 seconds then we say there is an error in the movement
      {
        client.publish(topic_sub, "motion_error_down"); //We just publish the error to the same place that we subscribe
        break;
      }

    }

    stopMotor();
}

void stopMotor(){

  digitalWrite(in3, HIGH);
  digitalWrite(in4, LOW);
  analogWrite(enB, speed);

  startTime = millis(); //Start time for going up
  
  while (digitalRead(BUTTON_PIN_TOP) == 0) //While we wait for button
  {
    
    if (millis() - startTime >= 8000) //If it takes more than 4 seconds then we say there is an error in the movement
    {
      client.publish(topic_sub, "motion_error_up"); //We just publish the error to the same place that we subscribe
      break;
    }
  }
  digitalWrite(in3, LOW);
  digitalWrite(in4, LOW);
  analogWrite(enB, 0);

  client.publish(topic_sub, "finished");
  
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

void StopperingRunning(){
  // Setting contraints for PID
  int kp = 1;
  int ki = 0.025;
  int kd = 0.0; 
  int tolerance = 10;

  // Calculate time difference
  long currT = micros();
  float deltaT = ((float) (currT - prevT))/( 1.0e6 );
  prevT = currT;

  portENTER_CRITICAL(&mux);
  pos = posi;
  portEXIT_CRITICAL(&mux);

  // For calculating error, dError og iError
  // error
  int e = pos - target;
  // derivative
  float dedt = (e-ePrev)/(deltaT);
  // integral
  eIntegral = eIntegral + e*deltaT;
  // control signal
  float u = kp*e + kd*dedt + ki*eIntegral;
  float pwr = fabs(u);

  if (pwr > 255) pwr = 255;
  int dir = (u < 0) ? -1 : 1;
  setMotor(dir, pwr, PWM, IN1, IN2);
  ePrev = e;

  if (state == 1 && abs(pos - 1200) < tolerance) {
    target = 0;
    state = 2;
    client.publish(topic_pub, "Reached 1200 – returning to 0");
  } else if (state == 2 && abs(pos) < tolerance) {
    setMotor(0, 0, PWM, IN1, IN2);
    state = 0;
    eIntegral = 0;
    client.publish(topic_pub, "Returned to 0 – motor stopped");
  }

  

}

