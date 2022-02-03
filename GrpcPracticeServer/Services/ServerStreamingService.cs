using Grpc.Core;
using Protos.Streaming;
using static Protos.Streaming.StreamingService;

namespace GrpcPracticeServer.Services
{
    public class ServerStreamingService : StreamingServiceBase
    {
        public ServerStreamingService() { }

        public override async Task ServerStreaming(
            ServerStreamRequest request, 
            IServerStreamWriter<ServerStreamResponse> responseStream, 
            ServerCallContext context)
        {
            for (int i = 0; i < 10; i++)
            {
                string message = $"{request.Name} - response time : {DateTime.Now}";
                Console.WriteLine(message);

                await responseStream.WriteAsync(
                    new ServerStreamResponse
                    {
                        Message = message
                    });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
