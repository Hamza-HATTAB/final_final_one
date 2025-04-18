using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThesesModels;
using ContactsModels;

namespace UserModels
{
    public enum RoleUtilisateur
    {
        Admin,
        SimpleUser,
        Etudiant
    }

    public class User
    {
        [Key] // Clé primaire pour la base de données
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nom { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } // Doit être haché avant stockage

        [Required]
        public RoleUtilisateur Role { get; set; } // Enum pour différencier les types d'utilisateurs

        // Firebase User ID (link to Firebase Auth)
        [MaxLength(128)] // Max length for Firebase UID
        public string FirebaseUid { get; set; }

        // Reference to the profile picture object in Cloud Storage
        [MaxLength(255)] // Example max length, adjust as needed for GCS object names
        public string ProfilePicRef { get; set; } // Nullable, stores the GCS object name (e.g., "profile_pics/firebase_uid.jpg")

        // 📌 Relations avec d'autres entités
        public virtual ICollection<Theses> Theses { get; set; } = new List<Theses>(); // Les thèses publiées
        public virtual ICollection<Contacts> ContactsEnvoyes { get; set; } = new List<Contacts>(); // Contacts envoyés
        public virtual ICollection<Contacts> ContactsRecus { get; set; } = new List<Contacts>(); // Contacts reçus
    }
}
