// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Finds the newest release of the public plugin repo without the GitHub REST API
    /// (60 anonymous calls an hour per IP): <c>&lt;repo&gt;/releases/latest</c> redirects to
    /// <c>.../releases/tag/vX.Y.Z</c>, and the tag in that redirect is the answer.
    /// The request carries no data about the user or project.
    /// </summary>
    internal static class TryllUpdateCheck
    {
        static readonly Regex TagInLocation = new Regex(@"/releases/tag/v?(?<version>[0-9][0-9A-Za-z.\-+]*)$");

        /// <summary>Newest released version (without a leading 'v'), or null on any failure.</summary>
        internal static async Task<string> LatestVersionAsync(string repoUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(repoUrl))
                return null;
            try
            {
                var handler = new HttpClientHandler { AllowAutoRedirect = false };
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd($"TryllUnity/{TryllVersion.Version}");
                    var request = new HttpRequestMessage(HttpMethod.Head, repoUrl.TrimEnd('/') + "/releases/latest");
                    using (var response = await client.SendAsync(request, ct).ConfigureAwait(false))
                        return ParseLocation(response.Headers.Location?.ToString());
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string ParseLocation(string location)
        {
            if (string.IsNullOrEmpty(location))
                return null;
            var match = TagInLocation.Match(location);
            return match.Success ? match.Groups["version"].Value : null;
        }

        /// <summary>
        /// True when <paramref name="candidate"/> is a newer release than
        /// <paramref name="current"/>. Compares major.minor.patch; a pre-release
        /// (<c>-rc.1</c>) sorts before its release. Unparseable input is never newer.
        /// </summary>
        internal static bool IsNewer(string candidate, string current)
        {
            if (!TryParse(candidate, out var a, out string preA) || !TryParse(current, out var b, out string preB))
                return false;
            for (int i = 0; i < 3; i++)
                if (a[i] != b[i])
                    return a[i] > b[i];
            if (preA == preB) return false;
            if (preA.Length == 0) return true;   // 1.2.0 > 1.2.0-rc.1
            if (preB.Length == 0) return false;
            return string.CompareOrdinal(preA, preB) > 0;
        }

        static bool TryParse(string version, out int[] parts, out string pre)
        {
            parts = new int[3];
            pre = "";
            if (string.IsNullOrEmpty(version))
                return false;
            string core = version.TrimStart('v');
            int plus = core.IndexOf('+');
            if (plus >= 0) core = core.Substring(0, plus);
            int dash = core.IndexOf('-');
            if (dash >= 0)
            {
                pre = core.Substring(dash + 1);
                core = core.Substring(0, dash);
            }
            string[] fields = core.Split('.');
            if (fields.Length != 3)
                return false;
            for (int i = 0; i < 3; i++)
                if (!int.TryParse(fields[i], out parts[i]))
                    return false;
            return true;
        }
    }
}
