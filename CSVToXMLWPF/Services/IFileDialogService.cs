using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVToXMLWPF.Services
{
    public interface IFileDialogService
    {
        List<string> OpenFileDialog(string filter);        // 파일 선택 창 띄우고 선택한 파일의 경로 반환 메서드
        string SaveFileDialog(string filter, string title);   // 파일 저장할 경로를 반환(매개변수는 파일 형식, 파일 저장 대화 상자 제목)
        void OpenSavedFileDialog(string filePath);                   // 변환된 XML 파일을 여는 메서드
    }
}
