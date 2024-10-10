using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace CSVToXMLWPF.ViewModels
{

    public class CsvView : CsvRecord
    {
        // csv 파일의 헤더에는 No가 없어서 CsvRecord를 상속받는 CsvView에 행마다 번호를 부여하기 위한 용도로 사용될 No 생성
        public int No { get; set; }
        public CsvView(int no, CsvRecord csvRecord)
        {
            No = no;
            Label = csvRecord.Label;
            // Address는 행에 순차적으로 부여할 예정이라 IndexConverter 사용
            Name = csvRecord.Name;
            DataType = csvRecord.DataType;
            Multi = csvRecord.Multi;
        }   
    }

    public class CsvRecord : BindableBase
    {
        // Label은 읽기 전용
        public string Label { get; set; }

        // Address는 IndexConverter로 0부터 모든 행에 순차적으로 숫자 부여할 것(CSV 파일의 Address에 값이 없을 경우가 있기 때문)
        //public string Address { get; set; }

        // 수정 가능한 프로퍼티
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);       // SetProperty를 사용해 값 변경 알림
        }

        private string _dataType;
        public string DataType
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }

        private string _multi;
        public string Multi
        {
            get => _multi;
            set => SetProperty(ref _multi, value);
        }
    }
}
