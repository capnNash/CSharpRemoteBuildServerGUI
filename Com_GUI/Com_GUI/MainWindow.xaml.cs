
////////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - GUI for getting source files, adding build    //
//                      request structure, creating build request,    // 
//                      getting xml files, starting and shutting      //
//                      building process                              //
//                                                                    //
// Author: Nishant Agrawal, email-nagraw01@syr.edu                    //
// Application: CSE681 - Software Modelling Analysis, Project 4       //
// Environment: C# console                                            //
////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Initialize window component and diplay the GUI
 * testExec() function to show all the requirements
 * button functionalities to get source files, add drivers, create test request, get xml files, start and shut building process
 *
 * Public Interface
 * ----------------
 * testExec(); - executive function to display that all requirements has been met
 * 
 * Required Files:
 * ---------------
 * MainWindow.xaml.cs
 * App.xaml.cs
 * TestRequest.cs
 * IcomPro.cs
 * ComProService.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 28 Sep 2017
 * ver 2.0 : 24 Oct 2017
 * ver 3.0 : 3 Dec 2017
 * - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;

namespace Com_GUI
{
    //Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        public List<string> testD { get; set; }
        public List<TestDriver> driverList { get; set; }

        private Comm cm { get; set; } = null;
        Dictionary<string, Action<CommMessage>> messageDispatcher { get; set; } = new Dictionary<string, Action<CommMessage>>();
        Thread rcvThread { get; set; } = null;
        public MainWindow()
        {
            InitializeComponent();
            testD = new List<string>();
            processnumber.Text = "3";

            cm = new Comm(8090, "/Client", System.IO.Directory.GetCurrentDirectory());

            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();

            initializeMessageDispatcher();

            startRepo();
            startBuilder();
            startHarness();
        }

        //message dispatcher
        void initializeMessageDispatcher()
        {
            messageDispatcher["setAllFiles"] = (CommMessage msg) =>
            {
                foreach (string fileName in msg.arguments)
                {
                    if (fileName.Contains("Driver"))
                    {
                        if(!driverListBox.Items.Contains(fileName))
                            driverListBox.Items.Add(System.IO.Path.GetFileName(fileName));
                    }
                    else
                    {
                        if (!testFilesListBox.Items.Contains(fileName))
                            testFilesListBox.Items.Add(System.IO.Path.GetFileName(fileName));
                    }
                }
                Console.Write("\n Test Drivers and tested files list are now updated in the List boxes.\n---------------------------------------------------- \n");
            };

            messageDispatcher["setXmlFiles"] = (CommMessage msg) =>
            {
                foreach (string fileName in msg.arguments)
                {
                    if (!xmlListBox.Items.Contains(fileName))
                        xmlListBox.Items.Add(System.IO.Path.GetFileName(fileName));
                }
                Console.Write("\n Xml files list is now updated in the List box.\n---------------------------------------------------- \n");
            };
        }
       
        //----< define processing for GUI's receive thread >-------------
        // pass the Dispatcher's action value to the main thread for execution
        void rcvThreadProc()
        {
            Console.Write("\n  starting client's receive thread");
            while (true)
            {
                CommMessage msg = cm.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;

                Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
            }
        }

