using Grpc.Core;
using Protos.Streaming;
using static Protos.Streaming.StreamingService;

namespace GrpcPracticeServer.Services
{
    public class StreamingService : StreamingServiceBase
    {
        public StreamingService() { }

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

        public override async Task<ClientStreamResponse> ClientStreaming(
           IAsyncStreamReader<ClientStreamRequest> requestStream,
           ServerCallContext context)
        {
            //var caller = requestStream.Current.Caller;
            while (await requestStream.MoveNext())
            {
                ClientStreamRequest request = requestStream.Current;
                Console.WriteLine(request.Caller + " " + request.Message);
            }
            return new ClientStreamResponse { Message = $"Done" };
        }

        public override async Task BiStreaming(IAsyncStreamReader<BiStreamingRequest> requestStream, IServerStreamWriter<BiStreamingResponse> responseStream, ServerCallContext context)
        {
            var readTask = Task.Run(async () =>
            {
                await foreach (var message in requestStream.ReadAllAsync())
                {
                    Console.WriteLine("Received : " + message.Message);
                }
            });

            while (!readTask.IsCompleted)
            {
                Console.WriteLine("send");
                await responseStream.WriteAsync(new BiStreamingResponse { Message = "BiStreaming response" });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
