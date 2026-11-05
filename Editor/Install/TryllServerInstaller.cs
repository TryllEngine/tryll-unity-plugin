// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Tryll.Client.Internal;
using UnityEngine;

namespace Tryll.Client.Editor
{
    internal enum TryllServerInstallState
    {
        /// <summary>The package carries <c>.Server/</c> (plugin development, partner archive).</summary>
        Bundled,
        /// <summary>The per-user install for this package version exists.</summary>
        Installed,
        /// <summary>A released package without its server yet.</summary>
        NotInstalled,
        /// <summary>No bundled server and no pin: nothing to download.</summary>
        NoPin,
    }

    /// <summary>
    /// Downloads, verifies and installs the Tryll server for a released package into
    /// <see cref="TryllServerCache"/>. No UI: the Welcome window drives it and polls
    /// <see cref="Progress"/>. Every install path ends in <see cref="InstallZipAsync"/>,
    /// which refuses a zip whose SHA-256 is not in the package's pin.
    /// </summary>
    internal static class TryllServerInstaller
    {
        internal const string TokenEnvVar = "TRYLL_SERVER_DOWNLOAD_TOKEN";

        static readonly Regex GitHubReleaseAsset = new Regex(
            @"^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/releases/download/(?<tag>[^/]+)/(?<file>[^/]+)$",
            RegexOptions.Compiled);

        /// <summary>0..1 while a download runs; read by the UI thread.</summary>
        internal static float Progress => Volatile.Read(ref s_progress);
        internal static bool Busy => Volatile.Read(ref s_busy) != 0;
        static float s_progress;
        static int s_busy;

        internal sealed class Result
        {
            public bool Ok;
            /// <summary>ok, hash_mismatch, network, cancelled, extract_failed, no_pin, busy.</summary>
            public string Code = "ok";
            public string Message = "";
            public string Variant = "";
            public long Bytes;
            public double Seconds;

            internal static Result Fail(string code, string message) => new Result { Ok = false, Code = code, Message = message };
        }

        internal static TryllServerInstallState State(TryllServerPin pin, out string installedVariant)
        {
            installedVariant = null;
            if (TryllServerProcess.EditorHasBundledServer())
                return TryllServerInstallState.Bundled;
            string version = pin != null ? pin.version : TryllVersion.Version;
            if (TryllServerCache.FindInstalled(version, out installedVariant) != null)
                return TryllServerInstallState.Installed;
            return pin == null ? TryllServerInstallState.NoPin : TryllServerInstallState.NotInstalled;
        }

        /// <summary>Download <paramref name="variantName"/> from the pin's URL and install it.</summary>
        internal static async Task<Result> DownloadAsync(TryllServerPin pin, string variantName, CancellationToken ct)
        {
            if (pin == null)
                return Result.Fail("no_pin", "This package does not name a server build.");
            var variant = pin.Find(variantName);
            if (variant == null)
                return Result.Fail("no_pin", $"This package does not name a '{variantName}' server build.");
            string baseUrl = pin.EffectiveBaseUrl();
            if (string.IsNullOrEmpty(baseUrl))
                return Result.Fail("no_pin", "This package has no download location. Use \"Install from zip\".");
            if (Interlocked.Exchange(ref s_busy, 1) != 0)
                return Result.Fail("busy", "A server download is already running.");

            var started = DateTime.UtcNow;
            string temp = Path.Combine(Path.GetTempPath(), $"tryll-server-{Guid.NewGuid():N}.zip");
            try
            {
                Volatile.Write(ref s_progress, 0f);
                string url = baseUrl + variant.file;
                await DownloadToFileAsync(url, temp, variant.size, ct).ConfigureAwait(false);
                var result = await InstallZipAsync(pin, temp, variant.name, ct).ConfigureAwait(false);
                result.Seconds = (DateTime.UtcNow - started).TotalSeconds;
                return result;
            }
            catch (OperationCanceledException)
            {
                return Result.Fail("cancelled", "Download cancelled.");
            }
            catch (HttpRequestException ex)
            {
                return Result.Fail("network", ex.Message);
            }
            catch (IOException ex)
            {
                return Result.Fail("network", ex.Message);
            }
            finally
            {
                TryDelete(temp);
                Volatile.Write(ref s_progress, 0f);
                Volatile.Write(ref s_busy, 0);
            }
        }

