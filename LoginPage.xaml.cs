using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    /// <summary>
    /// LoginPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void Btn_goto_home_Click(object sender, RoutedEventArgs e)
        {

            NavigationService.Navigate(new Uri("HomePage.xaml", UriKind.Relative));

        }

        private void Btn_goto_join_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("JoinPage.xaml", UriKind.Relative));
        }
    }
}
