using System.Windows;
using System.Windows.Media.Imaging;

namespace PomodoroTaskbar.Windows;

internal static class MascotArt
{
    private static readonly BitmapSource[] Poses = Load();
    internal static BitmapSource Pose(int index) => Poses[Math.Clamp(index, 0, Poses.Length - 1)];

    private static BitmapSource[] Load()
    {
        var sheet = new BitmapImage();
        sheet.BeginInit(); sheet.CacheOption = BitmapCacheOption.OnLoad;
        sheet.UriSource = new Uri("pack://application:,,,/Nekodoro;component/Assets/Cat-sprite.png");
        sheet.EndInit(); sheet.Freeze();
        var result = new BitmapSource[6];
        int width = sheet.PixelWidth / 3, height = sheet.PixelHeight / 2;
        for (int i = 0; i < result.Length; i++)
        {
            // The idle timer extends slightly beyond its nominal cell. Keep it complete,
            // and start the reading crop after it so no red sliver leaks into that pose.
            var rectangle = i switch
            {
                0 => new Int32Rect(0, 0, width + 44, height),
                1 => new Int32Rect(width + 44, 0, width - 44, height),
                _ => new Int32Rect((i % 3) * width, (i / 3) * height, width, height)
            };
            var pose = new CroppedBitmap(sheet, rectangle);
            pose.Freeze(); result[i] = pose;
        }
        return result;
    }
}
