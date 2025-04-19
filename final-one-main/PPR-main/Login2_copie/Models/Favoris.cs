using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserModels;
using ThesesModels;

namespace FavorisModels
{
    public class Favoris
    {
        [Key] // 📌 Clé primaire
        public int Id { get; set; }

        [Required] // 📌 Clé étrangère vers l'utilisateur
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User Utilisateur { get; set; } // Relation avec User

        [Required] // 📌 Clé étrangère vers la thèse
        public int TheseId { get; set; }

        [ForeignKey("TheseId")]
        public virtual Theses These { get; set; } // Relation avec These
    }
}
