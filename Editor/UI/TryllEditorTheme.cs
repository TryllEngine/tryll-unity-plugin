// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tryll.Client.Editor
{
    /// <summary>
    /// Host / semantic / component roles for Tryll editor windows.
    /// Surfaces resolve through <c>TryllEditorTheme.uss</c> and Unity
    /// <c>--unity-colors-*</c> variables. Semantic RGB is authored here only.
    /// </summary>
    internal static class TryllEditorTheme
    {
        public const string HostRootClass = "tryll-host-root";
        public const string HostBgClass = "tryll-host-bg";
        public const string HostPanelClass = "tryll-host-panel";
        public const string HostTextClass = "tryll-host-text";
        public const string HostDimClass = "tryll-host-dim";
        public const string HostLineClass = "tryll-host-line";
        public const string HostInputClass = "tryll-host-input";

        const string UssPackagePath = "Packages/com.tryll.client/Editor/UI/TryllEditorTheme.uss";

        static readonly CustomStyleProperty<Color> PropBg = new CustomStyleProperty<Color>("--tryll-bg");
        static readonly CustomStyleProperty<Color> PropPanel = new CustomStyleProperty<Color>("--tryll-panel");
        static readonly CustomStyleProperty<Color> PropPanel2 = new CustomStyleProperty<Color>("--tryll-panel2");
        static readonly CustomStyleProperty<Color> PropLine = new CustomStyleProperty<Color>("--tryll-line");
        static readonly CustomStyleProperty<Color> PropLine2 = new CustomStyleProperty<Color>("--tryll-line2");
        static readonly CustomStyleProperty<Color> PropTxt = new CustomStyleProperty<Color>("--tryll-txt");
        static readonly CustomStyleProperty<Color> PropDim = new CustomStyleProperty<Color>("--tryll-dim");
        static readonly CustomStyleProperty<Color> PropInput = new CustomStyleProperty<Color>("--tryll-input");

        static StyleSheet _sheet;

        // ── Semantic (authored sRGB; adapter-only) ────────────────────────────
        public static readonly Color Accent  = new Color(0.306f, 0.631f, 1.000f);
        public static readonly Color ColOk   = new Color(0.306f, 0.769f, 0.478f);
        public static readonly Color ColWarn = new Color(0.878f, 0.663f, 0.235f);
        public static readonly Color ColErr  = new Color(0.878f, 0.396f, 0.369f);
        public static readonly Color Loaded  = new Color(0.690f, 0.420f, 1.000f);
        public static readonly Color DiffAdded   = new Color(0.620f, 0.906f, 0.722f);
        public static readonly Color DiffRemoved = new Color(0.950f, 0.650f, 0.620f);
        public static readonly Color DiffSame    = new Color(0.784f, 0.800f, 0.831f);

        /// <summary>User bubble fill. Component role; not a host surface.</summary>
        public static readonly Color UserBub = new Color(0.220f, 0.400f, 0.620f);

        public static Color MicActive => ColErr;

        // ── Host surfaces (USS / EditorStyles; never sampled Tryll hex) ───────
        // Constructor-safe fallbacks: EditorWindows are ScriptableObjects, and
        // Dialog Lab variants initialize from Accent during serialization.
        // EditorGUIUtility.isProSkin / EditorStyles are illegal in that path.
        static readonly Color FallbackTxt = new Color(0.82f, 0.82f, 0.82f);
        static bool _dark = true;
        static bool _hostResolvedFromUss;
        static HostColorSnapshot _notifiedHost;

        /// <summary>
        /// Raised only when resolved host colours actually change. Windows rebind
        /// retained inline styles from their enable/disable lifecycle; do not
        /// restart sessions in the handler.
        /// </summary>
        public static event System.Action HostColorsChanged;

        struct HostColorSnapshot
        {
            public Color Bg, Panel, Panel2, Line, Line2, Txt, Dim, Faint, CodeBg;
            public bool Dark;
        }

        public static Color Bg     { get; private set; } = Color.Lerp(Color.black, FallbackTxt, 0.14f);
        public static Color Panel  { get; private set; } = Color.Lerp(Color.Lerp(Color.black, FallbackTxt, 0.14f), FallbackTxt, 0.06f);
        public static Color Panel2 { get; private set; } = Color.Lerp(Color.Lerp(Color.black, FallbackTxt, 0.14f), FallbackTxt, 0.12f);
        public static Color Line   { get; private set; } = new Color(FallbackTxt.r, FallbackTxt.g, FallbackTxt.b, 0.22f);
        public static Color Line2  { get; private set; } = new Color(FallbackTxt.r, FallbackTxt.g, FallbackTxt.b, 0.34f);
        public static Color Txt    { get; private set; } = FallbackTxt;
        public static Color Dim    { get; private set; } = new Color(0.65f, 0.65f, 0.65f);
        public static Color Faint  { get; private set; } = Color.Lerp(FallbackTxt, Color.Lerp(Color.black, FallbackTxt, 0.14f), 0.55f);
        public static Color CodeBg { get; private set; } = Color.Lerp(Color.Lerp(Color.black, FallbackTxt, 0.14f), Color.black, 0.18f);

        /// <summary>True for a dark host skin. Picks artwork that has one variant per skin (the logo).</summary>
        public static bool IsDark => _dark;

        /// <summary>Assistant bubble fill. Lifted off the window so the card reads as a surface.</summary>
        public static Color AsstBub => Color.Lerp(Bg, Txt, _dark ? 0.14f : 0.08f);
        public static Color SelBub => Color.Lerp(AsstBub, Accent, _dark ? 0.22f : 0.16f);

        public static Color OkBg     => Wash(ColOk, 0.18f);
        public static Color WarnBg   => Wash(ColWarn, 0.18f);
        public static Color ErrBg    => Wash(ColErr, 0.18f);
        public static Color ErrCell  => Wash(ColErr, 0.22f);
        public static Color SkipCell => Wash(Faint, 0.20f);
        public static Color OkBorder   => Wash(ColOk, 0.45f);
        public static Color WarnBorder => Wash(ColWarn, 0.45f);
        public static Color ErrBorder  => Wash(ColErr, 0.45f);
        public static Color ChipBg     => new Color(0f, 0f, 0f, _dark ? 0.22f : 0.08f);

        public static Color SegmentOnBg => Color.Lerp(Panel2, Accent, _dark ? 0.45f : 0.28f);
        public static Color SegmentOnText => Color.Lerp(Color.white, Accent, 0.12f);
        public static Color CellBody => Txt;
        public static Color BandText => Color.Lerp(Color.white, UserBub, 0.12f);
        public static Color SummaryUser => Color.Lerp(Color.white, UserBub, 0.22f);
        public static Color SectionHeader => Panel;
        public static Color SectionBody => Bg;

        public static Color Hairline => new Color(0f, 0f, 0f, _dark ? 0.30f : 0.15f);
        public static Color PanelTint => _dark
            ? new Color(1f, 1f, 1f, 0.03f) : new Color(0f, 0f, 0f, 0.03f);
        public static Color HeaderBg => _dark
            ? new Color(1f, 1f, 1f, 0.05f) : new Color(0f, 0f, 0f, 0.05f);
        public static Color TrackBg => _dark
            ? new Color(1f, 1f, 1f, 0.10f) : new Color(0f, 0f, 0f, 0.10f);
        public static Color SelBg => Color.Lerp(Panel, Accent, _dark ? 0.35f : 0.22f);

        // Call-to-action buttons and link chips (Welcome, Help & Community).
        public static Color PrimaryBg => Accent;
        public static Color PrimaryHoverBg => Color.Lerp(Accent, Color.white, 0.15f);
        public static Color PrimaryText => SegmentOnText;
        public static Color ChipHoverBg => Color.Lerp(Panel2, Txt, _dark ? 0.10f : 0.06f);

        public const float LineH = 17f;
        public const float Gap   = 8f;
        public const float RadiusChip = 8f;
        public const float RadiusBand = 8f;
        public const float RadiusCell = 6f;
        public const float RadiusCard = 4f;
        public const float RadiusHeader = 3f;
        public const float RadiusDot = 3f;
        public const float RadiusBar = 2f;

        /// <summary>
        /// Load the host USS. Do not paint <paramref name="root"/> when it is an
        /// EditorWindow root — an explicit background-color there makes UITK
        /// BaseFields lose the editor theme and render as white inputs.
        /// </summary>
        public static void Attach(VisualElement root, bool paintWindowBackground = false)
        {
            if (root == null)
                return;

            SeedFromEditorStyles();
            StyleSheet sheet = LoadSheet();
            if (sheet != null && !root.styleSheets.Contains(sheet))
                root.styleSheets.Add(sheet);

            root.AddToClassList(HostRootClass);
            if (paintWindowBackground)
                root.AddToClassList(HostBgClass);

            root.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        static bool TryReadProSkin(out bool dark)
        {
            try
            {
                dark = EditorGUIUtility.isProSkin;
                return true;
            }
            catch (UnityException)
            {
                dark = _dark;
                return false;
            }
        }

        static void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (TryReadProSkin(out bool dark))
                _dark = dark;
            ICustomStyle cs = evt.customStyle;
            if (cs.TryGetValue(PropBg, out Color bg)) Bg = bg;
            if (cs.TryGetValue(PropPanel, out Color panel)) Panel = panel;
            if (cs.TryGetValue(PropPanel2, out Color panel2)) Panel2 = panel2;
            if (cs.TryGetValue(PropLine, out Color line)) Line = line;
            if (cs.TryGetValue(PropLine2, out Color line2)) Line2 = line2;
            if (cs.TryGetValue(PropTxt, out Color txt)) Txt = txt;
            if (cs.TryGetValue(PropDim, out Color dim)) Dim = dim;
            if (cs.TryGetValue(PropInput, out Color input)) CodeBg = input;
            Faint = Color.Lerp(Txt, Bg, 0.55f);
            _hostResolvedFromUss = true;
            NotifyHostColorsIfChanged();
        }

        static void SeedFromEditorStyles()
        {
            if (!TryReadProSkin(out bool dark))
            {
                if (!_hostResolvedFromUss)
                    ApplyHostFallback(dark);
                return;
            }

            // A later Attach must not clobber USS-resolved surfaces with
            // EditorStyles approximations. Only refresh the mode flag so
            // Hairline / TrackBg / ChipBg follow a live dark/light switch
            // until CustomStyleResolved arrives.
            if (_hostResolvedFromUss)
            {
                if (_dark == dark)
                    return;
                _dark = dark;
                NotifyHostColorsIfChanged();
                return;
            }

            Color txt = dark ? FallbackTxt : new Color(0.15f, 0.15f, 0.15f);
            Color dim = dark ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.35f, 0.35f, 0.35f);
            try
            {
                GUIStyle label = EditorStyles.label;
                if (label != null)
                    txt = label.normal.textColor;
                GUIStyle mini = EditorStyles.miniLabel;
                if (mini != null)
                    dim = mini.normal.textColor;
            }
            catch (System.NullReferenceException)
            {
            }

            _dark = dark;
            Txt = txt;
            Dim = dim;
            ApplyHostSurfaces(dark, txt);
            NotifyHostColorsIfChanged();
        }

        static void ApplyHostFallback(bool dark)
        {
            Color txt = dark ? FallbackTxt : new Color(0.15f, 0.15f, 0.15f);
            Txt = txt;
            Dim = dark ? new Color(0.65f, 0.65f, 0.65f) : new Color(0.35f, 0.35f, 0.35f);
            ApplyHostSurfaces(dark, txt);
            NotifyHostColorsIfChanged();
        }

        static HostColorSnapshot CaptureHost() => new HostColorSnapshot
        {
            Bg = Bg,
            Panel = Panel,
            Panel2 = Panel2,
            Line = Line,
            Line2 = Line2,
            Txt = Txt,
            Dim = Dim,
            Faint = Faint,
            CodeBg = CodeBg,
            Dark = _dark,
        };

        static bool HostEquals(HostColorSnapshot a, HostColorSnapshot b) =>
            a.Dark == b.Dark
            && a.Bg == b.Bg && a.Panel == b.Panel && a.Panel2 == b.Panel2
            && a.Line == b.Line && a.Line2 == b.Line2
            && a.Txt == b.Txt && a.Dim == b.Dim && a.Faint == b.Faint
            && a.CodeBg == b.CodeBg;

        static void NotifyHostColorsIfChanged()
        {
            HostColorSnapshot now = CaptureHost();
            if (HostEquals(now, _notifiedHost))
                return;
            _notifiedHost = now;
            HostColorsChanged?.Invoke();
        }

        /// <summary>
        /// Test hook: paint a synthetic host palette without touching the
        /// developer's Unity theme. Broadcasts only when values change.
        /// </summary>
        internal static void DebugApplySyntheticHostColorsForTests(Color txt, Color bg, bool dark)
        {
            _dark = dark;
            Txt = txt;
            Dim = Color.Lerp(txt, bg, 0.35f);
            ApplyHostSurfaces(dark, txt);
            Bg = bg;
            Panel = Color.Lerp(bg, txt, 0.06f);
            Panel2 = Color.Lerp(bg, txt, 0.12f);
            Faint = Color.Lerp(txt, bg, 0.55f);
            CodeBg = Color.Lerp(bg, dark ? Color.black : Color.white, dark ? 0.18f : 0.06f);
            _hostResolvedFromUss = true;
            NotifyHostColorsIfChanged();
        }

        /// <summary>
        /// Test hook: apply a USS-style resolve, including the live skin flag,
        /// without touching the developer's Unity theme.
        /// </summary>
        internal static void DebugApplyResolvedHostColorsForTests(Color txt, Color bg, bool dark)
        {
            _dark = dark;
            Txt = txt;
            Dim = Color.Lerp(txt, bg, 0.35f);
            Bg = bg;
            Panel = Color.Lerp(bg, txt, 0.06f);
            Panel2 = Color.Lerp(bg, txt, 0.12f);
            Line = new Color(txt.r, txt.g, txt.b, 0.22f);
            Line2 = new Color(txt.r, txt.g, txt.b, 0.34f);
            Faint = Color.Lerp(txt, bg, 0.55f);
            CodeBg = Color.Lerp(bg, dark ? Color.black : Color.white, dark ? 0.18f : 0.06f);
            _hostResolvedFromUss = true;
            NotifyHostColorsIfChanged();
        }

        internal static void DebugSeedFromEditorStylesForTests() => SeedFromEditorStyles();

        internal static void DebugResetHostColorsForTests()
        {
            _hostResolvedFromUss = false;
            SeedFromEditorStyles();
        }

        static void ApplyHostSurfaces(bool dark, Color txt)
        {
            Bg = dark ? Color.Lerp(Color.black, txt, 0.14f) : Color.Lerp(Color.white, txt, 0.08f);
            Panel = Color.Lerp(Bg, txt, 0.06f);
            Panel2 = Color.Lerp(Bg, txt, 0.12f);
            Line = new Color(txt.r, txt.g, txt.b, 0.22f);
            Line2 = new Color(txt.r, txt.g, txt.b, 0.34f);
            Faint = Color.Lerp(txt, Bg, 0.55f);
            CodeBg = Color.Lerp(Bg, dark ? Color.black : Color.white, dark ? 0.18f : 0.06f);
        }

        static StyleSheet LoadSheet()
        {
            if (_sheet != null)
                return _sheet;
            _sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPackagePath);
            if (_sheet != null)
                return _sheet;
            string[] guids = AssetDatabase.FindAssets("TryllEditorTheme t:StyleSheet");
            if (guids != null && guids.Length > 0)
                _sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return _sheet;
        }

        public static Color Wash(Color fg, float alpha) => new Color(fg.r, fg.g, fg.b, alpha);

        /// <summary>
        /// Variant identity colours, by position. Categorical steps from the dataviz
        /// reference palette (dark-surface steps: blue, violet, magenta, orange, aqua), with
        /// no green or amber: next to pass/fail tints a green or amber variant reads as a
        /// verdict. Past five variants the colours repeat; the label carries identity.
        /// </summary>
        static readonly Color[] VariantCycle =
        {
            new Color(0.224f, 0.529f, 0.898f), // blue    #3987e5
            new Color(0.565f, 0.522f, 0.914f), // violet  #9085e9
            new Color(0.835f, 0.318f, 0.506f), // magenta #d55181
            new Color(0.851f, 0.349f, 0.149f), // orange  #d95926
            new Color(0.098f, 0.620f, 0.439f), // aqua    #199e70
        };

        public static Color VariantSwatch(int index)
        {
            int i = index % VariantCycle.Length;
            if (i < 0) i += VariantCycle.Length;
            return VariantCycle[i];
        }

        public static Color NodeColor(string type)
        {
            switch (type)
            {
                case "RetrieveNode":            return new Color(0.306f, 0.631f, 1.000f);
                case "TransformNode":           return new Color(0.420f, 0.440f, 0.475f);
                case "GenerateNode":            return new Color(0.690f, 0.420f, 1.000f);
                case "CannedResponseNode":      return new Color(0.306f, 0.769f, 0.478f);
                case "BranchNode":              return new Color(0.700f, 0.620f, 0.180f);
                case "GenerateAndSpeakNode":    return new Color(0.831f, 0.451f, 0.925f);
                case "ToolCallNode":            return new Color(0.235f, 0.706f, 0.729f);
                case "RegexGuardrailNode":      return new Color(0.906f, 0.541f, 0.318f);
                case "ClassifyIntentNode":      return new Color(0.478f, 0.549f, 0.902f);
                case "ClassifyIntentLLMNode":   return new Color(0.612f, 0.510f, 0.929f);
                case "ClassifyAttributesLLMNode": return new Color(0.545f, 0.455f, 0.835f);
                case "IntentToInstructionNode": return new Color(0.400f, 0.635f, 0.851f);
                case "InstructionNode":         return new Color(0.541f, 0.667f, 0.549f);
                case "SpeakNode":               return new Color(0.851f, 0.400f, 0.612f);
                case "PauseNode":               return new Color(0.514f, 0.514f, 0.565f);
                default:                        return Line2;
            }
        }

        public static Label Chip(string text, Color fg) =>
            Chip(text, fg, Wash(fg, 0.14f), Wash(fg, 0.45f));

        public static Label Chip(string text, Color fg, Color bg, Color border)
        {
            var l = new Label(text);
            l.style.fontSize = 10; l.style.color = fg;
            l.style.backgroundColor = bg;
            SetBorder(l, border, 1); SetRadius(l, RadiusChip);
            l.style.paddingLeft = 5; l.style.paddingRight = 5;
            l.style.paddingTop = 0; l.style.paddingBottom = 1;
            l.style.marginRight = 4; l.style.marginTop = 2;
            l.style.flexShrink = 0;
            return l;
        }

        public static Label Muted(string text)
        {
            var l = new Label(text);
            l.style.color = Faint; l.style.whiteSpace = WhiteSpace.Normal;
            l.style.fontSize = 11; l.style.marginTop = 3;
            return l;
        }

        public static VisualElement Placeholder(string text)
        {
            var l = new Label(text);
            l.style.color = Faint;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.marginTop = 60;
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        public static VisualElement Heading(string title)
        {
            var box = new VisualElement();
            box.style.marginBottom = 14;
            var h = new Label(title);
            h.style.unityFontStyleAndWeight = FontStyle.Bold;
            h.style.fontSize = 13;
            h.style.marginBottom = 2;
            box.Add(h);
            return box;
        }

        public static VisualElement SectionBox(string title)
        {
            var s = new VisualElement();
            s.style.paddingLeft = 16; s.style.paddingRight = 16;
            s.style.paddingTop = 12; s.style.paddingBottom = 12;
            s.style.borderBottomWidth = 1; s.style.borderBottomColor = Hairline;
            if (title != null)
            {
                var h = new Label(title.ToUpperInvariant());
                h.style.fontSize = 10; h.style.color = Dim; h.style.marginBottom = 8;
                h.style.unityFontStyleAndWeight = FontStyle.Bold;
                s.Add(h);
            }
            return s;
        }

        public static VisualElement TimingBar(LabCell c, float height) =>
            TimingBar((IReadOnlyList<LabRouteNode>)(c?.Route), height);

        public static VisualElement TimingBar(IReadOnlyList<LabRouteNode> route, float height)
        {
            if (route == null || route.Count == 0)
                return TimingBar((IReadOnlyList<TryllTurnRouteNode>)null, height);
            var mapped = new TryllTurnRouteNode[route.Count];
            for (int i = 0; i < route.Count; i++)
            {
                mapped[i] = new TryllTurnRouteNode
                {
                    Name = route[i].Name, Type = route[i].Type, Exit = route[i].Exit, Ms = route[i].Ms,
                };
            }
            return TimingBar(mapped, height);
        }

        public static VisualElement TimingBar(IReadOnlyList<TryllTurnRouteNode> route, float height)
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.height = height; bar.style.marginTop = 4; bar.style.flexShrink = 0;
            bar.style.backgroundColor = ChipBg;
            SetRadius(bar, RadiusBar);
            bar.style.overflow = Overflow.Hidden;

            int total = route == null ? 0 : route.Sum(n => n.Ms);
            if (total <= 0)
            {
                var flat = new VisualElement { style = { flexGrow = 1 } };
                flat.style.backgroundColor = Line2;
                bar.Add(flat);
                return bar;
            }
            foreach (var n in route)
            {
                if (n.Ms <= 0) continue;
                var seg = new VisualElement { style = { flexGrow = n.Ms } };
                seg.style.backgroundColor = NodeColor(n.Type);
                bar.Add(seg);
            }
            return bar;
        }

        public static VisualElement Swatch(Color c)
        {
            var s = new VisualElement();
            s.style.width = 8; s.style.height = 8; s.style.flexShrink = 0;
            s.style.backgroundColor = c; SetRadius(s, RadiusBar);
            s.style.marginRight = 5;
            return s;
        }

        public static void SetRadius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r;
            e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r;
            e.style.borderBottomRightRadius = r;
        }

        public static void SetBorder(VisualElement e, Color c, float w)
        {
            e.style.borderTopWidth = w;    e.style.borderTopColor = c;
            e.style.borderBottomWidth = w; e.style.borderBottomColor = c;
            e.style.borderLeftWidth = w;   e.style.borderLeftColor = c;
            e.style.borderRightWidth = w;  e.style.borderRightColor = c;
        }
    }
}
