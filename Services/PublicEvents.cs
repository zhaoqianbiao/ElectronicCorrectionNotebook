using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace ElectronicCorrectionNotebook.Services
{
    public class PublicEvents
    {
        public static void PlaySystemSound()
        {
            var mediaPlayer = new MediaPlayer();
            mediaPlayer.Source = MediaSource.CreateFromUri(new Uri("ms-winsoundevent:Notification.Default"));
            mediaPlayer.Volume = 1.0; // 设置音量为最大值
            mediaPlayer.Play();
        }
    }
}
