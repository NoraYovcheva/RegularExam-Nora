using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using RegularExam.Models;

namespace RegularExam
{
    [TestFixture]
    public class StoryApiTests
    {
        private RestClient client;
        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        private static string createStoryId;
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI3MjM0MzE2ZC1iMzllLTQwYTItYmE1Yy1jMGVjZDZmMDYyMjMiLCJpYXQiOiIwOC8xNi8yMDI1IDA2OjExOjU0IiwiVXNlcklkIjoiYjJlMzg4NDMtZjg1OC00ZTY1LThkZGQtMDhkZGRiMWExM2YzIiwiRW1haWwiOiJub3JhMTJAbm9yYTEyLmNvbSIsIlVzZXJOYW1lIjoibm9yYTEyMTIiLCJleHAiOjE3NTUzNDYzMTQsImlzcyI6IlN0b3J5U3BvaWxfQXBwX1NvZnRVbmkiLCJhdWQiOiJTdG9yeVNwb2lsX1dlYkFQSV9Tb2Z0VW5pIn0.Ct9sIWL8hDhWSMapqNlEWtN5G-aufN9U0HiR--UTIy8";

        private const string LoginUserName = "nora1212";
        private const string LoginPassword = "nene1212";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginUserName, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string user, string password)
        {
            var testClient = new RestClient(BaseUrl);
            var request = new RestRequest($"/api/User/Authentication", Method.Post);

            request.AddJsonBody(new
            {
                UserName = user,
                Password = password
            });

            var response = testClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
               var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
               var token = content.GetProperty("accessToken").GetString();    
                
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate user. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }

        [Test, Order(1)]
        public void CreateNewStory_WithRequiredFields_ShouldReturnCreated()
        {
            var storyRequest = new StoryDTO
            {
                Title = "Story",
                Description = "This is a test stroy description.",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createStoryId = createResponse.GetProperty("storyId").GetString() ?? string.Empty;

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createStoryId, Is.Not.Null.And.Not.Empty, "Story ID should not be null or empty");
            Assert.That(response.Content, Does.Contain("Successfully created!"));
        }

        [Test, Order(2)]
        public void EditStory_ShouldReturnOk()
        {
            var editReques = new StoryDTO
            {
                Title = "Edited Story",
                Description = "This is an edited test story description.",
                Url = ""
            };           

            var request = new RestRequest($"/api/Story/Edit/{createStoryId}", Method.Put);
            request.AddQueryParameter("storyId", createStoryId);
            request.AddJsonBody(editReques);
            var response = client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Response status should be OK");
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStorySpoiler_ShouldReturnList()
        {
            var request = new RestRequest($"/api/Story/All", Method.Get);
            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Response status should be OK");

            var stories = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(stories, Is.Not.Null.And.Not.Empty, "Story ID should not be null or empty");
        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Response status should be OK");
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStoryWithoutRequiredFields_ShouldReturnBadRequest()
        {
            var story = new
            {
                Title = "",
                Description = ""
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Response status should be Bad Request");
        }

        [Test, Order(6)]
        public void EditNonExistingStory_ReturnNotFound()
        {    
            var changes = new
            {
                Title = "New Story",
                Description = "This is a new story description"
            };

            var request = new RestRequest("/api/Story/Edit/NewStory", Method.Put);
            request.AddJsonBody(changes);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingStory_ReturnBadRequest()
        {
            var request = new RestRequest("/api/Story/Delete/NewStory", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}