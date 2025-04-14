#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <WiFi.h>

#define BUTTON_PIN_BOTTOM 13
#define BUTTON_PIN_TOP 14

#define enB 16
#define in3 17
#define in4 18

unsigned long startTime;
unsigned long endTime;
unsigned long elapsedTime;

int speed = 140;

// WiFi-oplysninger AAU Smart Production
// const char* ssid = "smart_production_WIFI";
// const char* pass = "aau smart production lab";
// const char* mqtt_serv = "172.20.66.135";

// // WiFi-oplysninger Lucas Internetdeling
// const char* ssid = "Lucas - iPhone";
// const char* pass = "LNB12345";
// const char* mqtt_serv = "172.20.10.4";

// WiFi-oplysninger Luca Hjemmenet
const char* ssid = "5G_Router_4266C1";
const char* pass = "";
const char* mqtt_serv = "192.168.32.9";

// MQTT Topics
const char* topic_pub = "ACOPOS/movement";
const char* topic_sub = "ACOPOS/movement";

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
  // WiFi.begin(ssid, pass); // Kun når der er kode på nettet
  WiFi.begin(ssid);         // Kun når der IKKE er kode på nettet
  
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

}

void loop() {
  // Sikrer MQTT-forbindelse
  if (!client.connected()) {
    reconnect();
  }
  client.loop(); // Behandler indgående MQTT-beskeder

  // Send data hver 10. sekund <- Kan fjernes eller ændres til noget andet.
  long now = millis();
  if (now - lastMsg > 10000) {
    lastMsg = now;

    // Simulerede sensorværdier
    StaticJsonDocument<80> doc;
    char output[80];

    doc["t"] = random(0, 30);
    doc["p"] = random(10,40);
    doc["h"] = random(0,100);
    doc["g"] = random(0,10);

    serializeJson(doc, output);
    Serial.println(output);
    client.publish(topic_pub, output);
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
  if (String(topic) == "ACOPOS/movement") {
    if (message == "START") {
      Serial.println("Starting the motor / LED!");
      digitalWrite(ledPin, HIGH);  // Tænder motor/LED
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
    Serial.print("MQTT not Connected... Trying to connect");

    // Opret unikt klient-ID
    String clientId = "ESP32Client-";
    clientId += String(random(0xffff), HEX);

    // Forsøg at forbinde
    if (client.connect(clientId.c_str())) {
      Serial.println("Connected Successfully!");

      // Send bekræftelse på forbindelse
      StaticJsonDocument<100> doc;
      doc["clientId"] = clientId;
      doc["message"] = "Successfully Connected to MQTT";

      char output[100];
      serializeJson(doc, output);  
      client.publish(topic_pub, output);
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
  // digitalWrite(in3, LOW); //if it is, then we run in one direction
  // digitalWrite(in4, HIGH);
  //   int down_speed = 0;
  //   startTime = millis(); //Start time for going down
    
  //   while (digitalRead(BUTTON_PIN_BOTTOM) == 0)
  //   {

  //     if (down_speed <= speed)
  //     {
  //       down_speed = down_speed +1;
  //       analogWrite(enB, down_speed); // Send PWM signal to L298N Enable pin
  //     }

  //     if (millis() - startTime >= 8000) //If it takes more than 4 seconds then we say there is an error in the movement
  //     {
  //       Serial.println("motion_error_down");
  //       break;
  //     }
  //     //Do nothing while we wait for the button 
  //     //Put some time check here so that it doesn't run forever
  //   }
    Serial.println("MOTOREN KØRER NED");
    client.publish("ACOPOS/movement", "{\"status\":\"Filling Running\"}");
    digitalWrite(ledPin, HIGH);
    delay(5000);
    stopMotor();
}

void stopMotor(){
  // digitalWrite(in3, HIGH);
  // digitalWrite(in3, LOW);

  // digitalWrite(in3, HIGH);
  // digitalWrite(in4, LOW);


  // int up_speed = 0;
  // startTime = millis(); //Start time for going up
  
  // while (digitalRead(BUTTON_PIN_TOP) == 0)
  // {
  //   if (up_speed <= speed)
  //   {
  //     up_speed = up_speed +1;
  //     analogWrite(enB, up_speed); // Send PWM signal to L298N Enable pin
  //   }
    
  //   if (millis() - startTime >= 8000) //If it takes more than 4 seconds then we say there is an error in the movement
  //   {
  //     Serial.println("motion_error_up");
  //     break;
  //   }
  // }
  // digitalWrite(in3, LOW);
  // digitalWrite(in4, LOW);
  digitalWrite(ledPin, LOW);
  Serial.println("MOTOREN KØRER TIL TOPPEN");
  client.publish("ACOPOS/movement", "{\"status\":\"Filling idle\"}");
  
}
