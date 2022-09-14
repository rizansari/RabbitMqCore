using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMqCore.Common
{
    public class ArgumentStrings
    {
        public const string MessageTTL = "x-message-ttl";
        public const string AlternateExchange = "alternate-exchange";
        public const string AutoExpires = "x-expires";
        public const string MaxLength = "x-max-length";
        public const string MaxLengthBytes = "x-max-length-bytes";
        public const string OverflowBehavior = "x-overflow";
        public const string OverflowBehaviorDropHead = "drop-head";
        public const string OverflowBehaviorRejectPublish = "reject-publish";
        public const string DeadLetterExchange = "x-dead-letter-exchange";
        public const string DeadLetterRoutingKey = "x-dead-letter-routing-key";
    }
}
