// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Tryll.Client;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Feature-local Model Manager presentation core: decimal/download formatters,
    /// filter/placement predicates and download-job captions.
    /// </summary>
    internal static class TryllModelManagerPresentation
    {
        public static string FmtDecimalBytes(long b)
        {
            if (b >= 1_000_000_000) return (b / 1e9).ToString("0.00", CultureInfo.InvariantCulture) + " GB";
            if (b >= 1_000_000)     return (b / 1e6).ToString("0",    CultureInfo.InvariantCulture) + " MB";
            if (b <= 0)             return "—";
            return (b / 1e3).ToString("0", CultureInfo.InvariantCulture) + " KB";
        }

        public static string FmtSpeed(float bytesPerSec)
        {
            if (bytesPerSec >= 1_000_000f) return (bytesPerSec / 1e6f).ToString("0.0", CultureInfo.InvariantCulture) + " MB/s";
            if (bytesPerSec >= 1000f)      return (bytesPerSec / 1000f).ToString("0", CultureInfo.InvariantCulture) + " KB/s";
            return bytesPerSec.ToString("0", CultureInfo.InvariantCulture) + " B/s";
        }

        public static string FmtEta(float seconds)
        {
            if (seconds <= 0f || float.IsInfinity(seconds) || float.IsNaN(seconds)) return "—";
            int total = (int)Math.Round(seconds);
            int h = total / 3600, m = (total % 3600) / 60, s = total % 60;
            return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m}:{s:00}";
        }

        public static bool SupportsPlacement(string modelType) =>
            !string.Equals(modelType, "vad", StringComparison.OrdinalIgnoreCase);

        public static bool ShowsPlacement(bool isRegistered, string modelType) =>
            isRegistered && SupportsPlacement(modelType);

        public static bool MatchesSearch(string name, string huggingFaceRepo, string searchLower)
        {
            if (string.IsNullOrEmpty(searchLower)) return true;
            return (name ?? "").ToLowerInvariant().Contains(searchLower)
                || (huggingFaceRepo ?? "").ToLowerInvariant().Contains(searchLower);
        }

        /// <summary>
        /// Dimension match. When <paramref name="emptyMeansAll"/> is true (Unreal),
        /// an empty set does not constrain. When false (Unity), empty matches nothing.
        /// </summary>
        public static bool Included<T>(ICollection<T> selected, T value, bool emptyMeansAll)
        {
            if (selected == null || selected.Count == 0) return emptyMeansAll;
            return selected.Contains(value);
        }

        public static bool MatchesFilters(
            bool typeOk, bool engineOk, bool statusOk, bool audienceOk, bool registrationOk,
            string name, string huggingFaceRepo, string searchLower)
        {
            return typeOk && engineOk && statusOk && audienceOk && registrationOk
                && MatchesSearch(name, huggingFaceRepo, searchLower);
        }

        public static string FormatDownloadProgress(long bytes, long total, float speed, float remainSeconds) =>
            $"{FmtDecimalBytes(bytes)} / {FmtDecimalBytes(total)} · {FmtSpeed(speed)} · ETA {FmtEta(remainSeconds)}";

        public static string FormatDownloadDone(long size) =>
            $"{FmtDecimalBytes(size)} · done";

        public static string FormatDownloadFailed(string error) =>
            string.IsNullOrEmpty(error) ? "Failed" : $"Failed — {error}";

        public static bool ShouldApplyListGeneration(int requestGeneration, int currentGeneration)
            => requestGeneration == currentGeneration;

        public enum CatalogType { Language, Embedding, Stt, Tts, Vad }
        public enum DownloadPhase { Active, Completed, Failed }

        public struct CatalogRow
        {
            public string Name;
            public CatalogType Type;
            public TryllModelStatus Status;
            public string Engine;
            public string Audience;
            public string ToolFormat;
            public string ToolSupport;
            public string[] Files;
            public int ContextSize;
            public string Sampling;
            public bool IsLocalOnly;
            public bool IsIncomplete;
            public bool IsAbsent;
            public bool IsMalformed;
        }

        public sealed class DownloadJob
        {
            public string Name;
            public DownloadPhase Phase = DownloadPhase.Active;
            public ulong Bytes;
            public ulong Total;
            public float Percent;
            public string Error;
            public double LastTime;
            public ulong LastBytes;
            public float Speed;
        }

        public static CatalogType MapModelType(string t)
        {
            switch (t)
            {
                case "embedding": return CatalogType.Embedding;
                case "stt": return CatalogType.Stt;
                case "tts": return CatalogType.Tts;
                case "vad": return CatalogType.Vad;
                default: return CatalogType.Language;
            }
        }

        public static string AudienceOrUnset(string audience) =>
            string.IsNullOrEmpty(audience) ? "(unset)" : audience;

        public static string FormatSampling(in TryllSamplingParams s)
        {
            var parts = new List<string> { $"T={s.Temperature:0.##}" };
            if (s.TopP < 1.0f) parts.Add($"top_p={s.TopP:0.##}");
            if (s.TopK > 0) parts.Add($"top_k={s.TopK}");
            if (s.MinP > 0.0f) parts.Add($"min_p={s.MinP:0.##}");
            if (s.MaxTokens > 0) parts.Add($"max_tokens={s.MaxTokens}");
            if (s.RepeatPenalty != 1.0f) parts.Add($"repeat={s.RepeatPenalty:0.##}");
            return string.Join(", ", parts);
        }

        public static TryllInferenceEngine ParseEngine(string engine)
        {
            switch (engine)
            {
                case "mock": return TryllInferenceEngine.Mock;
                case "llama-cpp": return TryllInferenceEngine.LlamaCpp;
                case "onnx-genai": return TryllInferenceEngine.OnnxGenAI;
                case "windows-ml": return TryllInferenceEngine.WindowsML;
                case "openvino": return TryllInferenceEngine.OpenVino;
                case "tensorrt-llm": return TryllInferenceEngine.TensorRtLlm;
                case "sherpa-onnx": return TryllInferenceEngine.SherpaOnnx;
                case "onnx-runtime": return TryllInferenceEngine.OnnxRuntime;
                default: return TryllInferenceEngine.LlamaCpp;
            }
        }

        public static CatalogRow ProjectCatalog(in TryllModelInfo model)
        {
            bool malformed = string.IsNullOrEmpty(model.Name);
            return new CatalogRow
            {
                Name = model.Name ?? "",
                Type = MapModelType(model.ModelType),
                Status = model.Status,
                Engine = model.Engine ?? "",
                Audience = AudienceOrUnset(model.Audience),
                ToolFormat = model.ToolCallFormat ?? "",
                ToolSupport = model.ToolCallSupport ?? "",
                Files = model.Files ?? Array.Empty<string>(),
                ContextSize = model.ContextSize,
                Sampling = model.HasDefaultSampling ? FormatSampling(model.DefaultSampling) : null,
                IsLocalOnly = string.IsNullOrEmpty(model.HuggingFaceRepo),
                IsIncomplete = model.Status == TryllModelStatus.Incomplete,
                IsAbsent = model.Status == TryllModelStatus.Absent,
                IsMalformed = malformed,
            };
        }

        public static int ResolveRegistration(bool hasManifest, int stored) =>
            hasManifest ? stored : 0;

        public static bool IsMissingRegistered(int registration, TryllModelStatus status) =>
            registration != 0 && status == TryllModelStatus.Absent;

        public static bool ShouldPromptVadCompanion(CatalogType type, int newRegistration, int vadRegistration) =>
            type == CatalogType.Stt && newRegistration != 0 && vadRegistration == 0;

        public static TryllModelStatus StatusAfterDownloadProgress() => TryllModelStatus.Downloading;

        public static TryllModelStatus StatusAfterDownloadComplete(bool success) =>
            success ? TryllModelStatus.Downloaded : TryllModelStatus.Absent;

        public static bool ApplyDownloadProgress(
            List<DownloadJob> jobs, string name, ulong bytes, ulong total, float percent, double now)
        {
            var job = jobs.Find(j => j.Name == name);
            bool isNew = job == null;
            if (isNew)
            {
                job = new DownloadJob { Name = name, LastTime = now, LastBytes = bytes };
                jobs.Insert(0, job);
            }

            double dt = now - job.LastTime;
            if (!isNew && dt > 0.05 && bytes >= job.LastBytes)
            {
                float inst = (float)((bytes - job.LastBytes) / dt);
                job.Speed = job.Speed > 0f ? job.Speed + (inst - job.Speed) * 0.3f : inst;
                job.LastTime = now;
                job.LastBytes = bytes;
            }

            job.Phase = DownloadPhase.Active;
            job.Bytes = bytes;
            job.Total = total;
            job.Percent = percent;
            return isNew;
        }

        public static void ApplyDownloadComplete(
            List<DownloadJob> jobs, string name, bool success, string error)
        {
            var job = jobs.Find(j => j.Name == name);
            if (job == null)
            {
                job = new DownloadJob { Name = name };
                jobs.Insert(0, job);
            }
            job.Phase = success ? DownloadPhase.Completed : DownloadPhase.Failed;
            job.Error = error;
            job.Speed = 0f;
            if (success) job.Percent = 100f;
        }

        public static int ActiveDownloadCount(IEnumerable<DownloadJob> jobs)
        {
            int n = 0;
            if (jobs == null) return 0;
            foreach (var j in jobs)
            {
                if (j.Phase == DownloadPhase.Active) n++;
            }
            return n;
        }

        public static void ClearFinishedDownloads(List<DownloadJob> jobs) =>
            jobs?.RemoveAll(j => j.Phase != DownloadPhase.Active);
    }
}
