using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace EmployeeManagement.Security
{
    public class CanEditOnlyOtherAdminRolesAndClaimsHandler:AuthorizationHandler<ManageAdminRolesAndClaimsRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CanEditOnlyOtherAdminRolesAndClaimsHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor; 
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ManageAdminRolesAndClaimsRequirement requirement)
        {
            string? adminIdBeingEdited = _httpContextAccessor.HttpContext.GetRouteValue("id").ToString();
            if (adminIdBeingEdited is null)
            {
                return Task.CompletedTask;
            }
            string? loggedInAdminId = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
          
            if (context.User.IsInRole("Admin") &&
                context.User.HasClaim(x => x.Type == "Edit Role" && x.Value == "true") &&
                loggedInAdminId.ToLower() != adminIdBeingEdited.ToLower())
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;

        }

    }
}
