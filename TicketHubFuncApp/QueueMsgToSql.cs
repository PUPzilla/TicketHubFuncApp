using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TicketHubFuncApp
{
    public class QueueMsgToSql
    {
        private readonly string _connectionString;
        public QueueMsgToSql()
        {
            _connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("SQL connection string is not set in the environment variables.");
            }
        }

        [Function("QueueMsgToSql")]
        public async Task RunAsync(
            [QueueTrigger("tickethub", Connection = "AzureWebJobsStorage")] string message,
            FunctionContext context)
        {
            ILogger logger = context.GetLogger("QueueMsgToSql");
            logger.LogInformation($"Recieved queue-message: {message}");

            try
            {
                TicketPurchase? ticketPurchase = JsonConvert.DeserializeObject<TicketPurchase>(message);

                if (ticketPurchase == null)
                {
                    logger.LogError("Failed to deserialize message.");
                    return;
                }

                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"INSERT INTO TicketPurchases (ConcertId, Email, FirstName, LastName, Phone, Quantity, CreditCard, Expiration, SecurityCode, Address, City, Province, Country) VALUES (@ConcertId, @Email, @FirstName, @LastName, @Phone, @Quantity, @CreditCard, @Expiration, @SecurityCode, @Address, @City, @Province, @Country)";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ConcertId", ticketPurchase.ConcertId);
                        cmd.Parameters.AddWithValue("@Email", ticketPurchase.Email);
                        cmd.Parameters.AddWithValue("@FirstName", ticketPurchase.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", ticketPurchase.LastName);
                        cmd.Parameters.AddWithValue("@Phone", ticketPurchase.Phone);
                        cmd.Parameters.AddWithValue("@Quantity", ticketPurchase.Quantity);
                        cmd.Parameters.AddWithValue("@CreditCard", ticketPurchase.CreditCard);
                        cmd.Parameters.AddWithValue("@Expiration", ticketPurchase.Expiration);
                        cmd.Parameters.AddWithValue("@SecurityCode", ticketPurchase.SecurityCode);
                        cmd.Parameters.AddWithValue("@Address", ticketPurchase.Address);
                        cmd.Parameters.AddWithValue("@City", ticketPurchase.City);
                        cmd.Parameters.AddWithValue("@Provonce", ticketPurchase.Province);
                        cmd.Parameters.AddWithValue("@City", ticketPurchase.City);
                        cmd.Parameters.AddWithValue("@Country", ticketPurchase.Country);

                        await cmd.ExecuteNonQueryAsync();
                        logger.LogInformation($"Successfully inserted data into TicketPurchases table");
                    }
                }
            }
            catch (SqlException ex)
            {
                logger.LogError($"Exception: {ex.Message}. Stack Trace: {ex.StackTrace}");
            }
            catch (JsonException ex)
            {
                logger.LogError($"JSON exception: {ex.Message}. Stack Trace: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception: {ex.Message}. Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
