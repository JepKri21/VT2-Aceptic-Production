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


# Callback when connected
def on_connect(client, userdata, flags, rc):
    #Should subscribe to all the topics in the configuration
    client.subscribe(config_topic_prefix+"/#")


# Callback when a message is received
def on_message(client, userdata, msg):
    global all_shuttles
    global all_stations

    try: 
        data = json.loads(msg.payload.decode())

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
                client.subscribe(planar_topic_prefix+f"/Xbot{shuttle_id}/CMD/SubCMD")

            # Just example of how to use it
            #for shuttle_dict in all_shuttles:
            #    for name, info in shuttle_dict.items():
            #        print(f"{name}: ID={info['ID']}, Tool={info['Tool']}")
        

        #if msg.topic.startswith(config_topic_prefix +"/Planar/Stations"):
        if msg.topic == config_topic_prefix +"/Planar/Stations":
            stations = data["Stations"]
            for station in stations:
                station_name = station["Name"]
                named_station = {f"{station_name}" : station}
                all_stations.append(named_station)

                #Maybe if we save this topic to a list of topics then we can run through all
                # all those topics when we check new messages
                #print(f"This is that station topic that I subscribe to: {station_topic_prefix+f'{station_name}/DATA/State'}")
                client.subscribe(station_topic_prefix+f"{station_name}/DATA/State")
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
                        shuttle["CommandUuid"] = data["CommandUuid"]
                        

        # Regular expression pattern
        pattern = r"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot(\d+)/CMD/SubCMD$"
        match = re.fullmatch(pattern, msg.topic)
        

        if match:
            xbot_id = match.group(1)

            for shuttle_dict in all_shuttles:
                for name, shuttle in shuttle_dict.items():
                    if shuttle["ID"] == xbot_id:
                        shuttle["SubCMD"] = data["Task"]
                        print(f"Sub command: {data['Task']} updated for shuttle: {shuttle.get('ID')}")
                        

        

        match = re.fullmatch(r"^AAU/Fibigerstræde/Building14/FillingLine/([^/]+)/DATA/State$", msg.topic)
        if match:
            station_name = match.group(1)

            for station_dict in all_stations:
                for _, station in station_dict.items():
                    if station.get("Name").strip() == station_name.strip():
                        station["State"] = data.get("State")
                        print(f"Updated state of station '{station_name}' to: {station['State']}")
                        
                        break  # Optional: exit inner loop once station is found
                        
        # Regular expression pattern
        #pattern = r"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot(\d+)/DATA/State$"
        #match = re.fullmatch(pattern, msg.topic)
        #

        #if match:
        #    xbot_id = match.group(1)

        #    for shuttle_dict in all_shuttles:
        #        for name, shuttle in shuttle_dict.items():
        #            if shuttle["ID"] == xbot_id:
        #                shuttle["StationId"] = data["StationId"]
        #                print(f"StationID: {data['StationId']} updated for shuttle: {shuttle.get('ID')}")

        

    
    except json.JSONDecodeError as e:
        print("Error decoding JSON:", e)


