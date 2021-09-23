using log4net.Config;
using Microsoft.Extensions.Configuration;
//using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Xml;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using WireMock.Util;
using wmConsole.model;

namespace wmConsole
{
    class Program
    {
        #region Nested classes to support running as service
        public const string ServiceName = "WireMock.Net.Service";

        public class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = Program.ServiceName;
            }

            protected override void OnStart(string[] args)
            {
                StartWiremock();
            }

            protected override void OnStop()
            {
                Program.StopWiremock();
            }
        }
        #endregion

        private static WireMockServer _server;

        static void Main(string[] args)
        {
            //Setting the current directory explicitly is required if the application is running as Windows Service, 
            //as the current directory of a Windows Service is %WinDir%\System32 per default.
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));

            // running as service
            if (!Environment.UserInteractive)
            {
                using (var service = new Service())
                {
                    ServiceBase.Run(service);
                }
            }
            else
            {
                Console.WriteLine("Press any key to try wiremock");
                // running as console app
                StartWiremock();

                Console.WriteLine("\n Press any key to stop...");
                while (true)
                {
                    var input = Console.ReadLine();
                    if (input == "stop") { StopWiremock(); break; }
                }
            }
        }

        private static void StartWiremock()
        {
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            _server = WireMockServer.Start(new WireMockServerSettings
            {
                Urls = new[] { "http://*:9091/", "http://*:9092/" },
                StartAdminInterface = true,
                //"Authorization: Basic YWRtaW46cGFzcw==" (base64 for admin:pass)
                AdminUsername = "admin",
                AdminPassword = "pass",
                ReadStaticMappings = true,
                Logger = new WireMockLog4NetLogger(),
                AllowBodyForAllHttpMethods = true,
                AllowOnlyDefinedHttpStatusCodeInResponse = true,
            });

            //API Get
            _server.Given(Request.Create()
                .WithPath("/getapi")
                .UsingGet()
                ).RespondWith(
                Response.Create()
                .WithBody("Hello World!"));

            _server.Given(Request.Create().WithPath("/getqrystring")
            .UsingGet()
            .WithParam("name", new ExactMatcher("Joe"))
            ).RespondWith(
            Response.Create()
            .WithBody("{{request.query.name}}").WithTransformer()
            );

            _server.Given(Request.Create().WithPath("/getStudent/*")
              .UsingGet()
              ).RespondWith(
              Response.Create()
                  .WithBody("{{request.PathSegments.[1]}}").WithTransformer()
              );


            var datas = System.IO.File.ReadAllText("json/json01.json");

            _server.Given(Request.Create().WithPath("/GetDataInFile")
               .UsingGet()
               ).RespondWith(
               Response.Create()
                   .WithHeader("Access-Control-Allow-Origin", "*")
                   .WithBody(datas).WithDelay(10).WithTransformer(true));

            //API Post
            _server.Given(Request.Create().WithPath("/CreateEmployee")
                .UsingPost())
                .RespondWith(Response.Create()
                .WithBody("Create Success"));

            _server.Given(Request.Create().WithPath("/CreateEmployeeByName")
                .UsingPost()
                .WithBody(new JsonPartialMatcher(
                    JsonSerializer.Serialize(new { Name = "Joe" })
                    , true)))
                .RespondWith(Response.Create().WithBody(
              "Create Success!").WithTransformer(true));


            _server.Given(Request.Create().WithPath("/CreateStudent")
                .UsingPost())
                .RespondWith(Response.Create()
                .WithHeader("Content-Type", "application/json")
                .WithBody(("Create Success {{request.bodyAsJson.name }}"))
                .WithTransformer(true));

        }

        private static void StopWiremock()
        {
            _server.Stop();
        }
    }
}
