using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;

namespace JobTriggerPlatform.Tests.Helpers
{
    public static class MockUserHelper
    {
        public static ClaimsPrincipal CreateClaimsPrincipal(string userId, string userName, string email, string[] roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            return new ClaimsPrincipal(identity);
        }

        public static ControllerContext CreateControllerContext(ClaimsPrincipal user)
        {
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(m => m.User).Returns(user);

            return new ControllerContext
            {
                HttpContext = httpContext.Object
            };
        }
    }
}
