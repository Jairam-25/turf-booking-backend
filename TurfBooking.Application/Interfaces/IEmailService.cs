using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(
        string toEmail,
        string userName);

        Task SendPasswordResetEmailAsync(
            string toEmail,
            string userName,
            string resetToken);
    }
}
