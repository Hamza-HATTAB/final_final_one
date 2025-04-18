using System;

namespace DataGridNamespace
{
    public static class AppConfig
    {
        // Firebase
        public const string FirebaseApiKey = "AIzaSyBlIITlxpM3I_hova-rMnnL8JcFlBLpHFs";
        public const string FirebaseAuthBaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts";

        // Cloud SQL (via Cloud SQL Auth Proxy)
        public const string CloudSqlConnectionString = "Server=127.0.0.1;Port=3306;Database=gestion_theses;Uid=thesis_app_user;Pwd=123;SslMode=None;";

        // Cloud Storage
        public const string StorageBucket = "thesis-manager-files-hr-5173";
        public const string GenerateUploadUrlEndpoint = "https://us-central1-thesis-manager-backend.cloudfunctions.net/generateUploadUrl";
        public const string GenerateReadUrlEndpoint = "https://us-central1-thesis-manager-backend.cloudfunctions.net/generateReadUrl";
    }
}