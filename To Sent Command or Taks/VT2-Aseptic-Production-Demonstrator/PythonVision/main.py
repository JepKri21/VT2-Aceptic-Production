from opcua import Client, ua
import time
import requests
import paho.mqtt.client as mqtt
import uuid
from datetime import datetime
import json
import os
from pathlib import Path
from torchvision.transforms.v2 import Compose, Resize, Normalize, InterpolationMode
from anomalib.data import PredictDataset
from anomalib.engine import Engine
from anomalib.models import Padim
from anomalib.pre_processing import PreProcessor
import torch
import warnings
import gc
from PIL import Image
from torchvision.transforms import CenterCrop
warnings.filterwarnings("ignore")

# Configuration
OPC_UA_URL = "opc.tcp://192.168.10.116:4840"
NODE_ID = "ns=6;s=::Program:ImageNettime"
ACTIVATION_NODE_ID = "ns=6;s=::Program:ImageCapture"
HTTP_TRIGGER_URL = "http://192.168.200.8:8080/jpg?q=50"
mqtt_server = "172.20.66.135"
mqtt_port = 1883
 
# MQTT Topics
topic_vision_execute = "AAU/Fibigerstræde/Building14/FillingLine/Vision/CMD/Snapshot"
topic_vision_data = "AAU/Fibigerstræde/Building14/FillingLine/Vision/DATA/Anomaly"
topic_vision_status = "AAU/Fibigerstræde/Building14/FillingLine/Vision/DATA/State"
 
commandUuid = ""

# OPC UA Client Initialization
opcua_client = Client(OPC_UA_URL)  # Renamed from 'client' to 'opcua_client'
opcua_client.connect()




# Global variables
model = None
engine = None
initial_nettime = None
first_change = True


def clear_cuda_memory():
    torch.cuda.empty_cache()
    torch.cuda.ipc_collect()

def load_model():
    """Loads the anomaly detection model and engine."""
    global model, engine
    print("[INFO] Loading model into memory...")
    transform_padim = Compose([
        Resize(size=[224, 224], interpolation=InterpolationMode.BILINEAR, antialias=True),
        Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
    ])
    pre_processor_padim = PreProcessor(transform=transform_padim)

    model = Padim(
        backbone="resnet50",
        layers=["layer2", "layer3", "layer4"],
        pre_trained=True,
        n_features=550,
        pre_processor=pre_processor_padim,
    )
    engine = Engine(accelerator="cpu", devices=1)
    print("[INFO] Model loaded successfully.")

def predict_image(image_path):
    """Runs the prediction on the given image path."""
    global model, engine
    try:
        gc.collect()
        dataset = PredictDataset(path=Path(image_path))
        #predictions = engine.predict(
        #    model=model,
        #    dataset=dataset,
        #    ckpt_path="results/Padim/Images/v33/weights/lightning/model.ckpt",
        #)

        # Set model to evaluation mode
        model.eval()
        
        # Use no_grad to reduce memory usage during inference
        with torch.no_grad():
            predictions = engine.predict(
                model=model,
                dataset=dataset,
                ckpt_path="results/Padim/Images/v42/weights/lightning/model.ckpt",
            )
        
        # Clear CUDA cache after prediction to free memory
        torch.cuda.empty_cache()


        if predictions:
            for prediction in predictions:
                score = prediction.pred_score.item()
                label = bool(prediction.pred_label.item())
                print(f"[INFO] Prediction - Score: {score:.4f}, Label: {label}")
                clear_cuda_memory()

                # Explicitly delete prediction data after use
                del predictions
                gc.collect()  # Additional memory cleanup
                return score, label
        else:
            print("[WARNING] No prediction made.")
            return None, None
    except Exception as e:
        print(f"[ERROR] Prediction failed: {e}")
        gc.collect()
        return None, None






# MQTT Client Setup
def on_connect(mqtt_client, userdata, flags, rc):
    print("Connected with result code", rc)
    mqtt_client.subscribe(topic_vision_execute)

# MQTT Message Handling
def on_message(mqtt_client, userdata, msg):
    global commandUuid
    data = json.loads(msg.payload.decode())
    if msg.topic == topic_vision_execute:
        commandUuid = data.get("CommandUuid", str(uuid.uuid4()))
        publish_status("Executing")
        activate_node()

# MQTT Status Publishing
def publish_status(status): 
    payload = json.dumps({
        "CommandUuid": commandUuid,
        "TimeStamp": datetime.now().isoformat(),
        "State": status
    })
    mqtt_client.publish(topic_vision_status, payload)

