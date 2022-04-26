using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Protos.Authenticate.JwtAuthenticator;

namespace GrpcPracticeClient
{
    /// <summary>
    /// JWT 토큰을 구성하는 gRPC 인터셉터
    /// </summary>
    public class AuthInterceptor : Interceptor
    {
        ITokenProvider _tokenProvider;
        public AuthInterceptor(ITokenProvider tokenProvider) 
        {
            _tokenProvider = tokenProvider;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var headers = context.Options.Headers;

            if (headers == null)
            {
                headers = new Metadata();
                var options = context.Options.WithHeaders(headers);
                context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
            }

            headers.Add("Authorization", $"Bearer {_tokenProvider.GetToken()}");

            return continuation(request, context);
        }
    }

    public interface ITokenProvider
    {
        string GetToken();
    }

    /// <summary>
    /// grpc 인증 서비스를 통해 JWT 토큰을 제공합니다
    /// </summary>
    public class TokenProvider : ITokenProvider
    {
        public string GetToken()
        {
            var app = App.Current as App;
            if (app == null) return String.Empty;

            var client = app.Services.GetRequiredService<JwtAuthenticatorClient>();
            var response = client.Auth(new Protos.Authenticate.AuthRequest { Name = "test" });
            return response.Token;
        }
    }
}
