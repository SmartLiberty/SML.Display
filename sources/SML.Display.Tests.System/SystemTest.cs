namespace SML.Display.Tests.System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SystemTest
{
    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
    }

    [TestInitialize]
    public void TestInitialize()
    {
    }

    [TestCleanup]
    public void TestCleanup()
    {
    }

    [TestMethod]
    public void TestMethod()
    {
    }

    [TestMethod, TestCategory("Explicit"), Description("Need something to run the test.")]
    public void TestExplicitMethod()
    {
    }
}
