using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TestAssigment.Models;

namespace TestAssigment.ViewModels;

public class ModificationsViewModel : ObservableObject
{
    private ObservableCollection<string> _modifications;

    public ObservableCollection<string> Modifications
    {
        get => _modifications;
        set => SetProperty(ref _modifications, value);
    }
}