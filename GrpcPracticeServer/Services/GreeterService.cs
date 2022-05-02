using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Protos.Greet;
using System.Diagnostics;

namespace GrpcPracticeServer.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        [Authorize] //호출자에 대한 인증 요구
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            //Console.WriteLine($"receive greet : {DateTime.UtcNow.ToString("m:s:fff")}{Environment.NewLine}");
            //throw new RpcException(new Status(StatusCode.Unavailable, "haha")); //클라이언트가 재시도 하도록 예외 전송

            string ret = $"Hello {request.Name} {Guid.NewGuid()}";
            Console.WriteLine("Greeter service SayHello response : " + ret);

            ///요청 헤더 수신
            Console.WriteLine("========= print request headers ==========");
            context.RequestHeaders.ToList().ForEach(e => Console.WriteLine(e.Key + " " + e.Value));

            ///응답 트레일러를 추가합니다
            context.ResponseTrailers.Add("response_trailer", "Check this out");

            return Task.FromResult(new HelloReply
            {
                Message = ret
            });
        }
    }
}