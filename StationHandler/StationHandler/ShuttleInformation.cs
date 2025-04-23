using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StationHandler
{
    class ShuttleInformation
    {

        int shuttleID;
        int shuttleStatus;
        double[] shuttlePosition;
        string shuttleObjective;

        public int _shuttleID
        {
            get { return shuttleID; }
            set { shuttleID = value; }
        }

        public int _shuttleStatus
        {
            get { return shuttleStatus; }
            set { shuttleStatus = value; }
        }

        public string _shuttleObjective
        {
            get { return shuttleObjective; }
            set { shuttleObjective = value; }
        }

        public double[] _shuttlePosition
        {
            get { return shuttlePosition; }
            set { shuttlePosition = value; }
        }

    }
}
