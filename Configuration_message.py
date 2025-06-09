import paho.mqtt.client as mqtt
import uuid
from datetime import datetime
import json
import time


# MQTT broker details
mqtt_server = "172.20.66.135"
mqtt_port = 1883

config_topic_prefix = "AAU/Fibigerstræde/Building14/FillingLine/Configuration/DATA"


#=========== General station configuration ==========
general_station_config = {"Stations" : [
    {
        "Name": "Filling",
        "StationID": 1,
        "RequiredTool" : "Shuttle Rack",
        "Approach": [0.12, 0.12, 0.001, 0, 0, 0 ],
        "Station Position": [0.083, 0.114, 0.001, 0, 0, 90]
    },
    {
        "Name": "Stoppering",
        "StationID": 2,
        "RequiredTool" : "Shuttle Rack",
        "Approach": [0.12, 0.36, 0.001, 0, 0, 0],
        "Station Position": [0.1075, 0.3613, 0.001, 0, 0, 90]
    },
    {
        "Name": "Vision",
        "StationID": 3,
        "RequiredTool" : "Shuttle Rack",
        "Approach": [0.12, 0.6, 0.001, 0, 0, 0],
        "Station Position": [0.102, 0.587, 0.001, 0, 0, 90]
    },
    {
        "Name": "NeedleStation",
        "StationID": 4,
        "RequiredTool" : "Needle Clamp",
        "ApproachPick": [0.12, 0.12, 0.001, 0, 0, 0],
        "PickPosition": [0.12, 0.08, 0.001, 0, 0, 180],
        "ApproachPlace": [0.12, 0.12, 0.001, 0, 0, 0],  
        "PlacePosition": [0.106, 0.12, 0.001, 0, 0, 90]
    },
    {
        "Name": "FillingQueue",
        "StationID": 5,
        "RequiredTool" : "None",
        "Approach" : [0.6, 0.84, 0.001, 0, 0, 0],
        "FillingPosition1": [0.3, 0.06, 0.001, 0, 0, 0],
        "FillingPosition2": [0.42, 0.06, 0.001, 0, 0, 0],
        "FillingPosition3": [0.54, 0.06, 0.001, 0, 0, 0],
        "FillingPosition4": [0.66, 0.06, 0.001, 0, 0, 0]
    }
]
}



#=========== Full shuttle configuration ==========

#(1),2,5,6,7
full_shuttle_config = [
    {
        "ID" : "1",
        "Tool" : "Needle Clamp"
    },
    {
        "ID" : "2",
        "Tool" : "Shuttle Rack"
    },
    {
        "ID" : "5",
        "Tool" : "Shuttle Rack"
    },
    {
        "ID" : "6",
        "Tool" : "Shuttle Rack"
    },
    {
        "ID" : "7",
        "Tool" : "Shuttle Rack"
    }
]

#=========== Additional station configurations ==========
#Required tools
# The task it performs too, like Filling, incase you have two stations

filling_station = {
    "Name": "Filling", 
    "Function" : "Filling",
    "Station Specific Tasks" : {"Dispense" : "AAU/Fibigerstræde/Building14/FillingLine/Filling/CMD/Dispense", 
                                "Attach Needle" : "AAU/Fibigerstræde/Building14/FillingLine/Filling/CMD/Needle"},
    "Command Tasks" : {"Filling" : ["Approach", "Station Position", "Land", "Dispense", "Levitate", "Finished"],
                       "Needle" : ["Approach Pick", "Pick Position", "Approach Pick", "Approach Place", "Attach Needle", "Place Position", "ApproachPlace", "Dispense"]
                       }
    }

stoppering_station = {
    "Name": "Stoppering", 
    "Function" : "Stoppering",
    "Station Specific Tasks" : {"Plunge" : "AAU/Fibigerstræde/Building14/FillingLine/Stoppering/CMD/Plunge"},
    "Command Tasks" : {"Plunge" : ["Approach", "Station Position", "Land", "Plunge", "Levitate", "Finished"]}
    }

vision_station = {
    "Name": "Vision", 
    "Function" : "Vision",
    "Station Specific Tasks" : {"Snapshot" : "AAU/Fibigerstræde/Building14/FillingLine/Vision/CMD/Snapshot"},
    "Command Tasks" : {"Snapshot" : ["Approach", "Station Position", "Land", "Snapshot", "Levitate", "Finished"]}
    }

filling_queue = {
    "Name": "Filling Queue", 
    "Function" : "Queue",
    }

needle_station = {
    "Name": "Needle Station", 
    "Function" : "Attaching Needle",
    }


all_messages = [
    ("/Planar/Stations", general_station_config),
    ("/Planar/Shuttles", full_shuttle_config),
    ("/Station/Filling", filling_station),
    ("/Station/Stoppering", stoppering_station),
    ("/Station/Vision", vision_station),
    ("/Station/FillingQueue", filling_queue),
    ("/Station/NeedleStation", needle_station)
]


# Callback when connected
def on_connect(client, userdata, flags, rc):
    print("Hello")


# Callback when a message is received
def on_message(client, userdata, msg):
    print("hello")

# Create and connect MQTT client
client = mqtt.Client(protocol=mqtt.MQTTv311)  # or MQTTv5 for MQTT 5.0
client.on_connect = on_connect
client.on_message = on_message
client.connect(mqtt_server, mqtt_port, 60)

client.reconnect_delay_set(min_delay=1, max_delay=60)

client.loop_start()


time.sleep(1)
for topic, data in all_messages:
    json_message = json.dumps(data, indent=len(data))
    client.publish((config_topic_prefix+topic), json_message, retain=True)
    time.sleep(1)

