# VT2-Aceptic-Production

To run the demonstratior

The PC that holdes the PathPlanPMC and Vision Script needs to be connected to the Planar system
The C# program of the PathPlanPMC node need to be running
The main python Script of the Novo Vision folder do also need to run in VS code 

To Start the Filling and Stoppering Station they needs to be powered on and they will initialize 

To execute the logic controll of this demonstratior
The Configuration needs to be sent form the script Configuration_message and this needs to be sent to the system only when system change occures. (The message is retained)

After the Configuration mesage has been fully sent
Then the Task Handeling node needs to be started.
And at last the Command Handeling node needs to be started.