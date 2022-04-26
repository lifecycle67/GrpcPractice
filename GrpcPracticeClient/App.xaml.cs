using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using static Protos.Greet.Greeter;
using static Protos.Authenticate.JwtAuthenticator;

namespace GrpcPracticeClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost _host;
        public IServiceProvider Services => _host.Services;

        public App()
        {
            ///클라이언트 팩토리 구성을 통해 JWT토큰 인증 인터셉터를 Greeter 클라이언트에 추가합니다
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<ITokenProvider, TokenProvider>();
                    services.AddScoped<AuthInterceptor>();
                    services.AddGrpcClient<GreeterClient>(o =>
                    {
                        o.Address = new Uri("https://localhost:5001");
                    }).AddInterceptor<AuthInterceptor>(InterceptorScope.Client);
                    services.AddGrpcClient<JwtAuthenticatorClient>(o =>
                    {
                        o.Address = new Uri("https://localhost:5001");
                    });
                }).Build();
            _host.Start();
        }
    }
}
