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
    private const int MaxCheckResult = 100000;

    private Project? CurrentProject
    {
        get => _selectedProject;
        set
        {
            Message = value == null ? "" : "Загрузка проекта...";
            IsProjectLoaded = value != null;

            Title = value == null ? "" : value.Name;

            _selectedProject = value;
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (value != "")
            {
                this.RaiseAndSetIfChanged(ref _title, value + " | Проверки аппаратуры");
            }
            else
            {
                this.RaiseAndSetIfChanged(ref _title, "Проверки аппаратуры");
            }
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

    public string SelectedObject
    {
        get => _selectedObject;
        set
        {
            if (value != "")
                ModificationsViewModel.Modifications =
                    new ObservableCollection<string>(CurrentProject?.LoadModification(value) ?? []);
            else
            {
                ModificationsViewModel.Modifications.Clear();
                ChecksViewModel.CheckNums.Clear();
                ChecksViewModel.Checks.Clear();
            }

            this.RaiseAndSetIfChanged(ref _selectedObject, value);
        }
    }

    public string SelectedModification
    {
        get => _selectedModification;
        set
        {
            if (value != "" && SelectedObject != "")
                ChecksViewModel.CheckNums =
                    new ObservableCollection<int>(CurrentProject?.LoadCheckNums(SelectedObject, value) ?? []);
            else
            {
                ChecksViewModel.CheckNums = [];
                ChecksViewModel.Checks = [];
            }

            this.RaiseAndSetIfChanged(ref _selectedModification, value);
        }
    }

    public int SelectedCheckNum
    {
        get => _selectedCheckNum;
        set
        {
            if (value != 0 && SelectedModification != "" && SelectedObject != "")
                ChecksViewModel.Checks =
                    new ObservableCollection<Check>(
                        CurrentProject?.LoadChecks(SelectedObject, SelectedModification, value) ?? []);
            else
                ChecksViewModel.Checks.Clear();

            this.RaiseAndSetIfChanged(ref _selectedCheckNum, value);
        }
    }

    public ObjectsViewModel ObjectsViewModel { get; } = new ObjectsViewModel();

    public ModificationsViewModel ModificationsViewModel { get; } = new ModificationsViewModel();

    public ChecksViewModel ChecksViewModel { get; } = new ChecksViewModel();

    // Команды
    public ReactiveCommand<Unit, Unit> OpenProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseProjectCommand { get; }
    public ReactiveCommand<Unit, Unit> StartChecksCommand { get; }

    private Project? _selectedProject;

    private string _title = "Проверки аппаратуры";
    private bool _isProjectLoaded;
    private string _message = "";
    private string _selectedObject = "";
    private string _selectedModification = "";
    private int _selectedCheckNum = 0;

    public MainWindowViewModel()
    {
        OpenProjectCommand = ReactiveCommand.CreateFromTask(OpenProject);
        CloseProjectCommand = ReactiveCommand.Create(() => { CurrentProject = null; });
        StartChecksCommand = ReactiveCommand.Create(StartChecks);
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

                    ObjectsViewModel.Objects = new ObservableCollection<string>(CurrentProject.LoadObjects());
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

    private void StartChecks()
    {
        var checks = new Check[ChecksViewModel.Checks.Count];
        ChecksViewModel.Checks.CopyTo(checks, 0);

        foreach (var check in checks)
        {
            check.CheckResult = Random.Shared.Next(MaxCheckResult);
        }

        ChecksViewModel.Checks = new ObservableCollection<Check>(checks);
    }
}