﻿using System.ComponentModel.DataAnnotations;

namespace ServiceLibrary.Models
{
    public class SaleType
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Account { get; set; }
        public required string Type { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
