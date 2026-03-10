namespace SharedKernel;

public class Result
{
    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("Success result cannot have an error");
        if(!isSuccess && error is null)
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, string? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Cannot access value of a failed result");
            }
            return _value!;
        }
    }

    public static Result<T> Success(T value) => new(value, true, null);
    public new static Result<T> Failure(string error) => new(default, false, error);
}

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; } = [];
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    private PaginatedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PaginatedResult<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        => new(items, pageNumber, pageSize, totalCount);

    public static PaginatedResult<T> CreateEmpty(int pageSize = 10)
        => new([], 1, pageSize, 0);
}

public class PaginatedResultWithStatus<T> : Result
{
    private readonly PaginatedResult<T>? _data;

    private PaginatedResultWithStatus(PaginatedResult<T>? data, bool isSuccess, string? error)
        : base(isSuccess, error)
    {
        _data = data;
    }

    public PaginatedResult<T> Data
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("Cannot access data of a failed result");

            return _data!;
        }
    }

    public static PaginatedResultWithStatus<T> Success(PaginatedResult<T> data)
        => new(data, true, null);

    public static PaginatedResultWithStatus<T> Success(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        => new(PaginatedResult<T>.Create(items, pageNumber, pageSize, totalCount), true, null);

    public new static PaginatedResultWithStatus<T> Failure(string error)
        => new(null, false, error);
}