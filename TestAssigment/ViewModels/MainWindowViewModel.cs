using TestAssigment.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TestAssigment.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<Check> Checks { get; }

    public MainWindowViewModel()
    {
        var checks = new List<Check>
        {
            new Check(1, "Контакт 1", "Контакт 2", "Измерение напряжения", "Объект ЛОГ", "ЛОГ1", null)
        };
        Checks = new ObservableCollection<Check>(checks);
    }
}