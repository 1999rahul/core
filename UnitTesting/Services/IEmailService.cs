using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTesting.Services
{
    public interface IEmailService
    {
        Task SendOrderConformationEmailAsync(string toEmail, int orderId);
    }
}
