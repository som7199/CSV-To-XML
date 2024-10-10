using CSVToXMLWPF.Services;
using CSVToXMLWPF.Views;
using Prism.Ioc;
using System.Windows;

namespace CSVToXMLWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // FileDialogService 를 앱에 등록! 등록을 통해 ViewModel에서 이 서비스 사용이 가능해짐
            containerRegistry.Register<IFileDialogService, FileDialogService>();
        }
    }
}
