using System.IO;
using System.Reflection;
using Xamarin.Forms;

namespace PageCurl.Sample.media
{
    public class MediaTool
    {
        /// <summary>
        /// Returns a <see cref="Stream"/> to the given file name. Make sure to dispose it when you're done with it.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Stream GetMedia(string fileName)
        {
            string resourceID = $"PageCurl.Sample.media.{fileName}";
            Assembly assembly = GetType().GetTypeInfo().Assembly;

            return assembly.GetManifestResourceStream(resourceID);
        }
    }
}
