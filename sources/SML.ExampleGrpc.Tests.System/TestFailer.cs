namespace SML.ExampleGrpc.Tests.System;

using Core.Interfaces.Handlers;

internal class TestFailer : IProcessKiller
{
	public void Kill(string reason)
		=> throw new Exception("RabbitMq is unreachable, please check the RabbitMQ service");
}
