using System;
using System.Collections.Generic;
using System.Text;
using ReflectSoftware.Insight;

namespace TestHarness.Playgrounds
{
    public static class ConfigurationPlayground
    {
        static public void Run()
        {
            var ri = RILogManager.Get("Test");


            ri.EnterMethod("MyEnter");
            ri.SendMessage("Test1");
            ri.SendMessage("Test2");
            ri.ExitMethod("MyEnter");
        }
    }
}
