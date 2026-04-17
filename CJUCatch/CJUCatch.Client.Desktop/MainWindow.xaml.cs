using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using CJUCatch.Client.Desktop.Services;
using CJUCatch.Client.Desktop.Views;
using CJUCatch.Shared;

namespace CJUCatch.Client.Desktop;

public partial class MainWindow : Window
{
    private const string LocalServerUrl = "https://localhost:7275";
    private const int ComboDisplayMaxThreshold = 2_147_483_646;

    private readonly GlobalInputActivityMonitor _inputActivityMonitor = new();
    private readonly PresenceClient _presenceClient = new();
    private readonly ParticipantOverlayManager _participantOverlayManager;
    private readonly Dictionary<string, ParticipantSnapshot> _participants = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _presenceTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };
    private readonly DispatcherTimer _activityResetTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _comboResetTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _speechBubbleResetTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    private bool _allowClose;
    private string? _joinedInstanceCode;
    private string? _currentSpeechBubbleText;
    private CharacterWindow? _characterWindow;
    private double _pendingPositionX = 0.5;
    private double _pendingPositionY = 0.5;
    private PresenceState _pendingState = PresenceState.Idle;
    private int _comboCount;

    public MainWindow()
    {
        InitializeComponent();
        _participantOverlayManager = new ParticipantOverlayManager(_sessionId);
        DisplayNameTextBox.TextChanged += DisplayNameTextBox_TextChanged;

        _presenceClient.ParticipantJoined += snapshot => Dispatcher.Invoke(() =>
        {
            _participants[snapshot.SessionId] = snapshot;
            _participantOverlayManager.Upsert(snapshot);
            UpdateCurrentInstanceText();
            StatusTextBlock.Text = $"{snapshot.DisplayName}님이 인스턴스에 들어왔습니다.";
        });

        _presenceClient.ParticipantUpdated += snapshot => Dispatcher.Invoke(() =>
        {
            _participants[snapshot.SessionId] = snapshot;
            _participantOverlayManager.Upsert(snapshot);
            UpdateCurrentInstanceText();
        });

        _presenceClient.ParticipantLeft += sessionId => Dispatcher.Invoke(() =>
        {
            _participants.Remove(sessionId);
            _participantOverlayManager.Remove(sessionId);
            UpdateCurrentInstanceText();
            StatusTextBlock.Text = "참가자 한 명이 인스턴스를 나갔습니다.";
        });

        _presenceClient.SpeechBubbleUpdated += update => Dispatcher.Invoke(() =>
        {
            _participantOverlayManager.UpdateSpeechBubble(update.SessionId, update.Text);
        });

        _presenceTimer.Tick += async (_, _) =>
        {
            _presenceTimer.Stop();
            await SendPresenceAsync();
        };

        _activityResetTimer.Tick += (_, _) =>
        {
            _activityResetTimer.Stop();
            _pendingState = PresenceState.Idle;
            ApplyLocalActivityVisuals();
            QueuePresenceSend();
        };

        _comboResetTimer.Tick += (_, _) =>
        {
            _comboResetTimer.Stop();
            _comboCount = 0;
            ApplyLocalActivityVisuals();
        };
        _speechBubbleResetTimer.Tick += async (_, _) =>
        {
            _speechBubbleResetTimer.Stop();
            await ApplySpeechBubbleAsync(null);
        };

        _inputActivityMonitor.ActivityDetected += () => Dispatcher.Invoke(HandleInputActivity);
        _inputActivityMonitor.Install();

        Loaded += (_, _) =>
        {
            UpdateCurrentInstanceText();
            ApplyLocalActivityVisuals();
            RefreshLocalIdentity();
        };

        Closing += OnClosing;
    }

    public void AttachCharacterWindow(CharacterWindow characterWindow)
    {
        _characterWindow = characterWindow;
        _characterWindow.EnableComboShake = !(ComboShakeCheckBox.IsChecked ?? false);
        
        characterWindow.PositionChangedNormalized += (x, y) =>
        {
            _pendingPositionX = x;
            _pendingPositionY = y;
            QueuePresenceSend();
        };

        characterWindow.DragActivityChanged += isActive =>
        {
            _pendingState = isActive ? PresenceState.Active : PresenceState.Idle;
            ApplyLocalActivityVisuals();
            QueuePresenceSend();
        };
        characterWindow.ChatSubmitted += async text => await ApplySpeechBubbleAsync(text);

        RefreshLocalIdentity();
        ApplyLocalActivityVisuals();
    }

    public void CloseWithoutHiding()
    {
        _allowClose = true;
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private async void CreateInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_joinedInstanceCode))
        {
            try
            {
                SetBusy(true);
                await LeaveInstanceInternalAsync();
                StatusTextBlock.Text = "인스턴스에서 퇴장했습니다.";
            }
            catch (Exception ex)
            {
                ShowActionError("인스턴스 퇴장 실패", ex);
            }
            finally
            {
                SetBusy(false);
            }

            return;
        }

        try
        {
            SetBusy(true);

            var request = new CreateInstanceRequest(ReadDisplayName());
            var code = await _presenceClient.CreateInstanceAsync(LocalServerUrl, request);

            await JoinInstanceInternalAsync(code);
            MessageBox.Show(
                this,
                $"인스턴스가 생성되었습니다.\n코드: {code}",
                "인스턴스 생성",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowActionError("인스턴스 생성 실패", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void JoinInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new JoinInstanceDialog
        {
            Owner = this,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await JoinInstanceInternalAsync(dialog.InstanceCode);
        }
        catch (Exception ex)
        {
            ShowActionError("인스턴스 입장 실패", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HelpDialog
        {
            Owner = this,
        };

        dialog.ShowDialog();
    }

    private async Task JoinInstanceInternalAsync(string instanceCode)
    {
        if (!string.IsNullOrWhiteSpace(_joinedInstanceCode))
        {
            await LeaveInstanceInternalAsync();
        }

        var normalizedCode = instanceCode.Trim().ToUpperInvariant();
        if (!InputRules.IsValidInstanceCode(normalizedCode))
        {
            throw new InvalidOperationException($"인스턴스 코드는 {InputRules.InstanceCodeLength}자리 영문 대문자/숫자여야 합니다.");
        }

        var snapshots = await _presenceClient.JoinInstanceAsync(
            LocalServerUrl,
            new JoinInstanceRequest(normalizedCode, ReadDisplayName(), _sessionId));

        _joinedInstanceCode = normalizedCode;
        _participants.Clear();
        _participantOverlayManager.Clear();

        foreach (var snapshot in snapshots)
        {
            _participants[snapshot.SessionId] = snapshot;
            _participantOverlayManager.Upsert(snapshot);
        }

        UpdateCurrentInstanceText();
        StatusTextBlock.Text = $"{_joinedInstanceCode} 인스턴스에 입장했습니다.";
        QueuePresenceSend();
    }

    private async Task LeaveInstanceInternalAsync()
    {
        if (string.IsNullOrWhiteSpace(_joinedInstanceCode))
        {
            return;
        }

        _presenceTimer.Stop();
        await _presenceClient.DisconnectAsync();
        _joinedInstanceCode = null;
        _participants.Clear();
        _participantOverlayManager.Clear();
        _pendingState = PresenceState.Idle;
        _comboCount = 0;
        _currentSpeechBubbleText = null;
        _speechBubbleResetTimer.Stop();
        _characterWindow?.UpdateSpeechBubble(null);
        UpdateCurrentInstanceText();
        UpdateActionButtons();
        ApplyLocalActivityVisuals();
    }

    private string ReadDisplayName()
    {
        var displayName = DisplayNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("닉네임을 입력해주세요.");
        }

        return InputRules.NormalizeDisplayName(displayName);
    }

    private void SetBusy(bool isBusy)
    {
        CreateInstanceButton.IsEnabled = !isBusy;
        JoinInstanceButton.IsEnabled = !isBusy;
        DisplayNameTextBox.IsEnabled = !isBusy;
    }

    private void UpdateCurrentInstanceText()
    {
        if (string.IsNullOrWhiteSpace(_joinedInstanceCode))
        {
            CurrentInstanceCodeTextBlock.Text = "없음";
            UpdateActionButtons();
            return;
        }

        CurrentInstanceCodeTextBlock.Text = _joinedInstanceCode;
        UpdateActionButtons();
    }

    private void QueuePresenceSend()
    {
        if (string.IsNullOrWhiteSpace(_joinedInstanceCode))
        {
            return;
        }

        _presenceTimer.Stop();
        _presenceTimer.Start();
    }

    private async Task SendPresenceAsync()
    {
        if (string.IsNullOrWhiteSpace(_joinedInstanceCode))
        {
            return;
        }

        try
        {
            var localSnapshot = new ParticipantSnapshot(
                _sessionId,
                ReadDisplayName(),
                _pendingPositionX,
                _pendingPositionY,
                _pendingState);

            _participants[_sessionId] = localSnapshot;
            await _presenceClient.UpdatePresenceAsync(new PresenceUpdate(
                _pendingPositionX,
                _pendingPositionY,
                _pendingState));

            UpdateCurrentInstanceText();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"위치 동기화 실패: {ex.Message}";
        }
    }

    protected override async void OnClosed(EventArgs e)
    {
        _inputActivityMonitor.Dispose();
        _participantOverlayManager.Dispose();
        await _presenceClient.DisposeAsync();
        base.OnClosed(e);
    }

    private void HandleInputActivity()
    {
        _pendingState = PresenceState.Active;
        if (_comboCount < ComboDisplayMaxThreshold)
        {
            _comboCount++;
        }

        _activityResetTimer.Stop();
        _activityResetTimer.Start();

        _comboResetTimer.Stop();
        _comboResetTimer.Start();

        ApplyLocalActivityVisuals();
        QueuePresenceSend();
    }

    private void ApplyLocalActivityVisuals()
    {
        var comboLabel = _comboCount >= ComboDisplayMaxThreshold ? "MAX" : $"x{_comboCount}";

        if (string.IsNullOrWhiteSpace(_joinedInstanceCode))
        {
            StatusTextBlock.Text = $"준비 완료 · 로컬 콤보 {comboLabel}";
        }
        else
        {
            StatusTextBlock.Text = $"{_joinedInstanceCode} 참가 중 · 콤보 {comboLabel} · 참가자 {_participants.Count}명";
        }

        _characterWindow?.UpdateComboState(_comboCount, _pendingState == PresenceState.Active);
    }

    private void RefreshLocalIdentity()
    {
        _characterWindow?.UpdateIdentity(GetCurrentDisplayNameOrFallback());
        _characterWindow?.UpdateSpeechBubble(_currentSpeechBubbleText);
    }

    private string GetCurrentDisplayNameOrFallback()
    {
        var value = DisplayNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return "게스트";
        }

        return InputRules.NormalizeDisplayName(value);
    }

    private void ShowActionError(string title, Exception exception)
    {
        var message = GetFriendlyErrorMessage(exception);
        StatusTextBlock.Text = message;
        MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static string GetFriendlyErrorMessage(Exception exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (current is HttpRequestException)
            {
                return "로컬 서버에 연결할 수 없습니다. 서버가 켜져 있는지 확인해주세요.";
            }

            current = current.InnerException!;
        }

        return exception.Message;
    }

    private void DisplayNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var normalized = InputRules.NormalizeDisplayName(DisplayNameTextBox.Text);
        if (DisplayNameTextBox.Text != normalized)
        {
            var caretIndex = normalized.Length;
            DisplayNameTextBox.Text = normalized;
            DisplayNameTextBox.CaretIndex = caretIndex;
        }

        RefreshLocalIdentity();
    }

    private void UpdateActionButtons()
    {
        CreateInstanceButton.Content = string.IsNullOrWhiteSpace(_joinedInstanceCode)
            ? "인스턴스 생성"
            : "인스턴스 퇴장";
    }


    private async Task ApplySpeechBubbleAsync(string? text)
    {
        var normalized = string.IsNullOrWhiteSpace(text)
            ? null
            : InputRules.NormalizeSpeechBubble(text);

        _currentSpeechBubbleText = normalized;
        _characterWindow?.UpdateSpeechBubble(normalized);

        _speechBubbleResetTimer.Stop();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            _speechBubbleResetTimer.Start();
        }

        if (!string.IsNullOrWhiteSpace(_joinedInstanceCode))
        {
            try
            {
                await _presenceClient.UpdateSpeechBubbleAsync(normalized);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"말풍선 전송 실패: {GetFriendlyErrorMessage(ex)}";
            }
        }
    }

    private void ComboShakeCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_characterWindow != null)
        {
            _characterWindow.EnableComboShake = !(ComboShakeCheckBox.IsChecked ?? false);
        }
    }
}
