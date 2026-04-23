using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VendSys.Client.Application.Constants;
using VendSys.Client.Application.Interfaces;

namespace VendSys.Maui.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IApiService _apiService;
    private readonly IDexFileService _dexFileService;

    public event EventHandler<string>? OnSendFailed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendDexACommand))]
    [NotifyCanExecuteChangedFor(nameof(SendDexBCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearTablesCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isError;

    public MainViewModel(IApiService apiService, IDexFileService dexFileService)
    {
        _apiService = apiService;
        _dexFileService = dexFileService;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendDexAAsync() => await SendDexFileAsync(Machines.A);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendDexBAsync() => await SendDexFileAsync(Machines.B);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task ClearTablesAsync()
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Clear All Data",
            "This will permanently delete all records from both tables. This action cannot be undone. Are you sure?",
            "Yes, clear",
            "Cancel");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            var result = await _apiService.ClearAllDataAsync();
            if (result.IsSuccess)
            {
                StatusMessage = "All data cleared successfully.";
                IsError = false;
                await Shell.Current.DisplayAlert("Success", "Tables cleared successfully.", "OK");
            }
            else
            {
                StatusMessage = result.ErrorMessage ?? "An error occurred.";
                IsError = true;
                await Shell.Current.DisplayAlert("Error", "Failed to clear tables. Please try again.", "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSend() => !IsBusy;

    private async Task SendDexFileAsync(string machine)
    {
        IsBusy = true;
        try
        {
            var dexContent = _dexFileService.LoadDexFile(machine);
            var result = await _apiService.SendDexFileAsync(machine, dexContent);
            if (result.IsSuccess)
            {
                StatusMessage = $"Machine {machine} data sent successfully.";
                IsError = false;
            }
            else
            {
                StatusMessage = result.ErrorMessage ?? "An error occurred.";
                IsError = true;
                OnSendFailed?.Invoke(this, StatusMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            IsError = true;
            OnSendFailed?.Invoke(this, StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
