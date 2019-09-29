﻿//  ----------------------------------------------------------------------------------
//  Copyright Microsoft Corporation
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  ----------------------------------------------------------------------------------

namespace Correlation.Samples
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using DurableTask.Core;
    using Microsoft.ApplicationInsights.W3C;

    class RetryScenario
    {
        public async Task ExecuteAsync()
        {
            new TelemetryActivator().Initialize();

            using (
                TestOrchestrationHost host = TestHelpers.GetTestOrchestrationHost(false))
            {
                await host.StartAsync();
                var activity = new Activity("Start Orchestration");
#pragma warning disable 618
                activity.GenerateW3CContext();
#pragma warning restore 618
                activity.Start();
                var client = await host.StartOrchestrationAsync(typeof(RetryOrchestration), "Retry Scenario"); // TODO The parameter null will throw exception. (for the experiment)
                var status = await client.WaitForCompletionAsync(TimeSpan.FromSeconds(50));

                await host.StopAsync();
            }
        }
    }

    [KnownType(typeof(RetryActivity))]
    [KnownType(typeof(NonRetryActivity))]
    internal class RetryOrchestration : TaskOrchestration<string, string>
    {
        public override async Task<string> RunTask(OrchestrationContext context, string input)
        {
            await context.ScheduleTask<string>(typeof(NonRetryActivity), input);
            var retryOption = new RetryOptions(TimeSpan.FromMilliseconds(10), 3);
            return await context.ScheduleWithRetry<string>(typeof(RetryActivity), retryOption, input);
        }
    }


    internal class RetryActivity : TaskActivity<string, string>
    {
        private static int counter = 0;
        protected override string Execute(TaskContext context, string input)
        {
            counter++;
            if (counter == 1) throw new InvalidOperationException($"Counter = {counter}");

            Console.WriteLine($"Retry with Activity: Hello {input}");
            return $"Retry Hello, {input}!";
        }
    }

    internal class NonRetryActivity : TaskActivity<string, string>
    {
        protected override string Execute(TaskContext context, string input)
        {
            Console.WriteLine($"Non-Retry with Activity: Hello {input}");
            return $"Works well. Hello, {input}!";
        }
    }
}
