using System.Collections.ObjectModel;
using TestAssigment.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TestAssigment.ViewModels;

public class ChecksViewModel : ObservableObject
{
    private ObservableCollection<int> _checkNums;
    private ObservableCollection<Check> _checks;

    public ObservableCollection<int> CheckNums
    {
        get => _checkNums;
        set => SetProperty(ref _checkNums, value);
    }

    public ObservableCollection<Check> Checks
    {
        get => _checks;
        set => SetProperty(ref _checks, value);
    }
}