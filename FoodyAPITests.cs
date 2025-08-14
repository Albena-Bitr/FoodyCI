using System.Net;
using System.Text.Json;
using FoodyAPI.Models;
using RestSharp;
using RestSharp.Authenticators;
using NUnit.Framework;

namespace FoodyAPI
{
    public class FoodyAPITests : IDisposable
    {
        private RestClient client;
        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";
        private const string Username = "Lemonade";
        private const string Password = "123asd";
        private static string lastCreatedFoodId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetAccessToken(Username, Password);

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetAccessToken(string username, string password)
        {
            var authClient = new RestClient(BaseUrl);
            var login = new LoginBody
            {
                Username = Username,
                Password = Password
            };

            var request = new RestRequest("/api/User/Authentication");
            request.AddJsonBody(login);
            var response = authClient.Execute(request, Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var accessToken = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new InvalidOperationException("Access Token is null or empty");
                }
                return accessToken;
            }
            else
            {
                throw new InvalidOperationException($"Authentication failed with {response.StatusCode} and {response.Content}");
            }
        }

        [Test, Order(1)]
        public void FoodyTest_CreateNewFood_WithAllRequiredFields_ShouldSucceed()
        {
            // Arrange
            string name = "New Test Food";
            string description = "Test description";

            var newFood = new FoodDTO
            {
                Name = name,
                Description = description
            };
            var request = new RestRequest("/api/Food/Create");
            request.AddJsonBody(newFood);

            // Act
            var response = client.Execute(request, Method.Post);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Status code is {response.StatusCode}");

            Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

            var foodBody = JsonSerializer.Deserialize<ApiResponsDTO>(response.Content);

            Assert.That(foodBody, Is.Not.Null);

            lastCreatedFoodId = foodBody.FoodId.ToString();

            Assert.That(lastCreatedFoodId, Is.Not.Null);
        }

        [Test, Order(2)]
        public void FoodyTest_EditLastCreatedFoodName_ShouldSucceed()
        {
            // Arrange
            string newName = "New Edited Food";
            string expectedMessage = "Successfully edited";

            var request = new RestRequest($"/api/Food/Edit/{lastCreatedFoodId}");
            request.AddJsonBody(new[]
            {
             new
             {

                path = "/name",
                op = "replace",
                value = newName
             }
            });

            // Act
            var response = client.Execute(request, Method.Patch);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Status code is {response.StatusCode}");

            Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

            var foodBody = JsonSerializer.Deserialize<ApiResponsDTO>(response.Content);

            Assert.That(foodBody, Is.Not.Null);

            Assert.That(foodBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
        }

        [Test, Order(3)]
        public void FoodyTest_GetAllFoods_ShouldSucceed()
        {
            // Arrange        
            var request = new RestRequest("/api/Food/All");

            // Act
            var response = client.Execute(request, Method.Get);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Status code is {response.StatusCode}");

            Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

            var allFoods = JsonSerializer.Deserialize<ApiResponsDTO[]>(response.Content);

            Assert.That(allFoods, Is.Not.Null);

            Assert.That(allFoods.Length, Is.GreaterThan(0), "Returned items are less than one");
        }

        [Test, Order(4)]
        public void FoodyTest_RemoveFoodByID_ShouldSucceed()
        {
            // Arrange           
            string expectedMessage = "Deleted successfully!";

            var request = new RestRequest($"/api/Food/Delete/{lastCreatedFoodId}");
           
            // Act
            var response = client.Execute(request, Method.Delete);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Status code is {response.StatusCode}");

            Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

            var foodBody = JsonSerializer.Deserialize<ApiResponsDTO>(response.Content);

            Assert.That(foodBody, Is.Not.Null);

            Assert.That(foodBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
        }

        [Test, Order(5)]
        public void FoodyTest_CreateNewFood_WithInvalidData_ShouldFail()
        {
            // Arrange
            string name = "N";
            string description = "T";
           
            var newFood = new FoodDTO
            {
                Name = name,
                Description = description
            };
            var request = new RestRequest("/api/Food/Create");
            request.AddJsonBody(newFood);

            // Act
            var response = client.Execute(request, Method.Post);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Status code is {response.StatusCode}");                     

        }

        [Test, Order(6)]
        public void FoodyTest_EditnonExistingFood_ShouldFail()
        {
            // Arrange
            string wrongFoodId = "1234567";
            string newName = "New Edited Food";
            string expectedMessage = "No food revues...";

            var request = new RestRequest($"/api/Food/Edit/{wrongFoodId}");
            request.AddJsonBody(new[]
            {
             new
             {

                path = "/name",
                op = "replace",
                value = newName
             }
            });

            // Act
            var response = client.Execute(request, Method.Patch);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Status code is {response.StatusCode}");

            Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

            var foodBody = JsonSerializer.Deserialize<ApiResponsDTO>(response.Content);

            Assert.That(foodBody, Is.Not.Null);

            Assert.That(foodBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
        }

        [Test, Order(7)]
        public void FoodyTest_RemoveNonExistingFood_ShouldFail()
        {
            // Arrange
            string wrongFoodId = "123456";
            string expectedMessage = "Unable to delete this food revue!";

            var request = new RestRequest($"/api/Food/Delete/{wrongFoodId}");

            // Act
            var response = client.Execute(request, Method.Delete);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Status code is {response.StatusCode}");

            Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

            var foodBody = JsonSerializer.Deserialize<ApiResponsDTO>(response.Content);

            Assert.That(foodBody, Is.Not.Null);

            Assert.That(foodBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}