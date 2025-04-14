#define enB 9
#define in3 6
#define in4 7

#define BUTTON_PIN_BOTTOM 2
#define BUTTON_PIN_TOP 4

unsigned long startTime;
unsigned long endTime;
unsigned long elapsedTime;

int speed = 140;

void setup() {
  pinMode(enB, OUTPUT);
  pinMode(in3, OUTPUT);
  pinMode(in4, OUTPUT);
  pinMode(BUTTON_PIN_BOTTOM, INPUT_PULLUP);
  pinMode(BUTTON_PIN_TOP, INPUT_PULLUP);
  // Set initial rotation direction
  digitalWrite(in3, LOW); //Starting the motor at stand-still
  digitalWrite(in4, LOW);
  Serial.begin(9600);
  analogWrite(enB, 0); // Send PWM signal to L298N Enable pin
}

void loop() {

  //Should probably start by running to the top position

  if (Serial.available() > 0) 
    {   // Check if data is available
        char input = Serial.read(); // Read the incoming character

        if (input == 'r') 
        {  // Check if the character is 'r'

            digitalWrite(in3, LOW); //if it is, then we run in one direction
            digitalWrite(in4, HIGH);

            int down_speed = 0;
            startTime = millis(); //Start time for going down
            
            while (digitalRead(BUTTON_PIN_BOTTOM) == 0)
            {

              if (down_speed <= speed)
              {
                down_speed = down_speed +1;
                analogWrite(enB, down_speed); // Send PWM signal to L298N Enable pin
              }

              if (millis() - startTime >= 8000) //If it takes more than 4 seconds then we say there is an error in the movement
              {
                Serial.println("motion_error_down");
                break;
              }
              //Do nothing while we wait for the button 
              //Put some time check here so that it doesn't run forever
            }

            
            digitalWrite(in3, HIGH);
            digitalWrite(in4, LOW);


            int up_speed = 0;
            startTime = millis(); //Start time for going up
            
            while (digitalRead(BUTTON_PIN_TOP) == 0)
            {
              if (up_speed <= speed)
              {
                up_speed = up_speed +1;
                analogWrite(enB, up_speed); // Send PWM signal to L298N Enable pin
              }
              
              if (millis() - startTime >= 8000) //If it takes more than 4 seconds then we say there is an error in the movement
              {
                Serial.println("motion_error_up");
                break;
              }
            }
            digitalWrite(in3, LOW);
            digitalWrite(in4, LOW);
            
        }
    }

}
