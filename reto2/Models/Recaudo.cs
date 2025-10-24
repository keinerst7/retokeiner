using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace reto2.Models
{
    [Table("recaudos", Schema = "reto_keiner")]
    public class Recaudo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Estacion { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Sentido { get; set; }

        [Required]
        public DateTime Hora { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Categoria { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorTabulado { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}