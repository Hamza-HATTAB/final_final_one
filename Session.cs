using System;
using System.Diagnostics;
using UserModels;

namespace DataGridNamespace
{
    public static class Session
    {
        private static int? currentUserId;
        private static string currentUserName;
        private static RoleUtilisateur? currentUserRole;
        private static string firebaseUserId;
        private static string currentUserToken;
        private static bool isInitialized = false;

        public static int CurrentUserId
        {
            get
            {
                if (!currentUserId.HasValue)
                {
                    Debug.WriteLine("WARNING: Attempted to access CurrentUserId when not set");
                    return -1; // Return a sentinel value instead of throwing exception
                }
                return currentUserId.Value;
            }
        }

        public static string CurrentUserName
        {
            get
            {
                if (string.IsNullOrEmpty(currentUserName))
                {
                    Debug.WriteLine("WARNING: Attempted to access CurrentUserName when not set");
                    return string.Empty;
                }
                return currentUserName;
            }
        }

        public static RoleUtilisateur CurrentUserRole
        {
            get
            {
                if (!currentUserRole.HasValue)
                {
                    Debug.WriteLine("WARNING: Attempted to access CurrentUserRole when not set");
                    return RoleUtilisateur.SimpleUser; // Default to Simple user for safety
                }
                return currentUserRole.Value;
            }
        }

        public static string FirebaseUserId 
        {
            get
            {
                if (string.IsNullOrEmpty(firebaseUserId))
                {
                    Debug.WriteLine("WARNING: Attempted to access FirebaseUserId when not set");
                    return string.Empty;
                }
                return firebaseUserId;
            }
        }

        public static string CurrentUserToken
        {
            get
            {
                if (string.IsNullOrEmpty(currentUserToken))
                {
                    Debug.WriteLine("WARNING: Attempted to access CurrentUserToken when not set");
                    return string.Empty;
                }
                return currentUserToken;
            }
        }

        public static bool IsLoggedIn => isInitialized && currentUserId.HasValue && currentUserId.Value > 0;

        public static void Initialize(int userId, string userName, RoleUtilisateur role, string fbUserId = null, string userToken = null)
        {
            try
            {
                if (userId <= 0)
                {
                    Debug.WriteLine($"WARNING: Attempted to initialize Session with invalid user ID: {userId}");
                }

                currentUserId = userId;
                currentUserName = userName ?? string.Empty;
                currentUserRole = role;
                firebaseUserId = fbUserId;
                currentUserToken = userToken;
                isInitialized = true;

                Debug.WriteLine($"Session initialized: User ID={userId}, Name={userName}, Role={role}, Firebase UID={fbUserId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing session: {ex.Message}");
                // Reset state in case of partial initialization
                Clear();
                throw;
            }
        }

        public static void Clear()
        {
            currentUserId = null;
            currentUserName = null;
            currentUserRole = null;
            firebaseUserId = null;
            currentUserToken = null;
            isInitialized = false;
            Debug.WriteLine("Session cleared");
        }
    }
}
