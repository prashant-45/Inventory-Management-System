namespace IMS.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalChallans { get; set; }
        public List<string> Last5Days { get; set; } = new();
        public List<int> ChallansPerDay { get; set; } = new();
        public bool IsAdmin { get; set; }
    }
}
