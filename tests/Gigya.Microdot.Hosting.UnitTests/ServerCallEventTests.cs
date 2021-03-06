using System;
using System.Linq;
using Gigya.Microdot.SharedLogic.Events;
using NUnit.Framework;
using System.Threading.Tasks;
using Gigya.Microdot.Common.Tests;
using Gigya.Microdot.Hosting.Events;
using Gigya.Microdot.Hosting.UnitTests.NonOrleansMicroService;
using Gigya.Microdot.Interfaces.Events;
using Gigya.Microdot.SharedLogic.HttpService;
using Gigya.Microdot.Testing.Shared.Service;
using Ninject;
using Gigya.Microdot.Hosting.Configuration;

namespace Gigya.Common.OrleansInfra.FunctionalTests.Events
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class ServerCallEventTests
    {
        private const int REPEAT = 1;

        private SpyEventPublisher _flumeQueue;
        private ICalculatorService _serviceProxy;
        private NonOrleansServiceTester<CalculatorServiceHost> _serviceTester;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            //Environment.SetEnvironmentVariable("ZONE", "zone");
            //Environment.SetEnvironmentVariable("ENV", "env");

            var config = new HostConfiguration(new TestHostConfigurationSource(
                zone: "zone",
                deploymentEnvironment: "env",
                appName: "ICalculatorService"));

            _serviceTester = new NonOrleansServiceTester<CalculatorServiceHost>(config);

            _serviceTester.CommunicationKernel.Rebind<ICertificateLocator>().To<DummyCertificateLocator>();

            _serviceProxy = _serviceTester.GetServiceProxy<ICalculatorService>();
            
            _flumeQueue = (SpyEventPublisher)_serviceTester.Host.Kernel.Get<IEventPublisher>();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            _serviceTester?.Dispose();

            //Environment.SetEnvironmentVariable("ZONE", null);
            //Environment.SetEnvironmentVariable("ENV", null);
        }

        [Test]
        [Repeat(REPEAT)]
        public async Task SingleServerCall_CallSucceeds_PublishesEvent()
        {
            _flumeQueue.Clear();

            var requestId = nameof(SingleServerCall_CallSucceeds_PublishesEvent) + Guid.NewGuid();
            
            TracingContext.SetRequestID(requestId);
            TracingContext.TryGetRequestID();

            await _serviceProxy.Add(5, 3);
            await Task.Delay(100);
            var events = _flumeQueue.Events;
            var serverReq = (ServiceCallEvent) events.Single();
            Assert.AreEqual("serverReq", serverReq.EventType);
            Assert.AreEqual(nameof(ICalculatorService), serverReq.ServiceName);
            Assert.AreEqual("Add", serverReq.ServiceMethod);
            Assert.AreEqual(requestId, serverReq.RequestId);
        }
    }
}