# Activate and Deactivate OPC UA Node
def activate_node():
    try:
        node = opcua_client.get_node(ACTIVATION_NODE_ID)
        node.set_value(ua.DataValue(ua.Variant(True, ua.VariantType.Boolean)))
        print(f"[INFO] Node {ACTIVATION_NODE_ID} activated.")
        time.sleep(2)
        node.set_value(ua.DataValue(ua.Variant(False, ua.VariantType.Boolean)))
        print(f"[INFO] Node {ACTIVATION_NODE_ID} deactivated.")
    except Exception as e:
        print(f"[ERROR] Activation failed: {e}")

# If activation fails, check REST API, if none is available, run the following in admin terminal "route add 192.168.200.0 mask 255.255.255.0 192.168.10.116"

def save_image(response):
    image_folder = "abnormal"
    os.makedirs(image_folder, exist_ok=True)
    image_path = f"{image_folder}/image_{commandUuid}.jpg"
    with open(image_path, "wb") as f:
        f.write(response.content)
    print(f"[INFO] Image saved as '{image_path}'")

    crop_size = 800  
    crop = CenterCrop(crop_size)
    try:
        with Image.open(image_path) as img:
            cropped = crop(img)
            cropped.save(image_path)
            print(f"[INFO] Cropped and replaced: {image_path}")
    except Exception as e:
        print(f"Failed to crop {image_path}: {e}")

    return image_path

# SubHandler for OPC UA Node Changes
#class SubHandler(object):
#    def datachange_notification(self, node, val, data):
#        print(f"[NetTime Changed] New value: {val}")
#        process_image()

class SubHandler(object):
    def datachange_notification(self, node, val, data):
        global initial_nettime, first_change
        
        # On first change after startup, just store the value and skip prediction
        if initial_nettime is None:
            initial_nettime = val
            print(f"[INFO] Initial NetTime value stored: {initial_nettime}")
            publish_status("Idle")
            return

        # If the value equals the initial value and it's the first change, skip prediction
        if first_change:
            if val == initial_nettime:
                print("[INFO] Ignoring initial NetTime change to prevent old image prediction.")
                first_change = False  # Mark that the first change has been handled
                return
        
        print(f"[NetTime Changed] New value: {val}")
        process_image()


# Image Processing and Prediction
#def process_image():
#    try:
#        response = requests.get(HTTP_TRIGGER_URL)
#        if response.status_code == 200:
#            image_path = save_image(response)
#            prediction = os.system(f"python3 Prediction.py {image_path}")
#            result = "True" if prediction == 0 else "False"
#            publish_anomaly(result)
#    except Exception as e:
#        print(f"[ERROR] Image processing failed: {e}")


def process_image():
    try:
        response = requests.get(HTTP_TRIGGER_URL)
        if response.status_code == 200:
            image_path = save_image(response)
            # Call the integrated prediction function
            score, result = predict_image(image_path)
            anomaly = "True" if result else "False"
            publish_anomaly(anomaly)
            publish_status("Idle")
        else:
            print(f"[ERROR] Failed to retrieve image. Status code: {response.status_code}")
    except Exception as e:
        print(f"[ERROR] Image processing failed: {e}")



# Publish Anomaly Result
def publish_anomaly(result):
    payload = json.dumps({
        "CommandUuid": commandUuid,
        "TimeStamp": datetime.now().isoformat(),
        "Anomaly": result
    })
    mqtt_client.publish(topic_vision_data, payload)

# MQTT Client Initialization
mqtt_client = mqtt.Client()
mqtt_client.on_connect = on_connect
mqtt_client.on_message = on_message
mqtt_client.connect(mqtt_server, mqtt_port, 60)

mqtt_client.loop_start()

# OPC UA Client Setup
opcua_client = Client(OPC_UA_URL)

#try:
#    opcua_client.connect()
#    print("[OPC UA] Connected to server.")
#    handler = SubHandler()
#    sub = opcua_client.create_subscription(1000, handler)
#    nettime_node = opcua_client.get_node(NODE_ID)
#    sub.subscribe_data_change(nettime_node)

#    while True:
#        time.sleep(1)
#except KeyboardInterrupt:
#    mqtt_client.disconnect()
#    opcua_client.disconnect()
#    print("Disconnected.")


if __name__ == "__main__":
    try:
        # Load the model during startup
        load_model()

        mqtt_client.loop_start()
        opcua_client.connect()
        print("[OPC UA] Connected to server.")
        
        handler = SubHandler()
        sub = opcua_client.create_subscription(1000, handler)
        nettime_node = opcua_client.get_node(NODE_ID)
        sub.subscribe_data_change(nettime_node)

        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        mqtt_client.disconnect()
        opcua_client.disconnect()
        print("[INFO] Disconnected.")