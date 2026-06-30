using Axpo;
using Axpo.PowerPositionReporter.Application.Configurations;
using Axpo.PowerPositionReporter.Application.Extensions;
using Axpo.PowerPositionReporter.Application.Services;
using Axpo.PowerPositionReporter.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reqnroll;

namespace Axpo.PowerPositionReporter.BddTests.Support
    {
    /// <summary>
    /// Builds a real (non-mocked) DI container, scoped per scenario via
    /// Reqnroll's ScenarioContext, the same way IntegrationTestFixture does
    /// for the plain xUnit integration tests.
    /// </summary>
    [Binding]
    public class DependencyInjectionHooks
        {
        private readonly ScenarioContext _scenarioContext;

        public DependencyInjectionHooks ( ScenarioContext scenarioContext )
            {
            _scenarioContext = scenarioContext;
            }

        [BeforeScenario]
        public void SetUpContainer ( )
            {
            var csvReportPath = Path.Combine(Path.GetTempPath(), $"axpo-bdd-{Guid.NewGuid():N}");

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.IntegrationTests.json", optional: false)
                .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                    ["PowerPositionReporter:CsvReportPath"] = csvReportPath
                    })
                .Build();

            var services = new ServiceCollection();

            services.AddLogging (builder => builder.AddDebug ());
            services.Configure<AppSettings> (configuration.GetSection (AppSettings.SectionName));
            services.AddPowerTradeResilience (configuration);

            services.AddSingleton<IReportLogger, SerilogReportLogger> ();
            services.AddSingleton<IPowerService, PowerService> ();
            services.AddSingleton<IPowerTradeService, PowerTradeService> ();
            services.AddSingleton<IReportWriter, CsvReportWriter> ();
            services.AddSingleton<IPowerPositionReportService, PowerPositionReportService> ();

            var provider = services.BuildServiceProvider();

            _scenarioContext.Set (provider, "ServiceProvider");
            _scenarioContext.Set (csvReportPath, "CsvReportPath");
            }

        [AfterScenario]
        public void TearDownContainer ( )
            {
            var provider = _scenarioContext.Get<ServiceProvider>("ServiceProvider");
            var csvReportPath = _scenarioContext.Get<string>("CsvReportPath");

            provider.Dispose ();

            if ( Directory.Exists (csvReportPath) )
                {
                Directory.Delete (csvReportPath, recursive: true);
                }
            }
        }
    }