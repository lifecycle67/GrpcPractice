using Grpc.Net.Client;
using Protos.Greet;
using System.Windows;
using static Protos.Greet.Greeter;

namespace GrpcPracticeClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GreeterClient client = new GreeterClient(GrpcChannel.ForAddress("http://localhost:5000"));

            var response = client.SayHello(
                new HelloRequest
                {
                    Name = name.Text
                });

            MessageBox.Show(response.Message);
        }
    }
}
