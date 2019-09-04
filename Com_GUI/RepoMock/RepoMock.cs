////////////////////////////////////////////////////////////////////////
// RepoMock.cs - Demonstrates the functionalities of a Mock repository// 
//                 for storing test request and source code files     //
//                                                                    //
// Author: Nishant Agrawal, email-nagraw01@syr.edu                    //
// Application: CSE681 - Software Modelling Analysis, Project 4       //
// Environment: C# console                                            //
////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Store and get files from repository storage
 * send source files names to the GUI
 * send xml files names to the GUI
 * Send request files to motherbuilder on command from the GUI client
 * Send source files to builder processes after getting a request message
 * store test and build logs in the repo storage
 *
 * Public Interface
 * ----------------
 * RepoMock rp = new RepoMock();
 * 
 * Required Files:
 * ---------------
 * RepoMock.cs
 * IcomPro.cs
 * ComProService.cs
 * TestRequest.cs
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
using System.IO;
using System.Xml.Linq;

namespace Com_GUI
{

  public class RepoMock
  {
        public string storagePath { get; set; } = "..\\..\\..\\RepoMock\\RepoStorage";
        public List<string> files { get; set; } = new List<string>();
        private Comm cm { get; set; } = null;

        private int testReqCount { get; set; } = 22;
        
        //repomock constructor, starts comm and the thread for receiving messages
        public RepoMock()
        {
            string absPath = Path.GetFullPath(storagePath);
            cm = new Comm(9090, "/RepoMock", absPath);

            Thread rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }

        //----< define processing for GUI's receive thread >-------------

        void rcvThreadProc()
        {
            Console.Write("\n  starting repo's receive thread");
            while (true)
            {
                CommMessage msg = cm.getMessage();
                msg.show();
                if (msg.command == "sendsourcefiles")
                    sendSourceFiles(msg);

                else if (msg.command == "getAllFiles")
                {
                    getAllFiles("*.cs");
                    Console.WriteLine("\nSending a message back to the client with the source file names as message arguments.");
                }
                    
                else if (msg.command == "getXmlFiles")
                {
                    getAllFiles("*.xml");
                    Console.WriteLine("\nSending a message back to the client with the xml file names as message arguments.");
                }

                else if (msg.command == "createRequest")
                    createRequest(msg);

                else if (msg.command == "sendSelecXml")
                    sendRequests(msg);
            }
        }

        //function to get the files from repository storage
        private void getFiles(string path, string pattern)
        {
          files.Clear();
                string[] tempFiles = Directory.GetFiles(path, pattern);
                for (int i = 0; i < tempFiles.Length; ++i)
                {
                    tempFiles[i] = Path.GetFileName(tempFiles[i]);
                }
                files.AddRange(tempFiles);
        }

        //function to get files based on pattern and send as message args to the GUI client
        private void getAllFiles(string pattern)
        {
            getFiles(storagePath, pattern);
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);

            if(pattern == "*.cs") csndMsg.command = "setAllFiles";
            else csndMsg.command = "setXmlFiles";

            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:" + "8090/Client";
            csndMsg.from = "http://localhost:" + "9090/RepoMock";

            foreach(string file in files)
            {
                csndMsg.arguments.Add(file);
            }
            cm.postMessage(csndMsg);
        }

        //function to create a build request based on the message args from client
        private void createRequest(CommMessage rcvMsg)
        {
            List<TestDriver> testD = new List<TestDriver>();

            foreach (string testCase in rcvMsg.arguments)
            {
                TestDriver td1 = new TestDriver();
                List<string> tested = new List<string>();

                XElement doc = XElement.Parse(testCase);
                foreach (XElement element in doc.Elements())
                {
                    if (element.Name == "driver")
                    {
                        td1.driverName = element.Value;
                    }
                    else
                    {
                        tested.Add(element.Value);
                    }
                }
                td1.testedFiles = tested;
                testD.Add(td1);
            }
            TestRequest tr = new TestRequest();
            string fileName = "TestRequest" + testReqCount.ToString() + ".xml";
            string fileSpec = System.IO.Path.Combine(storagePath, fileName);
            fileSpec = System.IO.Path.GetFullPath(fileSpec);
            tr.author = "Nishant Agrawal";
            tr.testDrivers = testD;
            tr.makeRequest();
            tr.saveXml(fileSpec);
            testReqCount++;
            Console.WriteLine("\nTest Request successfully built and saved on repo...\n----------------------------------------------------\n");
        }

        //function to send source files to the builder processes 
        private void sendSourceFiles(CommMessage rcvmsg)
        {
                    CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
                    csndMsg.command = "connect";
                    csndMsg.author = "Nishant Agrawal";
                    csndMsg.to = rcvmsg.from;
                    csndMsg.from = "http://localhost:" + "9090/RepoMock";
                    cm.postMessage(csndMsg);

                    Console.WriteLine("\nSending source files to builderProcess {0} checking the arguments of 'sendsourcefiles' message from builder.\n----------------------------------------------------\n", rcvmsg.from);
                    foreach (string fileName in rcvmsg.arguments)
                    {
                        cm.postFile(fileName, rcvmsg.from, rcvmsg.fromStorage);
                    }

                    csndMsg.command = "sourceFilesSent";
                    cm.postMessage(csndMsg);
        }

        //function to send xml files to the mother builder
        private void sendRequests(CommMessage rcvMsg)
        {
            Console.WriteLine("\nSending xml requests selected by client to mother builder for build processing...\n----------------------------------------------------\n");
            
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "connect";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:" + "8080/MotherBuilder";
            csndMsg.from = "http://localhost:" + "9090/RepoMock";
            cm.postMessage(csndMsg);

            CommMessage rsndMsg = new CommMessage(CommMessage.MessageType.request);
            foreach (string file in rcvMsg.arguments)
            {
                cm.postFile(file, csndMsg.to, "..\\..\\..\\MotherBuilder\\mBuilderStorage");
                rsndMsg.arguments.Add(file);
            }

            rsndMsg.command = "allxmlsent";
            rsndMsg.author = "Nishant Agrawal";
            rsndMsg.to = "http://localhost:" + "8080/MotherBuilder";
            rsndMsg.from = "http://localhost:" + "9090/RepoMock";
            cm.postMessage(rsndMsg);
        }
        
    }

#if (TEST_REPOMOCK)

    ///////////////////////////////////////////////////////////////////
    // TestRepoMock class

    class TestRepoMock
    {
        static void Main(string[] args)
        {
            Console.Title = "RepoMock";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;

            Console.Write("\n  RepoMock Process");
            Console.Write("\n =====================\n");
            RepoMock repo = new RepoMock();
            Console.Write("\n\n");
        }
    }
#endif
}

