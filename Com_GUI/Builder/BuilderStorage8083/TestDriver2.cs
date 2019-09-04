using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBuild
{
  interface ITest
  {
    bool test();
  }
  public class TestDriver2 : ITest
  {
    public bool test()
    {
      TestedOne one = new TestedOne();
      one.sayOne();
      TestedTwo two = new TestedTwo();
      two.sayTwo();
      //TestedTwo two = new TestedTwo();
      two.sayOne();
      Console.WriteLine(" =========");
      return true;  // just pretending to test something
    }
    static void Main(string[] args)
    {
      Console.Write("\n  TestDriver2 running:");
      TestDriver2 td = new TestDriver2();
      td.test();
      Console.Write("\n\n");
    }
  }
}
