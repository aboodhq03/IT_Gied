namespace IT_Gied.Models
{
    public class ChatViewModel
    {
        public string Question { get; set; } = string.Empty;
        public string? Answer { get; set; }
        public List<AdvisorChatHistory> History { get; set; } = new();
    }
}
