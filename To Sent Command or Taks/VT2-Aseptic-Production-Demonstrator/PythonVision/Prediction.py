from pathlib import Path
from torchvision.transforms.v2 import Compose, Resize, Normalize, CenterCrop, InterpolationMode
from anomalib.data import PredictDataset
from anomalib.engine import Engine
from anomalib.models import Padim
from anomalib.pre_processing import PreProcessor
import warnings
warnings.filterwarnings("ignore")


#img_path = Path("abnormal/image_4.jpg")  # Replace with actual image path
# Trasnform is the exact same as the one i am using to train the model
transform_padim = Compose([
    #CenterCrop(900),
    Resize(size=[224, 224], interpolation=InterpolationMode.BILINEAR, antialias=True),
    Normalize(
        mean=[0.485, 0.456, 0.406],
        std=[0.229, 0.224, 0.225],
    ),
    #ToTensor(),
])
pre_processor_padim = PreProcessor(transform=transform_padim)

transform_data = Compose([
    #CenterCrop(750),
    Resize(size=[224, 224], interpolation=InterpolationMode.BILINEAR, antialias=True),
    #Normalize(
    #    mean=[0.485, 0.456, 0.406],
    #    std=[0.229, 0.224, 0.225],
    #),
    #ToTensor(),
])
pre_processor_data = PreProcessor(transform=transform_data)


# Load the image and apply the transform directly
#transformed_img = transform(img_path)  # Apply the transform pipeline

model = Padim(
    backbone="resnet50",             # Feature extraction backbone
    layers=["layer2", "layer3", "layer4"],  # Layers to extract features from
    pre_trained=True,
    n_features=550,
    pre_processor=pre_processor_padim,
)

engine = Engine(
    #callbacks=[early_stopping],  # Wrap early_stopping in a list
    accelerator="gpu",
    devices=1,
)

# Loading the image for prediction
dataset = PredictDataset(
    path=Path("abnormal/image_14.jpg"),
    #transform=pre_processor_data,  # Pass the transform directly
)

# Predict
predictions = engine.predict(
    model=model,
    dataset=dataset,
    ckpt_path="results/Padim/Images/v33/weights/lightning/model.ckpt",
)

# Output results
if predictions:
    for prediction in predictions:
        print(f"Score: {prediction.pred_score.item():.4f}, Label: {prediction.pred_label.item()}")
