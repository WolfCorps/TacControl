using System;
using System.Threading;

namespace TacControl.Server
{
    class Program
    {
        static void Main(string[] args)
        {

            Networking.Instance.Start();
            GameState.Instance.Init();



            Thread.Sleep(Timeout.Infinite);






        }
    }
}
