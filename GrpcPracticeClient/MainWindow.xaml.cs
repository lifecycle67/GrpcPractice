using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Threading;
using Protos.Greet;
using Protos.Streaming;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Protos.Blocking.BlockingService;
using static Protos.Greet.Greeter;
using static Protos.Streaming.StreamingService;

namespace GrpcPracticeClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GrpcChannel? _channel;
        AsyncManualResetEvent _resetEvent = new AsyncManualResetEvent(false);
        CancellationTokenSource? _serverStreamingCts;

        /// <summary>
        /// 구성된 채널을 가져옵니다
        /// </summary>
        public GrpcChannel Channel
        {
            get
            {
                if (_channel == null)
                {
                    ///GrpcChannel에서 JWT토큰 요청 및 인증 구성
                    //string token = string.Empty;
                    //JoinableTaskContext context = new JoinableTaskContext();
                    //context.CreateFactory(new JoinableTaskCollection(context)).Run(async () => { token = await AuthenticateAsync(name.Text); });
                    //var credentials = CallCredentials.FromInterceptor((context, metadata) =>
                    //{
                    //    if (!string.IsNullOrEmpty(token))
                    //    {
                    //        metadata.Add("Authorization", $"Bearer {token}");
                    //    }
                    //    return Task.CompletedTask;
                    //});

                    _channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions
                    {
                        //Credentials = ChannelCredentials.Create(new SslCredentials(), credentials),
                        ServiceConfig = new ServiceConfig
                        {
                            ///재시도 정책 구성
                            ///지연 시간은 0에서 설정 지연 시간에서 임의로 결정됨
                            MethodConfigs =
                            {
                                new MethodConfig
                                {
                                    Names = { MethodName.Default }, //MethodName.Default는 해당 채널에서 호출하는 모든 메서드에 적용됨
                                    RetryPolicy = new RetryPolicy
                                    {
                                        MaxAttempts = 5, //원래 시도를 포함한 최대 호출 시도 횟수입니다
                                        InitialBackoff = TimeSpan.FromSeconds(2), //다시 시도까지의 지연 시간의 초기값. 
                                        MaxBackoff = TimeSpan.FromSeconds(10), //최대 지연 시간.
                                        BackoffMultiplier = 2, //다시 시도할 때 마다 지연 시간에서 이 값을 곱함.
                                        RetryableStatusCodes = { StatusCode.DeadlineExceeded, StatusCode.Unavailable } //재시도할 상태 코드
                                    }
                                }
                            }
                        }
                    });
                }
                return _channel;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 단순 단항 서비스인 Greeter 서비스를 호출하는 동작을 수행
        /// </summary>
        private async void CallGreeterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = App.Current as App;
                if (app == null) return;

                var client = app.Services.GetRequiredService<GreeterClient>(); //서비스 컨테이너를 통해 인증 인터셉터가 구성된 클라이언트 인스턴스를 가져옵니다

                //GreeterClient client = new GreeterClient(Channel);

                ///요청 헤더를 추가한다
                Metadata entries = new Metadata();
                entries.Add(new Metadata.Entry("request_header", "Say hello!!!!"));

                var call = client.SayHelloAsync(
                    new HelloRequest
                    {
                        Name = name.Text
                    },
                    headers: entries);

                ///응답 헤더 수신
                //var headers = await call.ResponseHeadersAsync;
                //var h = headers.GetEnumerator();
                //while (h.MoveNext())
                //{
                //    MessageBox.Show($"response header key:{h.Current.Key} value:{h.Current.Value}");
                //}

                var response = await call.ResponseAsync;

                ///응답 트레일러 수신
                var trailers = call.GetTrailers();
                var t = trailers.GetEnumerator();
                while (t.MoveNext())
                {
                    MessageBox.Show($"response trailer key:{t.Current.Key} value:{t.Current.Value}");
                }

                MessageBox.Show(response.Message);
            }
            catch (RpcException ex)
            {
                MessageBox.Show(ex.Message);

                if (ex.Status.StatusCode == StatusCode.Unauthenticated)
                    Channel.Dispose();
            }
        }

        /// <summary>
        /// 서버 스트리밍 서비스를 호출하는 동작을 수행
        /// </summary>
        private async void ServerStreamButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = new StreamingServiceClient(Channel);

                _serverStreamingCts = new CancellationTokenSource();
                using var call = client.ServerStreaming(
                    new ServerStreamRequest
                    {
                        Name = name.Text
                    },
                    //deadline: DateTime.UtcNow.AddSeconds(5), ///스트리밍에서 최종 기한은 최초 요청에서 스트리밍이 완료되기까지의 시간이다. 
                                                               ///메세지 수신 사이에 타임아웃은 갱신되지 않는다.
                                                               ///ServerStreaming 서비스는 10초간 동작하므로 5초에서 DeadlineExceeded가 발생한다.
                    cancellationToken: _serverStreamingCts.Token);

                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    Responses.Items.Add(response.Message);
                }
            }
            catch (RpcException ex)
            {
                //deadline 초과
                if (ex.StatusCode == StatusCode.DeadlineExceeded)
                    MessageBox.Show(ex.Message);
                //호출 취소
                else if (ex.StatusCode == StatusCode.Cancelled)
                    MessageBox.Show(ex.Message);
            }
            catch { }
        }

        /// <summary>
        /// 클라이언트 스트리밍 서비스를 호출하는 동작을 수행
        /// </summary>
        private async void ClientStreamButton_Click(object sender, RoutedEventArgs e)
        {
            var client = new StreamingServiceClient(Channel);

            using var call = client.ClientStreaming();

            for (var i = 0; i < 10; i++)
            {
                await call.RequestStream.WriteAsync(new ClientStreamRequest { Caller = name.Text, Message = DateTime.Now.ToString() });
                await Task.Delay(1000);
            }
            await call.RequestStream.CompleteAsync(); //요청 전송이 끝났음을 서버에 알림

            var response = await call;
            MessageBox.Show(response.Message);
        }

        /// <summary>
        /// 양방향 스트리밍 서비스를 호출하는 동작을 수행
        /// </summary>
        private async void BiStreamButton_Click(object sender, RoutedEventArgs e)
        {
            var client = new StreamingServiceClient(Channel);

            using var call = client.BiStreaming(); ///양방향 스트리밍 시작

            ///메세지를 수신 받습니다. 양방향 스트리밍은 요청메세지를 전송하지 않아도 시작됩니다.
            var readTask = Task.Run(async () =>
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    Dispatcher.Invoke(() => Responses.Items.Add(response.Message));
                }
            });

            ///사용자 동작을 통해 신호받음 상태가 되면 요청 메세지를 전송합니다
            int count = 0;
            while (count < 3)
            {
                await _resetEvent.WaitAsync();
                await call.RequestStream.WriteAsync(new BiStreamingRequest { Message = "BiStreaming request " + DateTime.Now.ToString() });
                count++;
                _resetEvent.Reset();
            }

            await call.RequestStream.CompleteAsync(); //요청 전송이 끝났음을 서버에 알림
            Responses.Items.Add("Disconnected");
            await readTask;
        }
       
        private void BiStreamSendButton_Click(object sender, RoutedEventArgs e)
        {
            ///양방향 스트리밍 서비스를 통해 요청 메세지를 전송할 수 있도록 manual event를 신호받음 상태로 설정합니다
            _resetEvent.Set();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ///서버스트리밍 서비스 실행을 취소 합니다
            if (_serverStreamingCts != null)
                _serverStreamingCts.Cancel();
        }

        /// <summary>
        /// 실패하는 요청을 전송하고 재요청 실행을 확인한다
        /// </summary>
        private void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = new BlockingServiceClient(Channel);
                client.GetException(new Protos.Blocking.ExceptionRequest { Message = "" });
            }
            catch (RpcException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 사용자 인증. JWT 토큰 요청
        /// </summary>
        private async Task<string> AuthenticateAsync(string name)
        {
            try
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"https://localhost:5001/generateJwtToken?name={name}"),
                    Method = HttpMethod.Get,
                    Version = new Version(2, 0)
                };

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return String.Empty;
            }
        }
    }
}
