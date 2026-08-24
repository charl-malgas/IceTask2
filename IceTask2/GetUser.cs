using System.Net;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace UserManagementFunctions.Functions
{
    public class GetUser
    {
        private readonly ILogger _logger;

        public GetUser(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetUser>();
        }

        [Function("GetUser")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetUser function received a request.");

            try
            {
                // Get the user ID from the URL
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                string? userId = query["id"];

                // Check that an ID was provided
                if (string.IsNullOrWhiteSpace(userId))
                {
                    var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        message = "Please provide a user ID."
                    });

                    return badRequest;
                }

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

                // Find the user
                TableEntity entity = await tableClient.GetEntityAsync<TableEntity>(
                    "Users",
                    userId);

                // Return user information
                var response = req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    id = entity.RowKey,
                    name = entity.GetString("Name"),
                    email = entity.GetString("Email"),
                    age = entity.GetInt32("Age")
                });

                return response;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);

                await notFound.WriteAsJsonAsync(new
                {
                    message = "User not found."
                });

                return notFound;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user.");

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