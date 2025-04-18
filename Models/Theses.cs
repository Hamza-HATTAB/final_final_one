using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserModels;

namespace ThesesModels
{
    public class Theses
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Titre { get; set; }

        [Required]
        [MaxLength(255)]
        public string Auteur { get; set; }

        [Required]
        [MaxLength(100)]
        public string Speciality { get; set; }

        [Required]
        public TypeThese Type { get; set; }

        [Required]
        public string MotsCles { get; set; }

        [Required]
        public DateTime Annee { get; set; }

        [Required]
        public string Resume { get; set; }

        [Required]
        public string Fichier { get; set; }

        // 🔹 Clé étrangère pour l'utilisateur
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User Utilisateur { get; set; }
        
        [NotMapped]
        public bool IsFavorite { get; set; }
    }

    public enum TypeThese
    {
        Doctorat,
        Master
    }
}
