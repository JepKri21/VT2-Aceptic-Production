using PMCLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    class Actions
    {
        private int[] xBotIDs;
        private Dictionary<int, List<double[]>> trajectories;

        public Actions(int[] _xbotid, Dictionary<int, List<double[]>> _traj)
        {
            xBotIDs = _xbotid;
            trajectories = _traj;

        }

        


    }

}
