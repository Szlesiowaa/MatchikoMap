using System.Globalization;

namespace MatchikoMap.Models
{
    public class ConfirmEmailDto
    {
        public int userId { get; set; }
        public string token { get; set; } = String.Empty;
    }
}
