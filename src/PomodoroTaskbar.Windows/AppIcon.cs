using System.Drawing;
using System.Windows.Media.Imaging;

namespace PomodoroTaskbar.Windows;

internal static class AppIcon
{
    private static readonly Uri ResourceUri = new("pack://application:,,,/Nekodoro;component/Assets/App.ico");
    private static readonly BitmapFrame Artwork = LoadArtwork();

    internal static BitmapFrame WindowImage() => Artwork;

    private static BitmapFrame LoadArtwork()
    {
        var decoder = BitmapDecoder.Create(ResourceUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames.OrderBy(f => f.PixelWidth).Last();
        frame.Freeze(); return frame;
    }

    internal static Icon TrayImage()
    {
        using var stream = System.Windows.Application.GetResourceStream(ResourceUri)!.Stream;
        using var icon = new Icon(stream, 32, 32);
        return (Icon)icon.Clone();
    }
}
