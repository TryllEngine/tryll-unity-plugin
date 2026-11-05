// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Tryll.Client;
using Tryll.Client.Internal;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Copies the bundled server payload from the package's <c>.Server/</c> folder into the
    /// build output so a packaged Windows game can auto-launch <c>tryll_server.exe</c> with
    /// no path configuration.
    ///
    /// The selected build variant (<c>Default</c>/<c>Release</c>, per
    /// <see cref="TryllRuntimeSettings.ServerBuildVariant"/>) is flattened into
    /// <c>&lt;Build&gt;/Server/</c> next to the player exe (no models tree). Project
    /// storage (<see cref="TryllRuntimeSettings.StorageDataFolder"/>, default
    /// <c>TryllStorage/</c>) is staged to <c>&lt;Game&gt;_Data/StreamingAssets/Tryll/</c>
    /// with Production-manifest models under <c>models/</c>.
    /// </summary>
    internal sealed class TryllServerBuildPostprocessor : IPostprocessBuildWithReport
    {
        private const string BuildServerDir = "Server";

        /// <summary>Models folder name under the process storage root.</summary>
        internal const string StorageModelsDirName = "models";

        // Tryll Dev / legacy server caches that must never ship inside Server/.
        private static readonly string AppDataRel =
            Path.Combine("data", ".app-data");
        private static readonly string DevModelsRel =
            Path.Combine("data", ".models");

        // llama-cuda profile in tryll/cmake/TryllDependencyVersions.json plus ggml-cuda.dll.
        // Also skip leftover ORT CUDA extras if a stale .Server/ still has them.
        private static readonly HashSet<string> LlamaCudaDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ggml-cuda.dll",
            "cudart64_12.dll",
            "cublas64_12.dll",
            "cublasLt64_12.dll",
        };
        private static readonly HashSet<string> OrtCudaExtraDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "onnxruntime_providers_cuda.dll",
            "cufft64_11.dll",
            "cufftw64_11.dll",
            "cudnn64_9.dll",
            "cudnn_ops64_9.dll",
            "cudnn_cnn64_9.dll",
            "cudnn_adv64_9.dll",
            "cudnn_graph64_9.dll",
            "cudnn_heuristic64_9.dll",
            "cudnn_engines_precompiled64_9.dll",
            "cudnn_engines_runtime_compiled64_9.dll",
        };

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var platform = report.summary.platform;
            if (platform != BuildTarget.StandaloneWindows64 && platform != BuildTarget.StandaloneWindows)
                return; // Bundled server is Windows-only.

            TryllWorkflowModelPreBuildCheck.LogMultiManifestIfNeeded();

            string buildDir = Path.GetDirectoryName(report.summary.outputPath);
            if (string.IsNullOrEmpty(buildDir))
                throw new BuildFailedException("[Tryll] Could not determine build output directory; cannot stage server.");

            string dest      = Path.Combine(buildDir, BuildServerDir);
            string destTryll = ResolvePlayerStreamingTryllDir(report.summary.outputPath);
            try
            {
                var settings = TryllRuntimeSettings.Load();
                FailIfProjectStreamingTryllExists();

                string source = ResolveServerSource(settings, out string variant);
                if (source == null)
                {
                    string msg =
                        $"[Tryll] Bundled server variant '{variant}/' not found under .Server/; " +
                        "the packaged build will have no server to launch. Copy the server build there, " +
                        "or set AutoLaunchServer = false and manage the server yourself.";
                    if (settings == null || settings.AutoLaunchServer)
                        throw new BuildFailedException(msg);
                    Debug.LogWarning(msg);
                    return;
                }

                bool includeCuda = settings != null
                    && settings.LlamaCppBackend == TryllLlamaCppBackend.Auto;
                if (includeCuda && !File.Exists(Path.Combine(source, "ggml-cuda.dll")))
                {
                    throw new BuildFailedException(
                        "[Tryll] Llama.cpp backend is Auto but ggml-cuda.dll is missing from the " +
                        "bundled server payload. Build the server with TRYLL_CUDA=PREBUILT, or set " +
                        "Project Settings → Tryll Client → Llama Cpp Backend to Vulkan.");
                }

                // Resolve every Production model before anything is deleted or copied, so a
                // missing one fails in seconds instead of after gigabytes of copying.
                // TryllWorkflowModelPreBuildCheck runs the same plan before the player is built.
                string storageRoot = ResolveStorageRoot(settings);
                var production = LoadProductionNames();
                var modelPlan = PlanManifestModels(
                    source, Path.Combine(storageRoot, StorageModelsDirName), production);
                FailIfIncomplete(modelPlan);

                if (!TryResolveClientProd(source, out ClientProdSelection prod, out string prodError))
                {
                    if (settings == null || settings.AutoLaunchServer)
                        throw new BuildFailedException(prodError);
                    Debug.LogWarning(prodError);
                    return;
                }

                // Wipe any previous Server/ so demoted models and stale variant DLLs
                // from incremental rebuilds into the same folder never ship.
                if (Directory.Exists(dest))
                    Directory.Delete(dest, recursive: true);

                int copied = CopyServerPayload(source, dest, includeCuda);
                Directory.CreateDirectory(dest);
                File.Copy(prod.ExePath, Path.Combine(dest, "tryll_server.exe"), overwrite: true);
                File.Copy(prod.ManifestPath, Path.Combine(dest, "tryll_server.build.json"), overwrite: true);
                copied += 2;
                Debug.Log($"[Tryll] Copied bundled server payload '{variant}' ({copied} files) to '{dest}'" +
                          (includeCuda ? " (including llama.cpp CUDA DLLs)." : " (Vulkan: CUDA DLLs omitted)."));

                StageProjectUserModelsDir(dest);
                StageProjectUserModelsCatalog(dest);

                ApplyProductionLogging(dest);

                int storageFiles = StageStorageDirectory(source, storageRoot, destTryll, production, modelPlan);
                Debug.Log($"[Tryll] Staged storage ({storageFiles} files) to '{destTryll}'.");
            }
            catch (Exception ex)
            {
                // Unity has already replaced the player exe and _Data/ by the time this runs,
                // so the previous build cannot be restored. Remove what we staged instead: a
                // build folder without Server/ is visibly broken, while a half-staged one
                // (models but no downloads.json) looks runnable and is not.
                DeletePartialStaging(dest, destTryll);
                if (ex is BuildFailedException)
                    throw;
                throw new BuildFailedException(
                    $"[Tryll] Failed to stage bundled server payload: {ex.Message}");
            }
        }

        /// <summary>
        /// The Editor's server folder (bundled <c>.Server/&lt;variant&gt;/</c>, else the per-user
        /// install), or null when it does not exist. <paramref name="variant"/> is the folder
        /// name (<c>Default</c>, <c>Release</c>, <c>full</c> or <c>lite</c>) for logging.
        /// </summary>
        private static string ResolveServerSource(TryllRuntimeSettings settings, out string variant)
        {
            string source = TryllServerProcess.EditorServerDir(settings);
            variant = string.IsNullOrEmpty(source) ? "" : Path.GetFileName(source);
            return !string.IsNullOrEmpty(source) && Directory.Exists(source) ? source : null;
        }

        private static string ResolveStorageRoot(TryllRuntimeSettings settings)
        {
            return settings != null
                ? settings.ResolveStorageRootForRuntime()
                : TryllRuntimeSettings.ResolveStorageRootForRuntime(
                    TryllRuntimeSettings.DefaultStorageDataFolder);
        }

        /// <summary>
        /// Pre-build half of the model staging: fails the build before the player is
        /// compiled when a Production model or a shared asset it needs is not downloaded.
        /// Skips silently when the server payload is missing; the postprocessor reports that.
        /// </summary>
        internal static void FailIfProductionModelsUnavailable()
        {
            var settings = TryllRuntimeSettings.Load();
            string source = ResolveServerSource(settings, out _);
            if (source == null) return;

            var plan = PlanManifestModels(
                source,
                Path.Combine(ResolveStorageRoot(settings), StorageModelsDirName),
                LoadProductionNames());
            FailIfIncomplete(plan);
        }

        /// <summary>Best-effort removal of the folders this postprocessor stages.</summary>
        private static void DeletePartialStaging(params string[] dirs)
        {
            foreach (string dir in dirs)
            {
                try
                {
                    if (Directory.Exists(dir))
                        Directory.Delete(dir, recursive: true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Tryll] Could not remove partially staged '{dir}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Player storage root under the build's StreamingAssets folder.
        /// </summary>
        internal static string ResolvePlayerStreamingTryllDir(string playerExePath)
        {
            string buildDir = Path.GetDirectoryName(playerExePath);
            string gameName = Path.GetFileNameWithoutExtension(playerExePath);
            return Path.GetFullPath(Path.Combine(buildDir, gameName + "_Data", "StreamingAssets", "Tryll"));
        }

        /// <summary>
        /// Fail closed when the project already has <c>Assets/StreamingAssets/Tryll</c>,
        /// so Unity's unfiltered StreamingAssets copy cannot mix with our staged tree.
        /// </summary>
        internal static void FailIfProjectStreamingTryllExists()
        {
            string projectTryll = Path.Combine(
                TryllRuntimeSettings.ProjectRootDirectory(),
                "Assets", "StreamingAssets", "Tryll");
            if (Directory.Exists(projectTryll))
            {
                throw new BuildFailedException(
                    "[Tryll] Assets/StreamingAssets/Tryll already exists. Remove it — the " +
                    "build postprocessor stages TryllStorage into the player's " +
                    "StreamingAssets/Tryll/ and must not mix with a project copy. " +
                    "Authored content belongs in TryllStorage/ (outside Assets/).");
            }
        }

        /// <summary>
        /// Sets the staged server-config.json to production logging (log_mode=production,
        /// log_level=info) so the shipped game writes a single rotating log instead of per-run
        /// trace logs. Other settings are left as-is.
        /// </summary>
        private static void ApplyProductionLogging(string destServerDir)
        {
            string cfgPath = Path.Combine(destServerDir, "data", "server-config.json");
            if (!File.Exists(cfgPath)) return;

            string cfg = File.ReadAllText(cfgPath);
            cfg = Regex.Replace(cfg, "\"log_mode\"\\s*:\\s*\"[^\"]*\"",  "\"log_mode\": \"production\"");
            cfg = Regex.Replace(cfg, "\"log_level\"\\s*:\\s*\"[^\"]*\"", "\"log_level\": \"info\"");
            cfg = DisableTelemetry(cfg);
            File.WriteAllText(cfgPath, cfg);
            Debug.Log("[Tryll] Staged server-config.json set to production logging (log_mode=production, log_level=info).");
        }

        private static int CopyServerPayload(string source, string dest, bool includeCuda)
        {
            Directory.CreateDirectory(dest);

            string prefix = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            + Path.DirectorySeparatorChar;

            int copied = 0;
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (name == ".gitignore" || name == "README.md") continue; // bookkeeping only
                if (IsFlavorExecutableOrManifest(name)) continue;
                if (!includeCuda && (LlamaCudaDlls.Contains(name) || OrtCudaExtraDlls.Contains(name)))
                    continue;

                string rel = Path.GetFullPath(file).Substring(prefix.Length);
                // Never ship model trees or app-data from the plugin server payload.
                if (IsUnderRelativePrefix(rel, AppDataRel) || IsUnderRelativePrefix(rel, DevModelsRel))
                    continue;

                string target = Path.Combine(dest, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, overwrite: true);
                copied++;
            }
            return copied;
        }

        internal struct ClientProdSelection
        {
            public string ExePath;
            public string ManifestPath;
        }

        /// <summary>
        /// The player executable is ClientProd, either under its release name
        /// <c>tryll_server_client_prod.exe</c> or as <c>tryll_server.exe</c> in a
        /// ClientProd build directory. The manifest must say <c>ClientProd</c>.
        /// </summary>
        internal static bool TryResolveClientProd(
            string source, out ClientProdSelection selection, out string error)
        {
            selection = default;
            error = null;
            string renamedExe = Path.Combine(source, "tryll_server_client_prod.exe");
            string renamedManifest = Path.Combine(source, "tryll_server_client_prod.build.json");
            string directExe = Path.Combine(source, "tryll_server.exe");
            string directManifest = Path.Combine(source, "tryll-build.json");

            string exe;
            string manifest;
            if (File.Exists(renamedExe))
            {
                exe = renamedExe;
                manifest = renamedManifest;
            }
            else
            {
                exe = directExe;
                manifest = directManifest;
            }

            if (!File.Exists(exe) || !File.Exists(manifest))
            {
                error = "[Tryll] Player builds require a ClientProd server "
                    + "(tryll_server_client_prod.exe plus tryll_server_client_prod.build.json, "
                    + "or tryll_server.exe whose tryll-build.json says ClientProd).";
                return false;
            }

            string json = File.ReadAllText(manifest);
            var match = Regex.Match(json, "\"build_config\"\\s*:\\s*\"([^\"]*)\"");
            if (!match.Success || !string.Equals(match.Groups[1].Value, "ClientProd", StringComparison.Ordinal))
            {
                error = "[Tryll] Refusing to stage '" + exe
                    + "': its build manifest is not ClientProd.";
                return false;
            }

            selection.ExePath = exe;
            selection.ManifestPath = manifest;
            return true;
        }

        internal static string DisableTelemetry(string cfg)
        {
            if (string.IsNullOrEmpty(cfg))
                return cfg;
            int key = cfg.IndexOf("\"telemetry\"", StringComparison.Ordinal);
            if (key < 0)
                return cfg;
            int brace = cfg.IndexOf('{', key);
            if (brace < 0)
                return cfg;
            int depth = 0;
            int end = brace;
            for (; end < cfg.Length; ++end)
            {
                if (cfg[end] == '{')
                    depth++;
                else if (cfg[end] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        end++;
                        break;
                    }
                }
            }
            return cfg.Substring(0, key)
                + "\"telemetry\": { \"enabled\": false, \"level\": \"off\" }"
                + cfg.Substring(end);
        }

        private static bool IsFlavorExecutableOrManifest(string name)
        {
            return string.Equals(name, "tryll_server.exe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "tryll_server_client_prod.exe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "tryll-build.json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "tryll_server.build.json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "tryll_server_client_prod.build.json", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnderRelativePrefix(string relativePath, string prefix)
        {
            string norm = relativePath.Replace('/', Path.DirectorySeparatorChar);
            string pref = prefix.Replace('/', Path.DirectorySeparatorChar);
            return norm.StartsWith(pref + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || norm.Equals(pref, StringComparison.OrdinalIgnoreCase);
        }

        // ── Storage + manifest model staging ────────────────────────────────────

        /// <summary>
        /// Copy project storage into the player's StreamingAssets/Tryll, then stage
        /// Production models into models/ with a filtered downloads.json.
        /// </summary>
        private static int StageStorageDirectory(
            string serverSource,
            string storageRoot,
            string destTryll,
            HashSet<string> production,
            ModelStagingPlan modelPlan)
        {
            if (Directory.Exists(destTryll))
                Directory.Delete(destTryll, recursive: true);
            Directory.CreateDirectory(destTryll);

            var excludedLocalPaths = CollectLocalPathFoldersToExclude(serverSource, storageRoot, production);

            int copied = 0;
            if (Directory.Exists(storageRoot))
            {
                string prefix = Path.GetFullPath(storageRoot)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                foreach (string file in Directory.GetFiles(storageRoot, "*", SearchOption.AllDirectories))
                {
                    string rel = Path.GetFullPath(file).Substring(prefix.Length);
                    if (!ShouldCopyStorageFile(rel, excludedLocalPaths))
                        continue;

                    string target = Path.Combine(destTryll, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(file, target, overwrite: true);
                    copied++;
                }
            }

            string destModelsDir = Path.Combine(destTryll, StorageModelsDirName);
            int models = ExecuteModelStagingPlan(modelPlan, destModelsDir);
            if (models > 0)
                Debug.Log($"[Tryll] Staged {models} Production model(s) into '{destModelsDir}'.");

            return copied;
        }

        /// <summary>
        /// Whether a storage-relative path should be copied into StreamingAssets/Tryll.
        /// Excludes the models tree, <c>*.part</c> / temp files, and catalog
        /// <c>local_path</c> folders (Production models are staged separately).
        /// </summary>
        internal static bool ShouldCopyStorageFile(string relativePath, HashSet<string> excludedLocalPathPrefixes)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;

            string norm = relativePath.Replace('\\', '/');
            if (norm.StartsWith(StorageModelsDirName + "/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(norm, StorageModelsDirName, StringComparison.OrdinalIgnoreCase))
                return false;

            string name = Path.GetFileName(norm);
            if (IsTempOrPartFile(name))
                return false;

            if (excludedLocalPathPrefixes != null)
            {
                foreach (string excl in excludedLocalPathPrefixes)
                {
                    if (string.IsNullOrEmpty(excl)) continue;
                    string e = excl.Replace('\\', '/').TrimEnd('/');
                    if (norm.StartsWith(e + "/", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(norm, e, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }
            return true;
        }

        internal static bool IsTempOrPartFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            return fileName.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".temp", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith("~", StringComparison.Ordinal);
        }

        /// <summary>
        /// Storage-relative prefixes for every catalog <c>local_path</c> that sits
        /// inside the storage root. Those folders are never copied as raw storage;
        /// Production ones are staged into <c>models/</c> instead.
        /// </summary>
        /// <summary>
        /// Absolute catalog <c>local_path</c>. A relative value is joined to the
        /// storage root, matching the server. Returns null when it cannot be resolved.
        /// </summary>
        internal static string ResolveCatalogLocalPath(string localPath, string storageRoot)
        {
            if (string.IsNullOrEmpty(localPath))
                return null;
            if (Path.IsPathRooted(localPath))
                return Path.GetFullPath(localPath);
            if (string.IsNullOrEmpty(storageRoot))
                return null;
            return Path.GetFullPath(Path.Combine(storageRoot, localPath));
        }

        internal static HashSet<string> CollectLocalPathFoldersToExclude(
            string serverSource, string storageRoot, HashSet<string> production)
        {
            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(storageRoot) || !Directory.Exists(storageRoot))
                return excluded;

            string storagePrefix = Path.GetFullPath(storageRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            foreach (var model in ReadMergedCatalog(serverSource))
            {
                if (model?.variants == null) continue;
                foreach (var v in model.variants)
                {
                    string full = ResolveCatalogLocalPath(v.local_path, storageRoot);
                    if (string.IsNullOrEmpty(full) || !Directory.Exists(full))
                        continue;
                    if (!full.StartsWith(storagePrefix, StringComparison.OrdinalIgnoreCase))
                        continue;
                    string rel = full.Substring(storagePrefix.Length).Replace('\\', '/').TrimEnd('/');
                    if (!string.IsNullOrEmpty(rel))
                        excluded.Add(rel);
                }
            }
            // production is unused for exclusion — all local_path folders are excluded
            // from the storage copy (Production ones stage via ExecuteModelStagingPlan).
            _ = production;
            return excluded;
        }

        /// <summary>
        /// What staging the Production models would copy, resolved without touching the
        /// build output. Built once per check so the pre-build check and the postprocessor
        /// agree on what is available; anything left in <see cref="MissingModels"/> or
        /// <see cref="MissingAssets"/> fails the build before a single file is copied.
        /// </summary>
        internal sealed class ModelStagingPlan
        {
            public readonly List<StagingCopy>        Copies        = new List<StagingCopy>();
            public readonly List<RegistryEntry>      Staged        = new List<RegistryEntry>();
            public readonly List<AssetRegistryEntry> StagedAssets  = new List<AssetRegistryEntry>();
            public readonly List<string>             MissingModels = new List<string>();
            public readonly List<string>             MissingAssets = new List<string>();

            public bool HasMissing => MissingModels.Count > 0 || MissingAssets.Count > 0;
        }

        /// <summary>One copy into the models folder: a whole tree, or a single file.</summary>
        internal struct StagingCopy
        {
            public string Source;   // absolute
            public string DestRel;  // relative to the destination models folder
            public bool   IsTree;
        }

        /// <summary>
        /// Resolves where every <see cref="TryllModelRegistration.Production"/> model and the
        /// shared assets they depend on come from, checking each source file exists.
        /// Copies nothing.
        /// </summary>
        internal static ModelStagingPlan PlanManifestModels(
            string serverSource,
            string srcModelsDir,
            HashSet<string> production)
        {
            var plan = new ModelStagingPlan();
            if (production.Count == 0) return plan;

            var pending = new HashSet<string>(production);
            string registryPath = Path.Combine(srcModelsDir, "downloads.json");

            foreach (var entry in ReadRegistry(registryPath))
            {
                if (!pending.Contains(entry.name)) continue;
                string srcFolder = Path.Combine(srcModelsDir, entry.folder);
                if (!Directory.Exists(srcFolder))
                    continue;
                // The registry can outlive its files (deleted by hand, a re-download cut
                // off mid-way): only a folder holding every listed file is downloaded.
                if (!HasEveryFile(srcFolder, entry.files))
                    continue;

                plan.Copies.Add(new StagingCopy { Source = srcFolder, DestRel = entry.folder, IsTree = true });
                plan.Staged.Add(entry);
                pending.Remove(entry.name);
            }

            if (pending.Count > 0)
            {
                string storageRoot = Path.GetDirectoryName(srcModelsDir.TrimEnd(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                foreach (var model in ReadMergedCatalog(serverSource))
                {
                    if (!pending.Contains(model.name) || model.variants == null) continue;
                    foreach (var v in model.variants)
                    {
                        string resolvedLocal = ResolveCatalogLocalPath(v.local_path, storageRoot);
                        if (string.IsNullOrEmpty(resolvedLocal) || !Directory.Exists(resolvedLocal)) continue;
                        if (v.files == null || v.files.Count == 0) continue;
                        if (!v.files.TrueForAll(f => File.Exists(Path.Combine(resolvedLocal, f)))) continue;

                        string folder = !string.IsNullOrEmpty(v.huggingface_repo)
                            ? RepoToFolder(v.huggingface_repo)
                            : SanitizeFolder(model.name);

                        long total = 0;
                        var bakedRels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var f in v.files)
                        {
                            string srcFile = Path.Combine(resolvedLocal, f);
                            plan.Copies.Add(new StagingCopy { Source = srcFile, DestRel = Path.Combine(folder, f) });
                            total += new FileInfo(srcFile).Length;

                            // The bake writes bpe.vocab beside a speech model's tokens.txt;
                            // it is not in `files`, and Client Prod cannot derive it.
                            string fileDir = Path.GetDirectoryName(f) ?? string.Empty;
                            foreach (string baked in new[] { "bpe.vocab", "bpe.vocab.json" })
                            {
                                string rel = Path.Combine(fileDir, baked);
                                if (bakedRels.Add(rel) && File.Exists(Path.Combine(resolvedLocal, rel)))
                                    plan.Copies.Add(new StagingCopy { Source = Path.Combine(resolvedLocal, rel), DestRel = Path.Combine(folder, rel) });
                            }
                        }

                        plan.Staged.Add(new RegistryEntry
                        {
                            name = model.name, engine = v.engine, folder = folder,
                            files = v.files, total_bytes = total,
                        });
                        pending.Remove(model.name);
                        break;
                    }
                }
            }

            plan.MissingModels.AddRange(pending);
            PlanReachableAssets(serverSource, registryPath, production, plan);
            return plan;
        }

        private static void PlanReachableAssets(
            string serverSource,
            string registryPath,
            HashSet<string> production,
            ModelStagingPlan plan)
        {
            var needed = CollectReachableAssetIdsFromMerged(serverSource, production);
            if (needed.Count == 0) return;

            string srcModelsDir = Path.GetDirectoryName(registryPath);
            foreach (var entry in ReadRegistryAssets(registryPath))
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || !needed.Contains(entry.id))
                    continue;
                string srcFolder = Path.Combine(srcModelsDir, entry.folder ?? "");
                if (string.IsNullOrEmpty(entry.folder) || !Directory.Exists(srcFolder))
                    continue;
                if (!HasEveryFile(srcFolder, entry.files))
                    continue;

                plan.Copies.Add(new StagingCopy { Source = srcFolder, DestRel = entry.folder, IsTree = true });
                plan.StagedAssets.Add(entry);
                needed.Remove(entry.id);
            }

            plan.MissingAssets.AddRange(needed);
        }

        /// <summary>True when <paramref name="files"/> is non-empty and each exists under <paramref name="folder"/>.</summary>
        private static bool HasEveryFile(string folder, List<string> files)
        {
            return files != null && files.Count > 0
                && files.TrueForAll(f => !string.IsNullOrEmpty(f) && File.Exists(Path.Combine(folder, f)));
        }

        /// <summary>Throws <see cref="BuildFailedException"/> naming everything the plan could not source.</summary>
        private static void FailIfIncomplete(ModelStagingPlan plan)
        {
            if (!plan.HasMissing) return;

            var sb = new StringBuilder();
            if (plan.MissingModels.Count > 0)
            {
                sb.Append("[Tryll] These models are marked Production in the Tryll Model Manifest but are not ")
                  .Append("downloaded/available under the project storage models folder, so they cannot be staged:\n  - ")
                  .Append(string.Join("\n  - ", plan.MissingModels))
                  .Append("\nDownload them in the Tryll Model Manager (or fix their local_path), ")
                  .Append("or unmark them as Production, then build again.");
            }
            if (plan.MissingAssets.Count > 0)
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append("[Tryll] Production models require shared assets that are not downloaded: ")
                  .Append(string.Join(", ", plan.MissingAssets))
                  .Append("\nDownload them in the Tryll Model Manager, then build again.");
            }
            throw new BuildFailedException(sb.ToString());
        }

        /// <summary>
        /// Copies a complete plan into <paramref name="destModelsDir"/> and writes its
        /// filtered <c>downloads.json</c>. The plan must already have passed
        /// <see cref="FailIfIncomplete"/>.
        /// </summary>
        private static int ExecuteModelStagingPlan(ModelStagingPlan plan, string destModelsDir)
        {
            if (plan.Staged.Count == 0) return 0;   // no Production models: no registry

            foreach (var copy in plan.Copies)
            {
                string target = Path.Combine(destModelsDir, copy.DestRel);
                if (copy.IsTree)
                {
                    CopyTree(copy.Source, target);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(copy.Source, target, overwrite: true);
            }

            WriteRegistry(Path.Combine(destModelsDir, "downloads.json"), plan.Staged, plan.StagedAssets);
            return plan.Staged.Count;
        }

        private static HashSet<string> LoadProductionNames()
        {
            var names = new HashSet<string>();
            var manifest = TryllModelManifest.FindProjectManifest();
            if (manifest == null) return names;

            foreach (var e in manifest.Entries)
                if (e.Registration == TryllModelRegistration.Production && !string.IsNullOrEmpty(e.Name))
                    names.Add(e.Name);
            return names;
        }

        private static void CopyTree(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            string prefix = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            + Path.DirectorySeparatorChar;
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (IsTempOrPartFile(Path.GetFileName(file)))
                    continue;
                string rel    = Path.GetFullPath(file).Substring(prefix.Length);
                string target = Path.Combine(dest, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, overwrite: true);
            }
        }

        private static string RepoToFolder(string repo) => repo.Replace("/", "--");

        private static string SanitizeFolder(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '-');
            return name;
        }

        private static List<RegistryEntry> ReadRegistry(string path)
        {
            if (!File.Exists(path)) return new List<RegistryEntry>();
            var root = JsonUtility.FromJson<RegistryRoot>(File.ReadAllText(path));
            return root?.downloads ?? new List<RegistryEntry>();
        }

        private static List<AssetRegistryEntry> ReadRegistryAssets(string path)
        {
            if (!File.Exists(path)) return new List<AssetRegistryEntry>();
            var root = JsonUtility.FromJson<RegistryRoot>(File.ReadAllText(path));
            return root?.assets ?? new List<AssetRegistryEntry>();
        }

        private static void WriteRegistry(string path, List<RegistryEntry> entries, List<AssetRegistryEntry> assets)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(
                new RegistryRoot { downloads = entries, assets = assets }, prettyPrint: true));
        }

        private static HashSet<string> CollectReachableAssetIds(string catalogPath, HashSet<string> production)
        {
            var needed = new HashSet<string>();
            if (!File.Exists(catalogPath)) return needed;
            string json = File.ReadAllText(catalogPath);
            foreach (var name in production)
            {
                var nameMatch = Regex.Match(
                    json,
                    "\"name\"\\s*:\\s*\"" + Regex.Escape(name) + "\"");
                if (!nameMatch.Success) continue;
                string modelObject = ExtractJsonObjectContaining(json, nameMatch.Index);
                if (string.IsNullOrEmpty(modelObject)) continue;
                var depMatch = Regex.Match(modelObject, "\"dependencies\"\\s*:\\s*\\{([^}]*)\\}");
                if (!depMatch.Success) continue;
                foreach (Match id in Regex.Matches(depMatch.Groups[1].Value, "\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\""))
                {
                    if (!string.IsNullOrEmpty(id.Groups[2].Value))
                        needed.Add(id.Groups[2].Value);
                }
            }
            return needed;
        }

        private static string ExtractJsonObjectContaining(string json, int indexInside)
        {
            int start = 0;
            int depth = 0;
            for (int i = indexInside; i >= 0; --i)
            {
                if (json[i] == '}')
                    depth++;
                else if (json[i] == '{')
                {
                    if (depth == 0)
                    {
                        start = i;
                        break;
                    }
                    depth--;
                }
            }

            depth = 0;
            for (int i = start; i < json.Length; ++i)
            {
                if (json[i] == '{')
                    depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return json.Substring(start, i - start + 1);
                }
            }
            return string.Empty;
        }

        private static void StageProjectUserModelsDir(string destServerDir)
        {
            string projectModelsD = Path.Combine(
                TryllRuntimeSettings.ProjectRootDirectory(), "Tryll", "models.d");
            if (!Directory.Exists(projectModelsD))
                return;

            string destModelsD = Path.Combine(destServerDir, "data", "models.d");
            Directory.CreateDirectory(destModelsD);
            int n = 0;
            foreach (var src in Directory.GetFiles(projectModelsD, "*.json"))
            {
                string name = Path.GetFileName(src);
                File.Copy(src, Path.Combine(destModelsD, name), overwrite: true);
                ++n;
            }
            foreach (var src in Directory.GetFiles(projectModelsD))
            {
                if (src.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;
                string name = Path.GetFileName(src);
                File.Copy(src, Path.Combine(destModelsD, name), overwrite: true);
                ++n;
            }
            if (n > 0)
                Debug.Log($"[Tryll] Staged {n} file(s) from project Tryll/models.d into '{destModelsD}'.");
        }

        private static void StageProjectUserModelsCatalog(string destServerDir)
        {
            var settings = TryllRuntimeSettings.Load();
            string full = settings.ResolveUserModelsCatalogForLaunch();
            if (string.IsNullOrEmpty(full) || !File.Exists(full))
                return;

            string destDir = Path.Combine(
                destServerDir,
                TryllRuntimeSettings.StagedUserCatalogDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(destDir);
            string json = RewriteCatalogChatTemplates(full, destDir);
            string dest = Path.Combine(destDir, Path.GetFileName(full));
            File.WriteAllText(dest, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Debug.Log($"[Tryll] Staged custom models catalog '{settings.UserModelsCatalogPath}' into '{dest}'.");
        }

        private static string RewriteCatalogChatTemplates(string catalogPath, string destDir)
        {
            string json = File.ReadAllText(catalogPath);
            string catalogDir = Path.GetDirectoryName(catalogPath) ?? string.Empty;
            string root = Path.GetFullPath(TryllRuntimeSettings.ProjectRootDirectory());
            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            int n = 0;
            return Regex.Replace(
                json,
                "\"chat_template_file\"\\s*:\\s*\"([^\"]*)\"",
                match =>
                {
                    string value = match.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value))
                        return match.Value;

                    string src = Path.GetFullPath(Path.Combine(
                        catalogDir, value.Replace('/', Path.DirectorySeparatorChar)));
                    if (!src.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(src))
                    {
                        Debug.LogWarning(
                            "[Tryll] chat_template_file '" + value + "' in '" + catalogPath +
                            "' is missing or outside the project; left unchanged in the staged catalog.");
                        return match.Value;
                    }

                    string destRel = "sidecars/" + n + "_" + Path.GetFileName(src);
                    ++n;
                    string destFile = Path.Combine(destDir, destRel.Replace('/', Path.DirectorySeparatorChar));
                    string destParent = Path.GetDirectoryName(destFile);
                    if (!string.IsNullOrEmpty(destParent))
                        Directory.CreateDirectory(destParent);
                    File.Copy(src, destFile, overwrite: true);
                    return "\"chat_template_file\": \"" + destRel + "\"";
                },
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static List<CatalogModel> ReadMergedCatalog(string serverSource)
        {
            var byName = new Dictionary<string, CatalogModel>(StringComparer.Ordinal);
            void MergePath(string path, bool isBase)
            {
                if (!File.Exists(path)) return;
                CatalogRoot root;
                try
                {
                    root = JsonUtility.FromJson<CatalogRoot>(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    if (isBase) throw;
                    Debug.LogWarning(
                        $"[Tryll] [user catalog] '{path}' could not be parsed and was skipped: {ex.Message}");
                    return;
                }
                if (root?.models == null) return;
                foreach (var model in root.models)
                {
                    if (model == null || string.IsNullOrEmpty(model.name)) continue;
                    if (byName.ContainsKey(model.name))
                    {
                        if (!isBase)
                        {
                            Debug.LogWarning(
                                $"[Tryll] [user catalog] '{path}' defines model '{model.name}', " +
                                "which an earlier catalog already defines. The earlier/base entry wins.");
                        }
                        continue;
                    }
                    byName[model.name] = model;
                }
            }

            MergePath(Path.Combine(serverSource, "data", "models.json"), isBase: true);
            string userCatalog = TryllRuntimeSettings.Load().ResolveUserModelsCatalogForLaunch();
            if (!string.IsNullOrEmpty(userCatalog))
                MergePath(userCatalog, isBase: false);

            void MergeDir(string dir)
            {
                if (!Directory.Exists(dir)) return;
                var files = Directory.GetFiles(dir, "*.json");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (var f in files)
                    MergePath(f, isBase: false);
            }

            MergeDir(Path.Combine(serverSource, "data", "models.d"));
            MergeDir(Path.Combine(TryllRuntimeSettings.ProjectRootDirectory(), "Tryll", "models.d"));
            return new List<CatalogModel>(byName.Values);
        }

        private static HashSet<string> CollectReachableAssetIdsFromMerged(
            string serverSource, HashSet<string> production)
        {
            var needed = new HashSet<string>();
            var paths = new List<string> { Path.Combine(serverSource, "data", "models.json") };
            string userCatalog = TryllRuntimeSettings.Load().ResolveUserModelsCatalogForLaunch();
            if (!string.IsNullOrEmpty(userCatalog))
                paths.Add(userCatalog);
            void AddDir(string dir)
            {
                if (!Directory.Exists(dir)) return;
                var files = Directory.GetFiles(dir, "*.json");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                paths.AddRange(files);
            }
            AddDir(Path.Combine(serverSource, "data", "models.d"));
            AddDir(Path.Combine(TryllRuntimeSettings.ProjectRootDirectory(), "Tryll", "models.d"));

            foreach (var catalogPath in paths)
            {
                foreach (var id in CollectReachableAssetIds(catalogPath, production))
                    needed.Add(id);
            }
            return needed;
        }

        [Serializable] internal sealed class RegistryRoot
        {
            public List<RegistryEntry> downloads = new List<RegistryEntry>();
            public List<AssetRegistryEntry> assets = new List<AssetRegistryEntry>();
        }
        [Serializable] internal sealed class RegistryEntry
        {
            public string       name;
            public string       engine;
            public string       folder;
            public List<string> files;
            public long         total_bytes;
        }
        [Serializable] internal sealed class AssetRegistryEntry
        {
            public string       id;
            public string       folder;
            public List<string> files;
            public long         total_bytes;
        }

        [Serializable] private sealed class CatalogRoot    { public List<CatalogModel> models; }
        [Serializable] private sealed class CatalogModel   { public string name; public List<CatalogVariant> variants; }
        [Serializable] private sealed class CatalogVariant
        {
            public string       engine;
            public string       huggingface_repo;
            public string       local_path;
            public List<string> files;
        }
    }
}