def generate_station_command_payload():
    #Set the command UUId to the station (in this script)
    timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    unique_id = str(uuid.uuid4())

    payload = {
        "CommandUuid": unique_id,
        "TimeStamp": timestamp
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

def generate_shuttle_command_payload(unique_id, command):
    #Set the command Uuid to the shuttle (in this script) Maybe just do this outside, 
    #where I should know which shuttle it is
    timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')

    #print(f"Generated new command ID {unique_id}")

    payload = {
        "CommandUuid": unique_id,
        "Command": command,
        "TimeStamp": timestamp,
        
    }
    
    return json.dumps(payload)

def AssigningTasksBasedOnCommands():

    global all_queues

    for shuttle_dict in all_shuttles:
        for name, shuttle in shuttle_dict.items():
            command = shuttle.get("Command")
            if not command or command == "None":
                continue  # Skip shuttles without an active command

            # Only assign if "Command Tasks" doesn't exist or is empty
            if "Command Tasks" not in shuttle or not shuttle["Command Tasks"]:
                for station_dict in all_stations:
                    for station_name, station in station_dict.items():
                        if station.get("Function") == command:
                            station_tasks_dict = station.get("Command Tasks", {})
                            if isinstance(station_tasks_dict, dict) and station_tasks_dict:
                                first_task_key = next(iter(station_tasks_dict))  # Get first key
                                task_list = station_tasks_dict.get(first_task_key, [])
                                if isinstance(task_list, list):
                                    shuttle["Command Tasks"] = task_list.copy()
                                    print(f"Assigned tasks to shuttle {shuttle.get('ID')}: {task_list}")
                            else:
                                print(f"No valid task list for shuttle {shuttle.get('ID')} at station {station_name}")
                #Above we check the stations, here we check the queues


                for queue_dict in all_queues:
                    for queue_name, queue in queue_dict.items():
                        if queue["Name"] == command:
                            queue_tasks_dict = queue.get("Command Tasks", {})
                            if isinstance(queue_tasks_dict, dict) and queue_tasks_dict:
                                first_task_key = next(iter(queue_tasks_dict))  # Get first key
                                task_list = queue_tasks_dict.get(first_task_key, [])
                                if isinstance(task_list, list):
                                    shuttle["Command Tasks"] = task_list.copy()
                                    print(f"Assigned tasks to shuttle {shuttle.get('ID')}: {task_list}")
                            else:
                                print(f"No valid task list for shuttle {shuttle.get('ID')} at station {queue.get('Name')}")
                #Above we check the stations, here we check the queues

                #for queue_dict in all_queues:
                #    for queue_name, queue in queue_dict.items():
                #        if queue["Name"] == command:
                #            print(queue)
                #            occupancy_count = len(queue["QueueOccupancy"])
                #            task_keys = list(queue["Command Tasks"].keys())
#
                #            # Check if there are still unassigned task keys
                #            if occupancy_count < len(task_keys):
                #                task_key = task_keys[occupancy_count]
                #                task_list = queue["Command Tasks"].get(task_key)
#
                #                if isinstance(task_list, list):
                #                    shuttle["Command Tasks"] = task_list.copy()
                #                    queue["QueueOccupancy"].append(shuttle["ID"])
                #                    print(f"Assigned queue task '{task_key}' to shuttle {shuttle.get('ID')}: {task_list}")
                #            else:
                #                print(f"No available positions left in queue '{queue['Name']}' for shuttle {shuttle.get('ID')}'")
                #                #In theory you should then probably send a "None" value to the CMD of that shuttle
            
            



def ExecutingTasks():
    for shuttle_dict in all_shuttles:
        for name, shuttle in shuttle_dict.items():
            command = shuttle.get("Command")
            task_list = shuttle.get("Command Tasks", [])

            # Only continue if there's a command and a non-empty task list
            if not command or not task_list:
                continue

            if command == "Done": #REMOVE THIS IF IT DOESN'T WORK
                continue

            # Only continue if SubCMD is not present or is explicitly "None"
            if shuttle.get("SubCMD") is not None and shuttle.get("SubCMD") != "None":
                continue

            current_task = task_list[0]

            # === Special case: if the task is "Finished" ===
            if current_task == "Finished":
                # Find the station with a Function matching this command
                matching_station = None
                for station_dict in all_stations:
                    for station_name, station in station_dict.items():
                        if station.get("Function") == command:
                            matching_station = station
                            break
                    if matching_station:
                        break
                    
                if not matching_station:
                    print(f"No station found for shuttle {shuttle.get('ID')} and command '{command}'")
                    continue
                
                # Check if station is in 'Idle' state
                if matching_station.get("State") != "Idle":
                    print(f"Station for command '{command}' is not idle, skipping 'Finished' task for shuttle {shuttle.get('ID')}")
                    continue
                
                # If station is Idle, send 'None' to clear CMD and SubCMD
                subcmd_topic = f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle.get('ID')}/CMD/SubCMD"
                CMD_topic = f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle.get('ID')}/CMD"

                print(f"Shuttle {shuttle.get('ID')} has completed its command {shuttle.get('Command')}. Station is idle. Sending 'None'.")
                shuttle["Command Tasks"].pop(0)
                client.publish(subcmd_topic, generate_shuttle_task_payload(shuttle["CommandUuid"], "None"))
                client.publish(CMD_topic, generate_shuttle_command_payload(shuttle["CommandUuid"], "None"))
                time.sleep(0.1)
                continue

            # Find the station with a Function matching this command
            matching_station = None
            for station_dict in all_stations:
                for station_name, station in station_dict.items():
                    if station.get("Function") == command:
                        matching_station = station
                        break
                if matching_station:
                    break

            if not matching_station:
                #print(f"No station found for shuttle {shuttle.get('ID')} and command '{command}'")
                continue

            station_specific_tasks = matching_station.get("Station Specific Tasks", {})
            station_task_match =False


            #I need to check all the shuttles, if anythey have a StationId value equal to

            if station_specific_tasks != None:
                station_task_match = station_specific_tasks.get(current_task)
            if matching_station.get("State") == "Idle":
                if station_task_match:
                    # It's a station-specific task; send to the station topic
                    topic = station_task_match
                    payload = generate_station_command_payload()
                    print(f"Publishing station-specific task '{current_task}' to '{topic}' for shuttle {shuttle.get('ID')}")
                    client.publish(topic, payload)
                    matching_station["State"] = "Executing"
                    shuttle["Command Tasks"].pop(0)
                    time.sleep(0.1)
                else:
                    # Generic shuttle task; publish as SubCMD
                    subcmd_topic = f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle.get('ID')}/CMD/SubCMD"
                    #Might need to also make a check that the station is idle, because the station will not send "None" to the SubCMD.
                    payload = generate_shuttle_task_payload(shuttle.get("CommandUuid"), current_task)
                    #print(f"Publishing subtask '{current_task}' to '{subcmd_topic}' for shuttle {shuttle.get('ID')}")
                    shuttle["SubCMD"] = current_task
                    client.publish(subcmd_topic, payload)
                    shuttle["Command Tasks"].pop(0)
                    time.sleep(0.1)


