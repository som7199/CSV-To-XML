using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace CSVToXMLWPF.ViewModels
{
    // 바인딩될 값에 따라 다른 값을 지정해 주어야 할 때 IValueConverter, IMultiValueConverter 사용함
   
    public class IndexConverter : IMultiValueConverter
    {
        // ViewModel -> View
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var row = values[0] as CsvView;                               // 각 행의 데이터
            var rowList = values[1] as ObservableCollection<CsvView>;     // CsvRecords 리스트를 참조

            if (row != null && rowList != null)
            {
                // 데이터를 TextBlock.Text 속성에 표시하려면 ToString()으로 문자열 변환을 해줘야 하는데 그걸 빼먹어서 시간을 많이 써버렸다...^^
                // 인덱스 값을 0부터 시작하도록 반환 -> Address에는 값이 0부터 순차적으로 들어가야함!
                return rowList.IndexOf(row).ToString();
            }
            return 0;
        }

        // View -> ViewModel
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
