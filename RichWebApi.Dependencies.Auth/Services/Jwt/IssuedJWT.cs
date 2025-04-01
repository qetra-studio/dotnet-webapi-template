namespace RichWebApi.Services.Jwt;

public sealed record IssuedJWT(string Type, string Token, TimeSpan ExpiresIn);