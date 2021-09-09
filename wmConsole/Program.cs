using log4net.Config;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Xml;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using WireMock.Util;

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
            _server = WireMockServer.Start(new WireMockServerSettings
            {
                Urls = new[] { "http://*:9091/", "http://*:9092/" },
                StartAdminInterface = true,
                //"Authorization: Basic YWRtaW46cGFzcw==" (base64 for admin:pass)
                AdminUsername = "admin",
                AdminPassword = "pass",
                ReadStaticMappings = true,
                Logger = new WireMockLog4NetLogger()
            });

            _server.Given(Request.Create().WithPath("/getJson")
                .UsingGet()
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                ).RespondWith(
                Response.Create()
                    .WithStatusCode(System.Net.HttpStatusCode.OK)
                    .WithHeader("content-type", "application/json; charset=utf-8")
                    .WithBody(@"{ ""result"": ""jsonbodytest"" }")
                );

            _server.Given(Request.Create().WithPath("/getEmployee")
                .UsingGet()
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                ).RespondWith(Response.Create()
                .WithStatusCode(System.Net.HttpStatusCode.OK)
                .WithHeader("content-type", "application/json; charset=utf-8")
                .WithBody("{ " +
                          "\"employeeID\" : 12345," +
                          "\"employeeName\" : \"Preethi\"}")
                );

            _server.Given(Request.Create().WithPath("/getEmployee/*")
             .UsingGet()
             .WithHeader("Content-Type", "application/json; charset=utf-8")
             ).RespondWith(Response.Create()
             .WithHeader("content-type", "application/json; charset=utf-8")
             .WithBody("{" +
                       "\"employeeID\" : \"12345\"," +
                       "\"employeeName\" : \"Jacob\"}")
             );

            _server.Given(Request.Create().WithPath("/createUser")
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithBody(new JsonMatcher(new
                {
                    name = "Pao",
                    age = 18,
                    sex = "Male"
                }))
                .UsingPost()
                )
                .AtPriority(1)
                .RespondWith(Response.Create().WithBody("Create Success!").WithDelay(10).WithTransformer(true));


            _server.Given(Request.Create().WithPath("/getObject2")
             .UsingGet()
             .WithHeader("Content-Type", "application/json; charset=utf-8")
             ).RespondWith(Response.Create()
             .WithStatusCode(System.Net.HttpStatusCode.OK)
             .WithHeader("content-type", "application/json; charset=utf-8")
             //.WithBody("{object : [" +
             //          "\"employeeID\" : 12345," +
             //          "\"employeeName\" : \"Preethi\"]}")
             .WithBody("{\"employeeDetails\":[" +
                       "\"employeeID\" : \"12345\"" +
                       "\"employeeName\" : \"Preethi\"]}")
             );
        }

        private static void StopWiremock()
        {
            _server.Stop();
        }
    }
}
