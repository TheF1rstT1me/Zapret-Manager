using System.Globalization;
using System.Windows.Data;
using ZapretManager.Core_.Models;

namespace ZapretManager.UI_1.Converters
{
    public class ModeToImageConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MODE mode)
            {
                return mode switch
                {
                    MODE.Zapret => "/Assets/Logos/discordLogo.png",
                    MODE.TgWsProxy => "/Assets/Logos/telegramLogo.png",
                    _ => "/Assets/Logos/discordLogo.png"
                };
            }
            return "/Assets/Logos/discordLogo.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}