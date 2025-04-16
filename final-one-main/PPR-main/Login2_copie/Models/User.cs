using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThesesModels;
using ContactsModels;

namespace UserModels
{
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

        // 📌 Relations avec d'autres entités
        public virtual ICollection<Theses> Theses { get; set; } = new List<Theses>(); // Les thèses publiées
        public virtual ICollection<Contacts> ContactsEnvoyes { get; set; } = new List<Contacts>(); // Contacts envoyés
        public virtual ICollection<Contacts> ContactsRecus { get; set; } = new List<Contacts>(); // Contacts reçus
    }

    // 📌 Enumération des rôles d'utilisateur
    public enum RoleUtilisateur
    {
        Admin,
        Etudiant,
        SimpleUser
    }
}
