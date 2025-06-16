import paho.mqtt.client as mqtt
import uuid
from datetime import datetime
import json
import time
import re


# MQTT broker details
mqtt_server = "172.20.66.135"
mqtt_port = 1883

config_topic_prefix = "AAU/Fibigerstræde/Building14/FillingLine/Configuration/DATA"
planar_topic_prefix = "AAU/Fibigerstræde/Building14/FillingLine/Planar"
station_topic_prefix = "AAU/Fibigerstræde/Building14/FillingLine/"


#These will be lists of all the shuttles and stations
#Such that we can refer to them again
all_shuttles = []
all_stations = []
all_queues = []
command_order = []
batch_size = 0

#I MIGHT NEED TO GIVE EVERY SHUTTLE A "None" COMMAND FIRST, JUST IN MY SYSTEM

# Callback when connected
def on_connect(client, userdata, flags, rc):
    #Should subscribe to all the topics in the configuration
    client.subscribe(config_topic_prefix+"/#")


# Callback when a message is received
def on_message(client, userdata, msg):
    global command_order
    global all_shuttles
    global all_stations
    global all_queues
    global batch_size

    try: 
        data = json.loads(msg.payload.decode())

        #===========================================================
        #======================Just configuration messages =========
        #===========================================================
        if msg.topic == config_topic_prefix + "/Planar/Shuttles":
            #We just create all the shuttles as variables to dictionaries
            all_shuttles.clear()
            for shuttle in data:
                shuttle_id = shuttle["ID"]
                named_shuttle = {f"shuttle_{shuttle_id}" : shuttle}
                all_shuttles.append(named_shuttle)
                #Also subscribe to the shuttle state and command
                client.subscribe(planar_topic_prefix+f"/Xbot{shuttle_id}/DATA/State")
                client.subscribe(planar_topic_prefix+f"/Xbot{shuttle_id}/CMD")

            # Just example of how to use it
            #for shuttle_dict in all_shuttles:
            #    for name, info in shuttle_dict.items():
            #        print(f"{name}: ID={info['ID']}, Tool={info['Tool']}")
        

        #if msg.topic.startswith(config_topic_prefix +"/Planar/Stations"):
        if msg.topic == config_topic_prefix +"/Planar/Stations":
            stations = data["Stations"]
            for station in stations:
                station_name = station["Name"]
                named_station = {f"{station_name}_station" : station}
                all_stations.append(named_station)

                #Maybe if we save this topic to a list of topics then we can run through all
                # all those topics when we check new messages
                client.subscribe(station_topic_prefix+f"/{station_name}/DATA/State")
            #If Command Tasks is non-null

        if msg.topic.startswith(config_topic_prefix +"/Station"):
            #Here we just add all the traits to already existsting stations
            stations_to_remove = [] 

            for station_dict in all_stations:
                for station_name_key, station_data in station_dict.items():
                    if station_data['Name'] == data['Name']:
                        station_data["Function"] = data.get("Function")
                        station_data["QueueDirection"] = data.get("QueueDirection")
                        station_data["Station Specific Tasks"] = data.get("Station Specific Tasks")
                        station_data["Command Tasks"] = data.get("Command Tasks")
                        station_data["QueueOccupancy"] = data.get("QueueOccupancy")

                        # If the station is a queue, move it to all_queues and schedule for removal
                        if station_data["Function"] == "Queue":
                            already_in_queues = any(
                                station_data["Name"] == queue.get("Name")
                                for queue in all_queues
                            )
                            if not already_in_queues:
                                all_queues.append({station_data["Name"]: station_data})
                                stations_to_remove.append(station_dict)
                                print(f"Station '{station_data['Name']}' moved to all_queues.")

            # Remove after iteration to avoid mutating the list during the loop
            for station in stations_to_remove:
                all_stations.remove(station)

        if msg.topic == config_topic_prefix + "/System/BatchInfo":
            command_order = data["CommandOrder"]
            batch_size = data["BatchSize"]

        #===========================================================
        #====================== Other messages =====================
        #===========================================================

        # Regular expression pattern
        pattern = r"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot(\d+)/CMD$"
        match = re.fullmatch(pattern, msg.topic)
        

        if match:
            xbot_id = match.group(1)

            for shuttle_dict in all_shuttles:
                for name, shuttle in shuttle_dict.items():
                    if shuttle["ID"] == xbot_id:
                        shuttle["Command"] = data["Command"]


    
    except json.JSONDecodeError as e:
        print("Error decoding JSON:", e)

