using System.Collections.ObjectModel;

namespace TestAssigment.ViewModels;

public class ObjectsViewModel : ViewModelBase
{
    public ObservableCollection<string> Objects { get; set; }
}