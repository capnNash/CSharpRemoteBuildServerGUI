using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Com_GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Com_GUI.MainWindow mwin = new Com_GUI.MainWindow();
            mwin.Show();
            if (e.Args[0] == "testExec")
            {
                mwin.testExec();
            }
        }
    }
}
