﻿using PMCLIB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    class CommandSender
    {

        private MQTTPublisher mqttPublisher;
        //string brokerIP = "localhost";
        string brokerIP = "172.20.66.135";
        int port = 1883;
        string UNSPrefix = "AAU/Fibigerstræde/Building14/FillingLine/Planar/";
        int selector = 4;
        int xbotID;

        public CommandSender()
        {
            InitializeMqttPublisher();
        }
        public int setSelectorOne()
        {
            return selector;
        }

        private async void InitializeMqttPublisher()
        {
            mqttPublisher = new MQTTPublisher(brokerIP, port);
            await mqttPublisher.StartAsync();
        }
        

        public async void runCommandSender()
        {
            Console.Clear();
            Console.WriteLine(" CommandHandler Select Xbot");
            Console.WriteLine("0    Return");
            Console.WriteLine("1    Xbot2");
            Console.WriteLine("2    Xbot5");
            Console.WriteLine("3    Xbot6");
            Console.WriteLine("4    Xbot7");
            Console.WriteLine("5    Xbot1");
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {
                case '0':
                    selector = 1;
                    break;


                case '1':
                    xbotID = 7;
                    Command(xbotID);
                    break;
                case '2':
                    xbotID = 5;
                    Command(xbotID);
                    break;
                case '3':
                    xbotID = 6;
                    Command(xbotID);
                    break;
                case '4':
                    xbotID = 7;
                    Command(xbotID);
                    break;
                case '5':
                    xbotID = 1;
                    Command(xbotID);
                    break;
            }


        }

        private async void Command(int xbot)
        {
            Console.Clear();
            Console.WriteLine(" CommandHandler Select Command");
            Console.WriteLine("0    Return");
            Console.WriteLine("1    Filling");
            Console.WriteLine("2    Stoppering");
            Console.WriteLine("3    Vision");
            Console.WriteLine("4    FillingQueue1");
            Console.WriteLine("5    FillingQueue2");
            Console.WriteLine("6    FillingQueue3");
            Console.WriteLine("7    FillingQueue4");
            Console.WriteLine("8    FillingPickNeedle");
            Console.WriteLine("9    FillingPlaceNeedle");
            selector = 5;
            string commandUuid = Guid.NewGuid().ToString();
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            
            

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {
                case '0':
                    selector = 4;
                    xbotID = 0;
                    break;


                case '1':

                    var FillingMessage = new
                    {
                        CommandUuid = commandUuid,
                        Command = "Away",
                        TimeStamp = timestamp
                    };
                    string serializedMessage = JsonSerializer.Serialize(FillingMessage);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD", serializedMessage);
                    /*
                    var FillingQueue1Message1 = new
                    {
                        CommandUuid = commandUuid,
                        Command = "FillingQueue1",
                        TimeStamp = timestamp
                    };
                    string serialized2Message = JsonSerializer.Serialize(FillingQueue1Message1);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{2}/CMD", serialized2Message);
                    
                    var FillingQueue2Message1 = new
                    {
                        CommandUuid = commandUuid,
                        Command = "FillingQueue2",
                        TimeStamp = timestamp
                    };
                    string serialized3Message = JsonSerializer.Serialize(FillingQueue2Message1);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{3}/CMD", serialized3Message);
                    
                    var FillingQueue3Message1 = new
                    {
                        CommandUuid = commandUuid,
                        Command = "FillingQueue3",
                        TimeStamp = timestamp
                    };
                    string serialized4Message = JsonSerializer.Serialize(FillingQueue3Message1);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{4}/CMD", serialized4Message);*/
                    selector = 4;
                    xbotID = 0;
                    break;
                case '2':

                    var StopperingMessage = new
                    {
                        CommandUuid = commandUuid,
                        Task = "AwayPosition",
                        TimeStamp = timestamp
                    };
                    string serializedMessage1 = JsonSerializer.Serialize(StopperingMessage);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD/SubCMD", serializedMessage1);
                    selector = 4;
                    xbotID = 0;
                    break;
                case '3':

                    var VisionMessage = new
                    {
                        CommandUuid = commandUuid,
                        Task = "Rotation",
                        TimeStamp = timestamp
                    };
                    string serializedMessage2 = JsonSerializer.Serialize(VisionMessage);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD/SubCMD", serializedMessage2);
                    selector = 4;
                    xbotID = 0;
                    break;

                case '4':

                    var FillingQueue1Message = new
                    {
                        CommandUuid = commandUuid,
                        Task = "StationPosition",
                        TimeStamp = timestamp
                    };
                    string serializedMessage3 = JsonSerializer.Serialize(FillingQueue1Message);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD/SubCMD", serializedMessage3);
                    selector = 4;
                    xbotID = 0;
                    break;

                case '5':

                    var FillingQueue2Message = new
                    {
                        CommandUuid = commandUuid,
                        Command = "FillingQueue2",
                        TimeStamp = timestamp
                    };
                    string serializedMessage4 = JsonSerializer.Serialize(FillingQueue2Message);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD", serializedMessage4);
                    selector = 4;
                    xbotID = 0;
                    break;


                case '6':

                    var FillingQueue3Message = new
                    {
                        CommandUuid = commandUuid,
                        Command = "FillingQueue3",
                        TimeStamp = timestamp
                    };
                    string serializedMessage5 = JsonSerializer.Serialize(FillingQueue3Message);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD", serializedMessage5);
                    selector = 4;
                    xbotID = 0;
                    break;


                case '7':

                    var FillingQueue4Message = new
                    {
                        CommandUuid = commandUuid,
                        Command = "FillingQueue4",
                        TimeStamp = timestamp
                    };
                    string serializedMessage6 = JsonSerializer.Serialize(FillingQueue4Message);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD", serializedMessage6);
                    selector = 4;
                    xbotID = 0;
                    break;

                case '8':

                    var FillingPickNeedleMessage = new
                    {
                        CommandUuid = commandUuid,
                        Command = "FillingPickNeedle",
                        TimeStamp = timestamp
                    };
                    string serializedMessage7 = JsonSerializer.Serialize(FillingPickNeedleMessage);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD", serializedMessage7);
                    selector = 4;
                    xbotID = 0;
                    break;
                case '9':

                    var FillingPlaceNeedleMessage = new
                    {
                        CommandUuid = commandUuid,
                        Command = "FillingPlaceNeedle",
                        TimeStamp = timestamp
                    };
                    string serializedMessage8 = JsonSerializer.Serialize(FillingPlaceNeedleMessage);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD", serializedMessage8);
                    selector = 4;
                    xbotID = 0;
                    break;
                case 'p':

                    var PickPlaceStorageMessage = new
                    {
                        CommandUuid = commandUuid,
                        Command = "PickPlacerStorage",
                        TimeStamp = timestamp
                    };
                    string serializedMessage9 = JsonSerializer.Serialize(PickPlaceStorageMessage);
                    await mqttPublisher.PublishMessageAsync(UNSPrefix + $"Xbot{xbot}/CMD", serializedMessage9);
                    selector = 4;
                    xbotID = 0;
                    break;
            }
        }

    }
}

