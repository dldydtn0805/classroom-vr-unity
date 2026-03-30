using System;

namespace AgoraVR.Common.IDs
{

public readonly struct SessionId : IEquatable<SessionId>
{
    public SessionId(string value)
    {
        Value = value ?? string.Empty;
    }

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static SessionId CreateNew()
    {
        return new SessionId(Guid.NewGuid().ToString("N"));
    }

    public bool Equals(SessionId other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is SessionId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }
}
}