#============================================================================
#============================================================================
#=========================== THE ACTUAL LOGIC PART ==========================
#============================================================================
#============================================================================

def generate_station_command_payload():
    #Set the command UUId to the station (in this script)
    timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    unique_id = str(uuid.uuid4())

    payload = {
        "CommandUuid": unique_id,
        "TimeStamp": timestamp
    }

    return json.dumps(payload)

def generate_shuttle_command_payload(command):
    #Set the command Uuid to the shuttle (in this script) Maybe just do this outside, 
    #where I should know which shuttle it is
    unique_id = str(uuid.uuid4())
    timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')

    #print(f"Generated new command ID {unique_id}")

    payload = {
        "CommandUuid": unique_id,
        "Command": command,
        "TimeStamp": timestamp,
        
    }
    
    return json.dumps(payload)


def generate_shuttle_task_payload(unique_id, task):
    #Set the command Uuid to the shuttle (in this script) Maybe just do this outside, 
    #where I should know which shuttle it is
    timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')

    #print(f"Generated new command ID {unique_id}")

    payload = {
        "CommandUuid": unique_id,
        "Task": task,
        "TimeStamp": timestamp,
        
    }
    
    return json.dumps(payload)

def CheckCommandOrderList():
    global batch_size

    for shuttle_dict in all_shuttles:
        for name, shuttle in shuttle_dict.items():
            if not shuttle.get("CommandOrderList"): #If it does not have any pending commands
                if batch_size > -1:                  #AND there is still parts of the batch left to do
                    if shuttle["Tool"] == "Shuttle Rack": #This could be done better, like checking for all the stations and making sure that they have the same tool requirement
                        shuttle["CommandOrderList"] = command_order.copy()
                        batch_size = batch_size-1 #Then we remove it from the batch
                        print(f"New order given to shuttle{shuttle['ID']} and new batch size is {batch_size}")
    #I need a function that checks if a shuttle has a commandOrderList
    #If there is no list or it is empty AND the batch is bigger than 0, 
    # then we fill the shuttles commandOrderList up with one of the commandOrders


def CheckAndSendCommands():
    for shuttle_dict in all_shuttles:
        for name, shuttle in shuttle_dict.items():
            cmd_list = shuttle.get("CommandOrderList", [])
            if not cmd_list:
                continue

            if shuttle.get("Command") is not None and shuttle.get("Command") != "None":
                continue

            first_cmd = cmd_list[0]
            shuttle_id = shuttle.get("ID")
            tool = shuttle.get("Tool")

            # === PRIORITY: Check if this shuttle is first in a queue for this command ===
            is_front_of_queue = False
            for queue_dict in all_queues:
                for queue_name, queue in queue_dict.items():
                    if queue.get("Function") == first_cmd:
                        occupancy = queue.get("QueueOccupancy", [])
                        if occupancy and occupancy[0] == shuttle_id:
                            is_front_of_queue = True
                            break
                if is_front_of_queue:
                    break

            # === CASE 1: Shuttle is front of queue for the command ===
            if is_front_of_queue:
                for station_dict in all_stations:
                    for station_name, station in station_dict.items():
                        if station.get("Function") == first_cmd:
                            required_tool = station.get("RequiredTool")
                            if tool == required_tool:
                                print(f"{name} is FIRST in queue and allowed to execute '{first_cmd}' at {station_name}")
                                shuttle["Command"] = first_cmd
                                client.publish(
                                    f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle_id}/CMD",
                                    generate_shuttle_command_payload(first_cmd)
                                )
                                shuttle["CommandOrderList"].pop(0)
                                # Remove from queue occupancy

                                for queue_dict in all_queues:
                                    for queue_name, queue in queue_dict.items():
                                        occupancy = queue.get("QueueOccupancy", [])
                                        if shuttle_id in occupancy:
                                            occupancy.remove(shuttle_id)
                                            #queue["QueueOccupancy"] = occupancy


                                queue.get("QueueOccupancy", []).remove(shuttle_id)
                            else:
                                print(f"{name} is first in queue but has wrong tool for '{first_cmd}' — needs '{required_tool}', has '{tool}'")
                            break  # Only act on first matching station
                continue  # Skip to next shuttle

            # === CASE 2: No other shuttle is executing this command ===
            command_taken = any(
                other_shuttle.get("Command") == first_cmd
                for other_dict in all_shuttles
                for _, other_shuttle in other_dict.items()
                if other_shuttle is not shuttle
            )

            if not command_taken:
                for station_dict in all_stations:
                    for station_name, station in station_dict.items():
                        if station.get("Function") == first_cmd:
                            required_tool = station.get("RequiredTool")
                            if tool == required_tool:
                                print(f"{name} is allowed to execute '{first_cmd}' directly at {station_name}")
                                shuttle["Command"] = first_cmd
                                client.publish(
                                    f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle_id}/CMD",
                                    generate_shuttle_command_payload(first_cmd)
                                )
                                shuttle["CommandOrderList"].pop(0)
                            else:
                                print(f"{name} has wrong tool for '{first_cmd}' — needs '{required_tool}', has '{tool}'")
                            break
            else:
                # === CASE 3: Command is taken, fallback to QueueDirection ===
                for station_dict in all_stations:
                    for station_name, station in station_dict.items():
                        if station.get("Function") == first_cmd:
                            queue_direction = station.get("QueueDirection")
                            if queue_direction is not None:
                                # Find queue
                                matching_queue = None
                                for queue_dict in all_queues:
                                    for qname, queue in queue_dict.items():
                                        if queue.get("Name") == queue_direction:
                                            matching_queue = queue
                                            break
                                    if matching_queue:
                                        break

                                if matching_queue:
                                    queue_occupancy = matching_queue.get("QueueOccupancy", [])
                                    if shuttle_id not in queue_occupancy:
                                        print(f"{name} is being sent to queue '{queue_direction}' for '{first_cmd}'")
                                        client.publish(
                                            f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle_id}/CMD",
                                            generate_shuttle_command_payload(queue_direction)
                                        )
                                        queue_occupancy.append(shuttle_id)
                                        matching_queue["QueueOccupancy"] = queue_occupancy
                                        shuttle["Command"] = queue_direction
                                        #shuttle["CommandOrderList"].pop(0)
                                else:
                                    print(f"Queue direction '{queue_direction}' not found for station '{station_name}'")


                                    
                


