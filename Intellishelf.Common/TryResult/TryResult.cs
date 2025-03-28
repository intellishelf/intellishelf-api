using System.Diagnostics.CodeAnalysis;

namespace Intellishelf.Common.TryResult;

public record TryResult
{
    protected TryResult() => IsSuccess = true;
    protected TryResult(Error error) => (IsSuccess, Error) = (false, error);

    [MemberNotNullWhen(false, nameof(Error))]
    public virtual bool IsSuccess { get; protected init; }

    public Error? Error { get; }

    public static implicit operator TryResult(Error error) => new(error);
    public static TryResult Success() => new();

    public static TryResult<TResult> Success<TResult>(TResult value)
    {
        return new TryResult<TResult>(value);
    }
}

public record TryResult<TResult> : TryResult
{
    protected internal  TryResult(TResult item) => (Value, IsSuccess) = (item, true);
    private TryResult(Error error) : base(error) { }

    [MemberNotNullWhen(true, nameof(Value))]
    public override bool IsSuccess { get; protected init; }

    public TResult? Value { get; }

    public static implicit operator TryResult<TResult>(TResult value) => new(value);
    public static implicit operator TryResult<TResult>(Error error) => new(error);
}