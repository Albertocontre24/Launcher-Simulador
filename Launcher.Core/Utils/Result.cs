namespace Launcher.Core
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        public T? Value { get; }

        private Result(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        // ✅ Método para resultados correctos
        public static Result<T> Ok(T value)
        {
            return new Result<T>(true, value, null);
        }

        // ✅ Método para resultados con error
        public static Result<T> Fail(string error)
        {
            return new Result<T>(false, default, error);
        }
    }
}
