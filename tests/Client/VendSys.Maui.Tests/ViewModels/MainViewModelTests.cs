using NSubstitute;
using VendSys.Client.Application.Interfaces;
using VendSys.Client.Application.Models;
using VendSys.Maui.ViewModels;

namespace VendSys.Maui.Tests.ViewModels;

[TestFixture]
public class MainViewModelTests
{
    private IApiService _apiSub = null!;
    private IDexFileService _dexSub = null!;
    private MainViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _apiSub = Substitute.For<IApiService>();
        _dexSub = Substitute.For<IDexFileService>();
        _dexSub.LoadDexFile(Arg.Any<string>()).Returns("dex-content");
        _sut = new MainViewModel(_apiSub, _dexSub);
    }

    private void SetupApi(ApiResult result) =>
        _apiSub.SendDexFileAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(result));

    // ── IsBusy ────────────────────────────────────────────────────────────────

    [Test]
    public async Task IsBusy_IsTrueWhileCommandExecuting()
    {
        var tcs = new TaskCompletionSource<ApiResult>();
        _apiSub.SendDexFileAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(tcs.Task);

        var commandTask = _sut.SendDexACommand.ExecuteAsync(null);
        Assert.That(_sut.IsBusy, Is.True);

        tcs.SetResult(ApiResult.Success());
        await commandTask;

        Assert.That(_sut.IsBusy, Is.False);
    }

    // ── CanExecute ────────────────────────────────────────────────────────────

    [Test]
    public async Task BothCommands_CanExecute_IsFalse_WhileIsBusy()
    {
        var tcs = new TaskCompletionSource<ApiResult>();
        _apiSub.SendDexFileAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(tcs.Task);

        var commandTask = _sut.SendDexACommand.ExecuteAsync(null);
        Assert.Multiple(() =>
        {
            Assert.That(_sut.SendDexACommand.CanExecute(null), Is.False);
            Assert.That(_sut.SendDexBCommand.CanExecute(null), Is.False);
        });

        tcs.SetResult(ApiResult.Success());
        await commandTask;
    }

    [Test]
    public async Task BothCommands_CanExecute_IsTrue_AfterCommandCompletes()
    {
        SetupApi(ApiResult.Success());
        await _sut.SendDexACommand.ExecuteAsync(null);
        Assert.Multiple(() =>
        {
            Assert.That(_sut.SendDexACommand.CanExecute(null), Is.True);
            Assert.That(_sut.SendDexBCommand.CanExecute(null), Is.True);
        });
    }

    // ── Success ───────────────────────────────────────────────────────────────

    [Test]
    public async Task StatusMessage_IsNonEmpty_AfterSuccess()
    {
        SetupApi(ApiResult.Success());
        await _sut.SendDexACommand.ExecuteAsync(null);
        Assert.That(_sut.StatusMessage, Is.Not.Empty);
    }

    [Test]
    public async Task IsError_IsFalse_AfterSuccess()
    {
        SetupApi(ApiResult.Success());
        await _sut.SendDexACommand.ExecuteAsync(null);
        Assert.That(_sut.IsError, Is.False);
    }

    // ── Failure ───────────────────────────────────────────────────────────────

    [Test]
    public async Task StatusMessage_IsNonEmpty_AfterFailure()
    {
        SetupApi(ApiResult.Failure("Server error."));
        await _sut.SendDexACommand.ExecuteAsync(null);
        Assert.That(_sut.StatusMessage, Is.Not.Empty);
    }

    [Test]
    public async Task IsError_IsTrue_AfterFailure()
    {
        SetupApi(ApiResult.Failure("Server error."));
        await _sut.SendDexACommand.ExecuteAsync(null);
        Assert.That(_sut.IsError, Is.True);
    }

    [Test]
    public async Task OnSendFailed_IsRaised_WithMessage_AfterFailure()
    {
        SetupApi(ApiResult.Failure("Server error."));
        string? raisedMessage = null;
        _sut.OnSendFailed += (_, msg) => raisedMessage = msg;

        await _sut.SendDexACommand.ExecuteAsync(null);

        Assert.That(raisedMessage, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task OnSendFailed_IsNotRaised_AfterSuccess()
    {
        SetupApi(ApiResult.Success());
        var raised = false;
        _sut.OnSendFailed += (_, _) => raised = true;

        await _sut.SendDexACommand.ExecuteAsync(null);

        Assert.That(raised, Is.False);
    }
}
