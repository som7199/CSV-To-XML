using CsvHelper;    // for CSVReader
using CsvHelper.Configuration;
using CSVToXMLWPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;

namespace CSVToXMLWPF.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        // 파일 다이얼로그 서비스 참조
        private readonly IFileDialogService _fileDialogService;

        private string _title = "CSV To XML";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public string FilePath { get; set; }

        // 기존에 열려있던 탭은 다시 열지 않기 위해서 파일 경로를 담는 filePathList 선언
        private List<string> filePathList;

        // Open File 버튼을 눌려야 Root 입력과 Group Name 선택이 가능하도록 하기 위해
        private bool openFileChecked;
        public bool OpenFileChecked
        {
            get { return openFileChecked; }
            set { SetProperty(ref openFileChecked, value); }
        }

        // 사용자가 입력한 RootName
        private string _rootName;
        public string RootName
        {
            get { return _rootName; }
            set { SetProperty(ref _rootName, value); }
        }

        /* 
         * CSV 레코드를 담기 위한 ObservableCollection
         * ObservableCollection 사용 이유 => UI와 데이터 간의 동기화 지원(INotifyCollectionChanged 인터페이스를 구현하여 컬렉션 변경을 감지하고 UI에 이벤트 발생시킴)
         * UI에 데이터 바인딩 시 필수
         * List는 일반적인 데이터 처리 위해 사용, UI와 연결이 필요 없을 때!
         */
        //public ObservableCollection<CsvRecord> CsvRecords { get; private set; }       // 이건 그냥 단순히 Read한거 뿌리기 할 때만 사용

        // CsvView 타입의 객체들을 담은 CsvView
        // Name, DataType, Multi는 수정 가능하도록 구현해야해서 이렇게 바꿔줌!
        private ObservableCollection<CsvView> _csvView;
        
        public ObservableCollection<CsvView> CsvView
        {
            get { return _csvView; }
            set { SetProperty(ref _csvView, value); }
        }

        // 여러 개의 CsvTabViewModel 객체 담는 _tabItems
        /*
         * 탭에 표시되는 데이터는 TabItems에서 오고, 각 탭의 내용은 CsvTabViewModel의 CsvView라는 ObservableCollection에 바인딩되어 DataGrid로 나타남!
         * 각 탭은 CsvTabViewModel 객체로 구성되며, CsvTabViewModel에는 CsvView와 FilePath, FileName이 있음
         * 각 탭에는 하나의 CSV 파일이 대응됨 => CsvTabViewModel의 CsvView가 각 탭의 DataGrid에 바인딩되어야 각 탭마다 해당 파일 경로에 따른 파일의 데이터가 데이터 그리드에 표시됨
         */
        private ObservableCollection<CsvTabViewModel> _tabItems;
        public ObservableCollection<CsvTabViewModel> TabItems
        {
            get { return _tabItems; }
            set { SetProperty(ref _tabItems, value); }
        }

        // 1. Open File 했을 때 기존에 열려있던 탭 말고 새로 연 파일이 보이게 하기 위함
        // 2. SelectedTabIndex로 해당 탭의 CsvView 데이터를 XML로 변환하기 위함
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set { SetProperty(ref _selectedTabIndex, value); }
        }

        private DelegateCommand _openFileCommand;
        public DelegateCommand OpenFileCommand =>
            _openFileCommand ?? (_openFileCommand = new DelegateCommand(ExecuteOpenFileCommand));

        private DelegateCommand _saveXMLCommand;
        public DelegateCommand SaveXMLCommand =>
            _saveXMLCommand ?? (_saveXMLCommand = new DelegateCommand(ExecuteSaveXMLCommand));
        
        public MainWindowViewModel(IFileDialogService fileDialogService)
        {
            _fileDialogService = fileDialogService;
            TabItems = new ObservableCollection<CsvTabViewModel>();     // CsvTabViewModel 객체를 담을 TabItems
            filePathList = new List<string>();                          // 생성자에서 초기화
        }

        // 파일 경로를 바탕으로 파일을 읽고
        // 해당 파일의 내용을 DataGrid이랑 Binding 할 수 있도록 CsvView에 저장!
        void ExecuteOpenFileCommand()
        {
            try
            {
                string filter = "CSV 파일 (*.csv)|*.csv";
                List<string> filePaths = _fileDialogService.OpenFileDialog(filter);    // 파일 경로 여러 개 받아올 것!
                
                //TabItems.Clear();       // 기존 데이터 초기화(프로그램 종료 전 또 다른 파일을 열 때 그 전에 열었던 파일을 사라지게 하기 위함)
                // 그냥 기존에 열려있던 파일은 그대로 열어두도록 코드 수정을 해야겠다!!
                foreach (string filePath in filePaths)
                {
                    // 새로 여는 파일인 경우
                    if (!filePathList.Contains(filePath))
                    {
                        filePathList.Add(filePath);

                        var tabViewModel = LoadCsv(filePath);     // csv 파일을 읽어 CsvTabViewModel 생성
                        TabItems.Add(tabViewModel);               // TabItems에 추가하여 UI에 탭 생성
                    }
                }
                //MessageBox.Show(filePaths.Count.ToString());

                // 새로 추가된 탭의 인덱스를 설정하여 선택합니다.
                if (TabItems.Count > 0)
                {
                    SelectedTabIndex = TabItems.Count - 1; // 마지막으로 추가된 탭의 인덱스를 선택
                }

                // OpenFileChecked를 true로 설정 => Root와 Group Name 입력 가능
                OpenFileChecked = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"오류 발생: {ex.Message}");
            }
        }

        public CsvTabViewModel LoadCsv(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                Debug.WriteLine(filePath);
                List<CsvRecord> records = ReadCsv(filePath);

                // MainWindowViewModel 생성자가 아니라 여기서 CsvView 생성한 이유
                // 생성자에서 선언하면 여러 탭이 같은 CsvView 컬렉션을 참조하기 때문에 마지막으로 읽은 Csv 파일의 데이터로 덮어씌워질 수 있음
                // 즉 모든 탭이 같은 데이터를 참조하게 되는 문제가 발생,, 이를 해결하기 위해 각 탭마다 새로운 ObservableCollection<CsvView>를 생성하도록 함
                CsvView = new ObservableCollection<CsvView>();

                // ObservableCollection CsvView에 데이터 추가
                for (int i = 0; i < records.Count; i++)
                {
                    CsvView temp = new CsvView(i + 1, records[i]);
                    CsvView.Add(temp);
                    /*
                     CsvView 타입의 객체를 저장하는 CsvView ObservableCollection에다가
                     CsvView에 생성자를 통해 CsvRecord 헤더별 해당 값 + No가 포함된 temp 객체를 저장! (CsvView는 CsvRecord 를 상속받음)
                    */
                }

                // 파일명만 탭 헤더에 출력
                var fileName = filePath.Split('\\').ToList();
                string tabName = fileName[fileName.Count - 1];
                //MessageBox.Show(tabName);

                // 새로운 CsvTabViewModel 인스턴스 생성
                // CsvTabViewModel 생성자는 탭 제목으로 쓸 파일명이랑 CSV 파일 데이터 담은 CsvView 인자로 받음
                var tabViewModel = new CsvTabViewModel(filePath, CsvView, tabName)
                {
                    
                    FilePath = filePath,
                    CsvView = CsvView,      // CSV 데이터를 담는 ObservableCollection
                    FileName = tabName,     // 경로 대신 탭 헤더로 파일명을 표시하기 위함 
                };
                return tabViewModel;
            }
            return null;
        }

        // CSV 파일을 읽어 CsvRecord 리스트로 변환하는 메서드
        static List<CsvRecord> ReadCsv(string filePath)
        {
            // 주어진 파일 경로에서 데이터를 스트림 형태로 읽기
            using (var reader = new StreamReader(filePath))
            // CsvHelper 설치 필요
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,     // CSV 파일의 첫 번째 행에 헤더가 포함되어 있음
            }))
            {
                // CSVReader를 사용하여 CsvRecord 형식의 데이터를 읽고 리스트로 변환하여 반환
                return csv.GetRecords<CsvRecord>().ToList();
            }
        }

        // XML로 변환할 때는 행에 Name이 없으면  해당 행을 빼야함!
        // Address는 0부터 순차적으로 부여
        // PLCAddress - Bit Address - Read01 - Item - Name Address Label DataType Multi (이거는 나중에 UI에서 사용자가 선택해서 진행할 것 같기 때문에 프로님 코드 방식으로 2번 방법도 진행해보기)
        void ExecuteSaveXMLCommand()
        {
            // 탭이 선택되지 않은 경우(탭의 파일 이름이 없으면 예외 발생)
            if (TabItems.Count == 0)
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
                    //convertListToXML(filePath);           // 1번 방법 - #if ~ #endif에 있음
                    //convertDataTableToXML(filePath);        // 2번 방법
                    //convertListToXML(TabItems[SelectedTabIndex], filePath);           // 1번 방법
                    convertDataTableToXML(filePath, TabItems[SelectedTabIndex]);        // 2번 방법
                }

                // 저장된 XML 파일 열기
                _fileDialogService.OpenSavedFileDialog(filePath);
            }
        }

        // 1번 방법 활용 > 아예 선택된 CsvTabViewModel을 매개변수로 전달
        void convertListToXML(CsvTabViewModel csvTabViewModel,string filePath)
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
        void convertDataTableToXML(string filePath, CsvTabViewModel csvTabViewModel)
        {
            DataTable dt = ConvertDataGridToDataTable(csvTabViewModel);
            DataTableToXML(dt, filePath);
        }

        DataTable ConvertDataGridToDataTable(CsvTabViewModel csvTabViewModel)
        {
            /*
            DataTable : 단일 테이블, 즉 행과 열로 구성된 데이터 구조
            DataSet : 여러 개의 DataTable 포함이 가능한 컨테이너, 관계형 DB와 유사한 구조, 데이터 간의 관계(ex: 고객-주문)가 있는 경우에 유용
            나는 간단한 데이터니까 DataTable 사용할게
            */
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

#if false   // 단일 데이터그리드 XML 변환(초기 코드)
        // 1번 방법 - 박흥준 프로님께서 주신 코드 활용(List로 변환한 CsvRecord를 바로 XML로 변환하는 코드라서 CsvView를 List로 변환해서 이 코드 써보기)
        void convertListToXML(string filePath)
        {
            //private CsvTabViewModel SelectedTabViewModel => TabItems[SelectedTabIndex];
            List<CsvView> records = CsvView.ToList();
            int address = 0;

            XDocument xDoc = new XDocument();

            // XML로 변환
            // XElement 클래스를 사용하여 XML 데이터를 만들고, XML 데이터에 LINQ를 사용하여 XML 요소를 가공
            XElement xml = new XElement("Items",
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

        // 2번 방법 - DataGrid(바인딩된 데이터)를 기반으로 DataTable 생성, 생성한 DataTabled을 XML로 변환
        /*
        아 원래 계획은 데이터그리드에 있는 내용을 수정했을 때, 데이터그리드에 있는 내용 그대로 XML 파일을 저장하고, csv 파일도 저장하려고 했었는데
        그냥 지금 이 코드나 1번 코드로 XML 파일 만들고
            - if 해당 파일로 XML을 이미 만든 후에, 새롭게 변경됐다면 기존의 XML 파일을 덮어쓸 것인지 아니면 새로운 파일을 만들 것인지
        새로 생성된 XML 파일을 CSV 파일로 바꾸는 함수를 하나 더 작성하면 해결될 듯..!
        */
        void convertDataTableToXML(string filePath)
        {
            DataTable dt = ConvertDataGridToDataTable();
            DataTableToXML(dt, filePath);
        }

        DataTable ConvertDataGridToDataTable()
        {
            /*
            DataTable : 단일 테이블, 즉 행과 열로 구성된 데이터 구조
            DataSet : 여러 개의 DataTable 포함이 가능한 컨테이너, 관계형 DB와 유사한 구조, 데이터 간의 관계(ex: 고객-주문)가 있는 경우에 유용
            나는 간단한 데이터니까 DataTable 사용
            */
            DataTable dataTable = new DataTable("Item");

            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("Address");
            dataTable.Columns.Add("Label");
            dataTable.Columns.Add("DataType");
            dataTable.Columns.Add("Multi");

            int address = 0;
            foreach (var csvView in CsvView)
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
                DataSet ds = new DataSet("Items");
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
#endif
    }
}