        //executive function to display that all requirements has been met
        public void testExec()
        {
            Console.Write("\n Demonstration of Requirement 1:\n======================================================== \n");
            Console.Write("\n This project has been prepared using C#, the .Net Frameowrk, and Visual Studio 2017 \n");
            Console.Write("\n Demonstration of Requirement 2:\n======================================================== \n");
            Console.Write("\n This project includes a Message-Passing Communication Service built with WCF\n");
            Console.Write("\n Demonstration of Requirement 3:\n======================================================== \n");
            Console.Write("\n The Communication Service supports sending build requests to Pool Processes from the mother Builder process and sending and receiving files from repository\n");
            Console.Write("\n Demonstration of Requirement 4:\n======================================================== \n");
            Console.Write("\n This provide a Repository server that supports client browsing to find files to build, sends an XML build request and the cited files to the Build Server.\n\n\n");
            Console.Write("\n Demonstration of Requirement 5:\n======================================================== \n");
            Console.Write("\n This provides a Process Pool component(function allProcess())that creates a user inputted number of processes on command\n\n\n");
            Console.Write("\n Demonstration of Requirement 6:\n======================================================== \n");
            Console.Write("\n Pool Processes are using Communication prototype to access messages and xml request files from the mother Builder process\n");
            Console.Write("\n Demonstration of Requirement 7:\n======================================================== \n");
            Console.Write("\n Each Pool Process attempts to build each library, cited in a retrieved build request, creates a logfile with warnings and errors.\n\n\n");
            Console.Write("\n Demonstration of Requirement 8:\n======================================================== \n");
            Console.Write("\n Each builder process sends the build log to the repository and if the build succeeds, sends a test request and libraries to the Test Harness for execution.\n\n\n");
            Console.Write("\n Demonstration of Requirement 9:\n======================================================== \n");
            Console.Write("\n Test Harness attempts to load each test library it receives and execute it, and submits the test result logs to the Repository.\n\n\n");
            Console.Write("\n Demonstration of Requirement 10:\n======================================================== \n");
            Console.Write("\n Includes a Graphical User Interface, built using WPF\n");
            Console.Write("\n Demonstration of Requirement 11:\n======================================================== \n");
            Console.Write("\n The GUI client is a separate process, implemented with WPF and using message-passing communication. It sends message to get source files and xml files from the Repository. Selected Source files are packaged into a test library with a test element specifying driver and tested files, this process can be done for multiple tests.\n\n\n");
            Console.Write("\n Demonstration of Requirement 12:\n======================================================== \n");
            Console.Write("\n The GUI client sends build request structures to the repository for storage and transmission to the Build Server\n\n\n");
            Console.Write("\n Demonstration of Requirement 13:\n======================================================== \n");
            Console.Write("\n The GUI client can request the repository to send a build request in its storage to the Build Server for build processing.\n----------------------------------------------------\n\n");
             
            getfiles();
            execHelp1();
            getRequestfiles();
            execHelp2();
            sendStartProcessMessage();
        }

        //helper function for exec builds two test requests(1. build succeeds 2. build fails) with 3 test cases(3. test fails)
        private void execHelp1()
        {
            string test1 = "TestDriver1.cs TestedOne.cs TestedTwo.cs";
            string test2 = "TestDriver3.cs TestedThree.cs";
            testD.Add(xmlString(test1));
            testD.Add(xmlString(test2));
            buildrequestfunc();

            string test3 = "TestDriver2.cs TestedOne.cs TestedTwo.cs";
            testD.Add(xmlString(test3));
            buildrequestfunc();           
        }

        //helper function for exec, to send message to the repository to send the selected xml files to the mother builder for building 
        private void execHelp2()
        {
            Console.Write("\n Sending message to the repository to send the selected xml files to the mother builder for building.\n---------------------------------------------------- \n");
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "sendSelecXml";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:" + "9090/RepoMock";
            csndMsg.from = "http://localhost:" + "8090/Client";

            csndMsg.arguments.Add("TestRequest22.xml");
            csndMsg.arguments.Add("TestRequest23.xml");

            cm.postMessage(csndMsg);
        }

        //button function to get driver and tested files from repository
        private void Button_getfiles(object sender, RoutedEventArgs e)
        {
            getfiles();
        }

        //function to send message to the repository to get all driver and tested files
        private void getfiles()
        {
            Console.Write("\n Sending message to repository to get all the source files.\n");
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "getAllFiles";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:" + "9090/RepoMock";
            csndMsg.from = "http://localhost:" + "8090/Client";
            cm.postMessage(csndMsg);
        }

        //function to create xml string
        private string xmlString(string testCase)
        {
            string[] substrings = testCase.Split(' ');
            StringBuilder sb = new StringBuilder();
            sb.Append("<test>");
            sb.Append("<driver>" + substrings[0] + "</driver>");
            for (int i = 1; i < substrings.Length; i++)
            {
                sb.Append("<tested>" + substrings[i] + "</tested>");
            }
            sb.Append("</test>");

            return sb.ToString();
        }

        //function to send message to the repository to get all xml request files
        private void getRequestfiles()
        {
            Console.Write("\n Sending message to repository to get all xml request files.\n");
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "getXmlFiles";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:" + "9090/RepoMock";
            csndMsg.from = "http://localhost:" + "8090/Client";
            cm.postMessage(csndMsg);
        }

        //function to send message to the repository to send the selected xml files to the mother builder for building 
        private bool sendXmlToBuilder()
        {
            //when nothing is selected           
            if (xmlListBox.SelectedIndex == -1)
            {
                Console.Write("\n >>>Please select xml files and then press this button.\n");
                return false;
            }

            Console.Write("\n Sending message to the repository to send the selected xml files to the mother builder for building.\n---------------------------------------------------- \n");
            //send a message to the repo to send all files
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "sendSelecXml";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:" + "9090/RepoMock";
            csndMsg.from = "http://localhost:" + "8090/Client";

            for (int i = 0; i < xmlListBox.SelectedItems.Count; i++)
            {
                csndMsg.arguments.Add(xmlListBox.SelectedItems[i].ToString());
            }
            cm.postMessage(csndMsg);
            return true;
        }

