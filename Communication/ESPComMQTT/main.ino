#include "mqttHandler.h"
#include "FillingStation.h"
#include "config.h"

void setup(){
    Serial.begin(115200);
    SetupWiFi();
    SetupMQTT();
    SetupFilling();
}

void loop() {
    if (!mqttClient.connected()) {
        ReconnectMQTT();
    }
    mqttClient.loop();  
}
