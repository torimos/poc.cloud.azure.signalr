using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IoTEventsProcessor
{
    class AuthService
    {
        const string key = "d4ca54922bb0fb337591abd3e44453b95812802bece1081c39b7e731f5a65ed140293f7658fd5318192b0a75f201d8b372742909401b09eab3c0134555b7a0ea";

        public string Issuer { get; }
        public string Audience { get; }

        public AuthService(string issuer = null, string audience = null)
        {
            Issuer = issuer;
            Audience = audience;
        }

        public string GenerateToken(DateTime? expires = null, params Claim[] payload)
        {
            var securityKey = new SymmetricSecurityKey(ComputeShaHash(key));
            var signingCreds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var jwtPayload = payload != null ? new JwtPayload(payload) : new JwtPayload();
            var secToken = new JwtSecurityToken(
                issuer: Issuer, 
                audience: Audience, 
                claims: jwtPayload.Claims, 
                expires: expires ?? DateTime.UtcNow.AddHours(1), 
                signingCredentials: signingCreds);
            var handler = new JwtSecurityTokenHandler();
            var tokenString = handler.WriteToken(secToken);
            return tokenString;
        }

        public ClaimsPrincipal ValidateToken(string token, int clockSkewSeconds = 60)
        {
            try
            {
                SecurityToken validatedToken;
                var param = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.FromMinutes(clockSkewSeconds),
                    ValidIssuer = Issuer,
                    ValidateIssuer = Issuer != null,
                    ValidAudience = Audience,
                    ValidateAudience = Audience != null,
                    IssuerSigningKey = new SymmetricSecurityKey(ComputeShaHash(key)),
                };
                var handler = new JwtSecurityTokenHandler();
                var claims = handler.ValidateToken(token, param, out validatedToken);
                return claims;
            }
            catch(Exception ex)
            {
                return new ClaimsPrincipal();
            }
        }

        static byte[] ComputeShaHash(string rawData)
        {
            using (var hash = SHA256.Create())
            {
                return hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            }
        }
    }
}
