using Grpc.Core;
using Grpc.Net.Client;
using Protos.Greet;
using Protos.Streaming;
using System;
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

        public MainWindow()
        {
            InitializeComponent();
            _channel = GrpcChannel.ForAddress("http://localhost:5000");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GreeterClient client = new GreeterClient(_channel);

            var response = client.SayHello(
                new HelloRequest
                {
                    Name = name.Text
                });

            MessageBox.Show(response.Message);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var client = new StreamingServiceClient(_channel);

            using var call = client.ServerStreaming(
                new ServerStreamRequest
                {
                    Name = name.Text
                });

            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                Responses.Items.Add(response.Message);
            }
        }

        private async void ClientStreamButton_Click(object sender, RoutedEventArgs e)
        {
            var client = new StreamingServiceClient(_channel);

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
    }
}
