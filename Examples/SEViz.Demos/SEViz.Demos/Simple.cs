using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEViz.Demos
{
    public class Simple
    {
        public bool IfBranching(int condition)
        {
            if(condition > 10)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int SwitchBranching(int condition)
        {
            var divider = 0;
            switch(condition)
            {
                case 0:
                    return 0;
                case 1:
                    return -1;
                case 2:
                    return -2;
                default:
                    return (condition / divider);
            };
        }

        public void ForLoop(int bound, bool decision)
        {
            for(int i = 0; i < bound; i++)
            {
                if (decision)
                {
                    Console.WriteLine("True decision");
                }
                else
                {
                    Console.WriteLine("False decision");
                }
            }
        }
    }
}
