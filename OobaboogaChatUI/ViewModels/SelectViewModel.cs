using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace OobaboogaChatUI.ViewModels;

public partial class SelectViewModel : INotifyPropertyChanged
{
    public SelectViewModel()
    {
        SelectCommand = new RelayCommand(win =>
        {
            var window = (Window)win;
            window.DialogResult = true;
            window.Close();
        }, _ => IsValidData());
    }

    public SelectViewModel(List<string> items)
    {
        UseCollection(items);

        SelectCommand = new RelayCommand(win =>
        {
            var window = (Window)win;
            window.DialogResult = true;
            window.Close();
        }, _ => IsValidData());
    }

    public string SelectedItem { get; set; }
    public ObservableCollection<string> Items { get; set; }
    public RelayCommand SelectCommand { get; set; }

    public void UseCollection(List<string> items)
    {
        Items = new ObservableCollection<string>(items);
        SelectedItem = Items.FirstOrDefault();
    }

    private bool IsValidData()
    {
        if (Items == null || Items.Count == 0)
        {
            return false;
        }
        return Items.Contains(SelectedItem);
    }
}