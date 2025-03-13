using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VT2_Aseptic_Production_Demonstrator
{
    class MotionAction
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public MotionAction(int id, float x, float y)
        {
            Id = id;
            X = x;
            Y = y;
        }
    }
}
