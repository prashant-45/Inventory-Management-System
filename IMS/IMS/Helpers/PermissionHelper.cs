namespace IMS.Helpers
{
    public static class PermissionHelper
    {
        public static bool HasPermission(string permissionName)
        {
            var user = new HttpContextAccessor().HttpContext?.User;

            if (user == null || !user.Identity.IsAuthenticated)
                return false;

            return user.Claims.Any(c => c.Type == "Permission" && c.Value == permissionName);
        }
    }

}
