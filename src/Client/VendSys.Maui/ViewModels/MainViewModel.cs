using CommunityToolkit.Mvvm.ComponentModel;
using VendSys.Maui.Services;

namespace VendSys.Maui.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly IDexFileService _dexFileService;

    public MainViewModel(IApiService apiService, IDexFileService dexFileService)
    {
        _apiService = apiService;
        _dexFileService = dexFileService;
    }
}
