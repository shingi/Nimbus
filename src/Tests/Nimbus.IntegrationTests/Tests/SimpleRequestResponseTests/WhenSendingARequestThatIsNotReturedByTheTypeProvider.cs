﻿using System;
using System.Threading.Tasks;
using Nimbus.IntegrationTests.InfrastructureContracts;
using Nimbus.IntegrationTests.Tests.SimpleRequestResponseTests.MessageContracts;
using Nimbus.MessageContracts.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace Nimbus.IntegrationTests.Tests.SimpleRequestResponseTests
{
    public class WhenSendingARequestThatIsNotReturedByTheTypeProvider : TestForAllBuses
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

        public override async Task When()
        {
            await Bus.Request(new SomeRequestThatIsNotReturedByTheTypeProvider(), _timeout);
        }

        [Test]
        [TestCaseSource("AllBusesTestCases")]
        public async Task ABusExceptionIsThrown(ITestHarnessBusFactory busFactory)
        {
            await Given(busFactory);

            try
            {
                await When();
                Assert.Fail("Exception expected");
            }
            catch (Exception ex)
            {
                ex.ShouldBeTypeOf<BusException>();
                ex.Message.ShouldMatch(
                    @"^The type Nimbus.IntegrationTests.Tests.SimpleRequestResponseTests.MessageContracts.SomeRequestThatIsNotReturedByTheTypeProvider is not a recognised request type\. Ensure it has been registered with the builder with the WithTypesFrom method\.$");
            }
        }
    }
}