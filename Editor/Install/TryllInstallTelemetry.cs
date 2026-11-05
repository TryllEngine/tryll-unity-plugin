// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// The plugin's one event: <c>tryll.server.install</c>, sent after each server install
    /// attempt. Telemetry is part of the Tryll Runtime Licence and always on, and an install
    /// needs the licence (<see cref="TryllLicence"/>). It never blocks or fails an install:
    /// one try, a short timeout, errors swallowed.
    /// Properties are counts and versions only; no paths, names or project content.
    /// </summary>
    internal static class TryllInstallTelemetry
    {
        internal const string EventName = "tryll.server.install";
        // Same PostHog project and write-only ingest key as the server's sink
        // (tryll/data/server-config.json). An ingest key can only add events.
        internal const string Endpoint = "https://eu.i.posthog.com/capture/";
        internal const string ProjectKey = "phc_n5RuNgNFUfjtnXiExVTAAAgpsgZSEDS3a7xAxKWMdCEU";

        /// <summary>Test seam: replaces the HTTP send. Receives the JSON body.</summary>
        internal static Func<string, Task> DebugSenderForTests;

        internal struct Info
        {
            public string Result;     // TryllServerInstaller.Result.Code
            public string Version;
            public string Variant;
            public string Source;     // download | zip
            public string Channel;    // public | test | dev
            public long Bytes;
            public double Seconds;
        }

        /// <summary>Send without waiting. Must be called on the main thread (reads EditorPrefs).</summary>
        internal static void TrySend(Info info)
        {
            string body = BuildBody(info, TryllLicence.InstallId, Application.unityVersion, DateTime.UtcNow);
            var sender = DebugSenderForTests ?? PostAsync;
            _ = SendQuietlyAsync(sender, body);
        }

        static async Task SendQuietlyAsync(Func<string, Task> sender, string body)
        {
            try { await sender(body).ConfigureAwait(false); }
            catch (Exception) { /* telemetry must never surface an error */ }
        }

        static async Task PostAsync(string body)
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            using (var client = new HttpClient())
            using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
            using (await client.PostAsync(Endpoint, content, cts.Token).ConfigureAwait(false)) { }
        }

        internal static string BuildBody(Info info, string distinctId, string unityVersion, DateTime utcNow)
        {
            var props = new List<KeyValuePair<string, string>>
            {
                Pair("result", info.Result),
                Pair("version", info.Version),
                Pair("variant", info.Variant),
                Pair("source", info.Source),
                Pair("channel", info.Channel),
                Pair("engine", "unity"),
                Pair("engine_version", unityVersion),
            };
            var sb = new StringBuilder(512);
            sb.Append("{\"api_key\":").Append(Quote(ProjectKey));
            sb.Append(",\"event\":").Append(Quote(EventName));
            sb.Append(",\"distinct_id\":").Append(Quote(distinctId));
            sb.Append(",\"timestamp\":").Append(Quote(utcNow.ToString("o", CultureInfo.InvariantCulture)));
            sb.Append(",\"properties\":{\"$process_person_profile\":false");
            foreach (var p in props)
                sb.Append(',').Append(Quote(p.Key)).Append(':').Append(Quote(p.Value));
            sb.Append(",\"bytes\":").Append(info.Bytes.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"duration_s\":").Append(Math.Round(info.Seconds, 1).ToString(CultureInfo.InvariantCulture));
            sb.Append("}}");
            return sb.ToString();
        }

        static KeyValuePair<string, string> Pair(string key, string value)
            => new KeyValuePair<string, string>(key, value ?? "");

        static string Quote(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
