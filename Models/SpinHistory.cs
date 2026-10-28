using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanPC.Models
{
    public class SpinHistory
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public string PrizeName { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public string? GiftCode { get; set; }
        public bool IsClaimed { get; set; } = false;
    }
}