def runningQueues():
    global all_queues
    global all_shuttles

    for shuttle_dict in all_shuttles:
        for name, shuttle in shuttle_dict.items():
            command = shuttle.get("Command")

            if "Command Tasks" not in shuttle or not shuttle["Command Tasks"]:
                continue
            task_list = shuttle["Command Tasks"]
            # Only continue if there's a command and a non-empty task list
            if not command or not task_list:
                continue

            # Only continue if SubCMD is not present or is explicitly "None"
            if shuttle.get("SubCMD") is not None and shuttle.get("SubCMD") != "None":
                continue

            current_task = task_list[0]

            # === Special case: if the task is "Finished" ===
            if current_task == "Finished":
                # Find the station with a Function matching this command
                matching_queue = None
                for queue_dict in all_queues:
                    for queue_name, queue in queue_dict.items():
                        if queue.get("Name") == command:
                            matching_queue = queue
                            break
                    if matching_queue:
                        break
                    
                if not matching_queue:
                    print(f"No queue found for shuttle {shuttle.get('ID')} and command '{command}'")
                    continue
                
                # If station is Idle, send 'None' to clear CMD and SubCMD
                subcmd_topic = f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle.get('ID')}/CMD/SubCMD"
                CMD_topic = f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle.get('ID')}/CMD"

                print(f"Shuttle {shuttle.get('ID')} has completed its command {shuttle.get('Command')}. Sending 'None'.")
                shuttle["Command Tasks"].pop(0)
                client.publish(subcmd_topic, generate_shuttle_task_payload(shuttle["CommandUuid"], "None"))
                client.publish(CMD_topic, generate_shuttle_command_payload(shuttle["CommandUuid"], "None"))
                time.sleep(0.1)
                continue

            # Find the queue with a Function matching this command
            matching_queue = None
            for queue_dict in all_queues:
                for queue_name, queue in queue_dict.items():
                    if queue.get("Name") == command:
                        matching_queue = queue
                        break
                if matching_queue:
                    break

            if not matching_queue:
                #print(f"No queue found for shuttle {shuttle.get('ID')} and command '{command}'")
                continue

            queue_position_free = True
            for other_shuttle_dict in all_shuttles:
                for other_name, other_shuttle in other_shuttle_dict.items():
                    if other_shuttle.get("ID") == shuttle.get("ID"):
                        continue  # Skip the same shuttle
                    
                    if other_shuttle.get("SubCMD") == current_task and other_shuttle.get("Command") == command:
                        queue_position_free = False



            if queue_position_free:
                # Generic shuttle task; publish as SubCMD
                subcmd_topic = f"AAU/Fibigerstræde/Building14/FillingLine/Planar/Xbot{shuttle.get('ID')}/CMD/SubCMD"
                #Might need to also make a check that the station is idle, because the station will not send "None" to the SubCMD.
                payload = generate_shuttle_task_payload(shuttle.get("CommandUuid"), current_task)
                #print(f"Publishing subtask '{current_task}' to '{subcmd_topic}' for shuttle {shuttle.get('ID')}")
                shuttle["SubCMD"] = current_task
                client.publish(subcmd_topic, payload)
                shuttle["Command Tasks"].pop(0)
                time.sleep(0.1)
            else:
                print("Another shuttle it at that queue position")
                    






# Create and connect MQTT client
client = mqtt.Client(protocol=mqtt.MQTTv311)  # or MQTTv5 for MQTT 5.0
client.on_connect = on_connect
client.on_message = on_message
client.connect(mqtt_server, mqtt_port, 60)

client.reconnect_delay_set(min_delay=1, max_delay=60)

client.loop_start()

time.sleep(1)

for station_dict in all_stations:
    for station_name, station in station_dict.items():
        station["State"] = "Idle"
        #print(station)

while True:
    for station_dict in all_stations:
        for station_name, station in station_dict.items():
            if station.get("Name") == "NeedleStation":
                station["State"] = "Idle"
                #print(station)

    time.sleep(0.3)
    AssigningTasksBasedOnCommands()

    time.sleep(0.3)
    ExecutingTasks()
    runningQueues()


client.loop_stop()

