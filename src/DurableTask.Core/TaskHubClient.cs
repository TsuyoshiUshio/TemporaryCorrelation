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

namespace DurableTask.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DurableTask.Core.History;
    using DurableTask.Core.Serializing;
    using Microsoft.ApplicationInsights.W3C;

    /// <summary>
    ///     Client used to manage and query orchestration instances
    /// </summary>
    public sealed class TaskHubClient
    {
        readonly DataConverter defaultConverter;

        /// <summary>
        /// The orchestration service client for this task hub client
        /// </summary>
        public IOrchestrationServiceClient ServiceClient { get; }

        /// <summary>
        ///     Create a new TaskHubClient with the given OrchestrationServiceClient
        /// </summary>
        /// <param name="serviceClient">Object implementing the <see cref="IOrchestrationServiceClient"/> interface </param>
        public TaskHubClient(IOrchestrationServiceClient serviceClient)
        {
            ServiceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            this.defaultConverter = new JsonDataConverter();
        }

        /// <summary>
        ///     Create a new orchestration of the specified type with an automatically generated instance id
        /// </summary>
        /// <param name="orchestrationType">Type that derives from TaskOrchestration</param>
        /// <param name="input">Input parameter to the specified TaskOrchestration</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceAsync(Type orchestrationType, object input)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                NameVersionHelper.GetDefaultName(orchestrationType),
                NameVersionHelper.GetDefaultVersion(orchestrationType),
                null,
                input,
                null,
                null,
                null,
                null);
        }

        /// <summary>
        ///     Create a new orchestration of the specified type with the specified instance id
        /// </summary>
        /// <param name="orchestrationType">Type that derives from TaskOrchestration</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="input">Input parameter to the specified TaskOrchestration</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceAsync(
            Type orchestrationType,
            string instanceId,
            object input)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                NameVersionHelper.GetDefaultName(orchestrationType),
                NameVersionHelper.GetDefaultVersion(orchestrationType),
                instanceId,
                input,
                null,
                null,
                null,
                null);
        }

        /// <summary>
        ///     Create a new orchestration of the specified type with the specified instance id
        /// </summary>
        /// <param name="orchestrationType">Type that derives from TaskOrchestration</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="input">Input parameter to the specified TaskOrchestration</param>
        /// <param name="dedupeStatuses">States of previous orchestration executions to be considered while de-duping new orchestrations on the client</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceAsync(
            Type orchestrationType,
            string instanceId,
            object input,
            OrchestrationStatus[] dedupeStatuses
            )
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                NameVersionHelper.GetDefaultName(orchestrationType),
                NameVersionHelper.GetDefaultVersion(orchestrationType),
                instanceId,
                input,
                null,
                dedupeStatuses,
                null,
                null);
        }

        /// <summary>
        ///     Create a new orchestration of the specified name and version
        /// </summary>
        /// <param name="name">Name of the TaskOrchestration</param>
        /// <param name="version">Version of the TaskOrchestration</param>
        /// <param name="input">Input parameter to the specified TaskOrchestration</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceAsync(string name, string version, object input)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(name, version, null, input, null, null, null, null);
        }

        /// <summary>
        ///     Create a new orchestration of the specified name and version
        /// </summary>
        /// <param name="name">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="version">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="input">Input parameter to the specified TaskOrchestration</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceAsync(string name, string version, string instanceId, object input)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(name, version, instanceId, input, null, null, null, null);
        }

        /// <summary>
        ///     Create a new orchestration of the specified name and version
        /// </summary>
        /// <param name="name">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="version">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="input">Input parameter to the specified TaskOrchestration</param>
        /// <param name="tags">Dictionary of key/value tags associated with this instance</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceAsync(
            string name,
            string version,
            string instanceId,
            object input,
            IDictionary<string, string> tags)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(name, version, instanceId, input, tags, null, null, null);
        }

        /// <summary>
        ///     Create a new orchestration of the specified name and version
        /// </summary>
        /// <param name="name">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="version">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="input">Input parameter to the specified TaskOrchestration</param>
        /// <param name="tags">Dictionary of key/value tags associated with this instance</param>
        /// <param name="dedupeStatuses">States of previous orchestration executions to be considered while de-duping new orchestrations on the client</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceAsync(
            string name,
            string version,
            string instanceId,
            object input,
            IDictionary<string, string> tags,
            OrchestrationStatus[] dedupeStatuses)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(name, version, instanceId, input, tags, dedupeStatuses, null, null);
        }

        /// <summary>
        ///     Creates an orchestration instance, and raises an event for it, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationType">Type that derives from TaskOrchestration</param>
        /// <param name="orchestrationInput">Input parameter to the specified TaskOrchestration</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceWithRaisedEventAsync(
            Type orchestrationType,
            object orchestrationInput,
            string eventName,
            object eventData)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                NameVersionHelper.GetDefaultName(orchestrationType),
                NameVersionHelper.GetDefaultVersion(orchestrationType),
                null,
                orchestrationInput,
                null,
                null,
                eventName,
                eventData);
        }

        /// <summary>
        ///     Creates an orchestration instance, and raises an event for it, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationType">Type that derives from TaskOrchestration</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="orchestrationInput">Input parameter to the specified TaskOrchestration</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceWithRaisedEventAsync(
            Type orchestrationType,
            string instanceId,
            object orchestrationInput,
            string eventName,
            object eventData)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                NameVersionHelper.GetDefaultName(orchestrationType),
                NameVersionHelper.GetDefaultVersion(orchestrationType),
                instanceId,
                orchestrationInput,
                null,
                null,
                eventName,
                eventData);
        }

        /// <summary>
        ///     Creates an orchestration instance, and raises an event for it, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationType">Type that derives from TaskOrchestration</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="orchestrationInput">Input parameter to the specified TaskOrchestration</param>
        /// <param name="dedupeStatuses">States of previous orchestration executions to be considered while de-duping new orchestrations on the client</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceWithRaisedEventAsync(
            Type orchestrationType,
            string instanceId,
            object orchestrationInput,
            OrchestrationStatus[] dedupeStatuses,
            string eventName,
            object eventData)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                NameVersionHelper.GetDefaultName(orchestrationType),
                NameVersionHelper.GetDefaultVersion(orchestrationType),
                instanceId,
                orchestrationInput,
                null,
                dedupeStatuses,
                eventName,
                eventData);
        }

        /// <summary>
        ///     Creates an orchestration instance, and raises an event for it, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationName">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="orchestrationVersion">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceWithRaisedEventAsync(
            string orchestrationName,
            string orchestrationVersion,
            string eventName,
            object eventData)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                orchestrationName,
                orchestrationVersion,
                null,
                null,
                null,
                null, eventName, eventData);
        }

        /// <summary>
        ///     Creates an orchestration instance, and raises an event for it, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationName">Name of the TaskOrchestration</param>
        /// <param name="orchestrationVersion">Version of the TaskOrchestration</param>
        /// <param name="orchestrationInput">Input parameter to the specified TaskOrchestration</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceWithRaisedEventAsync(
            string orchestrationName,
            string orchestrationVersion,
            object orchestrationInput,
            string eventName,
            object eventData)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                orchestrationName,
                orchestrationVersion,
                null, orchestrationInput,
                null,
                null,
                eventName,
                eventData);
        }

        /// <summary>
        ///     Creates an orchestration instance, and raises an event for it, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationName">Name of the TaskOrchestration</param>
        /// <param name="orchestrationVersion">Version of the TaskOrchestration</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="orchestrationInput">Input parameter to the specified TaskOrchestration</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceWithRaisedEventAsync(
            string orchestrationName,
            string orchestrationVersion,
            string instanceId,
            object orchestrationInput,
            string eventName,
            object eventData)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                orchestrationName,
                orchestrationVersion,
                instanceId,
                orchestrationInput, null, null,
                eventName,
                eventData);
        }

        /// <summary>
        ///     Creates an orchestration instance, and raises an event for it, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationName">Name of the TaskOrchestration</param>
        /// <param name="orchestrationVersion">Version of the TaskOrchestration</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="orchestrationInput">Input parameter to the specified TaskOrchestration</param>
        /// <param name="orchestrationTags">Dictionary of key/value tags associated with this instance</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceWithRaisedEventAsync(
            string orchestrationName,
            string orchestrationVersion,
            string instanceId,
            object orchestrationInput,
            IDictionary<string, string> orchestrationTags,
            string eventName,
            object eventData)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                orchestrationName,
                orchestrationVersion,
                instanceId,
                orchestrationInput,
                orchestrationTags,
                null,
                eventName,
                eventData);
        }

        /// <summary>
        ///     Creates an orchestration instance, and raises an event for it, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationName">Name of the TaskOrchestration</param>
        /// <param name="orchestrationVersion">Version of the TaskOrchestration</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="orchestrationInput">Input parameter to the specified TaskOrchestration</param>
        /// <param name="orchestrationTags">Dictionary of key/value tags associated with this instance</param>
        /// <param name="dedupeStatuses">States of previous orchestration executions to be considered while de-duping new orchestrations on the client</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        /// <returns>OrchestrationInstance that represents the orchestration that was created</returns>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceWithRaisedEventAsync(
            string orchestrationName,
            string orchestrationVersion,
            string instanceId,
            object orchestrationInput,
            IDictionary<string, string> orchestrationTags,
            OrchestrationStatus[] dedupeStatuses,
            string eventName,
            object eventData)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                orchestrationName,
                orchestrationVersion,
                instanceId,
                orchestrationInput,
                orchestrationTags,
                dedupeStatuses,
                eventName,
                eventData);
        }

        /// <summary>
        ///     Creates an orchestration instance, and raises an event for it, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationName">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="orchestrationVersion">Name of the orchestration as specified by the ObjectCreator</param>
        /// <param name="instanceId">Instance id for the orchestration to be created, must be unique across the Task Hub</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        public Task<OrchestrationInstance> CreateOrchestrationInstanceWithRaisedEventAsync(
            string orchestrationName,
            string orchestrationVersion,
            string instanceId,
            string eventName,
            object eventData)
        {
            return InternalCreateOrchestrationInstanceWithRaisedEventAsync(
                orchestrationName,
                orchestrationVersion,
                instanceId,
                null,
                null,
                null, eventName, eventData);
        }

        async Task<OrchestrationInstance> InternalCreateOrchestrationInstanceWithRaisedEventAsync(
            string orchestrationName,
            string orchestrationVersion,
            string orchestrationInstanceId,
            object orchestrationInput,
            IDictionary<string, string> orchestrationTags,
            OrchestrationStatus[] dedupeStatuses,
            string eventName,
            object eventData)
        {
            TraceContextBase requestTraceContext = null;

            // correlation 
            CorrelationTraceClient.Propagate(()=> { requestTraceContext = CreateOrExtractRequestTraceContext(eventName); });

            if (string.IsNullOrWhiteSpace(orchestrationInstanceId))
            {
                orchestrationInstanceId = Guid.NewGuid().ToString("N");
            }

            var orchestrationInstance = new OrchestrationInstance
            {
                InstanceId = orchestrationInstanceId,
                ExecutionId = Guid.NewGuid().ToString("N"),
            };

            string serializedOrchestrationData = this.defaultConverter.Serialize(orchestrationInput);
            var startedEvent = new ExecutionStartedEvent(-1, serializedOrchestrationData)
            {
                Tags = orchestrationTags,
                Name = orchestrationName,
                Version = orchestrationVersion,
                OrchestrationInstance = orchestrationInstance
            };

            var taskMessages = new List<TaskMessage>
            {
                new TaskMessage
                {
                    OrchestrationInstance = orchestrationInstance,
                    Event = startedEvent
                }
            };

            if (eventData != null)
            {
                string serializedEventData = this.defaultConverter.Serialize(eventData);
                taskMessages.Add(new TaskMessage
                {
                    OrchestrationInstance = new OrchestrationInstance
                    {
                        InstanceId = orchestrationInstanceId,

                        // to ensure that the event gets raised on the running
                        // orchestration instance, null the execution id
                        // so that it will find out which execution
                        // it should use for processing
                        ExecutionId = null
                    },
                    Event = new EventRaisedEvent(-1, serializedEventData) { Name = eventName }
                });
            }

            CorrelationTraceClient.Propagate(() => CreateAndTrackDependencyTelemetry(requestTraceContext));

            // Raised events and create orchestration calls use different methods so get handled separately
            await Task.WhenAll(taskMessages.Where(t => !(t.Event is EventRaisedEvent)).Select(sEvent => ServiceClient.CreateTaskOrchestrationAsync(sEvent, dedupeStatuses)));
            await ServiceClient.SendTaskOrchestrationMessageBatchAsync(taskMessages.Where(t => (t.Event is EventRaisedEvent)).ToArray());

            return orchestrationInstance;
        }

        TraceContextBase CreateOrExtractRequestTraceContext(string eventName)
        {
            TraceContextBase requestTraceContext = null;
            if (Activity.Current == null) // It is possible that the caller already has an activity.
            {
                requestTraceContext = TraceContextFactory.Create($"{TraceConstants.Client}: {eventName}");
                requestTraceContext.StartAsNew();
            }
            else
            {
                requestTraceContext = TraceContextFactory.Create(Activity.Current);
            }

            return requestTraceContext;
        }

        void CreateAndTrackDependencyTelemetry(TraceContextBase requestTraceContext)
        {
            TraceContextBase dependencyTraceContext = TraceContextFactory.Create(TraceConstants.Client);
            dependencyTraceContext.TelemetryType = TelemetryType.Dependency;
            dependencyTraceContext.SetParentAndStart(requestTraceContext);

            CorrelationTraceContext.Current = dependencyTraceContext;

            // Correlation
            CorrelationTraceClient.TrackDepencencyTelemetry(dependencyTraceContext);
            CorrelationTraceClient.TrackRequestTelemetry(requestTraceContext);
        }

        /// <summary>
        ///     Raises an event in the specified orchestration instance, which eventually causes the OnEvent() method in the
        ///     orchestration to fire.
        /// </summary>
        /// <param name="orchestrationInstance">Instance in which to raise the event</param>
        /// <param name="eventName">Name of the event</param>
        /// <param name="eventData">Data for the event</param>
        public async Task RaiseEventAsync(OrchestrationInstance orchestrationInstance, string eventName, object eventData)
        {
            if (string.IsNullOrWhiteSpace(orchestrationInstance?.InstanceId))
            {
                throw new ArgumentException(nameof(orchestrationInstance));
            }

            string serializedInput = this.defaultConverter.Serialize(eventData);
            var taskMessage = new TaskMessage
            {
                OrchestrationInstance = orchestrationInstance,
                Event = new EventRaisedEvent(-1, serializedInput) { Name = eventName }
            };

            await ServiceClient.SendTaskOrchestrationMessageAsync(taskMessage);
        }

        /// <summary>
        ///     Forcefully terminate the specified orchestration instance
        /// </summary>
        /// <param name="orchestrationInstance">Instance to terminate</param>
        public Task TerminateInstanceAsync(OrchestrationInstance orchestrationInstance)
        {
            return TerminateInstanceAsync(orchestrationInstance, string.Empty);
        }

        /// <summary>
        ///     Forcefully terminate the specified orchestration instance with a reason
        /// </summary>
        /// <param name="orchestrationInstance">Instance to terminate</param>
        /// <param name="reason">Reason for terminating the instance</param>
        public async Task TerminateInstanceAsync(OrchestrationInstance orchestrationInstance, string reason)
        {
            if (string.IsNullOrWhiteSpace(orchestrationInstance?.InstanceId))
            {
                throw new ArgumentException("orchestrationInstance");
            }

            await ServiceClient.ForceTerminateTaskOrchestrationAsync(orchestrationInstance.InstanceId, reason);
        }

        /// <summary>
        ///     Wait for an orchestration to reach any terminal state within the given timeout
        /// </summary>
        /// <param name="orchestrationInstance">Instance to terminate</param>
        /// <param name="timeout">Max timeout to wait</param>
        public Task<OrchestrationState> WaitForOrchestrationAsync(
            OrchestrationInstance orchestrationInstance,
            TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(orchestrationInstance?.InstanceId))
            {
                throw new ArgumentException(nameof(orchestrationInstance));
            }
            
            return ServiceClient.WaitForOrchestrationAsync(
                orchestrationInstance.InstanceId,
                orchestrationInstance.ExecutionId,
                timeout,
                CancellationToken.None);
        }

        /// <summary>
        ///     Wait for an orchestration to reach any terminal state within the given timeout
        /// </summary>
        /// <param name="orchestrationInstance">Instance to terminate</param>
        /// <param name="timeout">Max timeout to wait</param>
        /// <param name="cancellationToken">Task cancellation token</param>
        public Task<OrchestrationState> WaitForOrchestrationAsync(
            OrchestrationInstance orchestrationInstance,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(orchestrationInstance?.InstanceId))
            {
                throw new ArgumentException(nameof(orchestrationInstance));
            }

            return ServiceClient.WaitForOrchestrationAsync(
                orchestrationInstance.InstanceId,
                orchestrationInstance.ExecutionId,
                timeout,
                cancellationToken);
        }

        // Instance query methods
        // Orchestration states
        /// <summary>
        ///     Get a list of orchestration states from the instance storage table for the
        ///     most current execution (generation) of the specified instance.
        /// </summary>
        /// <param name="instanceId">Instance id</param>
        /// <returns>The OrchestrationState of the specified instanceId or null if not found</returns>
        /// <exception cref="InvalidOperationException">Thrown if instance store not configured</exception>
        public async Task<OrchestrationState> GetOrchestrationStateAsync(string instanceId)
        {
            IList<OrchestrationState> state = await GetOrchestrationStateAsync(instanceId, false);
            return state?.FirstOrDefault();
        }

        /// <summary>
        ///     Get a list of orchestration states from the instance storage table for either the most current
        ///     or all executions (generations) of the specified instance.
        /// </summary>
        /// <param name="instanceId">Instance id</param>
        /// <param name="allExecutions">
        ///     True if method should fetch all executions of the instance,
        ///     false if the method should only fetch the most recent execution
        /// </param>
        /// <returns>
        ///     List of OrchestrationState objects that represents the list of
        ///     orchestrations in the instance store
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown if instance store not configured</exception>
        public Task<IList<OrchestrationState>> GetOrchestrationStateAsync(string instanceId, bool allExecutions)
        {
            return ServiceClient.GetOrchestrationStateAsync(instanceId, allExecutions);
        }

        /// <summary>
        ///     Get a list of orchestration states from the instance storage table for the
        ///     most current execution (generation) of the specified instance.
        /// </summary>
        /// <param name="instance">Instance</param>
        /// <returns>The OrchestrationState of the specified instanceId or null if not found</returns>
        /// <exception cref="InvalidOperationException">Thrown if instance store not configured</exception>
        public Task<OrchestrationState> GetOrchestrationStateAsync(OrchestrationInstance instance)
        {
            return GetOrchestrationStateAsync(instance.InstanceId, instance.ExecutionId);
        }

        /// <summary>
        ///     Get a list of orchestration states from the instance storage table for the
        ///     specified execution (generation) of the specified instance.
        /// </summary>
        /// <param name="instanceId">Instance id</param>
        /// <param name="executionId">Execution id</param>
        /// <returns>The OrchestrationState of the specified instanceId or null if not found</returns>
        /// <exception cref="InvalidOperationException">Thrown if instance store not configured</exception>
        public Task<OrchestrationState> GetOrchestrationStateAsync(string instanceId, string executionId)
        {
            return ServiceClient.GetOrchestrationStateAsync(instanceId, executionId);
        }

        // Orchestration History

        /// <summary>
        ///     Get a string dump of the execution history of the specified orchestration instance
        ///     specified execution (generation) of the specified instance.
        /// </summary>
        /// <param name="instance">Instance</param>
        /// <returns>String with formatted JSON representing the execution history</returns>
        /// <exception cref="InvalidOperationException">Thrown if instance store not configured</exception>
        public Task<string> GetOrchestrationHistoryAsync(OrchestrationInstance instance)
        {
            if (string.IsNullOrWhiteSpace(instance?.InstanceId) ||
                string.IsNullOrWhiteSpace(instance.ExecutionId))
            {
                throw new ArgumentException("instance, instanceId and/or ExecutionId cannot be null or empty", nameof(instance));
            }

            return ServiceClient.GetOrchestrationHistoryAsync(instance.InstanceId, instance.ExecutionId);
        }

        /// <summary>
        ///     Purges orchestration instance state and history for orchestrations older than the specified threshold time.
        /// </summary>
        /// <param name="thresholdDateTimeUtc">Threshold date time in UTC</param>
        /// <param name="timeRangeFilterType">What to compare the threshold date time against</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if instance store not configured</exception>
        public Task PurgeOrchestrationInstanceHistoryAsync(DateTime thresholdDateTimeUtc,
            OrchestrationStateTimeRangeFilterType timeRangeFilterType)
        {
            return ServiceClient.PurgeOrchestrationHistoryAsync(thresholdDateTimeUtc, timeRangeFilterType);
        }
    }
}