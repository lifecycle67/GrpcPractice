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
                if (context.CancellationToken.IsCancellationRequested) //요청이 취소될 경우 동작을 중지합니다
                {
                    Console.WriteLine("request cancelled");
                    break;
                }

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
            while (await requestStream.MoveNext())
            {
                ClientStreamRequest request = requestStream.Current;
                Console.WriteLine(request.Caller + " " + request.Message);
            }
            return new ClientStreamResponse { Message = $"Done" };
        }

        public override async Task BiStreaming(IAsyncStreamReader<BiStreamingRequest> requestStream, IServerStreamWriter<BiStreamingResponse> responseStream, ServerCallContext context)
        {
            //요청 메세지 수신
            var readTask = Task.Run(async () =>
            {
                await foreach (var message in requestStream.ReadAllAsync())
                {
                    Console.WriteLine("Received : " + message.Message);
                }
            });

            while (!readTask.IsCompleted) //요청이 끝나면 응답 전송도 종료한다
            {
                Console.WriteLine("send");
                await responseStream.WriteAsync(new BiStreamingResponse { Message = "BiStreaming response" });
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
