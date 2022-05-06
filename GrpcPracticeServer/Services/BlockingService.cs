using Grpc.Core;
using Protos.Blocking;
using System.Diagnostics;
using static Protos.Blocking.BlockingService;

namespace GrpcPracticeServer.Services
{
    public class BlockingService : BlockingServiceBase
    {
        static List<string> _elapsedTime = new List<string>();
        static int _blockCount = 0;
        static Stopwatch _sw = new Stopwatch();

        public override Task<ExceptionResponse> GetException(ExceptionRequest request, ServerCallContext context)
        {
            var message = string.Empty;

            if (_sw.IsRunning)
            {
                _elapsedTime.Add(_sw.ElapsedMilliseconds.ToString());
                message = String.Join(',', _elapsedTime);
            }

            _blockCount++;

            if (_blockCount == 1)
                _sw.Start();
            else if (_blockCount == 5)
            {
                _sw.Stop();
                _blockCount = 0;
                _elapsedTime.Clear();
            }
            else
                _sw.Restart();
            
            throw new RpcException(new Status(StatusCode.Unavailable,"재요청 간격(ms) : " +  message));
        }

        public override async Task<LongtermDelayResponse> LongtermDelay(LongtermDelayRequest request, ServerCallContext context)
        {
            await Task.Delay(10000);
            return new LongtermDelayResponse { Message = "long time no see" };
        }
    }
}
