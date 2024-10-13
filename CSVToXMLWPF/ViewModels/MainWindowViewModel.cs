using CsvHelper;    // for CSVReader
using CsvHelper.Configuration;
using CSVToXMLWPF.Services;
using CSVToXMLWPF.Views;
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
using System.Windows;

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

        /* 
         * CSV 레코드를 담기 위한 ObservableCollection
         * ObservableCollection 사용 이유 => UI와 데이터 간의 동기화 지원(INotifyCollectionChanged 인터페이스를 구현하여 컬렉션 변경을 감지하고 UI에 이벤트 발생시킴)
         * UI에 데이터 바인딩 시 필수
         * List는 일반적인 데이터 처리 위해 사용, UI와 연결이 필요 없을 때!
         */
        //public ObservableCollection<CsvRecord> CsvRecords { get; private set; }       // 이건 그냥 단순히 Read한거 뿌리기 할 때만 사용

        // CsvView 타입의 객체들을 담은 ObservableCollection CsvView
        // Name, DataType, Multi는 수정 가능하도록 구현해야해서 이렇게 바꿔줌!
        private ObservableCollection<CsvView> _csvView;
        
        public ObservableCollection<CsvView> CsvView
        {
            get { return _csvView; }
            set { SetProperty(ref _csvView, value); }
        }

