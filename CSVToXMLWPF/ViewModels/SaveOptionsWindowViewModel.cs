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

        // SelectedTabs 콤보박스와 바인딩 될 사용자가 선택한 탭에 열려있는 파일(명)들, 초기에 사용자가 선택한 탭에 열려있는 파일들을 가져오기 위함
        private ObservableCollection<string> _selectedTabFiles;
        public ObservableCollection<string> SelectedTabFiles
        {
            get { return _selectedTabFiles; }
            set { SetProperty(ref _selectedTabFiles, value); }
        }

        // SelectedTabFiles를 복사한 FileList에는 SaveTabFiles에 없는 파일(명)들, 즉 사용자가 아직 선택하지 않은 파일들을 저장!!
        private ObservableCollection<string> _fileList;
        public ObservableCollection<string> FileList
        {
            get { return _fileList; }
            set 
            { 
                SetProperty(ref _fileList, value);
            }
        }

        // SaveTabs 콤보박스와 바인딩 될 사용자가 최종적으로 XML 파일로 변환하고 싶은 파일들
        private ObservableCollection<string> _saveTabFiles;
        public ObservableCollection<string> SaveTabFiles
        {
            get { return _saveTabFiles; }
            set 
            { 
                SetProperty(ref _saveTabFiles, value);
            }
        }

        // 사용자가 SelectedTabs 콤보박스에서 선택한 파일 ---------------------> 사용자가 선택한 파일을 바인딩해서 콤보박스에서 보여주기 위함
        private string _selectedAddFile;
        public string SelectedAddFile
        {
            get { return _selectedAddFile; }
            set { SetProperty(ref _selectedAddFile, value); }
        }

        // 사용자가 SaveTabs 콤보박스에서 선택한 파일
        private string _selectedRemoveFile;
        public string SelectedRemoveFile
        {
            get { return _selectedRemoveFile; }
            set { SetProperty(ref _selectedRemoveFile, value); }
        }

        // FileList의 첫 번째 값과 SaveTabFiles의 마지막 값이 콤보박스에 뜨게 하기 위함
        private int _selectedFileIndex;
        public int SelectedFileIndex
        {
            get { return _selectedFileIndex; }
            set { SetProperty(ref _selectedFileIndex, value); }
        }

        private int _selectedSaveFileIndex;
        public int SelectedSaveFileIndex
        {
            get { return _selectedSaveFileIndex; }
            set { SetProperty(ref _selectedSaveFileIndex, value); }
        }

        // FileList의 첫 번째 값과 SaveTabFiles의 마지막 값, 즉 각각의 파일명이 UI에서 조금 더 이쁘게 보였으면 해서 TextBlock의 Text와 바인딩할 속성 추가..^^
        private string _selectedFileName;
        public string SelectedFileName
        {
            get { return _selectedFileName; }
            set { SetProperty(ref _selectedFileName, value); }
        }

        private string _selectedSaveFileName;
        public string SelectedSaveFileName
        {
            get { return _selectedSaveFileName; }
            set { SetProperty(ref _selectedSaveFileName, value); }
        }

        // ========>
        // 아니 ComboBox의 TextBlock에 Text="{Binding SelectedFileName}" 해주면 파일명이 안 뜨고 (SelectedSaveFileName 얘도 마찬가지)
        // Text="{Binding}"하면 잘 뜸 ㅡㅡ
        // SelectedFileName이랑 SelectedSaveFileName이 ObservableCollection 타입이 아니라서 UI에 반영이 안 되는거야?!
        // Text="Binding"만 하면 Text="{Binding}"는 현재 DataContext에서 기본 값을 가져온대


        // 



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

            // NullReferenceException을 방지하기 위해 미리 초기화
            SelectedTabFiles = new ObservableCollection<string>();
            SaveTabFiles = new ObservableCollection<string>();
            FileList = new ObservableCollection<string>();
            
            UpdateSelectedTabFiles();       // 사용자가 선택한 탭(Read or Write)에 있는 파일들이 콤보박스에 보이게 하기 위한 메서드
        }

        private string _rootName;
        public string RootName
        {
            get { return _rootName; }
            set { SetProperty(ref _rootName, value); }
        }

        private DelegateCommand _addFileCommand;
        public DelegateCommand AddFileCommand =>
            _addFileCommand ?? (_addFileCommand = new DelegateCommand(ExecuteAddFileCommand));

        private DelegateCommand _removeFileCommand;
        public DelegateCommand RemoveFileCommand =>
            _removeFileCommand ?? (_removeFileCommand = new DelegateCommand(ExecuteRemoveFileCommand));
        
        private DelegateCommand _saveXMLCommand;
        public DelegateCommand SaveXMLCommand =>
            _saveXMLCommand ?? (_saveXMLCommand = new DelegateCommand(ExecuteSaveXMLCommand));

        // XML로 변환할 때는 행에 Name이 없으면  해당 행을 빼야함!
        // Address는 0부터 순차적으로 부여
        // PLCAddress - Bit Address - Read01 - Item - Name Address Label DataType Multi (이거는 나중에 UI에서 사용자가 선택해서 진행할 것 같기 때문에 프로님 코드 방식으로 2번 방법도 진행해보기)
        void ExecuteSaveXMLCommand()
        {
            // RootName을 입력하지 않은 경우
            if (string.IsNullOrEmpty(RootName))
            {
                MessageBox.Show("Root명을 지정해주세요", "❌⌨️❌", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ComboBox 선택하지 않은 경우


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
        // WriteXml() 사용 시 DataTable에 루트 요소 이름 지정이 불가능함 => DataSet을 사용하여 루트 요소를 지정해주기!
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

        // 사용자가 선택한 탭(Read/Write)에 따라 SelectedTabs 콤보박스에 바인딩
        // SelectedTabs 콤보박스량 FileList랑 바인딩해서 FileList의 내용이 SelectedTabs 콤보박스에 보이도록 하기
        // FileList는 SelectedTabFiles의 값을 복사하고 있음!!
        void UpdateSelectedTabFiles()
        {
            if (SelectedTabGroup == "Read")
            {
                foreach (var item in ReadTabItems)
                {
                    SelectedTabFiles.Add(item.FileName);
                    FileList.Add(item.FileName);
                }
            }
            else
            {
                foreach (var item in WriteTabItems)
                {
                    SelectedTabFiles.Add(item.FileName);
                    FileList.Add(item.FileName);
                }
            }
        }

        ObservableCollection<string> GetOriginal() => new ObservableCollection<string>(_selectedTabFiles.Select(item => item));

        void UpdateSelectedFileList()
        {
            // FileList는 SaveTabFiles에 없는 파일만 표시하면 되니까@
            // SelectedFileItems를 복사한 FileList를 만들고
            // 해당 FileList에 SaveTabs의 아이템이 포함되어 있으면 FileList에서 Remove하기
            FileList = GetOriginal();           // 원본 파일 리스트 가져와서
            foreach (var item in SaveTabFiles)
            {
                if (FileList.Contains(item))
                    FileList.Remove(item);      // SaveTabFiles에 포함된 파일 제거
            }

            // FileList가 갱신됐을 때에도 FileList[0] 값은 SelectTabs에, SaveTabFiles[SaveTabFiles.Count-1] 값은 SaveTabs 콤보박스에 뜨게 하고 싶어서 추가
            // SelectedIndex 값을 설정하면 해당 인덱스에 위치한 항목이 자동으로 콤보박스에 표시되는 걸 활용했음!!
            // UpdateSelectedFileList()는 Add File 이나 Remove File 클릭 시에 호출되는 메서드이기 때문에
            // 여기에서 SelectedTabs 콤보박스의 SelectedIndex와 바인딩된 SelectedFileIndex
            // SaveTabs 콤보박스의 SelectedIndex와 바인딩된 SelectedSaveFileIndex 의 값을 업데이트 해주면
            // 각각의 리스트가 갱신된 후에도 각각의 콤보박스에는 FileList의 첫번째 파일명이 보이고, SaveTabFiles의 마지막에 있는 파일명이 보임!!
            if (FileList.Count > 0)
            {
                SelectedFileIndex = 0;
                SelectedFileName = FileList[SelectedFileIndex];
            }

            if (SaveTabFiles.Count > 0)
            {
                SelectedSaveFileIndex = SaveTabFiles.Count - 1;
                SelectedSaveFileName = SaveTabFiles[SelectedSaveFileIndex];
            }
        }

        // Add File 버튼 클릭 시
        void ExecuteAddFileCommand()
        {
            if (!string.IsNullOrEmpty(SelectedAddFile) && !SaveTabFiles.Contains(SelectedAddFile))
            {
                SaveTabFiles.Add(SelectedAddFile);
                UpdateSelectedFileList();
            }
        }

        // Remove File 버튼 클릭 시
        void ExecuteRemoveFileCommand()
        {
            if (!string.IsNullOrEmpty(SelectedRemoveFile) && SaveTabFiles.Contains(SelectedRemoveFile))
            {
                SaveTabFiles.Remove(SelectedRemoveFile);    // SaveTabs에서 선택한 파일 제거
                UpdateSelectedFileList();
            }
        }
    }
}
