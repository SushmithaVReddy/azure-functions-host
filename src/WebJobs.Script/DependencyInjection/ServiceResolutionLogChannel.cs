﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;

namespace Microsoft.Azure.WebJobs.Script.DependencyInjection
{
    public class ServiceResolutionLogChannel
    {
        private static readonly object Lck = new object();
        private static ServiceResolutionLogChannel instance = null;
        private readonly Channel<ServiceResolutionInfo> channel = Channel.CreateUnbounded<ServiceResolutionInfo>();
        private readonly List<string> typeWhiteList = null;

        private bool didSpecializationHapppen = false;

        private ServiceResolutionLogChannel()
        {
            var typesToLog = Environment.GetEnvironmentVariable("FUNCTIONS_SERVICE_RESOLUTION_LOGS_WHITELIST_TYPE_PREFIX");
            if (!string.IsNullOrWhiteSpace(typesToLog))
            {
                typeWhiteList = typesToLog.Split(',').ToList();
            }
        }

        public static ServiceResolutionLogChannel Instance
        {
            get
            {
                lock (Lck)
                {
                    if (instance == null)
                    {
                        instance = new ServiceResolutionLogChannel();
                    }
                    return instance;
                }
            }
        }

        public ChannelReader<ServiceResolutionInfo> LogStream => channel.Reader;

        public void Send(ServiceResolutionInfo value)
        {
            if (!DidSpecializationHappen() || !ShouldLogThisType(value.Name))
            {
                return;
            }

            // not awaiting. Let the task run like fire & forget.
            channel.Writer.WriteAsync(value);
        }

        private bool ShouldLogThisType(string typeName)
        {
            if (typeWhiteList == null)
            {
                // log all types
                return true;
            }

            return typeWhiteList.Any(item => typeName.StartsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        private bool DidSpecializationHappen()
        {
            if (didSpecializationHapppen)
            {
                return true;
            }

            // This env variable is set shortly before specialization happens.
            // This allows us to ignore logging during place holder mode.
            var envVarValue = Environment.GetEnvironmentVariable("FUNCTIONS_SERVICE_RESOLUTION_LOG_ENABLED");
            if (!string.IsNullOrWhiteSpace(envVarValue) && envVarValue.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                didSpecializationHapppen = true;
            }

            return didSpecializationHapppen;
        }
    }
}