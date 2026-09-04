using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DQMBreedingTool.Converters
{
    public class FamilyToIconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? familyName = value as string;
            string? iconPath = familyName switch
            {
                "???계" => "pack://application:,,,/Assets/Images/Unknown.png",
                "드래곤계" => "pack://application:,,,/Assets/Images/Dragon.png",
                "마수계" => "pack://application:,,,/Assets/Images/Beast.png",
                "물질계" => "pack://application:,,,/Assets/Images/Material.png",
                "슬라임계" => "pack://application:,,,/Assets/Images/Slime.png",
                "악마계" => "pack://application:,,,/Assets/Images/Demon.png",
                "자연계" => "pack://application:,,,/Assets/Images/Nature.png",
                "좀비계" => "pack://application:,,,/Assets/Images/Zombie.png",
                _ => null
            };

            if (string.IsNullOrEmpty(iconPath)) return null;

            try
            {
                return new BitmapImage(new Uri(iconPath));
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}