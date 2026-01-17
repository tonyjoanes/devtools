namespace PipeTriage.Core;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// </summary>
public abstract record Result<T>
{
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string Error) : Result<T>;

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;

    public static Result<T> Ok(T value) => new Success(value);
    public static Result<T> Fail(string error) => new Failure(error);

    /// <summary>
    /// Maps a successful result to a new value using the provided function.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        this switch
        {
            Success(var value) => Result<TOut>.Ok(mapper(value)),
            Failure(var error) => Result<TOut>.Fail(error),
            _ => throw new InvalidOperationException("Unknown result type")
        };

    /// <summary>
    /// Binds a successful result to a new result using the provided function.
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder) =>
        this switch
        {
            Success(var value) => binder(value),
            Failure(var error) => Result<TOut>.Fail(error),
            _ => throw new InvalidOperationException("Unknown result type")
        };

    /// <summary>
    /// Asynchronously maps a successful result to a new value.
    /// </summary>
    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> mapper) =>
        this switch
        {
            Success(var value) => Result<TOut>.Ok(await mapper(value)),
            Failure(var error) => Result<TOut>.Fail(error),
            _ => throw new InvalidOperationException("Unknown result type")
        };

    /// <summary>
    /// Asynchronously binds a successful result to a new result.
    /// </summary>
    public async Task<Result<TOut>> BindAsync<TOut>(Func<T, Task<Result<TOut>>> binder) =>
        this switch
        {
            Success(var value) => await binder(value),
            Failure(var error) => Result<TOut>.Fail(error),
            _ => throw new InvalidOperationException("Unknown result type")
        };

    /// <summary>
    /// Matches on the result and executes the appropriate function.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure) =>
        this switch
        {
            Success(var value) => onSuccess(value),
            Failure(var error) => onFailure(error),
            _ => throw new InvalidOperationException("Unknown result type")
        };
}

/// <summary>
/// Extension methods for working with Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Task of Result to enable async binding.
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<TOut>>> binder)
    {
        var result = await resultTask;
        return await result.BindAsync(binder);
    }

    /// <summary>
    /// Converts a Task of Result to enable async mapping.
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, Task<TOut>> mapper)
    {
        var result = await resultTask;
        return await result.MapAsync(mapper);
    }

    /// <summary>
    /// Safely executes a function and wraps the result or exception in a Result.
    /// </summary>
    public static Result<T> Try<T>(Func<T> func)
    {
        try
        {
            return Result<T>.Ok(func());
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Safely executes an async function and wraps the result or exception in a Result.
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func)
    {
        try
        {
            var result = await func();
            return Result<T>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(ex.Message);
        }
    }
}
