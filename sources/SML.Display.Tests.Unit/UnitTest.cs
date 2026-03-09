namespace SML.Display.Tests.Unit;

using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class UnitTest
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

    public static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
        => new(Task.FromResult(response), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => [], () => { });
}
