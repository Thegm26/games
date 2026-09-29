using UnityEngine.UI;

namespace WhoEnters.Presentation
{
    /// <summary>
    /// UI-only integration seam. Art agents bind parchment/card/portrait sprites here later;
    /// presentation deliberately does not create fallback art or own raster assets.
    /// </summary>
    public sealed class PresentationBindings
    {
        public Text CaptionText;
        public Image ParchmentImage;
        public Image CardFrameImage;
        public Image PortraitImage;
        public Image VerdictStampImage;
    }
}
