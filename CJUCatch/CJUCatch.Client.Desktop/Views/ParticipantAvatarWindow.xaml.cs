using System.Windows;
using CJUCatch.Shared;

namespace CJUCatch.Client.Desktop.Views;

public partial class ParticipantAvatarWindow : Window
{
    public ParticipantAvatarWindow()
    {
        InitializeComponent();
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
