namespace SML.Display.Core.Exceptions;

using System.Runtime.Serialization;

[Serializable]
public class NotFoundException : Exception
{
    public string? NotFoundElement { get; init; }

    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }

    protected NotFoundException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
