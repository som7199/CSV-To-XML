using CSVToXMLWPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using System.Xml;

namespace CSVToXMLWPF.ViewModels
{
	public class SaveOptionsWindowViewModel : BindableBase
	{
        // 파일 다이얼로그 서비스 참조
        private readonly IFileDialogService _fileDialogService;

        private string _title = "CSV To XML";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        // 마지막으로 선택된 탭을 기준으로 XML을 변환해야하므로 마지막으로 선택된 탭을 저장하는 SelectedTabGroup 속성 생성
        private string _selectedTabGroup;
        public string SelectedTabGroup
        {
            get { return _selectedTabGroup; }
            set { SetProperty(ref _selectedTabGroup, value); }
        }

        // 현재 선택된 Read 탭 인덱스(UI에서 어떤 탭이 선택됐는지)
        private int _selectedReadTabIndex;
        public int SelectedReadTabIndex
        {
            get { return _selectedReadTabIndex; }
            set
            {
                SetProperty(ref _selectedReadTabIndex, value);
                //인덱스가 0 이상인 경우 선택된 탭이 있다는 의미
                if (value >= 0)
                {
                    SelectedTabGroup = "Read";
                }
            }
        }

        // 현재 선택된 Write 탭 인덱스(UI에서 어떤 탭이 선택됐는지)
        private int _selectedWriteTabIndex;
        public int SelectedWriteTabIndex
        {
            get { return _selectedWriteTabIndex; }
            set
            {
                SetProperty(ref _selectedWriteTabIndex, value);
                if (value >= 0)
                {
                    SelectedTabGroup = "Write";
                }
            }
        }

        // MainWindowViewModel에서 새롭게 분류한 ReadTabItems, WriteTabItems
        private ObservableCollection<CsvTabViewModel> _readTabItems;
        public ObservableCollection<CsvTabViewModel> ReadTabItems
        {
            get { return _readTabItems; }
            set { SetProperty(ref _readTabItems, value); }
        }

        private ObservableCollection<CsvTabViewModel> _writeTabItems;
        public ObservableCollection<CsvTabViewModel> WriteTabItems
        {
            get { return _writeTabItems; }
            set { SetProperty(ref _writeTabItems, value); }
        }


        // SaveOptionsWindowViewModel 생성자에서 MainWindowViewModel의 탭 정보를 전달
        public SaveOptionsWindowViewModel(IFileDialogService fileDialogService,
                                          ObservableCollection<CsvTabViewModel> readTabItems,
                                          ObservableCollection<CsvTabViewModel> writeTabItems,
                                          int selectedReadTabIndex,
                                          int selectedWriteTabIndex,
                                          string selectedTabGroup)
        {
            _fileDialogService = fileDialogService;
            ReadTabItems = readTabItems;
            WriteTabItems = writeTabItems;
            SelectedReadTabIndex = selectedReadTabIndex;
            SelectedWriteTabIndex = selectedWriteTabIndex;
            SelectedTabGroup = selectedTabGroup;
        }

        private string _rootName;
        public string RootName
        {
            get { return _rootName; }
            set { SetProperty(ref _rootName, value); }
        }

        private DelegateCommand _saveXMLCommand;
        public DelegateCommand SaveXMLCommand =>
            _saveXMLCommand ?? (_saveXMLCommand = new DelegateCommand(ExecuteSaveXMLCommand));

