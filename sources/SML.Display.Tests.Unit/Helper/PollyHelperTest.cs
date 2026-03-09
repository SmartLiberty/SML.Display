namespace SML.Display.Tests.Unit.Helper;

using Core.Data.Settings;
using Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using System;


[TestClass]
public class PollyHelperTest
{
    private int _errors;

    private BackoffSettings _backoffSettings = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _backoffSettings = new()
        {
            MaxAttempts = 4,
            InitialBackoff = TimeSpan.FromMicroseconds(1),
            MaxBackoff = TimeSpan.FromMicroseconds(3),
            BackoffMultiplier = 2
        };
    }

    [TestMethod]
    public async Task TestSuccessAsync()
    {
        // Arrange
        var policy = PollyHelper.CreateRetryPolicy(Mock.Of<ILogger>(), _backoffSettings);
        var context = new Context();

        // Act
        await policy.ExecuteAsync(Execute, context, CancellationToken.None);

        // Assert
        var retries = PollyHelper.GetRetries(context);
        Assert.IsNull(retries);
    }

    [TestMethod]
    public async Task TestSuccessAfter1ErrorAsync()
    {
        // Arrange
        _errors = 1;
        var policy = PollyHelper.CreateRetryPolicy(Mock.Of<ILogger>(), _backoffSettings);
        var context = new Context();

        // Act
        await policy.ExecuteAsync(Execute, context, CancellationToken.None);

        // Assert
        var retries = PollyHelper.GetRetries(context);
        Assert.IsNotNull(retries);
        Assert.AreEqual(1, retries);
    }

    [TestMethod]
    public async Task TestSuccessAfter2ErrorsAsync()
    {
        // Arrange
        _errors = 2;
        var policy = PollyHelper.CreateRetryPolicy(Mock.Of<ILogger>(), _backoffSettings);
        var context = new Context();

        // Act
        await policy.ExecuteAsync(Execute, context, CancellationToken.None);

        // Assert
        var retries = PollyHelper.GetRetries(context);
        Assert.IsNotNull(retries);
        Assert.AreEqual(2, retries);
    }

    [TestMethod]
    public async Task TestSuccessAfter3ErrorsAsync()
    {
        // Arrange
        _errors = 3;
        var policy = PollyHelper.CreateRetryPolicy(Mock.Of<ILogger>(), _backoffSettings);
        var context = new Context();

        // Act
        await policy.ExecuteAsync(Execute, context, CancellationToken.None);

        // Assert
        var retries = PollyHelper.GetRetries(context);
        Assert.IsNotNull(retries);
        Assert.AreEqual(3, retries);
    }

    [TestMethod]
    public async Task TestFailureAsync()
    {
        // Arrange
        _errors = 4;
        var policy = PollyHelper.CreateRetryPolicy(Mock.Of<ILogger>(), _backoffSettings);
        var context = new Context();
        try
        {
            // Act
            await policy.ExecuteAsync(Execute, context, CancellationToken.None);

            // Assert
            Assert.Fail("Expected exception missing");
        }
        catch (Exception e)
        {
            Assert.AreEqual("Test", e.Message);
        }
        var retries = PollyHelper.GetRetries(context);
        Assert.IsNotNull(retries);
        Assert.AreEqual(3, retries);
    }

    [TestMethod]
    public async Task TestOpenCircuitAsync()
    {
        // Arrange
        _errors = 8;
        var policy = PollyHelper.CreateRetryPolicy(Mock.Of<ILogger>(), _backoffSettings, new()
        {
            FailuresBeforeBreaking = 2,
            BreakDuration = TimeSpan.FromSeconds(10)
        });
        var context = new Context();
        var exceptions = new List<Exception>();
        for (var i = 0; i < 2; i++)
        {
            try
            {
                await policy.ExecuteAsync(Execute, context, CancellationToken.None);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }
        context = new Context();

        try
        {
            // Act
            await policy.ExecuteAsync(Execute, context, CancellationToken.None);

            // Assert
            Assert.Fail("Expected exception missing");
        }
        catch (Exception e)
        {
            Assert.AreEqual("The circuit is now open and is not allowing calls.", e.Message);
        }
        foreach (var e in exceptions)
        {
            Assert.AreEqual("Test", e.Message);
        }
        var retries = PollyHelper.GetRetries(context);
        Assert.IsNull(retries);
    }

    [TestMethod]
    public async Task TestSuccessAfterOpenCircuitAsync()
    {
        // Arrange
        _errors = 8;
        var policy = PollyHelper.CreateRetryPolicy(Mock.Of<ILogger>(), _backoffSettings, new()
        {
            FailuresBeforeBreaking = 2,
            BreakDuration = TimeSpan.FromMicroseconds(1)
        });
        var context = new Context();
        //var exceptions = new List<Exception>();
        for (var i = 0; i < 2; i++)
        {
            try
            {
                await policy.ExecuteAsync(Execute, context, CancellationToken.None);
            }
            catch (Exception)
            {
                //exceptions.Add(e);
            }
        }
        context = new Context();

        // Act
        await policy.ExecuteAsync(Execute, context, CancellationToken.None);

        // Assert
        var retries = PollyHelper.GetRetries(context);
        Assert.IsNull(retries);
    }

    [TestMethod]
    public async Task TestSuccessAfter1ErrorAfterOpenCircuitAsync()
    {
        // Arrange
        _errors = 9;
        var policy = PollyHelper.CreateRetryPolicy(Mock.Of<ILogger>(), _backoffSettings, new()
        {
            FailuresBeforeBreaking = 2,
            BreakDuration = TimeSpan.FromMicroseconds(1)
        });
        var context = new Context();
        //var exceptions = new List<Exception>();
        for (var i = 0; i < 2; i++)
        {
            try
            {
                await policy.ExecuteAsync(Execute, context, CancellationToken.None);
            }
            catch (Exception)
            {
                //exceptions.Add(e);
            }
        }
        context = new Context();

        // Act
        await policy.ExecuteAsync(Execute, context, CancellationToken.None);

        // Assert
        var retries = PollyHelper.GetRetries(context);
        Assert.IsNotNull(retries);
        Assert.AreEqual(1, retries);
    }

    private async Task Execute(Context context, CancellationToken token)
    {
        if (_errors-- > 0)
        {
            throw new Exception("Test");
        }
        await Task.CompletedTask;
    }
}
