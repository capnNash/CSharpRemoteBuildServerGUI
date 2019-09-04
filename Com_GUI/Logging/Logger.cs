////////////////////////////////////////////////////////////////////////
// Logger.cs - Helper class to create log files for builders and      // 
//             test harness                                           //
//                                                                    //
// Author: Nishant Agrawal, email-nagraw01@syr.edu                    //
// Application: CSE681 - Software Modelling Analysis, Project 4       //
// Environment: C# console                                            //
////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Creates a log file at the path provided
 * Adds info, output, errors and exit codes
 * 
 * Public Interface
 * ----------------
 * Logger log = new Logger(logPath);
 * log.Info("Build result for testcase: "+ entirePath);
 * log.Output(output);
 * log.Error(error);
 * log.exitCode(exitCode);
 * log.close();
 * 
 * Required Files:
 * ---------------
 * Logger.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 3 Dec 2017
 * - first release
 * 
 */

using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Com_GUI
{
    public class Logger
    {
        private string DatetimeFormat { get; set; }
        private string Filename { get; set; }
        FileStream fs { get; set; }

        // Logger constructor to create a log file at the given path
        public Logger(string name)
        {
            DatetimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
            Filename = name + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".log";
            fs = File.Create(Filename);
        }

        //function to close the logger file and the filestream
        public void close()
        {
            fs.Close();
        }

        //function to log the exit code into the file
        public void exitCode(string text)
        {
            WriteFormattedLog(LogLevel.EXITCODE, text);
        }

        //function to log the error into the file
        public void Error(string text)
        {
            WriteFormattedLog(LogLevel.ERROR, text);
        }


        //function to log the info into the file
        public void Info(string text)
        {
            WriteFormattedLog(LogLevel.INFO, text);
        }


        //function to log the output into the file
        public void Output(string text)
        {
            WriteFormattedLog(LogLevel.OUTPUT, text);
        }

        //function to add pretext before each type of log text
        private void WriteFormattedLog(LogLevel level, string text)
        {
            string pretext;
            switch (level)
            {
                case LogLevel.INFO: pretext = "\n"+DateTime.Now.ToString(DatetimeFormat) + " [INFO]    "; break;
                case LogLevel.EXITCODE: pretext = "\n" + DateTime.Now.ToString(DatetimeFormat) + " [EXITCODE]   "; break;
                case LogLevel.OUTPUT: pretext = "\n" + DateTime.Now.ToString(DatetimeFormat) + " [OUTPUT] "; break;
                case LogLevel.ERROR: pretext = "\n" + DateTime.Now.ToString(DatetimeFormat) + " [ERROR]   "; break;
                default: pretext = ""; break;
            }

            WriteLine(pretext + text);
        }

        //function to write into the file by converting string text into bytes
        private void WriteLine(string text, bool append = true)
        {
            try
            {
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(text);
                    fs.Write(info, 0, info.Length);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("error message form the logger:{0}", ex.Message);
            }
        }

        //log level flags to classify the type of log text
        [Flags]
        private enum LogLevel
        {
            INFO,
            EXITCODE,
            OUTPUT,
            ERROR
        }

        static void Main(string[] args)
        {
        }
    }

#if (TEST_LOGGER)

  ///////////////////////////////////////////////////////////////////
  // TestLogger class

  class TestLogger
  {
    static void Main(string[] args)
    {
        string logPath = testPath + "\\testLog";
        Logger log = new Logger(logPath);
        log.Info("Build result for testcase: "+ entirePath);
        log.Output(output);
        log.Error(error);
        log.exitCode(exitCode);
        log.close();

        Console.Write("\n\n");
    }
  }
#endif
}

