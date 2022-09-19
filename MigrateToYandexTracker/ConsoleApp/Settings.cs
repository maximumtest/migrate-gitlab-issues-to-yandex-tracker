namespace ConsoleApp
{
    public class Settings
    {
        public string Queue { get; set; }
        public string Token { get; set; }
        public string OrgNumber { get; set; }
        public string FileName { get; set; }

        public override string ToString()
        {
            return $"Queue: {Queue}\nToken: {Token}\n" +
                $"OrgNumber: {OrgNumber}\nFileName: {FileName}";
        }
    }
}