using CSVToXMLWPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using CSVToXMLWPF.Views;

namespace CSVToXMLWPF.ViewModels
{
    public class SaveOptionsWindowViewModel : BindableBase
    {
        // 파일 다이얼로그 서비스 참조
        private readonly IFileDialogService _fileDialogService;

        private string _title = "Set Save Options";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        // MainWindowViewModel에서 분류한 ReadTabItems, WriteTabItems
        private ObservableCollection<CsvTabViewModel> _tabItems;


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
            set { SetProperty(ref _fileList, value); }
        }

        // SaveTabs 콤보박스와 바인딩 될 사용자가 최종적으로 XML 파일로 변환하고 싶은 파일들
        private ObservableCollection<string> _saveTabFiles;
        public ObservableCollection<string> SaveTabFiles
        {
            get { return _saveTabFiles; }
            set { SetProperty(ref _saveTabFiles, value); }
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
        
        // =======================================================================================================================================================
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

        // 아니 ComboBox의 TextBlock에 Text="{Binding SelectedFileName}" 해주면 파일명이 안 뜨고 (SelectedSaveFileName 얘도 마찬가지)
        // Text="{Binding}"하면 잘 뜸..
        // Text="{Binding}"는 FileList/SaveTabFiles 컬렉션의 각 항목 전체를 바인딩
        // 각 항목의 ToString() 메서드를 사용하여 값을 출력하므로, 해당 항목이 문자열이나 객체일 경우 정상적으로 표시 ⇒ 그래서 SelectedAddFile이 string이라서 알아서 바인딩되는건가?!
        // SelectedAddFile/SelectedRemoveFile 이 string 타입이라서 알아서 잘 바인딩이 되는건가?
        // =======================================================================================================================================================

        private ObservableCollection<string> _groupName;
        public ObservableCollection<string> GroupName
        {
            get { return _groupName; }
            set { SetProperty(ref _groupName, value); }
        }

        private string _selectedGroupName;
        public string SelectedGroupName
        {
            get { return _selectedGroupName; }
            set { SetProperty(ref _selectedGroupName, value);}
        }

        // Read 탭인지 Write 탭인지 구분하기 위함!
        string selectedTab;

        // ==========================================================================================================================
        // SaveOptions창을 닫으면 SaveOptionsWindowViewModel의 소멸자가 호출되면서 OptionsDic과 DicKeys가 null이 되어버리는 문제 발생
        // MainWindowViewModel의 OptionsDic와 DicKeys를 참조해서 쓸 것! -> SaveOptionsWindowViewModel을 공유

        // Key는 사용자가 선택한 GroupName - Value는 사용자가 하나의 그룹으로 묶어 XML 파일로 변환할 csv 파일들
        private Dictionary<string, ObservableCollection<CsvTabViewModel>> _optionsDic;
        // OptionsDic의 Key값을 저장하는 리스트
        private List<string> _dicKeys;

        private DelegateCommand _addFileCommand;
        public DelegateCommand AddFileCommand =>
            _addFileCommand ?? (_addFileCommand = new DelegateCommand(ExecuteAddFileCommand));

        private DelegateCommand _removeFileCommand;
        public DelegateCommand RemoveFileCommand =>
            _removeFileCommand ?? (_removeFileCommand = new DelegateCommand(ExecuteRemoveFileCommand));

        private DelegateCommand _saveOptionsCommand;
        public DelegateCommand SaveOptionsCommand =>
            _saveOptionsCommand ?? (_saveOptionsCommand = new DelegateCommand(ExecuteSaveOptionsCommand));

        private DelegateCommand _backToOpenFile;
        public DelegateCommand BackToOpenFile =>
            _backToOpenFile ?? (_backToOpenFile = new DelegateCommand(ExecuteBackToOpenFile));

        private DelegateCommand _groupNameChangedCommand;
        public DelegateCommand GroupNameChangedCommand =>
            _groupNameChangedCommand ?? (_groupNameChangedCommand = new DelegateCommand(ExecuteGroupNameChangedCommand));

        

        // SaveOptionsWindowViewModel 생성자에서 MainWindowViewModel의 탭 정보를 전달
        public SaveOptionsWindowViewModel(IFileDialogService fileDialogService,
                                          ObservableCollection<CsvTabViewModel> TabItems,
                                          Dictionary<string, ObservableCollection<CsvTabViewModel>> optionsDic,
                                          List<string> dicKeys,
                                          string selectedTab
                                          )
        {
            _fileDialogService = fileDialogService;
            _tabItems = TabItems;
            _optionsDic = optionsDic;
            _dicKeys = dicKeys;

            this.selectedTab = selectedTab;
            // NullReferenceException을 방지하기 위해 미리 초기화
            SelectedTabFiles = new ObservableCollection<string>();
            SaveTabFiles = new ObservableCollection<string>();
            FileList = new ObservableCollection<string>();
            GroupName = new ObservableCollection<string>();

            //사용자가 클릭한 버튼(Set ReadSaveOpts/WriteSaveOpts)에 따라 SelectedTabs 콤보박스에 각각의 Read 탭의 파일 or Write 탭의 파일 바인딩
            UpdateSelectedTabFiles();
        }

        // 사용자가 GroupName 선택하여 이벤트가 발생했을 때 커멘드 실행
        void ExecuteGroupNameChangedCommand()
        {
            // 이미 사용자가 GroupName과 파일을 저장해놓은 경우
            // 해당 GroupName 선택 시 매칭되는 파일들을 보이게 하기 위함
            if (_optionsDic.ContainsKey(SelectedGroupName))
            {
                SaveTabFiles.Clear();
                foreach (var key in _dicKeys)
                {
                    if (key == SelectedGroupName)
                    {
                        foreach (var csvTabViewModel in _optionsDic[SelectedGroupName])
                        {
                            SaveTabFiles.Add(csvTabViewModel.FileName);
                        }
                        break;
                    }
                }
            }
            // ExecuteAddFileCommand()에서 SaveTabFiles에 있는 파일은 추가 안 되도록 해놨지만
            // 이렇게 리스트를 갱신해서 SaveTabFiles에 없는 파일들만 FileList에 보이는게 훨씬 보기 좋음!!
            UpdateSelectedFileList();
        }

        // 사용자가 클릭한 버튼(Set ReadSaveOpts/WriteSaveOpts)에 따라
        // SelectedTabs 콤보박스에 각각의 Read 탭의 파일 or Write 탭의 파일 바인딩
        void UpdateSelectedTabFiles()
        {
            for (int i = 1; i <= 5; i++)
            {
                GroupName.Add($"{selectedTab}0{i}");
            }

            foreach (var item in _tabItems)
            {
                SelectedTabFiles.Add(item.FileName);
            }

            // 저장하지 않을 파일들만 보임
            UpdateSelectedFileList();
        }

        ObservableCollection<string> GetOriginal() => new ObservableCollection<string>(_selectedTabFiles.Select(item => item));

        void UpdateSelectedFileList()
        {
            // FileList는 SaveTabFiles에 없는 파일만 표시하면 되니까 SelectedFileItems를 복사한 FileList를 만들고
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
                //SelectedFileName = FileList[SelectedFileIndex];
            }

            if (SaveTabFiles.Count > 0)
            {
                SelectedSaveFileIndex = SaveTabFiles.Count - 1;
                //SelectedSaveFileName = SaveTabFiles[SelectedSaveFileIndex];
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

        // Save Options 버튼 클릭 시
        // OptionsDic에 선택한 GroupName과 선택한 파일들 저장
        // 선택한 파일의 파일명은 SaveTabFiles 리스트에 저장되어 있음
        void ExecuteSaveOptionsCommand()
        {
            if (SaveTabFiles.Count == 0)
            {
                MessageBox.Show("GroupName에 추가할 파일을 선택해주세요.", "❌⌨️❌", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(SelectedGroupName))
            {
                MessageBox.Show("GroupName을 선택해주세요.", "❌⌨️❌", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            {
                // SaveTabFiles에 있는 파일명과 파일명이 같은 CsvTabViewModel들을 저장할 temp 컬렉션을 만들고,
                ObservableCollection<CsvTabViewModel> temp = new ObservableCollection<CsvTabViewModel>();

                foreach (var fileName in SaveTabFiles)
                {
                    // FirstOrDefault()는 조건을 충족하는 row를 못 찾으면 NULL을 리턴하므로 NULL이 반환되는 경우도 생각해줘야함!
                    // ReadTabItems에 있는 CsvTabViewModel중 FileName이 SaveTabFiles의 FileName과 같은 CsvTabViewModel 객체를 matchedCsvTab에 저장
                    CsvTabViewModel matchedCsvTab = _tabItems.FirstOrDefault(tab => tab.FileName.Equals(fileName));

                    // 딕셔너리의 키 값에 temp 컬렉션을 넣으면 중복 문제도 해결되고 아주 굿,, 싸부님 짱!!!
                    temp.Add(matchedCsvTab);
                }
                // 딕셔너리는 이렇게만 해줘도 알아서 키 값에 value 값을 넣음! (키가 없다면 키도 바로 생성하고 그 키에 temp 값을 넣어줌)
                _optionsDic[SelectedGroupName] = temp;

                // 사용자가 선택한 그룹네임이 OptionsDic에 저장되지 않았다면 _dicKeys는 해당 그룹네임을 포함하고 있지 않겠지!
                if (!_dicKeys.Contains(SelectedGroupName))
                {
                    _dicKeys.Add(SelectedGroupName);
                }
            }
            MessageBox.Show("옵션이 저장되었습니다!", "✨🗒️✨");

            Action closeWindowAction = CloseSaveOptionsWindow;
            closeWindowAction();
        }

        void ExecuteBackToOpenFile()
        {
            Action closeWindowAction = CloseSaveOptionsWindow;
            closeWindowAction();
        }

        void CloseSaveOptionsWindow()
        {
            // Dispatcher로 UI스레드에서 창을 닫기
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 현재 열려 있는 창 중에서 SaveOptionsWindow 타입의 창을 찾음
                // OfType<T>() : LINQ의 확장 메서드 중 하나로, 컬렉션에서 지정된 타입의 요소만 선택하여 반환, T는 찾고자 하는 객체의 타입
                var window = Application.Current.Windows.OfType<SaveOptionsWindow>().FirstOrDefault();
                window.Close();
            });
        }
    }
}
