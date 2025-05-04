using System.Data.Common;
using Dapper;
using Org.BouncyCastle.Bcpg.Sig;
using SHAW.DataAccess.Util;

public enum RoleType
{
    Student,
    Counselor
}

public static class EnumFactory
{
    private static bool RoleTypesLoaded = false;
    private static object LockRoleTypesLoaded = new object();

    private static Dictionary<string, int> GetEnumDict<TEnum>() where TEnum : Enum
    {
        string[] keys = Enum.GetNames(typeof(TEnum));
        int[] values = (int[])Enum.GetValues(typeof(TEnum));
        Dictionary<string, int> dict = new Dictionary<string, int>();
        for(int i = 0; i < keys.Length; i++)
        {
            dict.Add(keys[i], values[i]);
        }

        return dict;
    }

    private static DbConnection CreateDbConnection(IHostEnvironment env)
    {
        return new AutoDbConnection(DbConnectionFactory.CreateDbConnection(env));
    }

    private static void EnsureRolesInDatabase(IHostEnvironment env)
    {
        // Ensure database state reflects RoleType enum
        lock(LockRoleTypesLoaded)
        {
            if(RoleTypesLoaded)
            {
                return;
            }

            using(var connection = CreateDbConnection(env))
            {
                List<RoleModel> roles = connection.Query<RoleModel>(
                    "SELECT * FROM roles;"
                ).ToList();
                var dict = GetEnumDict<RoleType>();

                bool needToLoad = false;
                for(int i = 0; i < dict.Keys.Count; i++)
                {
                    if(i >= roles.Count)
                    {
                        needToLoad = true;
                        break;
                    }
                    
                    string key = dict.Keys.ElementAt(i);
                    RoleModel role = roles.ElementAt(i);
                    if(!role.Name.Equals(key))
                    {
                        needToLoad = true;
                        break;
                    }

                    if(dict.GetValueOrDefault(key) != role.Id)
                    {
                        needToLoad = true;
                        break;
                    }
                }

                if(!needToLoad)
                {
                    return;
                }

                // TODO insert into database
            }
        }
    }

    public static RoleType GetRoleType(IHostEnvironment env, string str)
    {
        EnsureRolesInDatabase(env);

        str = str.ToLower();
        switch(str)
        {
            case "student":
                return RoleType.Student;
            case "counselor":
                return RoleType.Counselor;
            default:
                throw new Exception("Role type not recognized");
        }
    }

    private class RoleModel
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
    }
}