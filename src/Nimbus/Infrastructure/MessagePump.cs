﻿using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Nimbus.Infrastructure.MessageSendersAndReceivers;

namespace Nimbus.Infrastructure
{
    internal class MessagePump : IMessagePump
    {
        private readonly INimbusMessageReceiver _receiver;
        private readonly IMessageDispatcher _dispatcher;
        private readonly ILogger _logger;
        private readonly IClock _clock;

        private bool _started;
        private readonly object _mutex = new object();

        public MessagePump(INimbusMessageReceiver receiver, IMessageDispatcher dispatcher, ILogger logger, IClock clock)
        {
            _receiver = receiver;
            _dispatcher = dispatcher;
            _logger = logger;
            _clock = clock;
        }

        public Task Start()
        {
            return Task.Run(() =>
                            {
                                lock (_mutex)
                                {
                                    if (_started)
                                        throw new InvalidOperationException("Message pump either is already running or was previously running and has not completed shutting down.");

                                    _logger.Debug("Message pump for {0} starting...", _receiver);
                                    _receiver.Start(Dispatch);
                                    _started = true;
                                    _logger.Debug("Message pump for {0} started", _receiver);
                                }
                            });
        }

        public Task Stop()
        {
            return Task.Run(() =>
                            {
                                lock (_mutex)
                                {
                                    if (!_started) return;

                                    _logger.Debug("Message pump for {0} stopping...", _receiver);
                                    _receiver.Stop();
                                    _started = false;
                                    _logger.Debug("Message pump for {0} stopped.", _receiver);
                                }
                            });
        }

        private async Task Dispatch(BrokeredMessage message)
        {
            try
            {
                Exception exception = null;

                try
                {
                    _logger.Debug("Dispatching message: {0} from {1}", message, message.ReplyTo);
                    await _dispatcher.Dispatch(message);
                    _logger.Debug("Dispatched message: {0} from {1}", message, message.ReplyTo);

                    _logger.Debug("Completing message {0}", message);
                    await message.CompleteAsync();
                    _logger.Debug("Completed message {0}", message);

                    return;
                }
                catch (Exception exc)
                {
                    exception = exc;
                }

                _logger.Error(exception, "Message dispatch failed");

                try
                {
                    _logger.Debug("Abandoning message {0} from {1}", message, message.ReplyTo);
                    await message.AbandonAsync(exception.ExceptionDetailsAsProperties(_clock.UtcNow));
                    _logger.Debug("Abandoned message {0} from {1}", message, message.ReplyTo);
                }
                catch (Exception exc)
                {
                    _logger.Error(exc, "Could not call Abandon() on message {0} from {1}. Possible lock expiry?", message, message.ReplyTo);
                }
            }
            catch (Exception exc)
            {
                _logger.Error(exc, "Unhandled exception in message pump");
            }
        }

        public void Dispose()
        {
            Stop().Wait();
        }
    }
}