////////////////////////////////////////////////////////////////////////
// TestHarness.cs - Demonstrates the functionalities of a Mock Test   // 
//               harness where the tests are executed as dll files    //
//                                                                    //
// Author: Nishant Agrawal, email-nagraw01@syr.edu                    //
// Application: CSE681 - Software Modelling Analysis, Project 4       //
// Environment: C# console                                            //
////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Gets a test request and the .dll files from each builder process
 * Enqueues each test request
 * Dequeues one by one and parses the test requests
 * Execute the dll files by invoking the class methods
 * Displays the results in the console and sends the log for each test request to the repository
 * 
 * Public Interface
 * ----------------
 * TestHarness tr = new TestHarness();
 * tr.runDllTests();
 * 
 * Required Files:
 * ---------------
 * TestHarness.cs
 * TestRequest.cs
 * IcomPro.cs
 * ComProService.cs
 * Logger.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 28 Sep 2017
 * ver 2.0 : 3 Dec 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;


namespace Com_GUI
{
  public class TestHarness
  {
        public string testPath { get; set; } = "..\\..\\..\\TestHarness\\TestHarnessStorage";
        public List<string> files { get; set; } = new List<string>();
        public TestRequest tr2 { get; set; }

        private Comm cm { get; set; } = null;

        public BlockingQueue<CommMessage> testRequests { get; set; } = null;

        //constructor- starts receive message thread, and start comm
        public TestHarness()
        {
            if (testRequests == null)
                testRequests = new BlockingQueue<CommMessage>();
            string absPath = Path.GetFullPath(testPath);
            cm = new Comm(9091, "/Harness", absPath);
            Thread threadProc = new Thread(threadFunction);
            threadProc.Start();
        }

        /*----< find all the files in TestHarness.testPath >-----------*/
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

        //dequeues each test request, parses it, gets all the required dll files in the test harness storage and executes them
        public void runDllTests()
        {
            while (true)
            {
                deleteLogs();
                CommMessage msg = testRequests.deQ();
                string logPath = testPath + "\\testLog";
                Logger log = new Logger(logPath);
                parseRequest(msg.arguments[0]);
                foreach (TestDriver driver in tr2.testDrivers)
                {
                    string library = driver.libName;
                    string fullPath = Path.GetFullPath(testPath + "\\" + library);
                    Console.WriteLine("\n\nExecuting test for {0}!!!\n===========================================", library);
                    Assembly assem = Assembly.LoadFrom(fullPath);
                    Type[] types = assem.GetExportedTypes();
                    log.Info("Executing test for "+library);
                    foreach (Type t in types)
                    {

                        MethodInfo[] mis = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
                        foreach (MethodInfo mi in mis)
                        {
                            try
                            {
                                if (mi.GetParameters().Length != 0)
                                {
                                    mi.Invoke(null, new Object[] { new string[] { "" } });
                                    log.Output("The test succeeded!");
                                    Console.WriteLine("\n\nThe test succeeded!");/* continue on error - calling factory function create throws */
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("\n\nThe test failed with exception: {0}\n\n", e.Message);/* continue on error - calling factory function create throws */
                                log.Error("The test failed with exception: " + e.Message);
                            }
                        }
                    }
                }
                log.close();
                sendLog();
            }            
        }

        //function to send test log files to the repository
        private void sendLog()
        {
            getFiles(testPath, "*.log");
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "sendingTestLog";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:9090/RepoMock";
            csndMsg.from = "http://localhost:9091/Harness";

            cm.postMessage(csndMsg);

            Console.WriteLine("\nSending test log file for the above test request to repository");

            cm.postFile(files[0], csndMsg.to, "..\\..\\..\\RepoMock\\RepoStorage");
        }

        //function to delete previously present log files from the buildstorage
        private void deleteLogs()
        {
            string[] filePaths = System.IO.Directory.GetFiles(testPath, "*.log");
            foreach (string filePath in filePaths)
            {
                System.IO.File.Delete(filePath);
            }
        }

        //parse a test request using the TestRequest package
        private void parseRequest(string fileName)
        {
            string[] xmlFile = Directory.GetFiles(testPath, fileName);
            string bfileSpec = System.IO.Path.GetFullPath(xmlFile[0]);
            Console.Write("\n Parsing the test request...\n----------------------------------------------------\n");
            tr2 = new TestRequest();
            tr2.loadXml(bfileSpec);
            tr2.parse("author");
            tr2.parse("dateTime");
            tr2.parseTh("test");
        }

        private void threadFunction()
        {
            while (true)
            {
                CommMessage rcvMsg = cm.getMessage();
                rcvMsg.show();
                if (rcvMsg.command == "executeTests")
                {
                    testRequests.enQ(rcvMsg);
                }
            }
        }
        static void Main(string[] args)
        {
            Console.Title = "Test Harness";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;

            Console.Write("\n  Mock test harness started here");
            Console.Write("\n ============================\n");

            TestHarness tr = new TestHarness();
            tr.runDllTests();
            Console.Write("\n\n");
        }
    }

#if (TEST_TESTHARNESS)

  ///////////////////////////////////////////////////////////////////
  // TestTestHarness class

  class TestTestHarness
  {
    static void Main(string[] args)
    {
      Console.Write("\n  Demonstration of Mock test harness");
      Console.Write("\n ============================");

      TestHarness tr = new TestHarness();
      tr.runDllTests();

      Console.Write("\n\n");
    }
  }
#endif
}
