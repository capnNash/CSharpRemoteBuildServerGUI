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
  public class TestDriver3 : ITest
  {
    public TestedThree th { get; set; }
    public bool test()
    {
      
      TestedThree three = new TestedThree();
      three.sayThree();
      return true;  // just pretending to test something
    }
    static void Main(string[] args)
    {
      Console.Write("\n  TestDriver3 running:");
      TestDriver3 td = new TestDriver3();
      string s1 = td.th.s;
      td.test();
      Console.Write("\n\n");
    }
  }
}
