// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using Google.FlatBuffers;
using Tryll;

namespace Tryll.Client.Internal
{
    internal static class TryllAgentLogFrames
    {
        public static string TableName(byte[] frame, bool local)
        {
            if (frame == null || frame.Length == 0)
                return "";
            if (local)
            {
                var envelope = LogLocalEnvelope.GetRootAsLogLocalEnvelope(new ByteBuffer(frame));
                return envelope.BodyType == LogLocalBody.NONE ? "" : envelope.BodyType.ToString();
            }
            var message = Message.GetRootAsMessage(new ByteBuffer(frame));
            return message.BodyType == MessageBody.NONE ? "" : message.BodyType.ToString();
        }
    }


    /// <summary>
    /// Builds <see cref="LogLocalEnvelope"/> bytes for events that never go on the wire.
    /// </summary>
    internal static class TryllAgentLogLocal
    {
        public static byte[] Connection(LogConnState state, string reason)
        {
            var fbb = new FlatBufferBuilder(128);
            var reasonOff = string.IsNullOrEmpty(reason) ? default : fbb.CreateString(reason);
            var body = LogConnectionState.CreateLogConnectionState(fbb, state, reasonOff);
            var envelope = LogLocalEnvelope.CreateLogLocalEnvelope(
                fbb, LogLocalBody.LogConnectionState, body.Value);
            LogLocalEnvelope.FinishLogLocalEnvelopeBuffer(fbb, envelope);
            return fbb.SizedByteArray();
        }

        public static byte[] Error(uint code, string codeName, string message, LogErrorOrigin origin)
        {
            var fbb = new FlatBufferBuilder(256);
            var nameOff = string.IsNullOrEmpty(codeName) ? default : fbb.CreateString(codeName);
            var messageOff = string.IsNullOrEmpty(message) ? default : fbb.CreateString(message);
            var body = LogLocalError.CreateLogLocalError(fbb, code, nameOff, messageOff, origin);
            var envelope = LogLocalEnvelope.CreateLogLocalEnvelope(
                fbb, LogLocalBody.LogLocalError, body.Value);
            LogLocalEnvelope.FinishLogLocalEnvelopeBuffer(fbb, envelope);
            return fbb.SizedByteArray();
        }

        public static byte[] Gap(LogGapReason reason)
        {
            var fbb = new FlatBufferBuilder(64);
            var body = LogCaptureGap.CreateLogCaptureGap(fbb, reason);
            var envelope = LogLocalEnvelope.CreateLogLocalEnvelope(
                fbb, LogLocalBody.LogCaptureGap, body.Value);
            LogLocalEnvelope.FinishLogLocalEnvelopeBuffer(fbb, envelope);
            return fbb.SizedByteArray();
        }

        public static byte[] AnswerText(
            ulong requestId, ulong agentId, string text, bool isDelta, bool isFinal, string nodeName)
        {
            var fbb = new FlatBufferBuilder(256);
            var textOff = fbb.CreateString(text ?? "");
            var nodeOff = string.IsNullOrEmpty(nodeName) ? default : fbb.CreateString(nodeName);
            var body = Tryll.AnswerText.CreateAnswerText(
                fbb, requestId, agentId, textOff, isDelta, isFinal, nodeOff);
            return FinishMessage(fbb, MessageBody.AnswerText, body.Value);
        }

        public static byte[] TurnComplete(
            ulong requestId, ulong agentId, TurnStatus status, string debugInfo, uint tokens)
        {
            var fbb = new FlatBufferBuilder(256);
            var debugOff = string.IsNullOrEmpty(debugInfo) ? default : fbb.CreateString(debugInfo);
            var body = Tryll.TurnComplete.CreateTurnComplete(
                fbb, requestId, agentId, status, debugOff, tokens);
            return FinishMessage(fbb, MessageBody.TurnComplete, body.Value);
        }

        static byte[] FinishMessage(FlatBufferBuilder fbb, MessageBody type, int body)
        {
            var message = Message.CreateMessage(fbb, type, body);
            Message.FinishMessageBuffer(fbb, message);
            return fbb.SizedByteArray();
        }
    }
}
