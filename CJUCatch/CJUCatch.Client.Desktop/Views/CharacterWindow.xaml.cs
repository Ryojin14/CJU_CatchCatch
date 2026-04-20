using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CJUCatch.Client.Desktop.Views;

public partial class CharacterWindow : Window
{
    private const string ParticleSpriteRelativePath = "Assets\\Particles\\particle-spark.png";
    private const int ComboDisplayMaxThreshold = 2_147_483_646;
    private readonly MainWindow _mainWindow;
    private readonly DispatcherTimer _particleTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly List<ParticleVisual> _particles = [];
    private int _lastComboCount;
    private BitmapImage? _idleSprite;
    private BitmapImage? _activeSprite;
    public bool EnableComboShake { get; set; } = true;
    private static readonly string ParticleChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ㄱㄴㄷㄹㅁㅂㅅㅇㅈㅊㅋㅌㅍㅎㅏㅑㅓㅕㅗㅛㅜㅠㅡㅣ!?@#$*";
    
    public event Action<double, double>? PositionChangedNormalized;
    public event Action<bool>? DragActivityChanged;
    public event Action<string>? ChatSubmitted;

    public CharacterWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;

        Loaded += (_, _) =>
        {
            Left = SystemParameters.WorkArea.Right - Width - 28;
            Top = SystemParameters.WorkArea.Bottom - Height - 36;
            PublishNormalizedPosition();
            LoadSprites();
        };

