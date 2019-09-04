////////////////////////////////////////////////////////////////////////
// Builder.cs - Demonstrates the functionalities of a Source code    // 
//               builder part of a pool process started by mother     //
//               builder                                              //
// Author: Nishant Agrawal, email-nagraw01@syr.edu                    //
// Application: CSE681 - Software Modelling Analysis, Project 4       //
// Environment: C# console                                            //
////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Send ready message to the mother builder
 * Get build request from mother builder
 * Parse the build request using TestRequest class
 * Ask repository to post source files to build storage
 * Get files from the build storage
 * Run the build process on the files
 * Show output, errors and exit codes on the console and log the same to a log file
 * Send the log file to the repo for storage
 * If build succeeds, create a test request
 * send the test request and the dll files to the test harness
 * once everything is done send a ready message back to the motherbuilder
 * 
 * Public Interface
 * ----------------
 * Builder bldr = new Builder();
 * 
 * Required Files:
 * ---------------
 * Builder.cs
 * TestRequest.cs
 * IcomPro.cs
 * ComProService.cs
 * Logger.cs
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
using System.Diagnostics;

namespace Com_GUI
{
  public class Builder
  {
        public string builderPath { get; set; } = "..\\..\\..\\Builder\\BuilderStorage";
        public List<string> sourcefiles { get; set; } = new List<string>();
        public List<string> files { get; set; } = new List<string>();
        public TestRequest tr2 { get; set; }
        private Comm cm { get; set; } = null;
        public int port { get; set; } = 0;
        public int execCount { get; set; } = 101;

        public List<string> testRequestDll { get; set; } = new List<string>();
        private int buildFailedToggle { get; set; } = 0;

        //builder constructor, starts comm and the thread to process the received messages from motherbuilder and repo
        public Builder(int portNum) {

            port = portNum;
            builderPath = Path.GetFullPath(builderPath + port.ToString());
            if (!Directory.Exists(builderPath))
                Directory.CreateDirectory(builderPath);
            cm = new Comm(port, "/Builder", builderPath);

            Thread threadProc = new Thread(threadFunction);
            threadProc.Start();
            sendReady();
        }

        //get files from the build storage and store in a list
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

        //parse a build request using the TestRequest package
        private void parseRequest(string bfileSpec)
        {
            Console.Write("\n Parsing the build request...\n----------------------------------------------------\n");
            tr2 = new TestRequest();
            tr2.loadXml(bfileSpec);
            tr2.parse("author");
            tr2.parse("dateTime");
            tr2.parseDr("test");
        }

        //runs the build process, after deleting the previous log, dll files
        private void runProcess(){
            buildFailedToggle = 0;
            deleteDLLs();
            deleteLogs();
            string logPath = builderPath + "\\buildLog"+port;
            Logger log = new Logger(logPath);
            string output ="";
            int i = 1;
            foreach (TestDriver td in tr2.testDrivers){
                Console.WriteLine("\n\nBuilding for test with {0}!!!\n===========================================\n\n", td.driverName);
                StringBuilder sb = new StringBuilder();
                sb.Append(td.driverName);
                foreach (string file in td.testedFiles)                {
                    sb.Append(" ");
                    sb.Append(file);
                }
                String entirePath = sb.ToString();
                string dllFileName = DateTime.Now.ToString("yyyyMMddTHHmmss") +"P"+port+"D"+i;
                i++;
                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "cmd.exe";
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.Arguments = string.Format("/Ccsc /target:library /out:{0}.dll {1}", dllFileName, entirePath);
                testRequestDll.Add(dllFileName+".dll");
                info.WorkingDirectory = builderPath;
                info.RedirectStandardError = true;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                p.StartInfo = info;
                p.Start();
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                string exitCode = "Exit Code: "+p.ExitCode;
                log.Info("Build result for testcase: "+ entirePath);
                log.Output(output);
                if (p.ExitCode == 1){
                    buildFailedToggle = 1;
                    log.Error(error);
                }
                log.exitCode(exitCode);
                Console.WriteLine("{0}", output);
                Console.WriteLine("{0}", error);
                Console.WriteLine("Exit Code: {0}", p.ExitCode);
            }
            log.close();
            sendBuildLogs();
        }

        //function to check the local storage of the builder for the source files required to process the test request
        private void checkCache()
        {
            Console.WriteLine("\n\nChecking the cache of this builder for the source files, if already there, not added to the 'sendsourcefile' message to repo!!!\n-----------------------------------------\n");

            getFiles(builderPath, "*.cs");
            foreach (TestDriver td in tr2.testDrivers)
            {
                if (!files.Contains(td.driverName))
                {
                    Console.WriteLine("This file is not in cache, requested from repo: {0}", td.driverName);
                    sourcefiles.Add(td.driverName);
                }
                else
                {
                    Console.WriteLine("This file is already in cache: {0}", td.driverName);
                }

                foreach (string file in td.testedFiles)
                {
                    if (!files.Contains(file))
                    {
                        Console.WriteLine("This file is not in cache, requested from repo: {0}", file);
                        sourcefiles.Add(file);
                    }
                    else
                    {
                        Console.WriteLine("This file is already in cache: {0}", file);
                    }

                }                
            }
            
        }

