using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Utility
{
    public class ValidEmailDomainAttribute : ValidationAttribute
    {
        private readonly string allowedDomain;

        public ValidEmailDomainAttribute(string allowedDomain)
        {
            this.allowedDomain = allowedDomain;
        }

        public override bool IsValid(object? value)
        {
            var strings = value.ToString().Split("@");

            return allowedDomain == strings[1];
        }
    }
}