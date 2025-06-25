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
#Here you just need to add the Name, StationID, Required tool and then all the relevant positions for the new station
#These positions can have any name
general_station_config = {"Stations" : [
    {
        "Name": "Filling",
        "StationID": 1,
        "RequiredTool" : "Shuttle Rack",
        "Approach": [0.12, 0.12, 0.001, 0, 0, 90 ],
        "StationPosition": [0.085, 0.113, 0.001, 0, 0, 90]
    },
    {
        "Name": "Stoppering",
        "StationID": 2,
        "RequiredTool" : "Shuttle Rack",
        "Approach": [0.12, 0.36, 0.001, 0, 0, 90],
        "StationPosition": [0.110, 0.360, 0.001, 0, 0, 90]
    },
    {
        "Name": "Vision",
        "StationID": 3,
        "RequiredTool" : "Shuttle Rack",
        "Approach": [0.12, 0.6, 0.001, 0, 0, 90],
        "StationPosition": [0.102, 0.587, 0.001, 0, 0, 90]
    },
    {
        "Name" : "Away",
        "StationID": 6,
        "RequiredTool" : "Shuttle Rack",
        "AwayPosition" : [0.600, 0.840, 0.001,0,0,0]
    },
    {
        "Name" : "Done",
        "StationID": 6,
        "RequiredTool" : "Shuttle Rack",
        "Done1" : [0.660, 0.120, 0.001,0,0,0],
        "Done2" : [0.660, 0.360, 0.001,0,0,0],
        "Done3" : [0.660, 0.600, 0.001,0,0,0]
    },
    {
        "Name": "NeedleStation",
        "StationID": 4,
        "RequiredTool" : "Needle Clamp",
        "ApproachPick": [0.12, 0.12, 0.001, 0, 0, 180],
        "PickPosition": [0.12, 0.08, 0.001, 0, 0, 180],
        "ApproachPlace": [0.12, 0.12, 0.001, 0, 0, 90],  
        "PlacePosition": [0.106, 0.12, 0.001, 0, 0, 90],
        "Away" : [0.179,0.179, 0.001, 0, 0, 90],
        "Storage" : [0.420, 0.900, 0.001, 0, 0, 90]
    },
    {
        "Name": "FillingQueue",
        "StationID": 5,
        "RequiredTool" : "None",
        "Approach" : [0.60, 0.360, 0.001, 0, 0, 0],
        "FillingPosition1": [0.3, 0.06, 0.001, 0, 0, 0],
        "FillingPosition2": [0.42, 0.06, 0.001, 0, 0, 0],
        "FillingPosition3": [0.54, 0.06, 0.001, 0, 0, 0],
        "FillingPosition4": [0.66, 0.06, 0.001, 0, 0, 0]
    }
]
}



#=========== Full shuttle configuration ==========
# To add other shuttles, just copy and paste one of the shuttles and change the ID and maybe the tool
full_shuttle_config = [
    {
        "ID" : "2",
        "Tool" : "Shuttle Rack"
    },
    {
        "ID" : "5",
        "Tool" : "Shuttle Rack"
    },
    {
        "ID" : "7",
        "Tool" : "Shuttle Rack" #This tool is the one we used to hold the syringes
    },
    {
        "ID" : "1",
        "Tool" : "Needle Clamp" #This tool we used to hold the filling needle and attach it to the station
    }
]

#=========== Additional station configurations ==========
#This is the station data that is not apart of the Planar system (although it is weird to have these as seperate messages from the rest of the station data)
#For these station you must have a Name that matches the Name from the other station configuration
# Also a function, station specific task (This could be done WAY better, but we were running out of time. Also, this is just optional) and lastly a list of command tasks.
# You might also add a queue direction, where a shuttle will go incase the station is occupied.

filling_station = {
    "Name": "Filling", 
    "Function" : "Filling",
    "QueueDirection" : "FillingQueue",
    "Station Specific Tasks" : {"Dispense" : "AAU/Fibigerstræde/Building14/FillingLine/Filling/CMD/Dispense"},
    "Command Tasks" : {"Filling" : [ "Approach","Rotation","StationPosition", "Land", "Dispense", "Levitate", "Finished"]}
    }

