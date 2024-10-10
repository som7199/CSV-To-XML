using Microsoft.Win32;          // 파일 다이얼로그 사용을 위해 추가
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace CSVToXMLWPF.Services
{
    public class FileDialogService : IFileDialogService
    {
        // 선택할 파일 창 띄우는 기능
        public List<string> OpenFileDialog(string filter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                // 여러 개의 파일 선택이 가능하도록
                Multiselect = true,
                Filter = filter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileNames.ToList();     // 파일이 선택되면 그 파일의 경로를 리스트로 반환
            }
            return new List<string>();    // 파일이 선택되지 않으면 null 반환
        }
                
        // 파일 저장 경로 및 저장할 파일의 이름 지정할 창 띄우는 기능
        public string SaveFileDialog(string filter, string title)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = filter,
                Title = title,
            };

            // ShowDialog() 메서드는 대화 상자를 화면에 표시하고, 사용자가 대화 상자에서 작업을 완료할 때까지 기다림
            // OK 버튼 클릭 시 true 반환, 취소 버튼 클릭 시 false 반환
            // saveFileDialog.FileName => 디렉터리 경로\사용자 지정 파일명
            return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;        // OK버튼 클릭 시 saveFileDialog.FileName 반환
        }

        // 파일 저장 후 파일이 저장된 (폴더에서 파일로 변경)파일을 자동으로 탐색기에서 열리도록 하는 기능
        public void OpenSavedFileDialog(string filePath)
        {
            if (File.Exists(filePath))      // 주어진 경로에 파일이 존재하는 지 확인
            {
                // 파일이 저장된 폴더 경로를 가져오기
                //string folderPath = Path.GetDirectoryName(filePath);    // 파일 경로에서 해당 파일이 위치한 폴더의 경로 반환

                // 탐색기에서 해당 폴더 열기 말고 파일을 열래 ㅎㅎ
                Process.Start(new ProcessStartInfo
                {
                    //FileName = folderPath,
                    FileName = filePath,
                    UseShellExecute = true,     // 운영 체제의 셸(Shell)을 사용해서 실행
                    Verb = "open"
                });
            }
        }

    }
}
