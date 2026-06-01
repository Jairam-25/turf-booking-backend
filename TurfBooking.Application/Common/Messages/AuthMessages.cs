namespace Application.Common.Messages
{

    public static class AuthMessages
    {
        public const string RegisterSuccess =
            "User registered successfully";

        public const string EmailAlreadyExists =
            "Email already exists";

        public const string InvalidCredentials =
            "Invalid email or password";

        public const string UserNotFound =
            "User not found";

        public const string InvalidToken =
            "Invalid token";

        public const string TokenExpired =
            "Token expired";

        public const string IncorrectEmailOrPassword =
            "Incorrect email or password";

        public const string PasswordResetSuccess =
            "Password reset successful";

        public const string ResetLinkSent =
            "Password reset link sent";

        public const string LoginMaxAttempt =
            "Maximum login attempts exceeded";
    }
}