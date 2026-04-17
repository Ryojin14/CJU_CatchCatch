using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using CJUCatch.Shared;

namespace CJUCatch.Client.Desktop.Views;

public partial class ParticipantAvatarWindow : Window
{
    public ParticipantAvatarWindow()
    {
        InitializeComponent();
        LoadCharacterImage();
    }

    private void LoadCharacterImage()
    {
        try
        {
            var basePath = AppContext.BaseDirectory;
            var path = Path.Combine(basePath, "Assets", "Characters", "sp1.png");
            if (File.Exists(path))
            {
                CharacterImage.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
            }
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
