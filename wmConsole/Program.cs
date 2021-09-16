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

            _server.Given(Request.Create().WithPath("/getJson")
                .UsingGet()
                .WithParam("name", new ExactMatcher("Pao"))
                ).RespondWith(
                Response.Create()
                    .WithStatusCode(System.Net.HttpStatusCode.OK)
                    .WithHeader("content-type", "application/json; charset=utf-8")
                    .WithBody("{{request.query.name}}").WithTransformer()
                );

            _server.Given(Request.Create().WithPath("/getJson2/ncrt-home")
              .UsingGet()
              ).RespondWith(
              Response.Create()
                  .WithHeader("Access-Control-Allow-Origin", "*")
                  .WithHeader("content-type", "application/json; charset=utf-8")
                  .WithStatusCode(System.Net.HttpStatusCode.OK)
                  .WithBody(JsonSerializer.Serialize(new Studentmodel { Name = "{{request.PathSegments.[0]}}", Age = 1, Sex = "Male" }, options)).WithTransformer()
              );

            //var data = new Studentmodel() { Name = "Pao", Age = 1, Sex = "Male" };

            _server.Given(Request.Create().WithPath("/createUser")
                .UsingPost()
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithBody(new JsonPartialMatcher(
                    JsonSerializer.Serialize(new { Name="Pao",Sex="Male"})
                    , true))
                )
                .AtPriority(1)
                .RespondWith(Response.Create().WithBody(
              "Create Success!  {{request.bodyAsJson.Name}} {{request.bodyAsJson.Age}}  {{request.bodyAsJson.Sex}} {{request.body}}")
                .WithDelay(10).WithTransformer(true));

            //Rex
            _server.Given(Request.Create().WithPath("/reg")
                .UsingPost()
                .WithBody(new RegexMatcher("H*"))
              ).RespondWith(Response.Create().WithBody("Hello matched with RegexMatcher ").WithTransformer());


            _server.Given(Request.Create().WithPath("/getInternalServerError")
                .UsingGet()
                ).RespondWith(
                Response.Create()
                    .WithHeader("Access-Control-Allow-Origin", "*")
                    .WithStatusCode(System.Net.HttpStatusCode.InternalServerError).WithDelay(5000)
                );


            StreamReader r = new StreamReader("F:/git/Wiremock.Net/wmConsole/json/json01.json");
            string json = r.ReadToEnd();
            _server.Given(Request.Create().WithPath("/GetDataInFile")
               .UsingGet()
               ).RespondWith(
               Response.Create()
                   .WithHeader("Access-Control-Allow-Origin", "*")
                   .WithBody(json).WithDelay(10).WithTransformer(true));

            _server.Given(Request.Create().WithPath("/ExamStructure")
               .UsingGet()//.UsingPost
               .WithHeader()
               .WithBody(""))
               .RespondWith(Response.Create()
               .WithHeader("")
               .WithStatusCode("")
               .WithBody("")
               .WithDelay(10)
               .WithTransformer(true));

            //.RespondWith(Response.Create().WithHeader("Content-Type", "application/json").WithBody(JsonSerializer.Serialize(data2.Name.Adapt("{{request.body}}"))
            //    ).WithTransformer(true));


            //.RespondWith(Response.Create().WithBody(
            //        JsonSerializer.Serialize(

            //    //data, options)).WithTransformer(true));
            //    new Studentmodel
            //    {
            //        Name = "{{request.bodyAsJson.Name}}",
            //        Age = "{{request.bodyAsJson.Age}}",
            //        Sex = "{{request.bodyAsJson.Sex}}"
            //    }, options)).WithTransformer(true));


            //"Create Success!  {{request.bodyAsJson.name}} {{request.bodyAsJson.age}}  {{request.body}}").WithDelay(10).WithTransformer(true));

            ///xxse

            //_server.Given(Request.Create().WithPath("/add/*/*")
            //  .UsingGet()
            //  ).RespondWith(
            //  Response.Create()
            //      .WithHeader("Access-Control-Allow-Origin", "*")
            //      .WithBody("{{request.PathSegments.[1]}} * {{request.PathSegments.[2]}}").WithTransformer()
            //  );

            //_server.Given(Request.Create().WithPath("/getEmployee")
            // .UsingGet()
            // .WithHeader("Content-Type", "application/json; charset=utf-8")
            // ).RespondWith(Response.Create()
            // .WithStatusCode(System.Net.HttpStatusCode.OK)
            // .WithHeader("content-type", "application/json; charset=utf-8")
            // .WithBody("{ " +
            //           "\"employeeID\" : 12345," +
            //           "\"employeeName\" : \"Preethi\"}")
            // );


            //_server.Given(Request.Create().WithPath("/getJson3/*")
            //    .UsingGet()
            //    ).RespondWith(
            //    Response.Create()
            //        .WithHeader("Access-Control-Allow-Origin", "*")
            //        .WithBody("{" + "\"employeeID\" : \"{{request.PathSegments.[0]}}\"}").WithTransformer()
            //    );



            //_server.Given(Request.Create().WithPath("/kyc")
            //   .UsingGet()
            //   ).RespondWith(
            //   Response.Create()
            //       .WithHeader("Access-Control-Allow-Origin", "*")
            //       .WithStatusCode(System.Net.HttpStatusCode.OK)
            //       .WithBody(JsonSerializer.Serialize(new Kycmodel { FName = "", LName = ""}, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })).WithTransformer()
            //   );

            //_server.Given(Request.Create().WithPath("/getJson2/ncrt-123456")
            //    .UsingGet()
            //    ).RespondWith(
            //    Response.Create()
            //        .WithStatusCode(System.Net.HttpStatusCode.OK)
            //        .WithBody(JsonSerializer.Serialize(new Studentmodel { Name = "hhh" }))
            //    );

        }

        private static void StopWiremock()
        {
            _server.Stop();
        }
    }
}
