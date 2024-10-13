using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVToXMLWPF.ViewModels
{
    // _tabItems는 여러 개의 CsvTabViewModel 객체를 담는 ObservableCollection
    // 각 CsvTabViewModel은 하나의 CSV 파일에 대한 정보를 담고 있음
    // 얘로 각각의 탭을 생성할 것!!
    public class CsvTabViewModel
    {
        // 탭 제목 (일단은 csv 파일 경로) -> 탭 제목만 출력하도록 MainWindowViewModel에서 작업
        public string FilePath {  get; set; }

        public string FileName { get; set; }

        // 탭에서 해당 CSV 파일의 데이터를 담고 있는 ObservableCollection
        public ObservableCollection<CsvView> CsvView { get; set; }

        public CsvTabViewModel(string filePath, ObservableCollection<CsvView> csvView, string fileName)
        {
            FilePath = filePath;
            CsvView = csvView;
            FileName = fileName;
        }
    }
}
