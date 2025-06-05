from opcua import Client
from opcua import ua
import time
import requests
import threading  # For handling the 100ms delay
import keyboard  # For detecting the spacebar press
 
# Configuration
OPC_UA_URL = "opc.tcp://192.168.10.116:4840"
NODE_ID = "ns=6;s=::Program:ImageNettime"  # <-- Replace with actual NodeId
ACTIVATION_NODE_ID = "ns=6;s=::Program:ImageCapture"  # <-- Replace with actual NodeId for activation
HTTP_TRIGGER_URL = "http://192.168.200.8:8080/jpg?q=50"
  
# Global flag to control periodic triggering
trigger_active = False   

class SubHandler(object):  
    def __init__(self):
        self.last_value = None  
        self.image_counter = 4500     # Counter to track image numbers
          
    def datachange_notification(self, node, val, data):
        if val != self.last_value: 
            print(f"[NetTime Changed] New value: {val}")          
            self.last_value = val
 
            # Send HTTP GET request
            try:
                response = requests.get(HTTP_TRIGGER_URL)
                print(f"[HTTP] GET sent → Status: {response.status_code}")
                
                # Save the image if the response is successful
                if response.status_code == 200:
                    # Ensure the folder exists 
                    import os
                    image_folder = "Images/normal"
                    os.makedirs(image_folder, exist_ok=True)
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         
                    # Increment the counter and create a unique   file name
                    self.image_counter += 1
                    image_path = f"{image_folder}/image_{self.image_counter}.jpg"

                    # Save the image
                    with open(image_path, "wb") as f:
                        f.write(response.content)
                    print(f"[INFO] Image saved as '{image_path}'")
            except Exception as e:
                print(f"[ERROR] HTTP request failed: {e}")
                                 
def activate_node(client, node_id):
    """Activate the specified OPC UA node."""
    try:
        node = client.get_node(node_id)
        # Write only the value using ua.DataValue
        node.set_value(ua.DataValue(ua.Variant(True, ua.VariantType.Boolean)))
        print(f"[INFO] Node {node_id} activated.")
    except Exception as e:
        print(f"[ERROR] Failed to activate node: {e}")

def deactivate_node(client, node_id):
    """Deactivate the specified OPC UA node."""
    try:
        node = client.get_node(node_id)
        # Write only the value using ua.DataValue
        node.set_value(ua.DataValue(ua.Variant(False, ua.VariantType.Boolean)))
        print(f"[INFO] Node {node_id} deactivated.")
    except Exception as e:
        print(f"[ERROR] Failed to deactivate node: {e}")   

#def handle_spacebar(client):
#    """Handle spacebar press to activate and deactivate the node."""
#    while True:
#        if keyboard.is_pressed("space"):  # Detect spacebar press
#            print("[INFO] Spacebar pressed. Activating node...")
#            activate_node(client, ACTIVATION_NODE_ID)
#            
#            # Deactivate the node after 100ms
#            threading.Timer(0.1, deactivate_node, args=(client, ACTIVATION_NODE_ID)).start()
#            
#            # Prevent multiple activations from a single press
#            while keyboard.is_pressed("space"):
#                pass


def periodic_trigger(client):
    """Trigger the node every 500ms."""
    global trigger_active
    while True:
        if trigger_active:
            print("[INFO] Triggering node...")
            activate_node(client, ACTIVATION_NODE_ID)
            
            # Deactivate the node after 100ms
            threading.Timer(0.1, deactivate_node, args=(client, ACTIVATION_NODE_ID)).start()
            
            # Wait for 500ms before triggering again
            time.sleep(0.5)
        else:
            time.sleep(0.1)  # Small delay to avoid busy-waiting

def handle_spacebar():
    """Toggle the periodic trigger on/off with the spacebar."""
    global trigger_active
    while True:
        if keyboard.is_pressed("space"):
            trigger_active = not trigger_active
            state = "started" if trigger_active else "stopped"
            print(f"[INFO] Periodic trigger {state}.")
            
            # Prevent multiple toggles from a single press
            while keyboard.is_pressed("space"):
                pass

def main():
    client = Client(OPC_UA_URL)

    try:
        client.connect()
        print("[OPC UA] Connected to server.")

        # Start monitoring the node
        nettime_node = client.get_node(NODE_ID)
        handler = SubHandler()
        sub = client.create_subscription(1000, handler)  # 1000 ms = 1s interval
        handle = sub.subscribe_data_change(nettime_node)

        # Start a thread for periodic triggering
        trigger_thread = threading.Thread(target=periodic_trigger, args=(client,), daemon=True)
        trigger_thread.start()

        # Start a thread to handle spacebar presses
        spacebar_thread = threading.Thread(target=handle_spacebar, daemon=True)
        spacebar_thread.start()

        print("[Monitoring] Listening for NetTime changes. Press Spacebar to start/stop the periodic trigger. Press Ctrl+C to exit.")
        while True:
            time.sleep(1)

    except Exception as e:
        print(f"[ERROR] {e}")
    finally:
        client.disconnect()
        print("[OPC UA] Disconnected.")

if __name__ == "__main__":
    main()