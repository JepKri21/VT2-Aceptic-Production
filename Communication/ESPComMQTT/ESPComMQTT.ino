#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <WiFi.h>

// WiFi-oplysninger
const char* ssid = "smart_production_WIFI";
const char* pass = "aau smart production lab";
const char* mqtt_serv = "172.20.66.135";

// MQTT Topics
const char* topic_pub = "ACOPOS/movement";
const char* topic_sub = "ACOPOS/movement";

WiFiClient espClient;
PubSubClient client(espClient);

// Variabler
long lastMsg = 0;
const int ledPin = 2; // Onboard LED på ESP32 (GPIO2)

void setup() {
  Serial.begin(115200);
  pinMode(ledPin, OUTPUT); // Opsæt LED som output

  // WiFi-forbindelse
  Serial.print("Connecting to WiFi: ");
  Serial.println(ssid);
  WiFi.begin(ssid, pass);
  
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  
  Serial.println("\nWiFi Connected!");
  Serial.print("IP Address: ");
  Serial.println(WiFi.localIP());

  // MQTT-opsætning
  client.setServer(mqtt_serv, 1883);
  client.setCallback(callback);
}

void loop() {
  // Sikrer MQTT-forbindelse
  if (!client.connected()) {
    reconnect();
  }
  client.loop(); // Behandler indgående MQTT-beskeder

  // Send data hver 10. sekund
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
      client.publish("ACOPOS/status", "{\"status\":\"Filling started\"}");
    } 
    else if (message == "STOP") {
      Serial.println("Stopping the motor / LED!");
      digitalWrite(ledPin, LOW);   // Slukker motor/LED
      client.publish("ACOPOS/status", "{\"status\":\"Filling stopped\"}");
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
