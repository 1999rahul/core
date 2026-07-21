namespace UnitTesting.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendOrderConformationEmailAsync(string toEmail, int orderId)
        {
            await Task.FromResult(() =>
            {
                Console.WriteLine($"Confirmation Email sent to {toEmail} for Order Id {orderId}");
            });
        }
    }
}
