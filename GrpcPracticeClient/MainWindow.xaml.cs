using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.VisualStudio.Threading;
using Protos.Greet;
using Protos.Streaming;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Protos.Greet.Greeter;
using static Protos.Streaming.StreamingService;

namespace GrpcPracticeClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GrpcChannel _channel;
        AsyncManualResetEvent _resetEvent = new AsyncManualResetEvent(false);

        public GrpcChannel Channel
        {
            get 
            { 
                if (_channel == null)
                {
                    string token = string.Empty;
                    JoinableTaskContext context = new JoinableTaskContext();
                    context.CreateFactory(new JoinableTaskCollection(context)).Run(async () => { token = await AuthenticateAsync(name.Text); });
                    var credentials = CallCredentials.FromInterceptor((context, metadata) =>
                    {
                        if (!string.IsNullOrEmpty(token))
                        {
                            metadata.Add("Authorization", $"Bearer {token}");
                        }
                        return Task.CompletedTask;
                    });

                    _channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions
                    {
                        Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
                    });
                }
                return _channel;
            }
        }

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                var defaultMethodConfig = new MethodConfig
                {
                    Names = { MethodName.Default }, //MethodName.Default는 해당 채널에서 호출하는 모든 메서드에 적용됨
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = 5, //원래 시도를 포함한 최대 호출 시도 횟수입니다
                        InitialBackoff = TimeSpan.FromSeconds(2), //다시 시도까지의 지연 시간의 초기값. 지연 시간은 0에서 설정 지연 시간사이에서 임의로 결정됨
                        MaxBackoff = TimeSpan.FromSeconds(10), //최대 지연 시간.
                        BackoffMultiplier = 2, //다시 시도할 때 마다 지연 시간에서 이 값을 곱함.
                        RetryableStatusCodes = { StatusCode.DeadlineExceeded, StatusCode.Unavailable } //재시도할 상태 코드
                    }
                };
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GreeterClient client = new GreeterClient(Channel);

                Metadata entries = new Metadata();
                entries.Add(new Metadata.Entry("request_header", "Say hello!!!!"));

                var call = client.SayHelloAsync(
                    new HelloRequest
                    {
                        Name = name.Text
                    },
                    headers: entries);
                
                //var headers = await call.ResponseHeadersAsync;
                //var h = headers.GetEnumerator();
                //while (h.MoveNext())
                //{
                //    MessageBox.Show($"response header key:{h.Current.Key} value:{h.Current.Value}");
                //}

                var response = await call.ResponseAsync;
                //var trailers = call.GetTrailers();
                //var t = trailers.GetEnumerator();
                //while (t.MoveNext())
                //{
                //    MessageBox.Show($"response trailer key:{t.Current.Key} value:{t.Current.Value}");
                //}

                MessageBox.Show(response.Message);
            }
            catch (RpcException ex)
            {
                MessageBox.Show(ex.Message);

                if (ex.Status.StatusCode == StatusCode.Unauthenticated)
                    _channel.Dispose();
            }
        }

        CancellationTokenSource _cts;
        AsyncServerStreamingCall<ServerStreamResponse> _call;
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                var client = new StreamingServiceClient(Channel);

                _cts = new CancellationTokenSource();
                _call = client.ServerStreaming(
                    new ServerStreamRequest
                    {
                        Name = name.Text
                    },
                    deadline: DateTime.UtcNow.AddSeconds(5),
                    cancellationToken: _cts.Token);

                await foreach (var response in _call.ResponseStream.ReadAllAsync())
                {
                    Responses.Items.Add(response.Message);
                }
                _call.Dispose();
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

        private async void ClientStreamButton_Click(object sender, RoutedEventArgs e)
        {
            var client = new StreamingServiceClient(Channel);

            using var call = client.ClientStreaming();

            for (var i = 0; i < 10; i++)
            {
                await call.RequestStream.WriteAsync(new ClientStreamRequest { Caller = name.Text, Message = DateTime.Now.ToString() });
                await Task.Delay(1000);
            }
            await call.RequestStream.CompleteAsync();

            var response = await call;
            MessageBox.Show(response.Message);
        }

        private async void BiStreamButton_Click(object sender, RoutedEventArgs e)
        {
            var client = new StreamingServiceClient(Channel);

            using var call = client.BiStreaming();

            var readTask = Task.Run(async () =>
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    Dispatcher.Invoke(() => Responses.Items.Add(response.Message));
                }
            });

            int count = 0;
            while (count < 3)
            {
                await _resetEvent.WaitAsync();
                await call.RequestStream.WriteAsync(new BiStreamingRequest { Message = "BiStreaming request " + DateTime.Now.ToString() });
                count++;
                _resetEvent.Reset();
            }

            await call.RequestStream.CompleteAsync();
            Responses.Items.Add("Disconnected");
            await readTask;
        }

        
        private void BiStreamSendButton_Click(object sender, RoutedEventArgs e)
        {
            _resetEvent.Set();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            //_call.Dispose();
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
                return default; 
            }
        }
    }
}
