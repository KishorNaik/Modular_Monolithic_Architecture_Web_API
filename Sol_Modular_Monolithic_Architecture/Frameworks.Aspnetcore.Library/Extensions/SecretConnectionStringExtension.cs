using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frameworks.Aspnetcore.Library.Extensions
{
    public static class SecretConnectionStringServiceExtension
    {
        public static string GetSecretConnectionString(this IConfiguration configuration, string dbSection)
        {
            // Get ConnectionString from the AppSetting File.
            var conStrBuilder = new SqlConnectionStringBuilder(configuration?.GetConnectionString(dbSection));

            // Get UserId and Password from the User secret.json
            SecretModel? secretModel = configuration?.GetSection("DB")?.Get<SecretModel>();

            // Append UserID and Password in the Connection String
            conStrBuilder.UserID = secretModel.UserID;
            conStrBuilder.Password = secretModel.Password;

            // Get Final Connectionstring
            var connection = conStrBuilder.ConnectionString;

            return connection;
        }
    }

    public class SecretModel
    {
        public String UserID { get; set; }

        public String Password { get; set; }
    }
}