stoppering_station = {
    "Name": "Stoppering", 
    "Function" : "Stoppering",
    "Station Specific Tasks" : {"Plunge" : "AAU/Fibigerstræde/Building14/FillingLine/Stoppering/CMD/Plunge"},
    "Command Tasks" : {"Plunge" : ["Approach", "Rotation", "StationPosition", "Land", "Plunge", "Levitate", "Finished"]}
    }



vision_station = {
    "Name": "Vision", 
    "Function" : "Vision",
    "Station Specific Tasks" : {"Snapshot" : "AAU/Fibigerstræde/Building14/FillingLine/Vision/CMD/Snapshot"},
    "Command Tasks" : {"Snapshot" : ["Approach", "Rotation", "StationPosition", "Land", "Snapshot", "Levitate", "Finished"]}
    }

away_station = {
    "Name": "Away", 
    "Function" : "Away",
    "Command Tasks" : {"Away" : ["AwayPosition", "Finished"]}
}

filling_queue = {
    "Name": "FillingQueue", 
    "Function" : "Queue",
    "Command Tasks" : {"QueuePositions" : ["FillingPosition4","FillingPosition3","FillingPosition2","FillingPosition1", "Finished"]},
    "QueueOccupancy" : []
    }


finished_station = {
    "Name": "Done", 
    "Function" : "Done",
    "Command Tasks" : {"Done" : ["Done1", "Done2", "Done3"]}
}

needle_station = {
    "Name": "NeedleStation", 
    "Function" : "NeedleStation",
    "Station Specific Tasks" : {"AttachNeedle" : "AAU/Fibigerstræde/Building14/FillingLine/Filling/CMD/Needle"},
    "Command Tasks" : {"Needle" : ["AttachNeedle", "ApproachPick", "Rotation", "PickPosition", "ApproachPick", "ApproachPlace", "Rotation", "PlacePosition", "ApproachPlace", "Rotation", "Away", "Storage", "Finished"]}
    }


#=========== General system configuration ==========
batch = { #Here you simply change the batchsize and then the order of commands that a shuttle must go through to complete 1 unit.
    "BatchSize" : 21,
    "CommandOrder" : ["Filling", "Stoppering", "Vision", "Away"] #We needed it to move "Away" from the vision station such that another shuttle could go there
}
#In the command handler we check these commands with the functions of the stations

#=========== ALL System configuration messages ==========
#REMEMBER TO ADD ANY NEW STATION TO THIS MESSAGE-------------------------------------------------------!
all_messages = [
    ("/Planar/Stations", general_station_config),
    ("/Planar/Shuttles", full_shuttle_config),
    ("/System/BatchInfo", batch),
    ("/Station/Filling", filling_station),
    ("/Station/Stoppering", stoppering_station),
    ("/Station/Vision", vision_station),
    ("/Station/Away", away_station),
    ("/Station/Finished", finished_station),
    ("/Station/FillingQueue", filling_queue),
    ("/Station/NeedleStation", needle_station)
]


# Callback when connected
def on_connect(client, userdata, flags, rc):
    print("")


# Callback when a message is received
def on_message(client, userdata, msg):
    print("")

# Create and connect MQTT client
client = mqtt.Client(protocol=mqtt.MQTTv311)  # or MQTTv5 for MQTT 5.0
client.on_connect = on_connect
client.on_message = on_message
client.connect(mqtt_server, mqtt_port, 60)

client.reconnect_delay_set(min_delay=1, max_delay=60)

client.loop_start()


time.sleep(1)

#This loop just publishes each message with a 0.5 second delay because it was not happy when I sent all of them at once :)
for topic, data in all_messages:
    json_message = json.dumps(data, indent=len(data))
    print("Published a message")
    client.publish((config_topic_prefix+topic), json_message, retain=True)
    time.sleep(0.5)

