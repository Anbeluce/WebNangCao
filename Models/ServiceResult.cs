namespace WebNangCao.Models
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }

        public static ServiceResult SuccessResult(string? message = null)
        {
            return new ServiceResult { Success = true, Message = message };
        }

        public static ServiceResult FailureResult(string errorMessage, string? message = null)
        {
            return new ServiceResult { Success = false, ErrorMessage = errorMessage, Message = message };
        }
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> SuccessResult(T data, string? message = null)
        {
            return new ServiceResult<T> { Success = true, Data = data, Message = message };
        }

        public new static ServiceResult<T> FailureResult(string errorMessage, string? message = null)
        {
            return new ServiceResult<T> { Success = false, ErrorMessage = errorMessage, Message = message };
        }
    }
}
