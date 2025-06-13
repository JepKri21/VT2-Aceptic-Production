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
        "StationPosition": [0.085, 0.113, 0.001, 0, 0, 90]
    },
    {
        "Name": "Stoppering",
        "StationID": 2,
        "RequiredTool" : "Shuttle Rack",
        "Approach": [0.12, 0.36, 0.001, 0, 0, 0],
        "StationPosition": [0.110, 0.360, 0.001, 0, 0, 90]
    },
    {
        "Name": "Vision",
        "StationID": 3,
        "RequiredTool" : "Shuttle Rack",
        "Approach": [0.12, 0.6, 0.001, 0, 0, 0],
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
        "ApproachPick": [0.12, 0.12, 0.001, 0, 0, 0],
        "PickPosition": [0.12, 0.08, 0.001, 0, 0, 180],
        "ApproachPlace": [0.12, 0.12, 0.001, 0, 0, 0],  
        "PlacePosition": [0.106, 0.12, 0.001, 0, 0, 90]
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


#Add the queues to the stations, such that the system knows which queue to send a shuttle to when there is no space

#=========== Full shuttle configuration ==========
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
        "Tool" : "Shuttle Rack"
    },
    
    #{
    #    "ID" : "4",
    #    "Tool" : "Shuttle Rack"
    #},
    #{
    #    "ID" : "7",
    #    "Tool" : "Shuttle Rack"
    #}
]

#=========== Additional station configurations ==========

filling_station = {
    "Name": "Filling", 
    "Function" : "Filling",
    "QueueDirection" : "FillingQueue",
    "Station Specific Tasks" : {"Dispense" : "AAU/Fibigerstræde/Building14/FillingLine/Filling/CMD/Dispense"},
    "Command Tasks" : {"Filling" : ["Approach", "Rotation", "StationPosition", "Land", "Dispense", "Levitate", "Finished"]}
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

#We can put the shuttles in a list of occupancy
#If they are in this list, then they can't be "Finished"
# hopefully they will continue through all the queue positions, but 
# They won't be given a new queue command
# But how do we get them out of this list?
# Maybe what we do is just check the first position in that list as the first shuttle to get a new command
# We'd just have to make a list of lists, for each queue. So if a station["Function"] is Queue, then we make a list for it


#You should also be pretty easily able to check if there are any shuttles that
# have the tasks of the next queue positions
#Because if they don't then we can skip that one.

needle_station = {
    "Name": "NeedleStation", 
    "Function" : "Attaching Needle",
    "Station Specific Tasks" : {"AttachNeedle" : "AAU/Fibigerstræde/Building14/FillingLine/Filling/CMD/Needle"},
    "Command Tasks" : {"Needle" : ["ApproachPick", "PickPosition", "ApproachPick", "ApproachPlace", "AttachNeedle", "PlacePosition", "ApproachPlace", "Dispense"]}
    }


#=========== General system configuration ==========
batch = {
    "BatchSize" : 3,
    "CommandOrder" : ["Filling", "Stoppering", "Vision", "Away"] #Remember to add vision
}
#In the command handler we check these commands with the functions of the stations

#=========== System configuration messages ==========

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
for topic, data in all_messages:
    json_message = json.dumps(data, indent=len(data))
    print("Published a message")
    client.publish((config_topic_prefix+topic), json_message, retain=True)
    time.sleep(0.5)

