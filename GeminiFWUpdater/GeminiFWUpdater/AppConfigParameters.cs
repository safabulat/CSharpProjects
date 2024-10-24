using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiFWUpdater
{
    internal class AppConfigParameters
    {
        public string ServiceIP { get; set; }
        public int ServicePORT { get; set; }
        public string ServiceEndpoint { get; set; }
        public int ServiceTimeout{ get; set; }
        public int TaskDelayTime { get; set; }

        public void printParams()
        {
            Console.WriteLine("ServisIP:\t" + ServiceIP);
            Console.WriteLine("ServisPORT:\t" + ServicePORT);
            Console.WriteLine("ServisEndpoint:\t" + ServiceEndpoint);
            Console.WriteLine("ServiceTimeout:\t" + ServiceTimeout);
            Console.WriteLine("TaskDelayTime:\t" + TaskDelayTime);
        }
    }
}