        LocationChanged += (_, _) => PublishNormalizedPosition();
        _particleTimer.Tick += (_, _) => UpdateParticles();
        _particleTimer.Start();
    }

    private void LoadSprites(int skinId = 1)
    {
        var basePath = AppContext.BaseDirectory;
        // skinId 1 = sp1/sp2 (고양이), skinId 2 = sp3/sp4 (새 모델)
        var idleFile = skinId == 2 ? "sp3.png" : "sp1.png";
        var activeFile = skinId == 2 ? "sp4.png" : "sp2.png";
        var idlePath = System.IO.Path.Combine(basePath, "Assets", "Characters", idleFile);
        var activePath = System.IO.Path.Combine(basePath, "Assets", "Characters", activeFile);

        if (File.Exists(idlePath))
        {
            _idleSprite = new BitmapImage();
            _idleSprite.BeginInit();
            _idleSprite.CacheOption = BitmapCacheOption.OnLoad;
            _idleSprite.UriSource = new Uri(idlePath);
            _idleSprite.EndInit();
            _idleSprite.Freeze();
        }
        else
        {
            _idleSprite = null;
        }

        if (File.Exists(activePath))
        {
            _activeSprite = new BitmapImage();
            _activeSprite.BeginInit();
            _activeSprite.CacheOption = BitmapCacheOption.OnLoad;
            _activeSprite.UriSource = new Uri(activePath);
            _activeSprite.EndInit();
            _activeSprite.Freeze();
        }
        else
        {
            _activeSprite = null;
        }

        CharacterImage.Source = _idleSprite;
    }

    // 판널에서 비대칭 스킨 전환 시 호출
    public void ChangeSkin(int skinId)
    {
        LoadSprites(skinId);
    }

    // 다른 사람이 채팅 시 왼쪽 위에 💬 뱃지 표시 (3초 후 자동 소멸)
    private DispatcherTimer? _chatBadgeTimer;
    public void ShowChatNotification(string senderName)
    {
        ChatNotificationNameBlock.Text = senderName;
        ChatNotificationBadge.Visibility = Visibility.Visible;

        _chatBadgeTimer?.Stop();
        _chatBadgeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _chatBadgeTimer.Tick += (_, _) =>
        {
            _chatBadgeTimer.Stop();
            ChatNotificationBadge.Visibility = Visibility.Collapsed;
        };
        _chatBadgeTimer.Start();
    }

    public void UpdateIdentity(string displayName)
    {
        NameTextBlock.Text = string.IsNullOrWhiteSpace(displayName) ? "게스트" : displayName;
    }

    public void UpdateSpeechBubble(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            SpeechBubbleBorder.Visibility = Visibility.Collapsed;
            return;
        }

        SpeechBubbleTextBlock.Text = text.Trim();
        SpeechBubbleBorder.Visibility = Visibility.Visible;
    }

    private void OpenControlPanel_Click(object sender, RoutedEventArgs e)
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void HideControlPanel_Click(object sender, RoutedEventArgs e)
    {
        _mainWindow.Hide();
    }

    private void ExitApplication_Click(object sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ExitApplication();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            if (e.ClickCount >= 2)
            {
                ChatInputContainer.Visibility = Visibility.Visible;
                ChatInputTextBox.Text = "";
                // Dispatcher를 이용해 약간의 지연 후 포커스 설정 (WPF 구조상 더 안전하게 포커스가 들어감)
                Dispatcher.InvokeAsync(() => ChatInputTextBox.Focus(), DispatcherPriority.Input);
                e.Handled = true;
                return;
            }

            DragActivityChanged?.Invoke(true);

            try
            {
                DragMove();
            }
            finally
            {
                PublishNormalizedPosition();
                DragActivityChanged?.Invoke(false);
            }
        }
    }

    private void PublishNormalizedPosition()
    {
        var area = SystemParameters.WorkArea;
        var x = area.Width <= Width ? 0.5 : (Left - area.Left) / (area.Width - Width);
        var y = area.Height <= Height ? 0.5 : (Top - area.Top) / (area.Height - Height);

        PositionChangedNormalized?.Invoke(
            Math.Clamp(x, 0.0, 1.0),
            Math.Clamp(y, 0.0, 1.0));
    }

    public void UpdateComboState(int comboCount, bool isActive)
    {
        ComboBadge.Visibility = comboCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        ComboTextBlock.Text = comboCount >= ComboDisplayMaxThreshold ? "MAX" : $"x{comboCount}";
        ComboBadge.Background = new SolidColorBrush(GetComboBadgeColor(comboCount));

        if (!isActive)
        {
            CharacterImage.Source = _idleSprite;
        }
        else
        {
            CharacterImage.Source = (comboCount % 2 == 0) ? _activeSprite : _idleSprite;
        }

        UpdateShake(comboCount, isActive);

        if (comboCount > _lastComboCount)
        {
            SpawnParticles(comboCount);
        }

        _lastComboCount = comboCount;
    }

    private void UpdateShake(int comboCount, bool isActive)
    {
        if (CharacterShakeTransform == null) return;

        if (!isActive || !EnableComboShake || comboCount <= 100)
        {
            CharacterShakeTransform.X = 0;
            CharacterShakeTransform.Y = 0;
            return;
        }

        double intensity = comboCount switch
        {
            > 400 => 6.0,
            > 300 => 4.0,
            > 200 => 2.5,
            _ => 1.0
        };

        CharacterShakeTransform.X = (Random.Shared.NextDouble() * 2 - 1) * intensity;
        CharacterShakeTransform.Y = (Random.Shared.NextDouble() * 2 - 1) * intensity;
    }

    private void SpawnParticles(int comboCount)
    {
        var count = Math.Min(3 + comboCount / 3, 10);
        var centerX = Width / 2;
        var centerY = 97.0; // 하단(키보드) 영역으로 내림 (축소된 높이에 맞춰 97.0 적용)

        for (var i = 0; i < count; i++)
        {
            var size = Random.Shared.NextDouble() * 6 + 7.5;
            // 사용자의 왼쪽 방향으로 약간 더 늘림
            var startX = centerX + Random.Shared.NextDouble() * 75 - 45;
            var startY = centerY + Random.Shared.NextDouble() * 12 - 6;
            var element = CreateParticleElement(size, comboCount);

            Canvas.SetLeft(element, startX);
            Canvas.SetTop(element, startY);
            ParticleCanvas.Children.Add(element);
            _particles.Add(new ParticleVisual
            {
                Element = element,
                StartX = startX,
                StartY = startY,
                VelocityX = Random.Shared.NextDouble() * 45 - 22.5,
                VelocityY = -(Random.Shared.NextDouble() * 135 + 75), // 위로 튀어오르는 힘 축소
                CreatedAt = DateTime.Now,
            });
        }
    }

    private void UpdateParticles()
    {
        if (_particles.Count == 0)
        {
            return;
        }

        var now = DateTime.Now;
        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            var age = (now - particle.CreatedAt).TotalSeconds;
            if (age >= 0.6)
            {
                ParticleCanvas.Children.Remove(particle.Element);
                _particles.RemoveAt(i);
                continue;
            }

            var progress = age;
            var x = particle.StartX + particle.VelocityX * progress;
            var y = particle.StartY + particle.VelocityY * progress + 600 * progress * progress; // 강한 중력 가속도

            Canvas.SetLeft(particle.Element, x);
            Canvas.SetTop(particle.Element, y);
            particle.Element.Opacity = 1.0 - (progress / 0.6);
        }
    }

    private FrameworkElement CreateParticleElement(double size, int comboCount)
    {
        var text = ParticleChars[Random.Shared.Next(ParticleChars.Length)].ToString();
        var color = GetRainbowColor(comboCount);

        return new TextBlock
        {
            Text = text,
            FontSize = size * 1.5,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(color),
            Opacity = 0.95,
            IsHitTestVisible = false,
        };
    }

    private static Color GetComboBadgeColor(int comboCount)
    {
        return comboCount switch
        {
            >= 400 => Color.FromRgb(220, 20, 60),  // Crimson (강렬한 빨강)
            >= 300 => Color.FromRgb(255, 69, 0),   // OrangeRed
            >= 200 => Color.FromRgb(255, 127, 80), // Coral
            >= 100 => Color.FromRgb(255, 160, 122),// LightSalmon
            _ => Color.FromRgb(244, 225, 138)      // Original #FFF4E18A
        };
    }

    private static Color GetRainbowColor(int comboCount)
    {
        double h = (comboCount * 5) % 360;
        double s = 1.0;
        double v = 0.95;
        
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }

    private void ChatInputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            SubmitChat();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            HideChatInput();
        }
    }

    private void SendChatButton_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        SubmitChat();
    }

    private void CancelChatButton_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        HideChatInput();
    }

    private void SubmitChat()
    {
        HideChatInput();
        ChatSubmitted?.Invoke(ChatInputTextBox.Text);
    }

    private void HideChatInput()
    {
        ChatInputContainer.Visibility = Visibility.Collapsed;
        Keyboard.ClearFocus();
    }

    private sealed class ParticleVisual
    {
        public required FrameworkElement Element { get; init; }
        public double StartX { get; init; }
        public double StartY { get; init; }
        public double VelocityX { get; init; }
        public double VelocityY { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