        // XML로 변환할 때는 행에 Name이 없으면  해당 행을 빼야함!
        // Address는 0부터 순차적으로 부여
        // PLCAddress - Bit Address - Read01 - Item - Name Address Label DataType Multi (이거는 나중에 UI에서 사용자가 선택해서 진행할 것 같기 때문에 프로님 코드 방식으로 2번 방법도 진행해보기)
        void ExecuteSaveXMLCommand()
        {
            // 탭이 선택되지 않은 경우(탭의 파일 이름이 없으면 예외 발생)
            if (ReadTabItems.Count == 0 || WriteTabItems.Count == 0)
            {
                MessageBox.Show("저장할 파일이 없습니다.", "❌📃❌", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // RootName을 입력하지 않은 경우
            if (string.IsNullOrEmpty(RootName))
            {
                MessageBox.Show("Root명을 지정해주세요", "❌⌨️❌", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            else
            {
                // 파일 저장 대화 상자
                string filter = "XML 파일 (*.xml)|*.xml";
                string title = "";
                string filePath = _fileDialogService.SaveFileDialog(filter, title);
                Debug.WriteLine(filePath);

                if (!string.IsNullOrEmpty(filePath))
                {
                    // 선택된 탭이 ReadTabItems에 있는지 WriteTabItems에 있는지 확인
                    if (SelectedTabGroup == "Read" && ReadTabItems[SelectedReadTabIndex] != null)
                    {
                        convertListToXML(ReadTabItems[SelectedReadTabIndex], filePath);           // 1번 방법
                        //convertDataTableToXML(ReadTabItems[SelectedReadTabIndex], filePath);        // 2번 방법
                    }

                    if (SelectedTabGroup == "Write" && WriteTabItems[SelectedWriteTabIndex] != null)
                    {
                        convertListToXML(WriteTabItems[SelectedWriteTabIndex], filePath);           // 1번 방법
                        //convertDataTableToXML(WriteTabItems[SelectedWriteTabIndex], filePath);        // 2번 방법
                    }
                }
                // 저장된 XML 파일 열기
                _fileDialogService.OpenSavedFileDialog(filePath);
            }
        }

        // 1번 방법 활용 > 아예 선택된 CsvTabViewModel을 매개변수로 전달
        void convertListToXML(CsvTabViewModel csvTabViewModel, string filePath)
        {
            List<CsvView> records = csvTabViewModel.CsvView.ToList();
            MessageBox.Show($"Original File Path: {csvTabViewModel.FilePath}\nSave Path: {filePath}");

            int address = 0;

            XDocument xDoc = new XDocument();

            // XML로 변환
            // XElement 클래스를 사용하여 XML 데이터를 만들고, XML 데이터에 LINQ를 사용하여 XML 요소를 가공
            XElement xml = new XElement(RootName,
                records.Where(record => !string.IsNullOrEmpty(record.Name)) // Name이 없는 항목 제외
                .Select(record => new XElement("Item",
                    new XElement("Name", record.Name),
                    new XElement("Address", (address++).ToString()),
                    new XElement("Label", record.Label),
                    new XElement("DataType", record.DataType),
                    new XElement("Multi", record.Multi)
                ))
            );
            // XML 파일로 저장
            xml.Save(filePath);
        }

        // 2번 방법 활용 => 1번과 마찬가지로 아예 선택된 CsvTabViewModel을 전달
        void convertDataTableToXML(CsvTabViewModel csvTabViewModel, string filePath)
        {
            DataTable dt = ConvertDataGridToDataTable(csvTabViewModel);
            DataTableToXML(dt, filePath);
        }

        DataTable ConvertDataGridToDataTable(CsvTabViewModel csvTabViewModel)
        {
            // DataTable : 단일 테이블, 즉 행과 열로 구성된 데이터 구조
            // DataSet : 여러 개의 DataTable 포함이 가능한 컨테이너, 관계형 DB와 유사한 구조, 데이터 간의 관계(ex: 고객-주문)가 있는 경우에 유용
            DataTable dataTable = new DataTable("Item");

            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Address");
            dataTable.Columns.Add("Label");
            dataTable.Columns.Add("DataType");
            dataTable.Columns.Add("Multi");
            
            int address = 0;
            foreach (var csvView in csvTabViewModel.CsvView)
            {
                if (!string.IsNullOrEmpty(csvView.Name))
                {
                    DataRow row = dataTable.NewRow();
                    row["Name"] = csvView.Name;
                    row["Address"] = (address++).ToString();
                    row["Label"] = csvView.Label;
                    row["DataType"] = csvView.DataType;
                    row["Multi"] = csvView.Multi;
                    dataTable.Rows.Add(row);
                }
            }
            return dataTable;
        }

        // WriteXml() 사용 시 DataTable에 루트 요소 이름 지정이 불가능함 ㅠㅠ
        // DataSet을 사용하여 루트 요소를 지정해주기!
        void DataTableToXML(DataTable dataTable, string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                // DataSet을 생성해 루트 요소 결정
                DataSet ds = new DataSet(RootName);
                ds.Tables.Add(dataTable);   // dataTable을 ds에 추가

                // filePath 위치에 XML 파일 생성
                // Indent = true => 들여쓰기
                using (var writer = XmlWriter.Create(filePath, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
                {
                    ds.WriteXml(writer);
                }
                MessageBox.Show("XML 저장 완료 : " + filePath);
            }
        }
        // 팀장님께서 나중에 엑셀 파일을 XML로 바꾸는건 DataTable 쓰시라고 하셨오
    }
}