        /// <summary>
        /// Verify <paramref name="zipPath"/> against the pin and unpack it into the per-user
        /// install. With <paramref name="expectedVariant"/> null (install from a zip the user
        /// picked), the variant is whichever pinned zip has the same SHA-256.
        /// </summary>
        internal static async Task<Result> InstallZipAsync(TryllServerPin pin, string zipPath, string expectedVariant, CancellationToken ct)
        {
            if (pin == null)
                return Result.Fail("no_pin", "This package does not name a server build, so it cannot check the zip.");
            if (!File.Exists(zipPath))
                return Result.Fail("extract_failed", $"File not found: {zipPath}");

            long bytes = new FileInfo(zipPath).Length;
            string sha = await Task.Run(() => Sha256Hex(zipPath, ct), ct).ConfigureAwait(false);
            var variant = expectedVariant != null ? pin.Find(expectedVariant) : pin.FindBySha256(sha);
            if (variant == null || !string.Equals(variant.sha256, sha, StringComparison.OrdinalIgnoreCase)
                || (variant.size > 0 && variant.size != bytes))
            {
                return new Result
                {
                    Ok = false, Code = "hash_mismatch", Bytes = bytes, Variant = expectedVariant ?? "",
                    Message = "The server zip does not match this package. It was not installed.",
                };
            }

            string dest = TryllServerCache.VariantDir(pin.version, variant.name);
            string staging = dest + ".partial-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            try
            {
                await Task.Run(() => ExtractSafely(zipPath, staging, ct), ct).ConfigureAwait(false);
                if (!File.Exists(Path.Combine(staging, TryllServerProcess.ExeName)))
                    return Result.Fail("extract_failed", $"The zip has no {TryllServerProcess.ExeName}.");
                ReplaceDirectory(staging, dest);
                return new Result { Ok = true, Code = "ok", Variant = variant.name, Bytes = bytes };
            }
            catch (OperationCanceledException)
            {
                return Result.Fail("cancelled", "Install cancelled.");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
            {
                return Result.Fail("extract_failed", ex.Message);
            }
            finally
            {
                TryDeleteDirectory(staging);
            }
        }

        // ------------------------------------------------------------------
        // Download
        // ------------------------------------------------------------------

        static async Task DownloadToFileAsync(string url, string path, long expectedSize, CancellationToken ct)
        {
            string token = Environment.GetEnvironmentVariable(TokenEnvVar);
            string directUrl = url;
            if (!string.IsNullOrEmpty(token))
                directUrl = await ResolvePrivateAssetUrlAsync(url, token, ct).ConfigureAwait(false);

            using (var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                using (var response = await client.GetAsync(directUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"HTTP {(int)response.StatusCode} for {Redact(url)}");
                    long total = response.Content.Headers.ContentLength ?? expectedSize;
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20))
                    {
                        var buffer = new byte[1 << 20];
                        long done = 0;
                        int read;
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                            done += read;
                            if (total > 0)
                                Volatile.Write(ref s_progress, Mathf.Clamp01((float)done / total));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Private repos (the test repos) serve release assets only through the API with a
        /// token. Look the asset up, then follow its one redirect by hand so the token is
        /// never sent to the storage host the redirect points at.
        /// </summary>
        static async Task<string> ResolvePrivateAssetUrlAsync(string url, string token, CancellationToken ct)
        {
            var match = GitHubReleaseAsset.Match(url);
            if (!match.Success)
                return url;
            string owner = match.Groups["owner"].Value, repo = match.Groups["repo"].Value;
            string tag = Uri.UnescapeDataString(match.Groups["tag"].Value);
            string file = Uri.UnescapeDataString(match.Groups["file"].Value);

            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using (var api = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) })
            {
                api.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var releaseRequest = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{Uri.EscapeDataString(tag)}");
                releaseRequest.Headers.Accept.ParseAdd("application/vnd.github+json");
                string json;
                using (var response = await api.SendAsync(releaseRequest, ct).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"GitHub API: HTTP {(int)response.StatusCode} for release {tag}");
                    json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                long assetId = FindAssetId(json, file);
                if (assetId <= 0)
                    throw new HttpRequestException($"Release {tag} has no asset named {file}");

                var assetRequest = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.github.com/repos/{owner}/{repo}/releases/assets/{assetId}");
                assetRequest.Headers.Accept.ParseAdd("application/octet-stream");
                using (var response = await api.SendAsync(assetRequest, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
                {
                    var location = response.Headers.Location;
                    if (((int)response.StatusCode == 302 || (int)response.StatusCode == 301) && location != null)
                        return location.IsAbsoluteUri ? location.AbsoluteUri : new Uri(new Uri("https://api.github.com"), location).AbsoluteUri;
                    throw new HttpRequestException($"GitHub API: expected a redirect for asset {file}, got HTTP {(int)response.StatusCode}");
                }
            }
        }

        [Serializable] sealed class ReleaseJson { public AssetJson[] assets; }
        [Serializable] sealed class AssetJson { public long id; public string name; }

        internal static long FindAssetId(string releaseJson, string fileName)
        {
            var release = JsonUtility.FromJson<ReleaseJson>(releaseJson);
            if (release?.assets == null) return 0;
            foreach (var asset in release.assets)
                if (asset != null && asset.name == fileName)
                    return asset.id;
            return 0;
        }

        static string UserAgent => $"TryllUnity/{TryllVersion.Version}";

        static string Redact(string url)
        {
            int q = url.IndexOf('?');
            return q < 0 ? url : url.Substring(0, q);
        }

        // ------------------------------------------------------------------
        // Verify and extract
        // ------------------------------------------------------------------

        internal static string Sha256Hex(string path, CancellationToken ct = default)
        {
            using (var sha = SHA256.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
            {
                var buffer = new byte[1 << 20];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    sha.TransformBlock(buffer, 0, read, null, 0);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                var hex = new System.Text.StringBuilder(64);
                foreach (byte b in sha.Hash)
                    hex.Append(b.ToString("x2"));
                return hex.ToString();
            }
        }

        /// <summary>
        /// Unpack <paramref name="zipPath"/> into <paramref name="destDir"/>. An entry that
        /// would land outside it (<c>../</c>, an absolute path, a drive letter) fails the
        /// whole install: a "zip slip" must never write anywhere else on disk.
        /// </summary>
        internal static void ExtractSafely(string zipPath, string destDir, CancellationToken ct = default)
        {
            string root = Path.GetFullPath(destDir);
            string rootWithSep = root.EndsWith(Path.DirectorySeparatorChar.ToString()) ? root : root + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(root);
            // ZipArchive (System.IO.Compression.dll) rather than ZipFile: it is referenced under
            // both API compatibility levels a project may pick (.NET Standard and .NET Framework).
            using (var stream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    ct.ThrowIfCancellationRequested();
                    string name = entry.FullName.Replace('\\', '/');
                    if (string.IsNullOrEmpty(name))
                        continue;
                    string target = Path.GetFullPath(Path.Combine(root, name));
                    if (!target.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException($"zip entry escapes the install folder: {entry.FullName}");
                    if (name.EndsWith("/", StringComparison.Ordinal))
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    using (var input = entry.Open())
                    using (var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20))
                        input.CopyTo(output, 1 << 20);
                }
            }
        }

        /// <summary>Swap <paramref name="staging"/> in as <paramref name="dest"/>.</summary>
        static void ReplaceDirectory(string staging, string dest)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            if (Directory.Exists(dest))
            {
                string old = dest + ".old-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                // A running server keeps its exe locked; moving the folder fails then.
                Directory.Move(dest, old);
                TryDeleteDirectory(old);
            }
            Directory.Move(staging, dest);
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception) { /* temp file; the OS cleans it up */ }
        }

        static void TryDeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch (Exception) { /* leftover staging folder; harmless, retried next install */ }
        }
    }
}
