namespace Datagrove.Pepinillo;

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Datagrove.Pepinillo;
// these are tests for the gherkin compiler. It is more conventional in C# to put these in their own project, so these probably go away at some point.

[TestClass]
[TestCategory("unit")]
public class GherkinTests
{
    [TestMethod]
    public void testBool()
    {
        var x = new Regex("((?i)recurring|(?i)nonrecurring)");
        var g = x.Match("Nonrecurring");
        var o = g.Groups.Values.ToArray()[1].Value;
        // this throws.
        //var o2 =  (bool)Convert.ChangeType(o,typeof(bool));
    }
    [TestMethod]
    public void testregex()
    {
        var r = new Regex("I am on the (.*) site as designated user (.*)");
        Assert.IsTrue(r.IsMatch("I am on the MBRR site as designated user Director of Marketing"));
    }

    [TestMethod]
    public void TestMethod1()
    {
        // all the microsoft software can't agree on working directory
        // finding directories is stupid hard.

        // Each feature file can have multiple scenarios, so we can make each feature a class and each scenario a method

        // Console.Write(Compiler.compile(new string[]{rootDir()+ "/lib/Products.feature"}));
    }
    string rootDir()
    {
        var d = Directory.GetCurrentDirectory(); //+"/../../../lib/Products.feature";
        var lst = d.Split("/");
        lst = lst.Take(lst.Count() - 6).ToArray();
        return d = String.Join("/", lst) + "/asi1/test";
    }
    [TestMethod]
    public void testAll()
    {
        var rd = rootDir();

        var asm = Assembly.LoadFrom(rd + "/bin/Debug/net7.0/test1.dll");

        var v10 = new BuildGherkin()
        {
            assembly = asm,
            tag = "v10",
            outputFile = rd + "/gherkin/v10.cs",
            featureFolder = new string[]{
                rd+"/Selenium/V10/Asi.Selenium.V10/Features",
                rd+"/Selenium/V10/Asi.Selenium.Shared.V10/Features",
            },

            stepSpace = new string[]{
                "Asi.Selenium.Shared",
                "Asi.Selenium.Web.V10",
                "Asi.Selenium.V10",
            }
        };
        v10.build();

        var v100 = new BuildGherkin()
        {
            assembly = asm,
            tag = "v100",
            outputFile = rd + "/gherkin/v100.cs",
            featureFolder = new string[]{
                rd+"/Selenium/V100/Asi.Selenium.V100/Features",
                rd+"/Selenium/V10/Asi.Selenium.Shared.V10/Features",
            },

            // we needed to prioritize the namespaces to take V100 where it exists
            stepSpace = new string[]{
                "Asi.Selenium.Shared",
                "Asi.Selenium.Web.V100",
                "Asi.Selenium.V100",
                // this causes ambiguity warnings, but there are shared steps in V10? 
                 "Asi.Selenium.V10"
            }
        };
        v100.build();

    }
}




/*
new BuildGherkin(){
    tag = "old10",
    outputFile = rootDir() + "/Selenium/gherkin/old10.cs",
    featureFolder = rootDir()+"/Selenium/Old/ASI_Selenium10/Features",

    stepSpace = new string[]{
        "Asi.Selenium.v100", 
        "Asi.Selenium.v10" 
    }
},
new BuildGherkin(){
    tag = "oldx",
    outputFile = rootDir() + "/Selenium/gherkin/oldx.cs",
    featureFolder = rootDir()+"/Selenium/Old/ASI_Selenium/Features",

    stepSpace = new string[]{
        "Asi.Selenium.v100", 
        "Asi.Selenium.v10"           
    }
}, */