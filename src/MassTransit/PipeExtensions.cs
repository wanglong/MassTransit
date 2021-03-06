// Copyright 2007-2014 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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


    public static class PipeExtensions
    {
        /// <summary>
        /// Get a payload from the pipe context
        /// </summary>
        /// <typeparam name="T">The payload type</typeparam>
        /// <param name="context">The pipe context</param>
        /// <returns>The payload, or throws a PayloadNotFoundException if the payload is not present</returns>
        public static T GetPayload<T>(this PipeContext context)
            where T : class
        {
            T payload;
            if (!context.TryGetPayload(out payload))
                throw new PayloadNotFoundException("The payload was not found: " + TypeMetadataCache<T>.ShortName);

            return payload;
        }
    }
}