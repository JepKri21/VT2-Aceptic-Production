﻿// See https://aka.ms/new-console-template for more information

using PMCLIB;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;


namespace VT2_Aseptic_Production_Demonstrator
{
    class Program
    {

        //Add diffrendce classes neede for the program here:
        private static XBotCommands xBotCommands = new XBotCommands();
        private connection_handler connectionHandler = new connection_handler();
        private realtime_movement_test realtimeMovement = new realtime_movement_test();
        private ListBasedMotionStructure listBasedMotionStructure = new ListBasedMotionStructure();
        private CommandSender commandSender = new CommandSender();
        //private Pathfinding pathfinding = new Pathfinding();




        //
        // Global variables:
        private int selector = 0;

        // XBot Ids
        // Change depending on the number of XBot's in the system
        public int[] xbot_ids = {1};


        string title = @"
                         _   _        __  __                        |
    /\                  | | (_)      |  \/  |                       |
   /  \   ___  ___ _ __ | |_ _  ___  | \  / | _____   _____ _ __    |
  / /\ \ / __|/ _ \ '_ \| __| |/ __| | |\/| |/ _ \ \ / / _ \ '__|   |
 / ____ \\__ \  __/ |_) | |_| | (__  | |  | | (_) \ V /  __/ |      | 
/_/    \_\___/\___| .__/ \__|_|\___| |_|  |_|\___/ \_/ \___|_|      |
                  | |                                               |
                  |_|                                               |
____________________________________________________________________| ";

        public async void Run()
        {
            do
            {
                Console.Title = "Aseptic Planar Technology";
                Console.WriteLine("DEMO Program V 1.1");

                while (selector == 0)
                {
                    // Connection to the PMC and aquire mastership
                    //Console.Clear();
                    //CONNECTIONSTATUS status = connectionHandler.ConnectAndGainMastership();
                    //Console.WriteLine(status);
                    

                    selector = 1;
                }

                while (selector == 1)
                {
                    //Console.Clear();
                    Console.WriteLine(title);
                    Console.WriteLine("Choose program by entering the appropriate number: ");
                    Console.WriteLine("0:   Run Calibration again");
                    Console.WriteLine("1:   Run Command Sender");
                    Console.WriteLine("ESC: Exit program");
                    ConsoleKeyInfo keyinfo = Console.ReadKey();

                    switch (keyinfo.KeyChar)
                    {
                        case '0':
                            selector = 0;
                            break;

                        case '1':
                            selector = 4;
                            break;
                        







                        case '\u001b': //escape key
                            return;
                        default:
                            Console.WriteLine("Invalid input");
                            selector = 1;
                            break;
                    }
                }
                
                while (selector == 4)
                {
                    commandSender.runCommandSender();
                    commandSender.setSelectorOne();
                }
                while (selector == 5)
                {
                    commandSender.runCommandSender();
                    commandSender.setSelectorOne();
                }
            } while (true);

        }


        static void Main(string[] args)
        {
            
            Program program = new Program();
            
            Console.WriteLine("Hello User, hope you make something good! :) :)");

            Thread thread1 = new Thread(new ThreadStart(program.Run));

            thread1.Start();
        }
    }
}


