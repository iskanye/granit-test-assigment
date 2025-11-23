using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TestAssigment.Models;

namespace TestAssigment.ViewModels;

public class ObjectsViewModel : ObservableObject
{
    private ObservableCollection<string> _objects;

    public ObservableCollection<string> Objects
    {
        get => _objects;
        set => SetProperty(ref _objects, value);
    }
}