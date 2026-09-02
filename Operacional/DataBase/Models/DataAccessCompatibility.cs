namespace Operacional.DataBase.Models;

[AttributeUsage(AttributeTargets.Class)]
public sealed class KeylessAttribute : Attribute;

public sealed class DbUpdateException : Exception
{
    public DbUpdateException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
