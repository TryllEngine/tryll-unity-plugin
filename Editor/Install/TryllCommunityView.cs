// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Tryll.Client.Editor.TryllEditorTheme;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Shared building blocks for the Welcome and Help &amp; Community windows: the brand
    /// header, step cards, buttons, link chips and text. Composition only — no base
    /// window type (see docs/dev/editor-window-checklist.md). Colours are theme roles, so
    /// a window rebuilds these on <see cref="TryllEditorTheme.HostColorsChanged"/>.
    /// </summary>
    internal static class TryllCommunityView
    {
        internal enum Tone { Ok, Warn, Neutral }

        const float RadiusButton = 4f;

        // ------------------------------------------------------------------
        // Links
        // ------------------------------------------------------------------

        /// <summary>(label, url) for every social channel; empty URLs are dropped.</summary>
        internal static List<(string Label, string Url)> CommunityLinks() => Filled(
            (TryllEditorUiContract.LinkDiscord, TryllCommunityLinks.Discord),
            (TryllEditorUiContract.LinkYoutube, TryllCommunityLinks.YouTube),
            (TryllEditorUiContract.LinkLinkedin, TryllCommunityLinks.LinkedIn),
            (TryllEditorUiContract.LinkInstagram, TryllCommunityLinks.Instagram),
            (TryllEditorUiContract.LinkTiktok, TryllCommunityLinks.TikTok),
            (TryllEditorUiContract.LinkX, TryllCommunityLinks.X),
            (TryllEditorUiContract.LinkGithub, TryllCommunityLinks.GitHub),
            (TryllEditorUiContract.LinkWebsite, TryllCommunityLinks.Website));

        /// <summary>
        /// (label, url) for docs and support; empty URLs are dropped. Discord is the window's
        /// main button and sits in <see cref="CommunityLinks"/>, so it is not repeated here.
        /// </summary>
        internal static List<(string Label, string Url)> HelpLinks() => Filled(
            (TryllEditorUiContract.LinkDocs, TryllCommunityLinks.Docs),
            (TryllEditorUiContract.WelcomeGetStarted, TryllCommunityLinks.GettingStarted),
            (TryllEditorUiContract.LinkEmail, TryllCommunityLinks.Email),
            (TryllEditorUiContract.LinkReportIssue, TryllCommunityLinks.ReportIssue));

        /// <summary>
        /// The "Join our Discord" call to action, or null while there is no invite link.
        /// </summary>
        internal static Button DiscordButton(bool primary)
        {
            if (string.IsNullOrEmpty(TryllCommunityLinks.Discord))
                return null;
            Action open = () => OpenUrl(TryllCommunityLinks.Discord);
            var button = primary
                ? PrimaryButton(TryllEditorUiContract.WelcomeJoinDiscord, open)
                : SecondaryButton(TryllEditorUiContract.WelcomeJoinDiscord, open);
            button.tooltip = TryllCommunityLinks.Discord;
            return button;
        }

        /// <summary>
        /// Opens <typeparamref name="T"/>, giving a new window <paramref name="size"/> so its
        /// whole page shows; an open window keeps the size the person gave it.
        /// </summary>
        internal static T OpenWindow<T>(string title, Vector2 minSize, Vector2 size) where T : EditorWindow
        {
            bool fresh = !EditorWindow.HasOpenInstances<T>();
            var window = EditorWindow.GetWindow<T>(title);
            window.minSize = minSize;
            if (fresh)
                window.position = new Rect(window.position.position, size);
            return window;
        }

        static List<(string Label, string Url)> Filled(params (string Label, string Url)[] links)
        {
            var filled = new List<(string, string)>();
            foreach (var link in links)
                if (!string.IsNullOrEmpty(link.Url))
                    filled.Add(link);
            return filled;
        }

        internal static VisualElement LinkRow(string elementName, List<(string Label, string Url)> links)
        {
            var row = Row();
            row.name = elementName;
            row.style.flexWrap = Wrap.Wrap;
            foreach (var (label, url) in links)
                row.Add(LinkChip(label, url));
            return row;
        }

        /// <summary>Test seam: receives the URL instead of the browser.</summary>
        internal static Action<string> DebugOpenUrlForTests;

        /// <summary>
        /// Opens an outside link. Docs links get the version folder first
        /// (<see cref="TryllVersionLookup.PinDocsUrl"/>).
        /// </summary>
        internal static void OpenUrl(string url)
        {
            string pinned = TryllVersionLookup.PinDocsUrl(url);
            if (DebugOpenUrlForTests != null)
                DebugOpenUrlForTests(pinned);
            else
                Application.OpenURL(pinned);
        }

        /// <summary>Rounded chip that opens <paramref name="url"/>; the tooltip shows the address.</summary>
        internal static Button LinkChip(string label, string url)
        {
            string shown = TryllVersionLookup.PinDocsUrl(url);
            var chip = new Button(() => OpenUrl(url))
            {
                text = label,
                tooltip = shown.StartsWith("mailto:", StringComparison.Ordinal) ? shown.Substring("mailto:".Length) : shown,
            };
            StyleButton(chip, Panel2, ChipHoverBg, Txt, Line, RadiusChip + 4f);
            chip.style.paddingLeft = chip.style.paddingRight = 12;
            return chip;
        }

        // ------------------------------------------------------------------
        // Buttons
        // ------------------------------------------------------------------

        /// <summary>Filled accent button for the one main action in a group.</summary>
        internal static Button PrimaryButton(string text, Action onClick, string name = null)
        {
            var button = new Button(onClick) { text = text };
            if (!string.IsNullOrEmpty(name)) button.name = name;
            StyleButton(button, PrimaryBg, PrimaryHoverBg, PrimaryText, PrimaryBg, RadiusButton);
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            return button;
        }

        /// <summary>Outlined button for every other action.</summary>
        internal static Button SecondaryButton(string text, Action onClick, string name = null)
        {
            var button = new Button(onClick) { text = text };
            if (!string.IsNullOrEmpty(name)) button.name = name;
            StyleButton(button, Panel2, ChipHoverBg, Txt, Line2, RadiusButton);
            return button;
        }

        /// <summary>Button drawn as an accent-coloured link: small, low-weight actions.</summary>
        internal static Button TextButton(string text, Action onClick, string name = null)
        {
            var button = new Button(onClick) { text = text };
            if (!string.IsNullOrEmpty(name)) button.name = name;
            button.style.backgroundColor = Color.clear;
            SetBorder(button, Color.clear, 0);
            button.style.color = Accent;
            button.style.paddingLeft = button.style.paddingRight = 4;
            button.style.marginLeft = 0;
            button.style.marginRight = 6;
            button.RegisterCallback<PointerEnterEvent>(_ => button.style.color = PrimaryHoverBg);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.color = Accent);
            return button;
        }

        /// <summary>
        /// Enable or disable a styled button. Inline colours hide the host's disabled look,
        /// so a disabled button is faded here instead.
        /// </summary>
        internal static void SetActionEnabled(Button button, bool enabled)
        {
            button.SetEnabled(enabled);
            button.style.opacity = enabled ? 1f : 0.45f;
        }

        static void StyleButton(Button button, Color bg, Color hoverBg, Color text, Color border, float radius)
        {
            button.style.backgroundColor = bg;
            button.style.color = text;
            SetBorder(button, border, 1);
            SetRadius(button, radius);
            button.style.paddingLeft = button.style.paddingRight = 14;
            button.style.paddingTop = button.style.paddingBottom = 3;
            button.style.minHeight = 24;
            button.style.marginLeft = 0;
            button.style.marginRight = 6;
            button.style.marginTop = 0;
            button.style.marginBottom = 6;
            button.RegisterCallback<PointerEnterEvent>(_ => button.style.backgroundColor = hoverBg);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.backgroundColor = bg);
        }

        // ------------------------------------------------------------------
        // Surfaces
        // ------------------------------------------------------------------

        /// <summary>Logo on a soft band, with a version chip on the right.</summary>
        internal static VisualElement BrandHeader(string logoElementName, string chip)
        {
            var band = Row();
            band.style.alignItems = Align.Center;
            band.style.justifyContent = Justify.SpaceBetween;
            band.style.backgroundColor = HeaderBg;
            SetRadius(band, RadiusCell);
            band.style.paddingLeft = band.style.paddingRight = 14;
            band.style.paddingTop = band.style.paddingBottom = 12;
            band.style.marginBottom = 14;
            var logo = Logo(logoElementName);
            logo.style.marginBottom = 0;
            band.Add(logo);
            if (!string.IsNullOrEmpty(chip))
                band.Add(Pill(chip, Tone.Neutral));
            return band;
        }

        /// <summary>Plain card with an optional header.</summary>
        internal static VisualElement Card(string header)
        {
            var card = new VisualElement();
            card.style.backgroundColor = Panel;
            SetBorder(card, Line, 1);
            SetRadius(card, RadiusCell);
            card.style.paddingLeft = card.style.paddingRight = 14;
            card.style.paddingTop = 10;
            card.style.paddingBottom = 6;
            card.style.marginBottom = 10;
            if (!string.IsNullOrEmpty(header))
                card.Add(Heading(header, 13));
            return card;
        }

        /// <summary>The number badge, status pill and accent edge of one setup step.</summary>
        internal sealed class StepChrome
        {
            internal VisualElement Card;
            internal Label Badge;
            internal Label Pill;

            internal void Set(bool done, string pillText, Tone tone)
            {
                Color edge = done ? ColOk : Accent;
                Card.style.borderLeftColor = edge;
                Badge.style.backgroundColor = edge;
                Badge.style.color = PrimaryText;
                Pill.text = pillText;
                PaintPill(Pill, tone);
            }
        }

        /// <summary>Card with a numbered badge, a title and a status pill on the right.</summary>
        internal static VisualElement StepCard(int number, string header, out StepChrome chrome)
        {
            var card = Card(null);
            card.style.borderLeftWidth = 3;

            var top = Row();
            top.style.alignItems = Align.Center;
            top.style.marginBottom = 6;
            var badge = new Label(number.ToString());
            badge.style.width = badge.style.height = 20;
            SetRadius(badge, 10);
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.fontSize = 11;
            badge.style.marginRight = 8;
            top.Add(badge);
            var title = Heading(header, 13);
            title.style.marginBottom = 0;
            title.style.flexGrow = 1;
            top.Add(title);
            var pill = Pill("", Tone.Neutral);
            top.Add(pill);
            card.Add(top);

            chrome = new StepChrome { Card = card, Badge = badge, Pill = pill };
            return card;
        }

        /// <summary>Small rounded status label.</summary>
        internal static Label Pill(string text, Tone tone)
        {
            var pill = new Label(text);
            pill.style.fontSize = 11;
            pill.style.paddingLeft = pill.style.paddingRight = 8;
            pill.style.paddingTop = pill.style.paddingBottom = 2;
            SetRadius(pill, RadiusChip + 2f);
            pill.style.unityFontStyleAndWeight = FontStyle.Bold;
            PaintPill(pill, tone);
            return pill;
        }

        static void PaintPill(Label pill, Tone tone)
        {
            switch (tone)
            {
                case Tone.Ok:
                    pill.style.backgroundColor = OkBg;
                    pill.style.color = ColOk;
                    break;
                case Tone.Warn:
                    pill.style.backgroundColor = WarnBg;
                    pill.style.color = ColWarn;
                    break;
                default:
                    pill.style.backgroundColor = ChipBg;
                    pill.style.color = Dim;
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Text and layout
        // ------------------------------------------------------------------

        internal static Label Heading(string text, int size)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = size;
            label.style.color = Txt;
            label.style.marginBottom = 4;
            return label;
        }

        internal static Label Paragraph(string text, bool dim = false)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.color = dim ? Dim : Txt;
            label.style.marginBottom = 6;
            return label;
        }

        /// <summary>Small bold label above a group of cards.</summary>
        internal static Label SectionLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 11;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Dim;
            label.style.marginBottom = 6;
            return label;
        }

        internal static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            return row;
        }

        /// <summary>The Tryll client version for header chips, e.g. <c>v0.9.0</c>.</summary>
        internal static string ClientVersionChip() => "v" + TryllVersionLookup.Client();

        static void SetBorder(VisualElement element, Color color, float width)
        {
            element.style.borderTopColor = element.style.borderBottomColor =
                element.style.borderLeftColor = element.style.borderRightColor = color;
            element.style.borderTopWidth = element.style.borderBottomWidth =
                element.style.borderLeftWidth = element.style.borderRightWidth = width;
        }

        static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius =
                element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = radius;
        }

        // ------------------------------------------------------------------
        // Logo
        // ------------------------------------------------------------------

        // 2x artwork: 480x129 px, shown at 240x64.5 points.
        internal const string LogoOnDarkPath = "Packages/com.tryll.client/Editor/Images/TryllLogoOnDark.png";
        internal const string LogoOnLightPath = "Packages/com.tryll.client/Editor/Images/TryllLogoOnLight.png";
        const float LogoWidth = 240f;

        /// <summary>The Tryll Engine logo for the current skin (light text on a dark skin).</summary>
        internal static Texture2D LogoTexture()
            => AssetDatabase.LoadAssetAtPath<Texture2D>(IsDark ? LogoOnDarkPath : LogoOnLightPath);

        /// <summary>
        /// The logo as a header image. A missing texture leaves an empty, zero-height
        /// element, so the window still builds.
        /// </summary>
        internal static Image Logo(string elementName)
        {
            var texture = LogoTexture();
            var image = new Image { name = elementName, image = texture, scaleMode = ScaleMode.ScaleToFit };
            if (texture == null)
                return image;
            image.style.width = LogoWidth;
            image.style.height = LogoWidth * texture.height / texture.width;
            image.style.marginBottom = Gap;
            return image;
        }
    }
}
