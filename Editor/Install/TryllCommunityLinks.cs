// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Every outside link the Welcome and Help &amp; Community windows show, in one place.
    /// An empty URL hides its button, so a channel that does not exist yet costs nothing.
    /// Labels are user-visible copy and live in <c>tryll/schema/editor_ui.json</c>.
    /// </summary>
    internal static class TryllCommunityLinks
    {
        internal const string Website = "https://tryllengine.com";
        internal const string Docs = TryllDocs.BaseUrl;
        internal const string GettingStarted = TryllDocs.GettingStartedUnity;
        /// <summary>What telemetry the server sends; the Welcome window's "Telemetry" button.
        /// The licence text itself is the package's LICENSE.md (<see cref="TryllLicence"/>).</summary>
        internal const string Telemetry = TryllDocs.Telemetry;
        /// <summary>How to report a bug; the Help &amp; Community window's "Report a bug" button.</summary>
        internal const string ReportBug = TryllDocs.ReportABug;
        internal const string GitHub = "https://github.com/TryllEngine";

        internal const string Discord = "https://discord.com/invite/DTKycjPpzs";
        internal const string YouTube = "https://www.youtube.com/@Tryll_engine";
        internal const string LinkedIn = "https://www.linkedin.com/company/tryllengine/posts/";
        internal const string Instagram = "https://www.instagram.com/tryll_engine";
        internal const string TikTok = "https://www.tiktok.com/@tryll_engine";
        internal const string X = "";

        internal const string TeamEmail = "team@tryllengine.com";
        internal const string Email = "mailto:" + TeamEmail;

        /// <summary>Issue tracker of the public plugin repo (<c>[unity.export].repo_url</c> in ci/release.toml).</summary>
        internal const string ReportIssue = "https://github.com/TryllEngine/tryll-unity-plugin/issues";

        /// <summary>
        /// Public plugin repo for "Check for updates" (<c>&lt;repo&gt;/releases/latest</c>).
        /// Leave it empty: <see cref="ResolveReleasesRepo"/> then uses the repo the server pin
        /// downloads from, which the release pipeline writes for every release.
        /// </summary>
        internal const string ReleasesRepo = "";

        /// <summary>
        /// Repo to check for newer releases: <see cref="ReleasesRepo"/> when set, else the GitHub
        /// repo in the pin's download URL. Empty for a development package (no pin).
        /// </summary>
        internal static string ResolveReleasesRepo(TryllServerPin pin)
            => !string.IsNullOrEmpty(ReleasesRepo) ? ReleasesRepo : TryllServerPin.RepoFromBaseUrl(pin?.base_url);
    }
}
