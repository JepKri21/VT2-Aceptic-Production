#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <WiFi.h>

#define BUTTON_PIN_BOTTOM 36
#define BUTTON_PIN_TOP 39

#define enB 19
#define in3 18
#define in4 5

unsigned long startTime;
unsigned long endTime;
unsigned long elapsedTime;

int speed = 140;

// WiFi-oplysninger AAU Smart Production
const char* ssid = "smart_production_WIFI";
const char* pass = "aau smart production lab";
const char* mqtt_serv = "172.20.66.135";

// MQTT Topics
const char* topic_pub = "AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/StationStatus";
const char* topic_sub = "AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/StationStatus";

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
  StopStoppering();
  analogWrite(enB,0);

}

void loop() {
  // Sikrer MQTT-forbindelse
  if (!client.connected()) {
    reconnect();
  }
  client.loop(); // Behandler indgående MQTT-beskeder

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
    } 
    else if (message == "STOP") {
      Serial.println("Stopping the motor / LED!");
      digitalWrite(ledPin, LOW);   // Slukker motor/LED
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

    client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/MQTTConnection", output);

    // Forsøg at forbinde
    if (client.connect(clientId.c_str())) {
      Serial.println("Connected Successfully!");

      // Send bekræftelse på forbindelse
      //StaticJsonDocument<100> doc;
      doc["clientId"] = clientId;
      doc["message"] = "Success";

      //char output[100];
      serializeJson(doc, output);  
      client.publish("AAU/Fiberstræde/Building14/FillingLine/Stations/StopperingStation/MQTTConnection", output);
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

void RunStoppering(){
  digitalWrite(in3, LOW); //if it is, then we run in one direction
  digitalWrite(in4, HIGH);
  analogWrite(enB, speed);
  startTime = millis(); //Start time for going down
    
    while (digitalRead(BUTTON_PIN_BOTTOM) == 0) //While we wait for button
    {


      if (millis() - startTime >= 8000) //If it takes more than 4 seconds then we say there is an error in the movement
      {
        client.publish(topic_pub, "motion_error_down");
        break;
      }

    }

    StopStoppering();
}

void StopStoppering(){

  digitalWrite(in3, HIGH);
  digitalWrite(in4, LOW);
  analogWrite(enB, speed);

  startTime = millis(); //Start time for going up
  
  while (digitalRead(BUTTON_PIN_TOP) == 0) //While we wait for button
  {
    
    if (millis() - startTime >= 8000) //If it takes more than 4 seconds then we say there is an error in the movement
    {
      client.publish(topic_pub, "motion_error_up");
      break;
    }
  }
  digitalWrite(in3, LOW);
  digitalWrite(in4, LOW);
  analogWrite(enB, 0);

  client.publish(topic_pub, "idle");
  
}
