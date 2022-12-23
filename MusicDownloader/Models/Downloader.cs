using System.IO;

namespace MusicDownloader.Models
{
    public class Downloader
    {
        private string _downloadsFolder { get; set; } = "/app/music/";

        public Downloader()
        {

        }

        public void Download(string link)
        {
            string path = _downloadsFolder + link + ".txt";
            _ = File.Create(path);
        }
    }
}
