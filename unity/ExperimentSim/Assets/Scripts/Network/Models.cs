using System;

[Serializable]
public class RegisterRequest
{
    public string name;
    public string email;
    public string password;
}

[Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[Serializable]
public class UserDto
{
    public int id;
    public string name;
    public string role;
}

[Serializable]
public class AuthResponse
{
    public string token;
    public UserDto user;
}
