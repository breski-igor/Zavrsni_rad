using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trainingAttendanceTracker.Models
{
    public enum PaymentType
    {
        Expense = 0,
        Income = 1
    }
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        [Range(0.01, 10000)]
        [Column(TypeName = "decimal(10,2)")] 
        public decimal Amount { get; set; }

        [Required]
        public PaymentType PaymentType { get; set; } = PaymentType.Expense;

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Category { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}