////////////////////////////////////////////////////////////////////////
// ComProService.cs - Defines sender, receiver and comm class, is used // 
//                     for posting files and messages via wcf         //
//                                                                    //
// Author: Nishant Agrawal, email-nagraw01@syr.edu                    //
// Application: CSE681 - Software Modelling Analysis, Project 4      //
// Environment: C# console                                            //
////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Defines sender, receiver and a comm class that can be used for communication using wcf
 *    
 * Public Interface
 * ----------------
 * Comm comm = new Comm("http://localhost", 8081);
 * comm.postMessage(CommMessage msg);
 * comm.getMessage();
 * comm.postFile(string filename, string to, string toStore);
 * comm.close();
 * Receiver rcvr = new Receiver();
 * rcvr.postMessage(CommMessage msg);
 * rcvr.getMessage();
 * rcvr.start();
 * rcvr.createCommHost(string address);
 * rcvr.openFileForWrite(string toStore, string fileName);
 * rcvr.writeFileBlock(byte[] block);
 * rcvr.closeFile();
 * Sender sndr = new Sender(baseAddress, port, rest);
 * sndr.postMessage(CommMessage msg);
 * sndr.getMessage();
 * sndr.createSendChannel(string address);
 * sndr.postFile(string fileName, string to, string toStore);
 * 
 * Required Files:
 * ---------------
 * IcomPro.cs
 * ComProService.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 24 Oct 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Threading;
using System.IO;

namespace Com_GUI
{
    ///////////////////////////////////////////////////////////////////
    // Receiver class - receives CommMessages and Files from Senders

    public class Receiver : IComPro
    {
        public static BlockingQueue<CommMessage> rcvQ { get; set; } = null;
        public string fileStorage { get; set; }
        ServiceHost commHost { get; set; } = null;
        FileStream fs { get; set; } = null;
        string lastError { get; set; } = "";

        /*----< constructor >------------------------------------------*/

        public Receiver()
        {
            if (rcvQ == null)
                rcvQ = new BlockingQueue<CommMessage>();
        }
        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
         * baseAddress is of the form: http://IPaddress or http://networkName
         */
        public void start(string baseAddress, int port, string restAddress)
        {
            string address = baseAddress + ":" + port.ToString() + restAddress;
            try {
                Console.WriteLine("Receiver Host started.......");
            }
            catch (Exception ex) { Console.WriteLine("err: {0}", ex.Message); }
            createCommHost(address);

        }
        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
         * address is of the form: http://IPaddress:8080/IPluggableComm
         */
        public void createCommHost(string address)
        {

            WSHttpBinding binding = new WSHttpBinding();
            Uri baseAddress = new Uri(address);
            commHost = new ServiceHost(typeof(Receiver), baseAddress);
            commHost.AddServiceEndpoint(typeof(IComPro), binding, baseAddress);
            try { commHost.Open(); } catch (Exception ex) { Console.WriteLine("err: {0}", ex.Message); }
        }
        /*----< enqueue a message for transmission to a Receiver >-----*/

        public void postMessage(CommMessage msg)
        {
            rcvQ.enQ(msg);
        }
        /*----< retrieve a message sent by a Sender instance >---------*/

        public CommMessage getMessage()
        {
            CommMessage msg = rcvQ.deQ();
            if (msg.type == CommMessage.MessageType.closeReceiver)
            {
                close();
            }
            return msg;
        }
        /*----< close ServiceHost >------------------------------------*/

        public void close()
        {
            Console.WriteLine("closing receiver - please wait");
            commHost.Close();
        }
        /*---< called by Sender's proxy to open file on Receiver >-----*/

