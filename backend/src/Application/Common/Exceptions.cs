namespace TelegramMarketplace.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) : this()
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage) : this()
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}

public class PaymentException : Exception
{
    public string? ErrorCode { get; }

    public PaymentException(string message, string? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class BusinessRuleException : Exception
{
    public string? ErrorCode { get; }

    public BusinessRuleException(string message, string? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }
}