        //button function to add driver after choosing a driver and its corresponding tested files
        private void Button_adddriver(object sender, RoutedEventArgs e)
        {
            //when nothing is selected           
            if (driverListBox.SelectedIndex == -1 || testFilesListBox.SelectedIndex == -1)
            {
                Console.Write("\n >>>Please select a test driver and corresponding tested files and then press this button.\n");
                return;
            }

            string driver = driverListBox.SelectedItem.ToString();
            string testCase = driver;
            for (int i = 0; i < testFilesListBox.SelectedItems.Count; i++)
            {
                testCase += " " + testFilesListBox.SelectedItems[i].ToString();
            }
            testD.Add(xmlString(testCase));
            Console.Write("\n Test Driver:{0} and its tested files have been added to the request. Press the 'Build Request' button when you are done adding all drivers.\n----------------------------------------------------\n", xmlString(testCase));

            Console.Write("\n Test Driver:{0} and its tested files have been added to the request. Press the 'Build Request' button when you are done adding all drivers.\n----------------------------------------------------\n", driver);
        }

        //button function to create a request after desired drivers and tested files have been added
        private void Button_buildrequest(object sender, RoutedEventArgs e)
        {
            if (testD.Count == 0)
            {
                Console.Write("\n >>>Add drivers and tested files first to your test request .\n");
            }
            else
            {
                buildrequestfunc();
                Console.Write("\n Your test request has been built and saved to the repository.\n---------------------------------------------------- \n");
            }
        }

        //function to create a request after desired drivers and tested files have been added
        private void buildrequestfunc()
        {
            Console.Write("\nSending message to the repository to build and save test request with selected source files.\n");
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "createRequest";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:" + "9090/RepoMock";
            csndMsg.from = "http://localhost:" + "8090/Client";
            csndMsg.arguments = testD;
            cm.postMessage(csndMsg);
            testD.Clear();
        }

        //button function to start the motherbuilder and sending it the no. of builder processes to start
        private void Button_getXml(object sender, RoutedEventArgs e)
        {
            getRequestfiles();
        }

        //button function to start the motherbuilder and sending it the no. of builder processes to start
        private void Button_startbuilder(object sender, RoutedEventArgs e)
        {
            if (!sendXmlToBuilder()) return;
            sendStartProcessMessage();
        }

        //button function to shut the builder processes
        private void Button_shutbuilder(object sender, RoutedEventArgs e)
        {
            Console.Write("\n Shutting down the builder processes by sending a message to the mother Builder.\n======================================================== \n");
            sendShutProcessMessage();
        }

        //function to start motherbuilder process
        private bool startBuilder()
        {
            Process proc = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = "..\\..\\..\\MotherBuilder\\bin\\debug\\MotherBuilder.exe";
            string absFileSpec = System.IO.Path.GetFullPath(info.FileName);

            Console.Write("\nAttempting to start Mother Builder process.........\n");

            proc.StartInfo = info;
            try
            {
                proc.Start();
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }

        //function to start repomock process
        private bool startRepo()
        {
            Process proc = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = "..\\..\\..\\RepoMock\\bin\\debug\\RepoMock.exe";
            string absFileSpec = System.IO.Path.GetFullPath(info.FileName);

            proc.StartInfo = info;
            try
            {
                proc.Start();
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }

        //function to start test harness process
        private bool startHarness()
        {
            Process proc = new Process();
            ProcessStartInfo info = new ProcessStartInfo();

            info.FileName = "..\\..\\..\\TestHarness\\bin\\debug\\TestHarness.exe";
            string absFileSpec = System.IO.Path.GetFullPath(info.FileName);

            proc.StartInfo = info;
            try
            {
                proc.Start();
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }

        //function to send message to the mother builder to start builder process
        private void sendStartProcessMessage()
        {
            Console.Write("\n Sending a message to the mother Builder to start builder processes.\n======================================================== \n");
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "startBuilders";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:" + "8080/MotherBuilder";
            csndMsg.from = "http://localhost:" + "8090/Client";
            csndMsg.arguments.Add(processnumber.Text);

            cm.postMessage(csndMsg);
        }

        //function to send a message to the mother Builder to kill builder processes
        private void sendShutProcessMessage()
        {
            Console.Write("\n Sending a message to the mother Builder to kill builder processes.\n======================================================== \n");
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "killBuilders";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:" + "8080/MotherBuilder";
            csndMsg.from = "http://localhost:" + "8090/Client";
            cm.postMessage(csndMsg);
        }
        //----< shut down comm when the main window closes >-------------

    }
}
