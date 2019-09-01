using System;

namespace SRPoc
{
    class Program
    {
        static void Main(string[] args)
        {
            new DeploymentHelper().Run().GetAwaiter().GetResult();
        }
    }
}
