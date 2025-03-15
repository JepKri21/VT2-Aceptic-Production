using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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
        private static WaitUntilTriggerParams time_params = new WaitUntilTriggerParams();

        double speedLinar = 0.5;
        double speedRotation = 0.1;
        double speedFinal = 0.0;
        double acclerationMax = 0.1;
        double rotationVel = 1;
        double rotationAcc = 1;
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

        public void RotateMotion(ushort cmdLabel, int xbotID, double targetAngle, string RotationDirection)
        {
            globalCmdLabel = cmdLabel;
            if (string.IsNullOrWhiteSpace(RotationDirection))
            {
                Console.WriteLine("Warning: RotationDirection is null or empty. Using default 'CW'.");
                RotationDirection = "CW";
            }

            targetAngle = (Math.PI / 180) * targetAngle;

            switch (RotationDirection.ToUpper())
            {
                case "CW":
                    _xbotCommand.RotaryMotionP2P(cmdLabel, xbotID, ROTATIONMODE.WRAP_TO_2PI_CW, targetAngle, rotationVel, rotationAcc, POSITIONMODE.ABSOLUTE);
                    break;

                case "CCW":
                    _xbotCommand.RotaryMotionP2P(cmdLabel, xbotID, ROTATIONMODE.WRAP_TO_2PI_CCW, targetAngle, rotationVel, rotationAcc, POSITIONMODE.ABSOLUTE);
                    break;
                default:
                    Console.WriteLine($"Warning: Invalid RotationDirection '{RotationDirection}'. Using default 'CW'.");
                    _xbotCommand.RotaryMotionP2P(cmdLabel, xbotID, ROTATIONMODE.WRAP_TO_2PI_CW, targetAngle, rotationVel, rotationAcc, POSITIONMODE.ABSOLUTE);
                    break;
            }

            
        }

        public void Stationairy(double secs, int xbotID)
        {
            time_params.delaySecs = secs;
            _xbotCommand.WaitUntil(0, xbotID, TRIGGERSOURCE.TIME_DELAY, time_params);
        }

    }
}
