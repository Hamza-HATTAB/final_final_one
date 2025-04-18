using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserModels;
using ThesesModels;

namespace ContactsModels
{
    public class Contacts
    {
        [Key] // 📌 Clé primaire
        public int Id { get; set; }

        [Required] // 📌 Clé étrangère vers l'expéditeur (utilisateur)
        public int ExpediteurId { get; set; }

        [ForeignKey("ExpediteurId")]
        public virtual User Expediteur { get; set; }

        [Required] // 📌 Clé étrangère vers la thèse concernée
        public int TheseId { get; set; }

        [ForeignKey("TheseId")]
        public virtual Theses These { get; set; }

        [Required] // 📌 Clé étrangère vers le destinataire (si c'est un message privé)
        public int DestinataireId { get; set; }

        [ForeignKey("DestinataireId")]
        public virtual User Destinataire { get; set; }

        [Required] // 📌 Contenu du message
        public string Message { get; set; }

        [Required] // 📌 Date d'envoi (timestamp automatique)
        public DateTime DateEnvoi { get; set; } = DateTime.Now;
    }
}
