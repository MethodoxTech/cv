using Microsoft.AspNetCore.Authentication;

namespace ChangeVersion.Server
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// HTTP header where clients must supply the API key.
        /// </summary>
        public string HeaderName { get; set; } = "X-Api-Key";
        /// <summary>
        /// The expected key value (loaded from configuration).
        /// </summary>
        public string Key { get; set; } = string.Empty;
    }
}