#if false
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
#endif
        /*
         * 이제 Read용 데이터그리드와 Write용 데이터그리드 두 개를 띄워야하기 때문에
         * 각각의 ReadTabItems와 WriteTabItems를 관리할 수 있는 리스트(ObservableCollection)를 만들어야 함!
         * 이 리스트는 각 파일에 대해 별도의 CsvTabViewModel을 갖게 됨!
         */
        // Read 탭에 대한 리스트, UI와 바인딩될 것
        private ObservableCollection<CsvTabViewModel> _readTabItems;
        public ObservableCollection<CsvTabViewModel> ReadTabItems
        {
            get { return _readTabItems; }
            set { SetProperty(ref _readTabItems, value); }
        }

        // Write 탭에 대한 리스트, UI와 바인딩될 것
        private ObservableCollection<CsvTabViewModel> _writeTabItems;
        public ObservableCollection<CsvTabViewModel> WriteTabItems
        {
            get { return _writeTabItems; }
            set { SetProperty(ref _writeTabItems, value); }
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

        private DelegateCommand _openFileCommand;
        public DelegateCommand OpenFileCommand =>
            _openFileCommand ?? (_openFileCommand = new DelegateCommand(ExecuteOpenFileCommand));

        // Set SaveOptions 버튼 클릭 시
        private DelegateCommand _setSaveOptionsCommand;
        public DelegateCommand SetSaveOptionsCommand =>
            _setSaveOptionsCommand ?? (_setSaveOptionsCommand = new DelegateCommand(ExecuteSetSaveOptionsCommand));


        public MainWindowViewModel(IFileDialogService fileDialogService)
        {
            _fileDialogService = fileDialogService;
            filePathList = new List<string>();                          // 생성자에서 초기화

            ReadTabItems = new ObservableCollection<CsvTabViewModel>();
            WriteTabItems = new ObservableCollection<CsvTabViewModel>();
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

                        // csv 파일을 읽어 CsvTabViewModel 생성(진짜 csv 파일 그대로 읽기만 한 상태)
                        var tabViewModel = LoadCsv(filePath);

                        // 해당 파일을 Read 탭과 Write 탭에 나눠서 보여야하기 때문에
                        // Label에 I가 포함되면 ReadTabItems에 추가, O가 포함되면 WriteTabItems에 추가되도록 함
                        DivideReadWrite(tabViewModel);            
                    }
                }

                // 새로 추가된 탭의 인덱스를 설정하여 선택합니다.
                if (ReadTabItems.Count > 0)
                {
                    SelectedReadTabIndex = ReadTabItems.Count - 1; // 마지막으로 추가된 탭의 인덱스를 선택
                }

                // 새로 추가된 탭의 인덱스를 설정하여 선택합니다.
                if (WriteTabItems.Count > 0)
                {
                    SelectedWriteTabIndex = WriteTabItems.Count - 1; // 마지막으로 추가된 탭의 인덱스를 선택
                }

                // OpenFileChecked를 true로 설정 => Root와 Group Name 입력 가능
                OpenFileChecked = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"오류 발생: {ex.Message}");
            }
        }

        // ReadTabItems에 추가할지 WriteTabItems에 추가할지 결정하는 함수 하나 만들어서 ExecuteOpenFileCommand()에서 호출
        // Label에 I를 포함하는 행만 포함하는 ReadCsvView를 만들고, 그 ReadCsvView를 CsvTabViewModel 생성자의 인자로 추가한 후, 해당 CsvTabViewModel을 ReadTabItems에 추가하는 방식
        void DivideReadWrite(CsvTabViewModel tabViewModel)
        {
            /*
             * LINQ의 Select 메서드에서 자동으로 제공되는 index 변수와 record 변수!
             * Select 메서드는 각 요소를 나타내는 record, 그 요소의 인덱스를 나타내는 index 두 개의 매개변수를 받을 수 있음!
             * Select 내부에서 index는 0부터 시작하므로 index + 1을 통해 No 값을 1부터 순차적으로 할당!
             * 아래의 코드의 Select문은 No 속성에 순차적인 번호를 부여한 후 그 행(record)을 반환!
             */

            // Label에 I를 포함하는 행(record)만 포함하는 ReadCsvView 생성
            var readCsvView = new ObservableCollection<CsvView>(tabViewModel.CsvView.Where(record => record.Label.Contains('I'))
                                                                                    .Select((record, index) => {
                                                                                        record.No = index + 1; 
                                                                                        return record; 
                                                                                    }));

            // Label에 O를 포함하는 행만 포함하는 WriteCsvView 생성
            var writeCsvView = new ObservableCollection<CsvView>(tabViewModel.CsvView.Where(record => record.Label.Contains('O'))
                                                                                     .Select((record, index) => { 
                                                                                         record.No = index + 1; 
                                                                                         return record; 
                                                                                     }));

            // ReatTabItems와 WriteTabItems에 (조건에 맞게 분류되어) 생성된 CsvTabViewModel 추가
            if (readCsvView.Count > 0)
            {
                ReadTabItems.Add(new CsvTabViewModel(tabViewModel.FilePath, readCsvView, tabViewModel.FileName));
            }

            if (writeCsvView.Count > 0)
            {
                WriteTabItems.Add(new CsvTabViewModel(tabViewModel.FilePath, writeCsvView, tabViewModel.FileName));
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

        void ExecuteSetSaveOptionsCommand()
        {
            // 탭이 선택되지 않은 경우(탭의 파일 이름이 없으면 예외 발생)
            if (ReadTabItems.Count == 0 || WriteTabItems.Count == 0)
            {
                MessageBox.Show("저장할 파일이 없습니다.", "❌📃❌", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ForNextStep())
            {
                var saveOptionsWindow = new SaveOptionsWindow();            // SaveOptions 창을 띄움!

                // SaveOptionsWindow의 DataContext에 ViewModel을 바인딩
                var saveOptionsWindowViewModel = new SaveOptionsWindowViewModel(_fileDialogService,
                    this.ReadTabItems,
                    this.WriteTabItems,
                    this.SelectedReadTabIndex,
                    this.SelectedWriteTabIndex,
                    this.SelectedTabGroup);

                // DataContext: SaveOptionsWindow의 DataContext를 SaveOptionsWindowViewModel 인스턴스로 설정하여
                // UI 요소들이 ViewModel의 속성과 명령에 바인딩될 수 있도록 한다.
                saveOptionsWindow.DataContext = saveOptionsWindowViewModel;

                // MainWindow를 SaveOptionsWindow의 Owner로 설정
                saveOptionsWindow.Owner = Application.Current.MainWindow;

                // MainWindow의 위치와 동일하게 창 위치 설정
                saveOptionsWindow.WindowStartupLocation = WindowStartupLocation.Manual; // 수동으로 위치 설정
                saveOptionsWindow.Left = Application.Current.MainWindow.Left;
                saveOptionsWindow.Top = Application.Current.MainWindow.Top;

                saveOptionsWindow.ShowDialog();
            }
        }

        bool ForNextStep()
        {
            if (SelectedTabGroup == "Read")
            {
                if(MessageBox.Show($"선택한 파일이 {SelectedTabGroup}탭에 있습니까?", "🗂️❓", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    return true;
                return false;
            }

            if (SelectedTabGroup == "Write")
            {
                if (MessageBox.Show($"선택한 파일이 {SelectedTabGroup}탭에 있습니까?", "🗂️❓", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    return true;
                return false;
            }
            return false;
        }
    }
}
