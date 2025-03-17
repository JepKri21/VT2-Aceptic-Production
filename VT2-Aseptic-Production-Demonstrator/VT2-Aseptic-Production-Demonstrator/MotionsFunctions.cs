using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMCLIB;

namespace VT2_Aseptic_Production_Demonstrator
{

    class MotionsFunctions
    {
        //this class contains a collection of xbot commands, such as discover xbots, mobility control, linear motion, etc.

        private static SystemCommands _systemCommand = new SystemCommands();
        private static XBotCommands _xbotCommand = new XBotCommands();

        double speedLinar = 0.5;
        double speedRotation = 0.1;
        double speedFinal = 0.0;
        double acclerationMax = 0.1;
        double rotationVel = 0.1;
        double rotationAcc = 0.1;
        ushort globalCmdLabel;

        public void LinarMotion(ushort cmdLabel, int xbotID, double tagPosX, double tagPosY, string pathType)
        {
            globalCmdLabel = cmdLabel;
            if (string.IsNullOrWhiteSpace(pathType))
            {
                Console.WriteLine("Warning: pathType is null or empty. Using default 'D'.");
                pathType = "D";
            }

            switch (pathType.ToUpper())
            {
                case "D":
                    _xbotCommand.LinearMotionSI(cmdLabel, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.DIRECT, tagPosX, tagPosY, speedFinal, speedLinar, acclerationMax);
                    break;
                case "XY":
                    _xbotCommand.LinearMotionSI(cmdLabel, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.XTHENY, tagPosX, tagPosY, speedFinal, speedLinar, acclerationMax);
                    break;
                case "YX":
                    _xbotCommand.LinearMotionSI(cmdLabel, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.YTHENX, tagPosX, tagPosY, speedFinal, speedLinar, acclerationMax);
                    break;
                default:                
                    Console.WriteLine($"Warning: Invalid pathType '{pathType}'. Using default 'D'.");
                    _xbotCommand.LinearMotionSI(cmdLabel, xbotID, POSITIONMODE.ABSOLUTE, LINEARPATHTYPE.DIRECT, tagPosX, tagPosY, speedFinal, speedLinar, acclerationMax);                   
                    break;

            }

            
                
        }

        public void RotateMotion(ushort cmdLabel, int xbotID, double tagAngle, string RotationDirection)
        {
            globalCmdLabel = cmdLabel;
            if (string.IsNullOrWhiteSpace(RotationDirection))
            {
                Console.WriteLine("Warning: RotationDirection is null or empty. Using default 'CW'.");
                RotationDirection = "CW";
            }

            switch (RotationDirection.ToUpper())
            {
                case "CW":
                    _xbotCommand.RotaryMotionP2P(cmdLabel, xbotID, ROTATIONMODE.WRAP_TO_2PI_CW, tagAngle, rotationVel, rotationAcc, POSITIONMODE.ABSOLUTE);
                    break;

                case "CCW":
                    _xbotCommand.RotaryMotionP2P(cmdLabel, xbotID, ROTATIONMODE.WRAP_TO_2PI_CCW, tagAngle, rotationVel, rotationAcc, POSITIONMODE.ABSOLUTE);
                    break;
                default:
                    Console.WriteLine($"Warning: Invalid RotationDirection '{RotationDirection}'. Using default 'CW'.");
                    _xbotCommand.RotaryMotionP2P(cmdLabel, xbotID, ROTATIONMODE.WRAP_TO_2PI_CW, tagAngle, rotationVel, rotationAcc, POSITIONMODE.ABSOLUTE);
                    break;
            }

            
        }

        public ushort RetrunCmdLabel()
        {
            return globalCmdLabel;
        }

    }
}