        public bool openFileForWrite(string toStore, string fileName)
        {
            try
            {
                Console.WriteLine("\nFile:{0} posted in local storage", fileName);

                string writePath = Path.GetFullPath(toStore + "\\" + fileName);
                fs = File.OpenWrite(writePath);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< write a block received from Sender instance >----------*/

        public bool writeFileBlock(byte[] block)
        {
            try
            {
                fs.Write(block, 0, block.Length);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< close Receiver's uploaded file >-----------------------*/

        public void closeFile()
        {
            fs.Close();
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Sender class - sends messages and files to Receiver

    public class Sender
    {
        private IComPro channel { get; set; }
        private ChannelFactory<IComPro> factory { get; set; } = null;
        private int port { get; set; } = 0;
        private string fromAddress { get; set; } = "";
        private string lastError { get; set; } = "";
        private string lastUrl { get; set; } = "";
        public string fileStorage { get; set; }
        /*----< constructor >------------------------------------------*/

        public Sender(string baseAddress, int listenPort, string restAddress)
        {
            port = listenPort;
            fromAddress = baseAddress + listenPort.ToString() + restAddress;
        }
        /*----< creates proxy with interface of remote instance >------*/

        public void createSendChannel(string address)
        {
            EndpointAddress baseAddress = new EndpointAddress(address);
            WSHttpBinding binding = new WSHttpBinding();
            factory = new ChannelFactory<IComPro>(binding, address);
            channel = factory.CreateChannel();
        }


        /*----< closes Sender's proxy >--------------------------------*/

        public void close()
        {
            if (factory != null)
                factory.Close();
        }

        /*----< main thread enqueues message for sending >-------------*/

        public void postMessage(CommMessage msg)
        {
            if (msg.type == CommMessage.MessageType.closeSender)
            {
                Console.WriteLine("Sender send thread quitting");
                return;
            }
            if (msg.to == lastUrl)
            {
                channel.postMessage(msg);
            }
            else
            {
                close();
                createSendChannel(msg.to);

                lastUrl = msg.to;
                channel.postMessage(msg);
            }
        }
        /*----< uploads file to Receiver instance >--------------------*/

        public bool postFile(string fileName, string to, string toStore)
        {
            if (to != lastUrl)
            {
                close();
                createSendChannel(to);
                lastUrl = to;
            }
            long blockSize = 1024;
            FileStream fs = null;
            long bytesRemaining;
            string path = Path.GetFullPath(fileStorage + "\\" + fileName);
            Console.WriteLine("\nPosting file:{0} to {1}", fileName, to);
            try
            {
                
                fs = File.OpenRead(path);
                bytesRemaining = fs.Length;
                channel.openFileForWrite(toStore, fileName);
                while (true)
                {
                    long bytesToRead = Math.Min(blockSize, bytesRemaining);
                    byte[] blk = new byte[bytesToRead];
                    long numBytesRead = fs.Read(blk, 0, (int)bytesToRead);
                    bytesRemaining -= numBytesRead;

                    channel.writeFileBlock(blk);
                    if (bytesRemaining <= 0)
                        break;
                }
                channel.closeFile();
                fs.Close();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
            return true;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Comm class combines Receiver and Sender

    public class Comm
    {
        public Receiver rcvr { get; set; } = null;
        public Sender sndr { get; set; } = null;
        string baseAddress { get; set; } = "http://localhost";

        /*----< constructor >------------------------------------------*/
        /*
         * - starts listener listening on specified endpoint
         */
        public Comm(int port, string rest, string fileStorage)
        {
            rcvr = new Receiver();
            rcvr.fileStorage = fileStorage;
            rcvr.start(baseAddress, port, rest);
            sndr = new Sender(baseAddress, port, rest);
            sndr.fileStorage = fileStorage;

        }
        /*----< post message to remote Comm >--------------------------*/

        public void postMessage(CommMessage msg)
        {
            sndr.postMessage(msg);
        }
        /*----< retrieve message from remote Comm >--------------------*/

        public CommMessage getMessage()
        {
            return rcvr.getMessage();
        }
        /*----< called by remote Comm to upload file >-----------------*/

        public bool postFile(string filename, string to, string toStore)
        {
            return sndr.postFile(filename, to, toStore);
        }

        //function to close comm sender and receiver
        public void close()
        {
            Console.Write("\n  Comm closing");
            rcvr.close();
            sndr.close();
        }
        static void Main(string[] args)
        {
        }
    }

#if (TEST_COMPROSERVICE)

  ///////////////////////////////////////////////////////////////////
  // TestComProService class

  class TestComProService
  {
    static void Main(string[] args)
    {
            Console.Write("\n  Demonstration of wcf communication service");
            Console.Write("\n ============================");

            Comm comm = new Comm("http://localhost", 8081);
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);

            csndMsg.command = "show";
            csndMsg.author = "Jim Fawcett";
            csndMsg.to = "http://localhost:8081/IPluggableComm";
            csndMsg.from = "http://localhost:8081/IPluggableComm";

            comm.postMessage(csndMsg);
            CommMessage crcvMsg = comm.getMessage();
            foreach (string fileName in rcvmsg.arguments)
            {
                cm.postFile(fileName, rcvmsg.from, rcvmsg.fromStorage);
            }
            Console.Write("\n\n");
    }
  }
#endif

}

