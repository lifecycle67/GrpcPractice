using Grpc.Core;
using Protos.Greet;

namespace GrpcPracticeServer.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            string ret = $"Hello {request.Name} {Guid.NewGuid()}";
            Console.WriteLine("Greeter service SayHello response : " + ret);
            Console.WriteLine("========= print request headers ==========");
            context.RequestHeaders.ToList().ForEach(e => Console.WriteLine(e.Key + " " + e.Value));
            
            context.ResponseTrailers.Add("response_trailer", "Check this out");

            return Task.FromResult(new HelloReply
            {
                Message = ret
            });
        }
    }
}