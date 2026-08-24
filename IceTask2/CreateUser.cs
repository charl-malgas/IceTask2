using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using UserManagementFunctions.Models;

namespace UserManagementFunctions.Functions
{
    public class CreateUser
    {
        private readonly ILogger _logger;

        public CreateUser(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CreateUser>();
        }

        [Function("CreateUser")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("CreateUser function received a request.");

            try
            {
                // Read JSON from request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                User? user = JsonSerializer.Deserialize<User>(
                    requestBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                // Validate user
                if (user == null ||
                    string.IsNullOrWhiteSpace(user.Id) ||
                    string.IsNullOrWhiteSpace(user.Name) ||
                    string.IsNullOrWhiteSpace(user.Email))
                {
                    var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);

                    await badRequest.WriteAsJsonAsync(new
                    {
                        message = "Invalid user data. Id, Name and Email are required."
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

                // Create/Get Users table
                TableClient tableClient =
                    serviceClient.GetTableClient("Users");

                await tableClient.CreateIfNotExistsAsync();

                // Create Azure Table entity
                TableEntity entity = new TableEntity
                {
                    PartitionKey = "Users",
                    RowKey = user.Id,

                    ["Name"] = user.Name,
                    ["Email"] = user.Email,
                    ["Age"] = user.Age
                };

                // Add user to table
                await tableClient.AddEntityAsync(entity);

                // Return successful response
                var response = req.CreateResponse(HttpStatusCode.Created);

                await response.WriteAsJsonAsync(new
                {
                    message = "User created successfully.",
                    user = user
                });

                return response;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Table Storage error.");

                var response = req.CreateResponse(
                    HttpStatusCode.InternalServerError);

                await response.WriteAsJsonAsync(new
                {
                    message = "Could not save the user.",
                    error = ex.Message
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error.");

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