        //thread function to process the received messages on the receiver host
        private void threadFunction()
        {
            while (true)
            {
                CommMessage rcvMsg = cm.getMessage();
                rcvMsg.show();
                if (rcvMsg.command == "xmlFileSent")
                {
                    string xmlfile = rcvMsg.arguments[0];
                    
                    //get the file from the storage and its full path
                    string[] xmlFile = Directory.GetFiles(builderPath, xmlfile);
                    string bfileSpec = System.IO.Path.GetFullPath(xmlFile[0]);
                    parseRequest(bfileSpec);
                    
                    //check for files in cache
                    checkCache();
                    sendRepoRequest();
                }
                else if (rcvMsg.command == "sourceFilesSent")
                {
                    string[] filePaths = System.IO.Directory.GetFiles(builderPath, "*.dll");
                    foreach (string filePath in filePaths)
                    {
                        System.IO.File.Delete(filePath);
                    }
                    Console.WriteLine("\nStarting build process for the current test request:\n======================================================== \n");
                    runProcess();
                    sendTestHDll();
                    sendReady();
                }
                else if (rcvMsg.command == "quit")
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
        }

        //function to send ready message to the mother builder
        private void sendReady()
        {
            Console.WriteLine("\nSending ready message to the mother builder.......\n");

            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "ready";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:8080/MotherBuilder";
            csndMsg.from = "http://localhost:" + port.ToString() + "/Builder";
            csndMsg.fromStorage = builderPath;
            cm.postMessage(csndMsg);
        }

        //send request message to the repo now for source files if they are not in cache
        private void sendRepoRequest()
        {
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "sendsourcefiles";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:9090/RepoMock";
            csndMsg.from = "http://localhost:" + port.ToString() + "/Builder";
            csndMsg.fromStorage = builderPath;
            foreach (string file in sourcefiles)
            {
                string fileP = builderPath + file;
                if (!File.Exists(fileP))
                {
                    csndMsg.arguments.Add(file);
                }
            }

            //if there are no arguments to the message don't post it
            if (csndMsg.arguments.Count != 0)
            {
                cm.postMessage(csndMsg);
                Console.WriteLine("\nRequest message sent to the repository for source files..........\n");
            }
            else
            {
                Console.WriteLine("\nIf all files are already in cache, no request message is sent to the repository for source files since all source files are already in builder local storage.\n");
                runProcess();
                sendTestHDll();
                sendReady();
            }

            sourcefiles.Clear();
        }

        private void sendTestHDll()
        {
            if (buildFailedToggle == 1)
            {
                Console.WriteLine("\nBuild failed!!");
                return;
            }
            Console.WriteLine("\nBuild successful!!");

            string thReq = createTestRequest();
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "connect";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:9091/Harness";
            csndMsg.from = "http://localhost:" + port.ToString() + "/Builder";
            cm.postMessage(csndMsg);

            Console.WriteLine("\nSending test request file to test harness from builder");
            getFiles(builderPath, thReq);
            cm.postFile(files[0], csndMsg.to, "..\\..\\..\\TestHarness\\TestHarnessStorage");
            csndMsg.arguments.Add(files[0]);
            //Console.WriteLine("\nSending test request file to test harness from builder {0}", files[0]);

            Console.WriteLine("\nSending dll files to test harness from builder");
            getFiles(builderPath, "*.dll");
            foreach (string fileName in files)
            {
                cm.postFile(fileName, csndMsg.to, "..\\..\\..\\TestHarness\\TestHarnessStorage");
            }

            csndMsg.command = "executeTests";
            cm.postMessage(csndMsg);
        }

        //function to send ready message to the mother builder
        private void sendBuildLogs()
        {

            Console.WriteLine("\nSending build logs to the repository.......\n");

            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "sendingBuildLog";
            csndMsg.author = "Nishant Agrawal";
            csndMsg.to = "http://localhost:9090/RepoMock";
            csndMsg.from = "http://localhost:" + port.ToString() + "/Builder";
            cm.postMessage(csndMsg);

            getFiles(builderPath, "*.log");

            cm.postFile(files[0], csndMsg.to, "..\\..\\..\\RepoMock\\RepoStorage");
        }

        //function to previously present dll files from the buildstorage
        public void deleteDLLs()
        {
            string[] filePaths = System.IO.Directory.GetFiles(builderPath, "*.dll");
            foreach (string filePath in filePaths)
            {
                System.IO.File.Delete(filePath);
            }
        }

        //function to delete previously present log files from the buildstorage
        private void deleteLogs()
        {
            string[] filePaths = System.IO.Directory.GetFiles(builderPath, "*.log");
            foreach (string filePath in filePaths)
            {
                System.IO.File.Delete(filePath);
            }
        }

        //function to create a test request to send to test harness
        private string createTestRequest()
        {
            List<TestDriver> testD = new List<TestDriver>();

            int i = 0;
            foreach (TestDriver driver in tr2.testDrivers)
            {

                driver.libName = testRequestDll[i];
                i++;

            }
            TestRequest trTemp = new TestRequest();
            string fileName = "TestExecRequest"+port+execCount+".xml";
            string fileSpec = System.IO.Path.Combine(builderPath, fileName);
            fileSpec = System.IO.Path.GetFullPath(fileSpec);

            trTemp.author = tr2.author;
            trTemp.testDrivers = tr2.testDrivers;
            trTemp.makeTestHRequest();
            trTemp.saveXml(fileSpec);
            execCount++;

            Console.WriteLine("\nTest Request successfully built and saved on builder storage...\n----------------------------------------------------\n");
            return fileName;
        }

        static void Main(string[] args)
        {
            int port = Int32.Parse(args[0]);
            Console.Title = "Builder"+port;
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;

            Console.Write("\n  Demo Builder Process on port {0}", port);
            Console.Write("\n ====================\n");

            Builder b1 = new Builder(port);
        }
  }
#if (TEST_BUILDER)

  ///////////////////////////////////////////////////////////////////
  // TestBuilder class

  class TestBuilder
  {
    static void Main(string[] args)
    {
      Console.Write("\n  Demonstration of souce code Builder");
      Console.Write("\n ============================");
      
      int port = Int32.Parse(args[0]);
      Builder bldr = new Builder(port);
    }
  }
#endif
}
