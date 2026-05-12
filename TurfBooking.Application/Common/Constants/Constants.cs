using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Constants;

public static class AppConstants
{
    public const int RefreshTokenExpiryDays = 7;
    public const int MaxLoginAttempts = 5;
    public const int LockoutMinutes = 15;
    public const int ExpiryMinutes = 30;
    public const int ByteNumber = 64;
}
