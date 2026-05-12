using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Messages;

public static class AuthMessages
{
    public const string InvalidCredentials =
        "Invalid credentials";

    public const string UserNotFound =
        "User not found";

    public const string EmailAlreadyExists =
        "Email already exists";

    public const string PasswordResetSuccess =
        "Password reset successful";

    public const string TokenExpired =
        "Token expired";

    public const string InvalidToken =
        "Invalid token";

    public const string RegisterSuccess =
        "User registered successfully";

    public const string LoginMaxAttempt =
        "You entered the wrong password more than 5 times. So account temporarily locked for 15 minutes.";

    public const string ResetTokenGenerated =
        "Reset token generated";
}
