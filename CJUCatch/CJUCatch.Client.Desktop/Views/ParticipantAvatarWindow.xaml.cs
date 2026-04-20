using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using CJUCatch.Shared;

namespace CJUCatch.Client.Desktop.Views;

public partial class ParticipantAvatarWindow : Window
{
    private BitmapImage? _idleSprite;
    private BitmapImage? _activeSprite;

    public ParticipantAvatarWindow()
    {
        InitializeComponent();
        LoadCharacterImages();
    }

    private void LoadCharacterImages()
    {
        try
        {
            var basePath = AppContext.BaseDirectory;

            var idlePath = Path.Combine(basePath, "Assets", "Characters", "sp1.png");
            if (File.Exists(idlePath))
            {
                _idleSprite = new BitmapImage();
                _idleSprite.BeginInit();
                _idleSprite.CacheOption = BitmapCacheOption.OnLoad;
                _idleSprite.UriSource = new Uri(idlePath, UriKind.Absolute);
                _idleSprite.EndInit();
                _idleSprite.Freeze();
            }

            var activePath = Path.Combine(basePath, "Assets", "Characters", "sp2.png");
            if (File.Exists(activePath))
            {
                _activeSprite = new BitmapImage();
                _activeSprite.BeginInit();
                _activeSprite.CacheOption = BitmapCacheOption.OnLoad;
                _activeSprite.UriSource = new Uri(activePath, UriKind.Absolute);
                _activeSprite.EndInit();
                _activeSprite.Freeze();
            }

            CharacterImage.Source = _idleSprite;
        }
        catch
        {
            // Ignore
        }
    }

    public void ApplySnapshot(ParticipantSnapshot snapshot)
    {
        NameTextBlock.Text = snapshot.DisplayName;

        var area = SystemParameters.WorkArea;
        Left = area.Left + snapshot.PositionX * Math.Max(0, area.Width - Width);
        Top = area.Top + snapshot.PositionY * Math.Max(0, area.Height - Height);

        // State에 따라 스프라이트 전환 (Active면 sp2, 아니면 sp1)
        CharacterImage.Source = snapshot.State == PresenceState.Active ? _activeSprite : _idleSprite;
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
}
