using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace VT2_Aseptic_Production_Demonstrator
{
    class Zone_Contol
    {
        //This class contains a collenction of functions the is used to control the PMC System 
        private static SystemCommands _systemCommand = new SystemCommands();
        //This class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.
        private static XBotCommands _xbotCommand = new XBotCommands();

        private double xMin = 0.240;
        private double xMax = 0.480;
        private double yMin = 0.480;
        private double yMax = 0.720; 

        
        public void ZoneDefine()
        {
            _systemCommand.DefineZone(1, xMin, yMin, xMax, yMax);
            _systemCommand.ZoneFenceControl(1, FENCEOPERATION.BUILD_FENCE);

        }
    }
}
