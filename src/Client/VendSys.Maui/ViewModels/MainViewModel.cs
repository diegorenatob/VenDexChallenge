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
        finally
        {
            IsBusy = false;
        }
    }
}
