using System.Windows;

namespace CSVToXMLWPF.Views
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 확인 창 띄우기
            MessageBoxResult result = MessageBox.Show("종료하시겠습니까?", "👀❓", MessageBoxButton.YesNo, MessageBoxImage.Question); 

            // 사용자가 Yes를 클릭하면 창을 닫고, No를 클릭하면 닫기 취소
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; // 창 닫기 취소
            }
        }

        private void OpenSaveWindowButton_Click(object sender, RoutedEventArgs e)
        {
            // MainWindow를 숨기기
            this.Hide();

            // SaveOptionsWindow를 열기
            SaveOptionsWindow saveWindow = new SaveOptionsWindow();
            saveWindow.ShowDialog();  // SaveWindow를 모달로 열기

            // SaveWindow가 닫히면 다시 MainWindow를 보이기
            this.Show();
        }
    }
}
