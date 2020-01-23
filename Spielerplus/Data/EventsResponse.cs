namespace Spielerplus.Data
{
    /// <summary>
    /// spielerplus api html response
    /// </summary>
    public class HtmlResponse
    {
        /// <summary>
        /// html string (server rendered)
        /// </summary>
        public string html { get; set; }

        /// <summary>
        /// optional count included when lists of items are returned (e.g. events)
        /// </summary>
        public int count { get; set; }
    }
}