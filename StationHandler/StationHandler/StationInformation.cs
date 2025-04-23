using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StationHandler
{
    class StationInformation
    {

        string stationName;
        string stationStatus;
        double[] stationPosition;
        int occupyingXbot;

        public string _stationName
        {
            get { return stationName; }
            set { stationName = value; }
        }

        public string _stationStatuc
        {
            get { return stationStatus; }
            set { stationStatus = value; }
        }

        public int _occupyingXbot
        {
            get { return occupyingXbot; }
            set { occupyingXbot = value; }
        }

        public double[] _stationPosition
        {
            get { return stationPosition; }
            set { stationPosition = value;}
        }


    }
}
