using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Logging;
using FastReport;
using System.Data;

namespace TestAssigment;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void ButtonClick(object sender, RoutedEventArgs e)
    {
        using Report report = new();

        report.Load("Employee List.fpx");
        report.Prepare();
        message.Text = report.SaveToString();
    }
}