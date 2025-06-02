using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        public required string CtgryName { get; set; }
    }
}
