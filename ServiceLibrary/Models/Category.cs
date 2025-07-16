using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceLibrary.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        public required string CtgryName { get; set; }
    }
}
