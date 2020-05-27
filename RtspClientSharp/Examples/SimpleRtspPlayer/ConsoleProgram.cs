using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleRtspPlayer
{
    class ConsoleProgram
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