#If no other shuttle has a currentCommand that matches the new Command of the current shuttle then we can send it that Command
#AND the current shuttle does not already have a Command
#AND the current shuttle has the correct tool for this Command

#If, however, some shuttle DOES have that currentCommand, then we check if the station with that Command/Function/Name? has a QueueDirection. And if it does then we send that

#I also think, that once we have sent it we can remove it from the list of tasks

#============================================================================
#============================================================================
#=========================== END OF ACTUAL LOGIC PART =======================
#============================================================================
#============================================================================



# Create and connect MQTT client
client = mqtt.Client(protocol=mqtt.MQTTv311)  # or MQTTv5 for MQTT 5.0
client.on_connect = on_connect
client.on_message = on_message
client.connect(mqtt_server, mqtt_port, 60)

client.reconnect_delay_set(min_delay=1, max_delay=60)

client.loop_start()

print("Sending attach needle comand")
CMD_topic = f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot1/CMD"
client.publish(CMD_topic, generate_shuttle_command_payload("NeedleStation"))

print("Waiting to finish")
time.sleep(30)
client.publish(CMD_topic, generate_shuttle_command_payload("None"))

client.publish("AAU/Fibigerstræde/Building14/FillingLine/Filling/CMD/Dispense",generate_station_command_payload())

print("Starting the real program")

running_script = True

while running_script:
    
    time.sleep(0.5)
    CheckCommandOrderList()
    time.sleep(0.5)
    CheckAndSendCommands()
    if batch_size == 0:
        running_script = False
    
#If
complete_list = ["Done1", "Done2", "Done3"]

while running_script == False:
    try: 
        for shuttle_dict in all_shuttles:
            for name, shuttle in shuttle_dict.items():
                cmd_list = shuttle.get("CommandOrderList", [])
                if len(cmd_list) == 0 and shuttle.get("Command") == "None" and shuttle.get("ID") != 1:
                    subcmd_topic = f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle.get('ID')}/CMD/SubCMD"
                    CMD_topic = f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle.get('ID')}/CMD"
                    client.publish(CMD_topic, generate_shuttle_command_payload("Done")) #When I send the command "Done" the command handler takes over and gives it all the tasks for the Done command
                    client.publish(subcmd_topic, generate_shuttle_task_payload("DONE", complete_list[0]))

                    complete_list.pop(0)
    except json.JSONDecodeError as e:
        print("Error decoding JSON:", e)

    CheckAndSendCommands()
    time.sleep(0.5)

client.loop_stop()


