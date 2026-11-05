// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;

namespace Tryll.Client
{
    /// <summary>
    /// Recovers a smooth audible-position estimate from
    /// <see cref="TryllSpeaker.ConsumedSamples"/>.
    ///
    /// That counter is a sawtooth that LEADS the true playing position by a
    /// whole DSP refill (measured at 100–400 ms jumps). Feeding it straight
    /// to a pose lookup puts the face hundreds of milliseconds early. This
    /// estimator is a minimum envelope: the device drains at exactly the sample
    /// rate and cannot have played more than it was given, so for every past
    /// observation, audible(now) &lt;= C(t) + (now − t) * rate, and the
    /// tightest such bound is the best estimate.
    ///
    /// Does NOT subtract Unity's DSP-ring output latency — the caller does
    /// that from queried <c>AudioSettings.GetDSPBufferSize</c>.
    /// </summary>
    public sealed class TryllTtsPlaybackClock
    {
        private const int RawHistorySize = 128;

        private struct RawSample { public long Value; public double At; }

        private readonly RawSample[] _rawHistory = new RawSample[RawHistorySize];
        private long _rawHistoryNext;

        private long   _rawPrev = -1;
        private double _rawPrevAt;
        private double _smoothedClock;
        private long   _maxRawStep;
        private int    _clockObservations, _clockNoChange, _clockStepN;
        private long   _clockStepMin = long.MaxValue, _clockStepMax;
        private double _clockStepSum;

        public bool  Smooth            { get; set; } = true;
        public float WindowSeconds     { get; set; } = 1.5f;
        public float OutputLeadTrimMs  { get; set; }
        public int   SampleRate        { get; set; }

        public long   RawPrev     => _rawPrev;
        public double Smoothed    => _smoothedClock;
        public int    Observations => _clockObservations;
        public int    NoChange    => _clockNoChange;
        public int    StepN       => _clockStepN;
        public long   StepMin     => _clockStepMin;
        public long   StepMax     => _clockStepMax;
        public double StepSum     => _clockStepSum;

        public void Reset()
        {
            _rawPrev = -1;
            _rawPrevAt = 0;
            _smoothedClock = 0;
            _rawHistoryNext = 0;
            _maxRawStep = 0;
            _clockObservations = _clockNoChange = _clockStepN = 0;
            _clockStepMin = long.MaxValue;
            _clockStepMax = 0;
            _clockStepSum = 0;
        }

        /// <summary>
        /// Estimate the audible stream position in TTS samples.
        /// <paramref name="consumedSamples"/> is <see cref="TryllSpeaker.ConsumedSamples"/>.
        /// <paramref name="nowUnscaled"/> is <c>Time.unscaledTimeAsDouble</c>.
        /// </summary>
        public long Evaluate(long consumedSamples, double nowUnscaled)
        {
            long raw = consumedSamples;
            double now = nowUnscaled;

            if (_rawPrev >= 0)
            {
                long d = raw - _rawPrev;
                _clockObservations++;
                if (d == 0) _clockNoChange++;
                else
                {
                    _clockStepN++;
                    _clockStepSum += d;
                    if (d < _clockStepMin) _clockStepMin = d;
                    if (d > _clockStepMax) _clockStepMax = d;
                    if (d > _maxRawStep) _maxRawStep = d;
                }
            }

            if (!Smooth || SampleRate <= 0)
            {
                _rawPrev = raw;
                _rawPrevAt = now;
                _smoothedClock = raw;
                return raw;
            }

            if (raw != _rawPrev)
            {
                // Anchor on the value BEFORE this jump — the moment the device
                // has just drained what it held. Anchoring on (R_new, now) is
                // when raw MOST overestimates; the min-envelope then degenerates
                // to raw itself.
                if (_rawPrev >= 0)
                {
                    _rawHistory[(int)(_rawHistoryNext % RawHistorySize)] =
                        new RawSample { Value = _rawPrev, At = now };
                    _rawHistoryNext++;
                }
                _rawPrev = raw;
                _rawPrevAt = now;
            }

            double best = double.MaxValue;
            int have = (int)Math.Min(_rawHistoryNext, RawHistorySize);
            for (int i = 0; i < have; i++)
            {
                var s = _rawHistory[i];
                double age = now - s.At;
                if (age < 0.0 || age > WindowSeconds) continue;
                double bound = s.Value + age * SampleRate;
                if (bound < best) best = bound;
            }
            if (best == double.MaxValue) best = raw;
            if (best > raw) best = raw;

            if (OutputLeadTrimMs != 0f) best -= OutputLeadTrimMs * SampleRate / 1000.0;
            if (best < 0.0) best = 0.0;
            if (best < _smoothedClock) best = Math.Min(_smoothedClock, raw);

            _smoothedClock = best;
            return (long)_smoothedClock;
        }

        public string StepSummary()
        {
            if (_clockStepN == 0) return "clock: no raw steps observed yet";
            double avg = _clockStepSum / _clockStepN;
            float frameSpacing = SampleRate > 0 ? SampleRate * 0.04f : 640f;
            return $"clock raw steps over {_clockObservations} Updates: " +
                   $"min {_clockStepMin} max {_clockStepMax} avg {avg:F0} samples " +
                   $"({SamplesToMs((long)avg):F1} ms avg), {_clockNoChange} Updates saw no change. " +
                   $"One curve frame = {frameSpacing:F0} samples (40 ms). " +
                   $"avg step / frame = {avg / frameSpacing:F2}" +
                   (avg >= frameSpacing
                       ? "  <<< >= 1 frame per step: the raw clock alone cannot produce a " +
                         "fractional frame index, which is why the lerp looked bypassed."
                       : "");
        }

        public float SamplesToMs(long samples)
            => SampleRate > 0 ? samples * 1000f / SampleRate : 0f;
    }
}
