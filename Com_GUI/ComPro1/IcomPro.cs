////////////////////////////////////////////////////////////////////////
// IComPro.cs - Contails IcomPro interface that is used by receiver   // 
//              class and for wcf channel to post message and files   //
//                                                                    //
// Author: Nishant Agrawal, email-nagraw01@syr.edu                    //
// Application: CSE681 - Software Modelling Analysis, Project 4       //
// Environment: C# console                                            //
////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Defines IcomPro interface and Commmessage class
 *    
 * Public Interface
 * ----------------
 * CommMessage cmsg = new CommMessage();
 * cmsg.show();
 * 
 * Required Files:
 * ---------------
 * IcomPro.cs
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
using System.Runtime.Serialization;
using System.Threading;

namespace Com_GUI
{
    using Command = String;             // Command is key for message dispatching, e.g., Dictionary<Command, Func<bool>>
    using EndPoint = String;            // string is (ip address or machine name):(port number)
    using Argument = String;
    using ErrorMessage = String;

    [ServiceContract(Namespace = "Com_GUI")]
    public interface IComPro
    {
        /*----< support for message passing >--------------------------*/

        [OperationContract(IsOneWay = true)]
        void postMessage(CommMessage msg);

        // private to receiver so not an OperationContract
        CommMessage getMessage();

        /*----< support for sending file in blocks >-------------------*/

        [OperationContract]
        bool openFileForWrite(string toStore, string name);

        [OperationContract]
        bool writeFileBlock(byte[] block);

        [OperationContract(IsOneWay = true)]
        void closeFile();
    }

    [DataContract]
    public class CommMessage
    {
        public enum MessageType
        {
            [EnumMember]
            connect,           // initial message sent on successfully connecting
            [EnumMember]
            request,           // request for action from receiver
            [EnumMember]
            reply,             // response to a request
            [EnumMember]
            closeSender,       // close down client
            [EnumMember]
            closeReceiver      // close down server for graceful termination
        }

        /*----< constructor requires message type >--------------------*/

        public CommMessage(MessageType mt)
        {
            type = mt;
        }
        /*----< data members - all serializable public properties >----*/

        [DataMember]
        public MessageType type { get; set; } = MessageType.connect;

        [DataMember]
        public string to { get; set; }

        [DataMember]
        public string from { get; set; }
        [DataMember]
        public string fromStorage { get; set; }

        [DataMember]
        public string author { get; set; }

        [DataMember]
        public Command command { get; set; }

        [DataMember]
        public List<Argument> arguments { get; set; } = new List<Argument>();

        [DataMember]
        public int threadId { get; set; } = Thread.CurrentThread.ManagedThreadId;

        [DataMember]
        public ErrorMessage errorMsg { get; set; } = "no error";

        public void show()
        {
            Console.Write("\n  Received CommMessage:");
            Console.Write("\n    MessageType : {0}", type.ToString());
            Console.Write("\n    to          : {0}", to);
            Console.Write("\n    from        : {0}", from);
            Console.Write("\n    author      : {0}", author);
            Console.Write("\n    command     : {0}", command);
            Console.Write("\n    arguments   :");
            if (arguments.Count > 0)
                Console.Write("\n");
            foreach (string arg in arguments)
                Console.Write("{0} ", arg);
            Console.Write("\n    ThreadId    : {0}", threadId);
            Console.Write("\n    errorMsg    : {0}\n", errorMsg);
        }
    }
#if (TEST_COMMMESSAGE)

  ///////////////////////////////////////////////////////////////////
  // TestCommMessage class

  class TestCommMessage
  {
    static void Main(string[] args)
    {
            Console.Write("\n  Demonstration of message class");
            Console.Write("\n ============================");

            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);

            csndMsg.command = "show";
            csndMsg.author = "Jim Fawcett";
            csndMsg.to = "http://localhost:8081/IPluggableComm";
            csndMsg.from = "http://localhost:8081/IPluggableComm";
            csndMsg.show();

            Console.Write("\n\n");
    }
  }
#endif
}
