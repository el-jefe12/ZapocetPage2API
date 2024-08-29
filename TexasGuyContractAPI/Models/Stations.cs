namespace TexasGuyContractAPI.Models
{
    public class Stations
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public int FrequencyInMinutes { get; set; } // How often to send JSONs
    }
}
