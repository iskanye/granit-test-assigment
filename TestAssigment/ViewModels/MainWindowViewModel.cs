using System;
using TestAssigment.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia;
using ReactiveUI;

namespace TestAssigment.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public Project? CurrentProject
    {
        get => _selectedProject;
        set
        {
            Message = value == null ? "" : "Загрузка проекта...";
            IsProjectLoaded = value != null;
            this.RaiseAndSetIfChanged(ref _selectedProject, value);
        }
    }

    public bool IsProjectLoaded
    {
        get => _isProjectLoaded;
        set => this.RaiseAndSetIfChanged(ref _isProjectLoaded, value);
    }

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    public ObservableCollection<Check> Checks
    {
        get => _checks;
        set => this.RaiseAndSetIfChanged(ref _checks, value);
    }

    public ReactiveCommand<Unit, Unit> OpenProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseProjectCommand { get; }

    private Project? _selectedProject;
    private bool _isProjectLoaded;
    private string _message = "";
    private ObservableCollection<Check> _checks = [];

    public MainWindowViewModel()
    {
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProject);
        CloseProjectCommand = ReactiveCommand.Create(() => { CurrentProject = null; });
    }

    private async Task OpenProject()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: { } mainWindow
            })
        {
            var storageProvider = mainWindow.StorageProvider;

            var options = new FolderPickerOpenOptions
            {
                Title = "Открыть папку проекта",
                AllowMultiple = false,
            };

            var folder = await storageProvider.OpenFolderPickerAsync(options);

            if (folder.Count > 0)
            {
                try
                {
                    CurrentProject = new Project(folder[0].Path);
                    Message = "";
                }
                catch (FileNotFoundException ex)
                {
                    Message = "Невозможно найти проект в папке: " + folder[0].Path.LocalPath;
                }
                catch (Exception ex)
                {
                    Message = "Ошибка при загрузке проекта: " + ex.Message;
                }
            }
        }
    }
}