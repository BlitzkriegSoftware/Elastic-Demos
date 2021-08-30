namespace STW.Simple.Console.Libs
{
    /// <summary>
    /// Encoding Helper
    /// </summary>
    public static class EncodingHelper
    {
        /// <summary>
        /// Convert string to byte array
        /// </summary>
        /// <param name="text">string</param>
        /// <returns>byte array</returns>
        public static byte[] EncodeToBytes(string text)
        {
            return System.Text.UTF8Encoding.UTF8.GetBytes(text);
        }

        /// <summary>
        /// Convert byte array to string
        /// </summary>
        /// <param name="content">byte array</param>
        /// <returns>string</returns>
        public static string DecodeFromBytes(byte[] content)
        {
            if (content is null) return string.Empty;
            return System.Text.UTF8Encoding.UTF8.GetString(content);
        }

    }
}
