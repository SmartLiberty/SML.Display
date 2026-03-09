namespace SML.ExampleGrpc.Tests.Unit.Events;

using Core.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class QueueSuffixTest
{
	[TestInitialize]
    public void Test()
    {
        // Arrange
        var suffix = new QueueSuffix("suffix");
        var queue = "queue";

        // Act
        var result = suffix.AddSuffix(queue);

        // Assert
        Assert.AreEqual("queue-suffix", result);
    }
}
