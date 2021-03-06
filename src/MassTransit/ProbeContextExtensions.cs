﻿// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit
{
    using Util;


    public static class ProbeContextExtensions
    {
        public static ProbeContext CreateConsumerFactoryScope<TConsumer>(this ProbeContext context, string source)
        {
            ProbeContext scope = context.CreateScope("consumerFactory");
            scope.Add("source", source);
            scope.Add("consumerType", TypeMetadataCache<TConsumer>.ShortName);

            return scope;
        }

        public static ProbeContext CreateFilterScope(this ProbeContext context, string filterType)
        {
            ProbeContext scope = context.CreateScope("filters");

            scope.Add("filterType", filterType);

            return scope;
        }

        public static ProbeContext CreateMessageScope(this ProbeContext context, string messageType)
        {
            ProbeContext scope = context.CreateScope(messageType);

            return scope;
        }
    }
}