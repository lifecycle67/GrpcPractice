using Grpc.Core;
using Protos.Authenticate;
using static Protos.Authenticate.JwtAuthenticator;

namespace GrpcPracticeServer.Services
{
    /// <summary>
    /// 사용자 이름의 유효성을 확인하고 JWT토큰을 생성, 전달하는 서비스를 나타냅니다
    /// </summary>
    public class JwtAuthService : JwtAuthenticatorBase
    {
        public override Task<AuthResponse> Auth(AuthRequest request, ServerCallContext context)
        {
            var token = JwtHelper.GenerateJwtToken(request.Name);

            return Task.FromResult(new AuthResponse { Token = token });
        }
    }
}
