﻿using Nimbus.Infrastructure.RequestResponse;
using Nimbus.MessageContracts;
using NUnit.Framework;
using Shouldly;

namespace Nimbus.UnitTests.DispatcherTests
{
    public class WhenExtractingAHandlerMethodForAMulticastRequest
    {
        [Test]
        public void WeShouldGetTheRightMethod()
        {
            var request = new SomeInternalRequest();
            var handlerMethod = MulticastRequestMessageDispatcher.GetGenericDispatchMethodFor(request);

            handlerMethod.ShouldNotBe(null);
        }

        public class SomeInternalRequest : BusRequest<SomeInternalRequest, SomeInternalResponse>
        {
        }

        public class SomeInternalResponse : IBusResponse
        {
        }
    }
}