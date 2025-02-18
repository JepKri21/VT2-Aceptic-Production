using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{

    
    
    internal class connection_handler
    {
            private SystemCommands _sysCommand = new SystemCommands();
            private XBotCommands _botCommand = new XBotCommands();

            /// <summary>
            /// Automatically connects to PMC and gains mastership over it.
            /// Local IP of PC must be 192.168.100.41 with subnet mask 255.255.0.0 to connect to smartlab ACOPOS6D table
            /// </summary>
            /// <returns>CONNECTIONSTATUS of PMC</returns>
            public CONNECTIONSTATUS ConnectAndGainMastership()
            {
                bool isConnected = _sysCommand.AutoSearchAndConnectToPMC(); //connect to PMC
                if (!isConnected) return CONNECTIONSTATUS.CONNECTION_ERROR; //Check status - return error if not connected

                _sysCommand.GainMastership(); // gain mastership
                if (_sysCommand.IsMaster() == TriState.FALSE) { return CONNECTIONSTATUS.MASTERSHIP_ERROR; } //check mastership - return error if mastership is not established


                _botCommand.ActivateXBOTS();
                bool control = WaitUntilFullCtrl(); //wait until control over PMC is achieved
                if (control) //if achieved 
                {
                    Console.WriteLine("connection established");
                    return CONNECTIONSTATUS.OK; // return 'OK' 
                }
                else //if not achieved
                {
                    Console.WriteLine("connection failed to establish");
                    return CONNECTIONSTATUS.TIMEOUT_ERROR; // return 'TIMEOUT_ERROR' 
                }
            }

            /// <summary>
            ///     Releases mastership and disconnects from PMC
            /// </summary>
            public void Disconnect()
            {
                _botCommand.DeactivateXBOTS();
                _sysCommand.ReleaseMastership();
                _sysCommand.DisconnectFromPMC();
            }


            /// <summary>
            ///     Puts the system to sleep until full control (or intelligent control) over the PMC is achieved. Returns error if control is not achieved within 10 seconds
            /// </summary>
            /// <returns>
            ///     True if control is achieved, false if control is not achieved within 10 seconds.
            /// </returns>
            private bool WaitUntilFullCtrl()
            {
                for (int i = 0; i < 20; i++)
                {
                    PMCSTATUS _status = _sysCommand.GetPMCStatus();
                    if (_status == PMCSTATUS.PMC_FULLCTRL || _status == PMCSTATUS.PMC_INTELLIGENTCTRL)
                        return true;
                    Thread.Sleep(500);
                }
                return false;
            }
    }
        /// <summary>
        /// Enum that specifies connection status of PMC. OK (0) if ok, CONNECTION_ERROR (1) if connection could not be established, 
        /// MASTERSHIP_ERROR (2) if mastership could not be gained, TIMEOUT_ERROR (3) if full control could not be established.
        /// </summary>
        enum CONNECTIONSTATUS
        {
            OK,                 //0
            CONNECTION_ERROR,   //1
            MASTERSHIP_ERROR,   //2
            TIMEOUT_ERROR       //3
        }
    }
}
