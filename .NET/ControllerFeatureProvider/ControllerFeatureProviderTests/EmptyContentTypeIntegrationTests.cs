using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ControllerFeatureProviderTests.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ControllerFeatureProviderTests
{
    public class EmptyContentTypeIntegrationTests
    {
        [Test]
        public async Task Controller_ShouldReturn200WithJsonOutput_IfNoContentTypeIsProvided()
        {
            // arrange
            var builder = new WebHostBuilder()
                .UseStartup<StartupWithEmptyContentTypeFormatter>();

            var server = new TestServer(builder);
            var client = server.CreateClient();

            //act
            var message = new HttpRequestMessage(HttpMethod.Post, "/200")
            {
                Content = new StringContent("{ 'amount': 1}", Encoding.UTF8)
            };
            message.Content.Headers.ContentType = null;
            var response = await client.SendAsync(message);

            //assert
            Stream responseBody = await response.Content.ReadAsStreamAsync();
            var result = new JsonSerializer().Deserialize<InputRequestData>(new JsonTextReader(new StreamReader(responseBody)));
            Assert.AreEqual(1, result.Amount);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        private class StartupWithEmptyContentTypeFormatter
        {
            public void ConfigureServices(IServiceCollection services)
            {
                _ = services ?? throw new ArgumentNullException(nameof(services));

                services
                    .AddMvc(o => { o.AddEmptyContentTypeFormatter(); })
                    .WithController<TestController>();
            }

            public void Configure(IApplicationBuilder app)
            {
                _ = app ?? throw new ArgumentNullException(nameof(app));
                app.UseMvc();
            }
        }

        private class TestController : Controller
        {
            [HttpPost("/200")]
            public IActionResult HandlePost200([FromBody]InputRequestData data)
            {
                return Json(data);
            }
        }

        private class InputRequestData
        {
            public uint Amount { get; set; }
        }
    }
}