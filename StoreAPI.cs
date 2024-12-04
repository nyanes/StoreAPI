using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Identity;
using Stores;

using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Azure.Data.Tables.Models;
using System.Net.Http;
using System.Threading.Tasks;


namespace StoreAPI
{
    public class StoreAPI
    {
        private readonly ILogger<StoreAPI> _logger;
        private const string StorageAccountConnection = "AzureWebJobsStorage"; // Connection string in appsettings
        private const string TableName = "stores";
        private readonly IConfiguration _configuration;
        public StoreAPI(ILogger<StoreAPI> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function("store")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            
            var method = req.Method.ToLower();
            var tableClient = await GetTableClientAsync();

            if (method == "post")
            {
                _logger.LogInformation("Processing a POST request to save store data.");
                return await HandlePostAsync(req, tableClient);
            }
            else if (method == "get")
            {
                _logger.LogInformation("Processing a GET request to retrieve store data.");
                return await HandleGetAsync(req, tableClient);
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await response.WriteStringAsync("Invalid HTTP method. Use 'GET' or 'POST'.");
            return response;
        }

        private async Task<HttpResponseData> HandlePostAsync(HttpRequestData req, TableClient tableClient)
        {
            try
            {
                // Read request body and deserialize
                string requestBody = await req.ReadAsStringAsync();
                var store = JsonConvert.DeserializeObject<Store>(requestBody);

                if (store == null)
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Invalid store data.");
                    return badResponse;
                }

                // Check if store number already exists
                var existingEntity = tableClient.Query<TableEntity>(
                    filter: $"PartitionKey eq '{store.StoreNo}'").FirstOrDefault();

                if (existingEntity != null)
                {
                    var conflictResponse = req.CreateResponse(System.Net.HttpStatusCode.Conflict);
                    await conflictResponse.WriteStringAsync($"A store with StoreNo '{store.StoreNo}' already exists.");
                    return conflictResponse;
                }

                // Add new store entity to Azure Table Storage
                var storeEntity = new TableEntity(store.StoreNo, store.Name)
        {
            { "Name", store.Name },
            { "Active", store.Active },
            { "CountryCode", store.Country.Code },
            { "CountryDescription", store.Country.Description },
            { "Phone", store.Phone },
            { "Email", store.Email },
            { "ManagerName", store.Manager.Name },
            { "ManagerEmail", store.Manager.Email },
            { "VisitGeoPosLon", store.VisitGeoPos.Lon },
            { "VisitGeoPosLat", store.VisitGeoPos.Lat },
            { "VisitAddressStoreName", store.VisitAddress.StoreName },
            { "VisitAddress", store.VisitAddress.Address },
            { "City", store.VisitAddress.City },
            { "Zipcode", store.VisitAddress.Zipcode },
            { "OpeningHours", JsonConvert.SerializeObject(store.OpeningHours) }
        };

                await tableClient.AddEntityAsync(storeEntity);

                var okResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await okResponse.WriteStringAsync("Store information saved successfully.");
                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving store data.");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An error occurred while processing the request.");
                return errorResponse;
            }
        }


        private async Task<HttpResponseData> HandleGetAsync(HttpRequestData req, TableClient tableClient)
        {
            try
            {
                // Retrieve all stores using LINQ
                List<TableEntity> queryResults = tableClient.Query<TableEntity>().ToList();                
                IEnumerable<Store> stores = queryResults.Select(entity => new Store
                {
                    StoreNo = entity.PartitionKey,
                    Name = entity.GetString("Name"),
                    Active = entity.GetBoolean("Active") ?? false,
                    Country = new Country
                    {
                        Code = entity.GetString("CountryCode"),
                        Description = entity.GetString("CountryDescription")
                    },
                    Phone = entity.GetString("Phone"),
                    Email = entity.GetString("Email"),
                    Manager = new Manager
                    {
                        Name = entity.GetString("ManagerName"),
                        Email = entity.GetString("ManagerEmail")
                    },
                    VisitGeoPos = new VisitGeoPos
                    {
                        Lon = entity.GetDouble("VisitGeoPosLon") ?? 0,
                        Lat = entity.GetDouble("VisitGeoPosLat") ?? 0
                    },
                    VisitAddress = new VisitAddress
                    {
                        StoreName = entity.GetString("VisitAddressStoreName"),
                        Address = entity.GetString("VisitAddress"),
                        City = entity.GetString("City"),
                        Zipcode = entity.GetString("Zipcode")
                    },
                    OpeningHours = JsonConvert.DeserializeObject<List<OpeningHour>>(entity.GetString("OpeningHours") ?? "[]")
                });

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(stores);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving store data.");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An error occurred while processing the request.");
                return errorResponse;
            }
        }

        private async Task<TableClient> GetTableClientAsync()
        {
            // Retrieve the connection string from environment variables
            var connectionString = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Storage account connection string is not configured.");
            }

            // Create the TableServiceClient using the connection string
            var tableServiceClient = new TableServiceClient(connectionString);

            // Get the TableClient for the specific table
            var tableClient = tableServiceClient.GetTableClient("stores");

            // Create the table if it does not exist
            await tableClient.CreateIfNotExistsAsync();

            return tableClient;
        }

    }
}
