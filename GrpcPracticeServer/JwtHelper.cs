using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GrpcPracticeServer
{
    public class JwtHelper
    {
        /// <summary>
        /// JWT 토큰 생성
        /// </summary>
        public static string GenerateJwtToken(string name)
        {
            if (name != "test") //단순 예제용으로 사용자 비교를 하드코딩으로 구성함
            {
                throw new SecurityTokenException("not authenticated user");
            }

            var claims = new[] { new Claim(ClaimTypes.Name, name) };
            var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("ExampleServer", "ExampleClients", claims, expires: DateTime.Now.AddSeconds(60), signingCredentials: credentials);
            return JwtTokenHandler.WriteToken(token);
        }

        public static readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();
        public static readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());
    }
}
