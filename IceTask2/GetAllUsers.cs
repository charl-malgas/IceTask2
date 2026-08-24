using System.Net;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace UserManagementFunctions.Functions
{
    public class GetAllUsers
    {
        private readonly ILogger _logger;

        public GetAllUsers(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetAllUsers>();
        }

        [Function("GetAllUsers")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetAllUsers function received a request.");

            try
            {
                // Get Azure Storage connection string
                string? connectionString =
                    Environment.GetEnvironmentVariable("AzureTableStorage");

                if (string.IsNullOrEmpty(connectionString))
                {
                    var configError = req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                    await configError.WriteAsJsonAsync(new
                    {
                        message = "AzureTableStorage connection string is missing."
                    });

                    return configError;
                }

                // Connect to Azure Table Storage
                TableServiceClient serviceClient =
                    new TableServiceClient(connectionString);

                TableClient tableClient =
                    serviceClient.GetTableClient("Users");

                // Create a list for all users
                var users = new List<object>();

                // Retrieve all entities from the Users table
                await foreach (TableEntity entity in tableClient.QueryAsync<TableEntity>())
                {
                    users.Add(new
                    {
                        id = entity.RowKey,
                        name = entity.GetString("Name"),
                        email = entity.GetString("Email"),
                        age = entity.GetInt32("Age")
                    });
                }

                // Return all users
                var response = req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(users);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users.");

                var response = req.CreateResponse(
                    HttpStatusCode.InternalServerError);

                await response.WriteAsJsonAsync(new
                {
                    message = "An unexpected error occurred."
                });

                return response;
            }
        }
    }
}