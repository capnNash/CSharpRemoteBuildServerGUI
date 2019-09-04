////////////////////////////////////////////////////////////////////////
// MotherBuilder.cs - Demos the functionalities of a mother builder that
//                      starts the builder process pool and manages them// 
//                                                                    //
// Author: Nishant Agrawal, email-nagraw01@syr.edu                    //
// Application: CSE681 - Software Modelling Analysis, Project 4       //
// Environment: C# console                                            //
////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Get test requests from repository via wcf
 * Starts build process pool
 * Enqueues ready messages from builders and the request files in separate blocking queues 
 * Dequeues ready message and a request file one by one and send to the ready builder
 * 
 * Public Interface
 * ----------------
 * MotherBuilder mb = new MotherBuilder();
 * mb.receiveAction();- thread function to handle the received messages on the receiver host
 * mb.sendRequest();- function to forward xml request files to the builder processes
 * 
 * Required Files:
 * ---------------
 * MotherBuilder.cs
 * IcomPro.cs
 * ComProService.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 24 Oct 2017
 * ver 2.0 : 3 Dec 2017
 * - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Com_GUI
{
    public class MotherBuilder
    {
        private Comm cm { get; set; } = null;
        public string motherbuilderPath { get; set; } = "..\\..\\..\\MotherBuilder\\mBuilderStorage";
        public List<string> files { get; set; } = new List<string>();
        public int count { get; set; } = 0;

        public BlockingQueue<CommMessage> readyQueue { get; set; } = null;
        public BlockingQueue<string> requestFiles { get; set; } = null;

        //constructor that starts the comm for motherbuilder
        public MotherBuilder()
        {
            if (readyQueue == null)
                readyQueue = new BlockingQueue<CommMessage>();
            if (requestFiles == null)
                requestFiles = new BlockingQueue<string>();
            string absPath = Path.GetFullPath(motherbuilderPath);
            cm = new Comm(8080, "/MotherBuilder", absPath);

        }

        //thread function to handle the received messages on the receiver host
        public void receiveAction()
        {
            while (true)
            {
                CommMessage rcvMsg = cm.getMessage();
                rcvMsg.show();
                if (rcvMsg.command == "ready")
                {
                    Console.Write("\nEnqueuing ready message on ready queue...\n----------------------------------------------------\n");
                    readyQueue.enQ(rcvMsg);
                }
                else if (rcvMsg.command == "allxmlsent")
                {

                    //enqueue all files in the blocking queue
                    Console.Write("\nEnqueuing all xml files on a file queue...\n----------------------------------------------------\n");
                    foreach (string file in rcvMsg.arguments) requestFiles.enQ(file);
                }

                else if (rcvMsg.command == "killBuilders")
                {
                    shutBuilders();
                }

                else if (rcvMsg.command == "startBuilders")
                {
                    count = Int32.Parse(rcvMsg.arguments[0]);
                    allProcess();
                }
            }

        }

        //function to forward xml request files to the builder processes
        public void sendRequest()
        {
            while (true)
            {
                string file = requestFiles.deQ();

                CommMessage rcvMsg = readyQueue.deQ();
                string sendTo = rcvMsg.from;
                Console.Write("\nDequeuing xml file:{0} and posting file to {1}...\n----------------------------------------------------\n",file, sendTo);

                //connect before postFile
                CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
                csndMsg.command = "connect";
                csndMsg.author = "Nishant Agrawal";
                csndMsg.to = sendTo;
                csndMsg.from = "http://localhost:" + "8080/MotherBuilder";
                cm.postMessage(csndMsg);

                cm.postFile(file, sendTo, rcvMsg.fromStorage);

                //send message after file sent
                csndMsg.command = "xmlFileSent";
                csndMsg.arguments.Add(file);
                cm.postMessage(csndMsg);
            }
        }

        //function to shut all builder processes
        private void shutBuilders()
        {
            if (readyQueue.size() == 0) return;
            Console.WriteLine("\nSending quit messages to the builder processes...\n----------------------------------------------------\n");

            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "quit";
            csndMsg.author = "Nishant Agrawal";            
            csndMsg.from = "http://localhost:" + "8080/MotherBuilder";

            int port = 8081;
            for (int i = 1; i <= count; ++i)
            {
                csndMsg.to = "http://localhost:" + port +"/Builder";
                cm.postMessage(csndMsg);
                port++;
            }
            readyQueue.clear();
        }

        //function to create a builder process on a port
        static bool createProcess(int i)
        {
                Process proc = new Process();
                string fileName = "..\\..\\..\\Builder\\bin\\debug\\Builder.exe";
                string absFileSpec = Path.GetFullPath(fileName);

                string commandline = i.ToString();
                try
                {
                    Process.Start(fileName, commandline);
                }
                catch (Exception ex)
                {
                    Console.Write("\n  {0}", ex.Message);
                    return false;
                }
                return true;
        }

        //starts all the builderprocesses by calling createProcess() count times
        private void allProcess()
        {
            if (readyQueue.size() != 0) return;
            Console.Write("\nAttempting to start {0} builder processes...\n----------------------------------------------------\n", count);
            int port = 8081;
            for (int i = 1; i <= count; ++i)
            {
                if (createProcess(port))
                {
                    Console.Write(" - succeeded");
                    port++;
                }
                else
                {
                    Console.Write(" - failed");
                }
            }
        }


        //get files from the mother builder storage and store in a list
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
        static void Main(string[] args)
        {
            Console.Title = "MotherBuilder";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;

            Console.Write("\n  Mother Builder Process");
            Console.Write("\n =====================\n");
            MotherBuilder mb = new MotherBuilder();

            Thread rcvActionThread = new Thread(mb.receiveAction);
            rcvActionThread.Start();

            Thread sendReqThread = new Thread(mb.sendRequest);
            sendReqThread.Start();
        }
    }
#if (TEST_MOTHERBUILDER)

  ///////////////////////////////////////////////////////////////////
  // TestMotherBuilder class

  class TestMotherBuilder
  {
    static void Main(string[] args)
    {
            Console.Write("\n  Demonstration of mother Builder");
            Console.Write("\n ============================");

            MotherBuilder mb = new MotherBuilder();

            Thread rcvActionThread = new Thread(mb.receiveAction);
            rcvActionThread.Start();

            Thread sendReqThread = new Thread(mb.sendRequest);
            sendReqThread.Start();

            Console.Write("\n\n");
    }
  }
#endif
}
