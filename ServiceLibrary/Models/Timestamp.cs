using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class Timestamp
    {
        [Key]
        public int Id { get; set; }
        public DateTime? TsIn { get; set; }
        public DateTime? TsOut { get; set; }

        public decimal? CashInDrawerAmount { get; set; } = 0;
        public decimal? CashOutDrawerAmount { get; set; } = 0;
        public decimal? WithdrawnDrawerAmount { get; set; } = 0;

        public bool IsTrainMode { get; set; } = false;
        public required User ManagerIn { get; set; }
        public User? ManagerOut { get; set; }
    }
}
