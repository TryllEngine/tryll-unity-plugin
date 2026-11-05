// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Threading;
using Tryll.Client.Internal;
using UnityEditor;
using UnityEngine;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// CI smoke entry for a released package installed by Git URL:
    /// <c>Unity -batchmode -projectPath &lt;fresh project&gt; -executeMethod
    /// Tryll.Client.Editor.TryllInstallSmoke.Run</c>, with <c>TRYLL_SERVER_BASE_URL</c>
    /// pointing at the server zips (a local web server) and <c>TRYLL_SERVER_CACHE</c> at a
    /// scratch folder. Downloads the pinned server, verifies and installs it, and exits
    /// 0 on success. <c>TRYLL_SMOKE_VARIANT</c> picks the variant (default <c>lite</c>).
    /// <c>TRYLL_SMOKE_EXPECT_TYPES</c> (full type names, <c>;</c>-separated) fails the run
    /// when a type did not compile, e.g. code behind a versionDefine. Not used by the editor UI.
    /// </summary>
    public static class TryllInstallSmoke
    {
        public static void Run()
        {
            string expected = Environment.GetEnvironmentVariable("TRYLL_SMOKE_EXPECT_TYPES");
            foreach (string typeName in (expected ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (FindType(typeName.Trim()) == null)
                {
                    Exit(6, $"type {typeName.Trim()} was not compiled");
                    return;
                }
                Debug.Log($"[TryllInstallSmoke] compiled: {typeName.Trim()}");
            }

            var pin = TryllServerPin.LoadForPackage();
            if (pin == null)
            {
                Exit(2, "the package has no server pin (Editor/Install/TryllServerPin.json)");
                return;
            }
            string variant = Environment.GetEnvironmentVariable("TRYLL_SMOKE_VARIANT");
            if (string.IsNullOrEmpty(variant))
                variant = TryllServerCache.LiteVariant;

            // The installer never resumes on the main thread, so waiting here cannot deadlock.
            var task = TryllServerInstaller.DownloadAsync(pin, variant, CancellationToken.None);
            if (!task.Wait(TimeSpan.FromMinutes(30)))
            {
                Exit(3, "timed out");
                return;
            }
            var result = task.Result;
            if (!result.Ok)
            {
                Exit(4, $"{result.Code}: {result.Message}");
                return;
            }
            string dir = TryllServerCache.FindInstalled(pin.version, out string installed);
            if (dir == null || installed != variant)
            {
                Exit(5, $"the {variant} server is not in the cache after install");
                return;
            }
            Debug.Log($"[TryllInstallSmoke] OK: {variant} {pin.version} installed in {dir} " +
                      $"({result.Bytes} bytes, {result.Seconds:0.0} s)");
            EditorApplication.Exit(0);
        }

        static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }

        static void Exit(int code, string reason)
        {
            Debug.LogError($"[TryllInstallSmoke] FAILED ({code}): {reason}");
            EditorApplication.Exit(code);
        }
    }
}
