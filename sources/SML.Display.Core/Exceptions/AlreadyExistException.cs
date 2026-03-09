namespace SML.Display.Core.Exceptions;

using System.Runtime.Serialization;

[Serializable]
public class AlreadyExistException : Exception
{
    public string? AlreadyExistsElement { get; init; }

    public AlreadyExistException()
    {
    }

    public AlreadyExistException(string message)
        : base(message)
    {
    }

    public AlreadyExistException(string message, Exception inner)
        : base(message, inner)
    {
    }

    protected AlreadyExistException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }
}
