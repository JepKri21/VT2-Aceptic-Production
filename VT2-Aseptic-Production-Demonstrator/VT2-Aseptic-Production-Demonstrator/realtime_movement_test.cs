using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    internal class realtime_movement_test
    {
        private static SystemCommands _systemCommand = new SystemCommands();
        //this class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.
        private static XBotCommands _xbotCommand = new XBotCommands();
        WaitUntilTriggerParams CMD_params = new WaitUntilTriggerParams();
        int selector = 2;

        public int setSelectorOne()
        {
            return selector;
        }
        
        public void runRealtimeMovementTest(int[] xbot_ids)
        {
            selector = 1;
            Console.Clear();
            Console.WriteLine(" Realtime Movement Test");
            Console.WriteLine("0    Return ");
            Console.WriteLine("1    Run Code ");

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            switch (keyInfo.KeyChar)
            {

                case '0':
                    selector = 1;
                    break;

                case '1':
                    
                    selector = 2;
                    
                    break;
            }

        }
        



    }
}
