using System.Collections.ObjectModel;
using TestAssigment.Models;

namespace TestAssigment.ViewModels;

public class ChecksViewModel
{
    public ObservableCollection<int> CheckNums { get; set; }
    public ObservableCollection<Check> Checks { get; set; }
}