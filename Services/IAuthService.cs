using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Auth;

namespace WebNangCao.Services
{
    public interface IAuthService
    {
        Task<ServiceResult> SendRegisterOtpAsync(RegisterVM model, ISession session);
        Task<ServiceResult> VerifyOtpAndCreateUserAsync(VerifyOtpVM model, ISession session);
        Task<ServiceResult<string>> LoginAsync(LoginVM model);
    }
}
