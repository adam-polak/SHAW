public enum RoleType
{
    Student,
    Counselor
}

public static class EnumFactory
{
    public static RoleType GetRoleType(string str)
    {
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
}