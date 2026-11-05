// Copyright (c) 2026 Tryll. All Rights Reserved.
// SPDX-License-Identifier: LicenseRef-Tryll
// This file is part of Tryll Engine and is licensed under the Tryll Engine License; see LICENSE.md shipped with this plugin. By using this file, you accept that License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tryll.Client.Editor
{
    internal enum TryllFieldKind { String, MultilineString, Int, Float, Bool, Enum }

    /// <summary>One field a variant is allowed to override, addressed by a dotted
    /// <see cref="FieldPath"/> relative to a node's params object.</summary>
    internal sealed class TryllOverridableField
    {
        public string FieldPath;
        public string DisplayName;
        public TryllFieldKind Kind;
        public Type EnumType;
        public bool IsOptional;
        public float? Min;
        public float? Max;

        public Func<TryllNodeParamsBase, string> Get;
        public Func<TryllNodeParamsBase, string, string> TrySet; // null error = success
    }

    /// <summary>
    /// Reflects over a node's public <see cref="TryllNodeParamsBase"/> fields to find
    /// what a Dialog Lab variant may override, and applies string-valued overrides onto
    /// a cloned params object. Generic by design — it does not enumerate node types.
    /// </summary>
    internal static class TryllNodeFieldReflection
    {
        private static bool IsStructural(FieldInfo f) =>
            f.GetCustomAttribute<TryllStructuralAttribute>() != null;

        public static IReadOnlyList<TryllOverridableField> EnumerateOverridableFields(Type paramsType)
        {
            var result = new List<TryllOverridableField>();
            foreach (var f in paramsType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsStructural(f)) continue;

                if (f.FieldType == typeof(TryllSamplingOverrides))
                {
                    result.AddRange(EnumerateSamplingFields(f));
                    continue;
                }

                if (f.FieldType == typeof(TryllOutputFilterOverrides))
                {
                    result.AddRange(EnumerateOutputFilterFields(f));
                    continue;
                }

                var scalar = TryMakeScalarField(f);
                if (scalar != null) result.Add(scalar);
            }
            return result;
        }

        public static bool TryGetField(Type paramsType, string fieldPath, out TryllOverridableField field)
        {
            field = EnumerateOverridableFields(paramsType)
                .FirstOrDefault(f => f.FieldPath == fieldPath);
            return field != null;
        }

        /// <summary>Validate every override against the given graph. Does not mutate.</summary>
        public static List<string> CollectOverrideProblems(
            TryllGraphDescription graph, IEnumerable<TryllParamOverride> overrides)
        {
            var problems = new List<string>();
            var byName = (graph?.Nodes ?? new List<TryllNodeDescription>())
                .Where(n => n != null && n.Params != null)
                .ToDictionary(n => n.Name, n => n.Params.GetType());

            foreach (var o in overrides ?? Enumerable.Empty<TryllParamOverride>())
            {
                if (o == null) continue;
                if (!byName.TryGetValue(o.NodeName ?? "", out var paramsType))
                {
                    problems.Add($"{o.NodeName}.{o.FieldPath}: node not in this workflow");
                    continue;
                }
                if (!TryGetField(paramsType, o.FieldPath, out var field))
                {
                    problems.Add($"{o.NodeName}.{o.FieldPath}: field not overridable on {paramsType.Name}");
                    continue;
                }
                string err = ValidateValue(field, o.Value);
                if (err != null)
                    problems.Add($"{o.NodeName}.{o.FieldPath}: {err}");
            }
            return problems;
        }

        public static bool TryApply(
            TryllNodeParamsBase target,
            IEnumerable<TryllParamOverride> overrides,
            out string error)
        {
            error = null;
            if (target == null)
            {
                error = "missing params target";
                return false;
            }
            var fields = EnumerateOverridableFields(target.GetType()).ToDictionary(f => f.FieldPath);
            foreach (var o in overrides ?? Enumerable.Empty<TryllParamOverride>())
            {
                if (o == null) continue;
                if (!fields.TryGetValue(o.FieldPath, out var field))
                {
                    error = $"unknown field '{o.FieldPath}'";
                    return false;
                }
                string setErr = field.TrySet(target, o.Value);
                if (setErr != null)
                {
                    error = $"{o.FieldPath}: {setErr}";
                    return false;
                }
            }
            return true;
        }

        /// <summary>Legacy Apply that silently skips unknown paths — prefer <see cref="TryApply"/>.</summary>
        public static void Apply(TryllNodeParamsBase target, IEnumerable<TryllParamOverride> overrides) =>
            TryApply(target, overrides, out _);

        private static IEnumerable<TryllOverridableField> EnumerateSamplingFields(FieldInfo samplingField)
        {
            foreach (var sub in typeof(TryllSamplingOverrides).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (sub.FieldType == typeof(TryllOptional<int>))
                    yield return OptionalField<int>(samplingField, sub, int.TryParse);
                else if (sub.FieldType == typeof(TryllOptional<float>))
                {
                    yield return OptionalField<float>(samplingField, sub, (string s, out float v) =>
                        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v));
                }
            }
        }

        private static IEnumerable<TryllOverridableField> EnumerateOutputFilterFields(FieldInfo filterField)
        {
            foreach (var sub in typeof(TryllOutputFilterOverrides).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                ReadBounds(sub, out float? min, out float? max);
                string path = $"{filterField.Name}.{sub.Name}";
                string display = $"Output Filter ▸ {SplitPascalCase(sub.Name)}";

                if (sub.FieldType == typeof(bool))
                {
                    yield return new TryllOverridableField
                    {
                        FieldPath = path,
                        DisplayName = display,
                        Kind = TryllFieldKind.Bool,
                        Get = p =>
                        {
                            var f = (TryllOutputFilterOverrides)filterField.GetValue(p)
                                ?? new TryllOutputFilterOverrides();
                            return ((bool)sub.GetValue(f)) ? "true" : "false";
                        },
                        TrySet = (p, value) =>
                        {
                            if (!bool.TryParse(value, out var b)) return "invalid bool";
                            var f = (TryllOutputFilterOverrides)filterField.GetValue(p)
                                ?? new TryllOutputFilterOverrides();
                            sub.SetValue(f, b);
                            filterField.SetValue(p, f);
                            return null;
                        },
                    };
                }
                else if (sub.FieldType == typeof(int))
                {
                    yield return new TryllOverridableField
                    {
                        FieldPath = path,
                        DisplayName = display,
                        Kind = TryllFieldKind.Int,
                        Min = min,
                        Max = max,
                        Get = p =>
                        {
                            var f = (TryllOutputFilterOverrides)filterField.GetValue(p)
                                ?? new TryllOutputFilterOverrides();
                            return Convert.ToString(sub.GetValue(f), CultureInfo.InvariantCulture);
                        },
                        TrySet = (p, value) =>
                        {
                            string verr = ValidateParsed(TryllFieldKind.Int, value, min, max, required: true, out var parsed);
                            if (verr != null) return verr;
                            var f = (TryllOutputFilterOverrides)filterField.GetValue(p)
                                ?? new TryllOutputFilterOverrides();
                            sub.SetValue(f, (int)parsed);
                            filterField.SetValue(p, f);
                            return null;
                        },
                    };
                }
            }
        }

        private delegate bool TryParse<T>(string s, out T value);

        private static TryllOverridableField OptionalField<T>(
            FieldInfo samplingField, FieldInfo sub, TryParse<T> tryParse)
            where T : struct
        {
            string path = $"{samplingField.Name}.{sub.Name}";
            ReadBounds(sub, out float? min, out float? max);
            var kind = typeof(T) == typeof(int) ? TryllFieldKind.Int : TryllFieldKind.Float;
            return new TryllOverridableField
            {
                FieldPath = path,
                DisplayName = $"Sampling ▸ {SplitPascalCase(sub.Name)}",
                Kind = kind,
                IsOptional = true,
                Min = min,
                Max = max,
                Get = p =>
                {
                    var s = (TryllSamplingOverrides)samplingField.GetValue(p);
                    if (s == null) return "";
                    var opt = (TryllOptional<T>)sub.GetValue(s);
                    return opt.HasValue ? Convert.ToString(opt.Value, CultureInfo.InvariantCulture) : "";
                },
                TrySet = (p, value) =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        var sClear = (TryllSamplingOverrides)samplingField.GetValue(p) ?? new TryllSamplingOverrides();
                        sub.SetValue(sClear, TryllOptional<T>.None);
                        samplingField.SetValue(p, sClear);
                        return null;
                    }
                    string verr = ValidateParsed(kind, value, min, max, required: false, out _);
                    if (verr != null) return verr;
                    if (!tryParse(value, out var parsed)) return $"invalid {kind.ToString().ToLowerInvariant()}";
                    var s = (TryllSamplingOverrides)samplingField.GetValue(p) ?? new TryllSamplingOverrides();
                    sub.SetValue(s, (TryllOptional<T>)parsed);
                    samplingField.SetValue(p, s);
                    return null;
                },
            };
        }

        private static TryllOverridableField TryMakeScalarField(FieldInfo f)
        {
            var type = f.FieldType;
            // Prompt fields carry [TryllLocalizable(true)] instead of [TextArea]: a field
            // gets one property drawer, and the localizable one draws the text area itself.
            bool multiline = f.GetCustomAttribute<TextAreaAttribute>() != null
                || f.GetCustomAttribute<TryllLocalizableAttribute>()?.Multiline == true;
            ReadBounds(f, out float? min, out float? max);

            if (type == typeof(string))
            {
                return new TryllOverridableField
                {
                    FieldPath = f.Name,
                    DisplayName = SplitPascalCase(f.Name),
                    Kind = multiline ? TryllFieldKind.MultilineString : TryllFieldKind.String,
                    Get = p => (string)f.GetValue(p) ?? "",
                    TrySet = (p, v) => { f.SetValue(p, v ?? ""); return null; },
                };
            }
            if (type == typeof(int))
            {
                return new TryllOverridableField
                {
                    FieldPath = f.Name,
                    DisplayName = SplitPascalCase(f.Name),
                    Kind = TryllFieldKind.Int,
                    Min = min,
                    Max = max,
                    Get = p => Convert.ToString(f.GetValue(p), CultureInfo.InvariantCulture),
                    TrySet = (p, v) =>
                    {
                        string err = ValidateParsed(TryllFieldKind.Int, v, min, max, required: true, out var parsed);
                        if (err != null) return err;
                        f.SetValue(p, (int)parsed);
                        return null;
                    },
                };
            }
            if (type == typeof(float))
            {
                return new TryllOverridableField
                {
                    FieldPath = f.Name,
                    DisplayName = SplitPascalCase(f.Name),
                    Kind = TryllFieldKind.Float,
                    Min = min,
                    Max = max,
                    Get = p => Convert.ToString(f.GetValue(p), CultureInfo.InvariantCulture),
                    TrySet = (p, v) =>
                    {
                        string err = ValidateParsed(TryllFieldKind.Float, v, min, max, required: true, out var parsed);
                        if (err != null) return err;
                        f.SetValue(p, (float)parsed);
                        return null;
                    },
                };
            }
            if (type == typeof(bool))
            {
                return new TryllOverridableField
                {
                    FieldPath = f.Name,
                    DisplayName = SplitPascalCase(f.Name),
                    Kind = TryllFieldKind.Bool,
                    Get = p => ((bool)f.GetValue(p)) ? "true" : "false",
                    TrySet = (p, v) =>
                    {
                        if (!bool.TryParse(v, out var b)) return "invalid bool";
                        f.SetValue(p, b);
                        return null;
                    },
                };
            }
            if (type.IsEnum)
            {
                return new TryllOverridableField
                {
                    FieldPath = f.Name,
                    DisplayName = SplitPascalCase(f.Name),
                    Kind = TryllFieldKind.Enum,
                    EnumType = type,
                    Get = p => f.GetValue(p).ToString(),
                    TrySet = (p, v) =>
                    {
                        if (!Enum.IsDefined(type, v)) return $"invalid enum value '{v}'";
                        f.SetValue(p, Enum.Parse(type, v));
                        return null;
                    },
                };
            }
            return null;
        }

        public static string ValidateValue(TryllOverridableField field, string value)
        {
            if (field.Kind == TryllFieldKind.String || field.Kind == TryllFieldKind.MultilineString)
                return null;
            if (field.Kind == TryllFieldKind.Bool)
                return bool.TryParse(value, out _) ? null : "invalid bool";
            if (field.Kind == TryllFieldKind.Enum)
                return Enum.IsDefined(field.EnumType, value) ? null : $"invalid enum value '{value}'";
            return ValidateParsed(field.Kind, value, field.Min, field.Max, !field.IsOptional, out _);
        }

        private static string ValidateParsed(
            TryllFieldKind kind, string value, float? min, float? max, bool required, out double parsed)
        {
            parsed = 0;
            if (string.IsNullOrWhiteSpace(value))
                return required ? "value required" : null;

            if (kind == TryllFieldKind.Int)
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    return "invalid int";
                parsed = i;
            }
            else if (kind == TryllFieldKind.Float)
            {
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fl))
                    return "invalid float";
                parsed = fl;
            }
            else return null;

            if (min.HasValue && parsed < min.Value) return $"must be ≥ {min.Value.ToString(CultureInfo.InvariantCulture)}";
            if (max.HasValue && parsed > max.Value) return $"must be ≤ {max.Value.ToString(CultureInfo.InvariantCulture)}";
            return null;
        }

        private static void ReadBounds(FieldInfo f, out float? min, out float? max)
        {
            min = null;
            max = null;
            var optRange = f.GetCustomAttribute<TryllOptionalRangeAttribute>();
            if (optRange != null) { min = optRange.Min; max = optRange.Max; return; }
            var optMin = f.GetCustomAttribute<TryllOptionalMinAttribute>();
            if (optMin != null) { min = optMin.Min; return; }
            var range = f.GetCustomAttribute<RangeAttribute>();
            if (range != null) { min = range.min; max = range.max; return; }
            var unityMin = f.GetCustomAttribute<MinAttribute>();
            if (unityMin != null) min = unityMin.min;
        }

        private static string SplitPascalCase(string name)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) sb.Append(' ');
                sb.Append(name[i]);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// UI Toolkit panel for authoring one <see cref="TryllDialogLabVariant"/>'s
    /// overrides against a given graph. Structural fields never appear.
    /// </summary>
    internal sealed class TryllVariantPanel : VisualElement
    {
        private readonly TryllGraphDescription _graph;
        private readonly TryllDialogLabVariant _variant;
        private readonly Action _onChanged;

        private DropdownField _nodeDropdown;
        private DropdownField _fieldDropdown;
        private VisualElement _valueHost;
        private VisualElement _listHost;
        private Label _addError;
        private string _pendingValue = "";

        public TryllVariantPanel(TryllGraphDescription graph, TryllDialogLabVariant variant, Action onChanged)
        {
            _graph = graph;
            _variant = variant;
            _onChanged = onChanged;
            BuildUi();
        }

        private void BuildUi()
        {
            Clear();
            style.flexDirection = FlexDirection.Column;

            var addRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };

            var nodeNames = (_graph?.Nodes ?? new List<TryllNodeDescription>()).Select(n => n.Name).ToList();
            _nodeDropdown = new DropdownField("Node", nodeNames, nodeNames.Count > 0 ? 0 : -1);
            _nodeDropdown.style.flexGrow = 1;
            _nodeDropdown.RegisterValueChangedCallback(_ => RefreshFieldDropdown());
            addRow.Add(_nodeDropdown);

            _fieldDropdown = new DropdownField("Field", new List<string>(), -1);
            _fieldDropdown.style.flexGrow = 1;
            _fieldDropdown.RegisterValueChangedCallback(_ => RefreshValueEditor());
            addRow.Add(_fieldDropdown);

            Add(addRow);

            _valueHost = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
            Add(_valueHost);

            _addError = new Label();
            _addError.style.color = TryllEditorTheme.ColWarn;
            _addError.style.fontSize = 10;
            _addError.style.display = DisplayStyle.None;
            Add(_addError);

            var note = new Label(
                "Model / context-size / graph-wiring fields are not offered here — they " +
                "require a fresh agent (ChangeParams rejects them: 3006). To compare models " +
                "or context sizes, assign a different Agent on this variant.");
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.fontSize = 10;
            note.style.opacity = 0.6f;
            note.style.marginBottom = 6;
            Add(note);

            _listHost = new VisualElement();
            Add(_listHost);

            RefreshFieldDropdown();
            RefreshOverrideList();
        }

        private TryllNodeDescription SelectedNode() =>
            _graph?.Nodes?.FirstOrDefault(n => n.Name == _nodeDropdown.value);

        private void RefreshFieldDropdown()
        {
            var node = SelectedNode();
            var fields = node?.Params != null
                ? TryllNodeFieldReflection.EnumerateOverridableFields(node.Params.GetType())
                : Array.Empty<TryllOverridableField>();

            var names = fields.Select(f => f.DisplayName).ToList();
            _fieldDropdown.choices = names;
            _fieldDropdown.index = names.Count > 0 ? 0 : -1;
            RefreshValueEditor();
        }

        private TryllOverridableField SelectedField()
        {
            var node = SelectedNode();
            if (node?.Params == null) return null;
            var fields = TryllNodeFieldReflection.EnumerateOverridableFields(node.Params.GetType());
            return fields.FirstOrDefault(f => f.DisplayName == _fieldDropdown.value);
        }

        private void RefreshValueEditor()
        {
            _valueHost.Clear();
            _addError.style.display = DisplayStyle.None;
            var field = SelectedField();
            var node = SelectedNode();
            if (field == null || node?.Params == null) return;

            _pendingValue = field.Get(node.Params);
            VisualElement editor = field.Kind switch
            {
                TryllFieldKind.Bool => MakeBoolEditor(),
                TryllFieldKind.Enum => MakeEnumEditor(field),
                TryllFieldKind.Int => MakeNumericEditor(true),
                TryllFieldKind.Float => MakeNumericEditor(false),
                TryllFieldKind.MultilineString => MakeTextEditor(true),
                _ => MakeTextEditor(false),
            };
            // The editor takes the leftover width and may shrink below its text's width;
            // the button keeps its natural size (default flex-shrink 1 squeezed it away).
            editor.style.flexGrow = 1;
            editor.style.flexShrink = 1;
            editor.style.minWidth = 0;
            _valueHost.Add(editor);

            var add = new Button(() => AddOverride(node.Name, field)) { text = "Add override" };
            add.style.flexShrink = 0;
            if (field.Kind == TryllFieldKind.MultilineString)
                add.style.alignSelf = Align.FlexStart;
            _valueHost.Add(add);
        }

        private VisualElement MakeTextEditor(bool multiline)
        {
            var tf = new TextField { value = _pendingValue, multiline = multiline };
            if (multiline)
            {
                // Wrap long prompts to the row width and scroll past ~10 lines, like [TextArea(3, 10)].
                tf.style.minHeight = 40;
                tf.style.maxHeight = 160;
                tf.style.whiteSpace = WhiteSpace.Normal;
                tf.verticalScrollerVisibility = ScrollerVisibility.Auto;
            }
            tf.RegisterValueChangedCallback(e => _pendingValue = e.newValue);
            return tf;
        }

        private VisualElement MakeNumericEditor(bool isInt)
        {
            if (isInt)
            {
                int.TryParse(_pendingValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv);
                var field = new IntegerField { value = iv };
                field.RegisterValueChangedCallback(e =>
                    _pendingValue = e.newValue.ToString(CultureInfo.InvariantCulture));
                _pendingValue = field.value.ToString(CultureInfo.InvariantCulture);
                return field;
            }
            float.TryParse(_pendingValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var fv);
            var ff = new FloatField { value = fv };
            ff.RegisterValueChangedCallback(e =>
                _pendingValue = e.newValue.ToString(CultureInfo.InvariantCulture));
            _pendingValue = ff.value.ToString(CultureInfo.InvariantCulture);
            return ff;
        }

        private VisualElement MakeBoolEditor()
        {
            var tg = new Toggle { value = _pendingValue == "true" };
            tg.RegisterValueChangedCallback(e => _pendingValue = e.newValue ? "true" : "false");
            return tg;
        }

        private VisualElement MakeEnumEditor(TryllOverridableField field)
        {
            var names = Enum.GetNames(field.EnumType).ToList();
            int idx = names.IndexOf(_pendingValue);
            var dd = new DropdownField(names, idx >= 0 ? idx : 0);
            dd.RegisterValueChangedCallback(e => _pendingValue = e.newValue);
            _pendingValue = dd.value;
            return dd;
        }

        private void AddOverride(string nodeName, TryllOverridableField field)
        {
            string err = TryllNodeFieldReflection.ValidateValue(field, _pendingValue);
            if (err != null)
            {
                _addError.text = err;
                _addError.style.display = DisplayStyle.Flex;
                return;
            }
            _addError.style.display = DisplayStyle.None;

            var existing = _variant.Overrides.FirstOrDefault(o => o.NodeName == nodeName && o.FieldPath == field.FieldPath);
            if (existing != null) existing.Value = _pendingValue;
            else _variant.Overrides.Add(new TryllParamOverride { NodeName = nodeName, FieldPath = field.FieldPath, Value = _pendingValue });
            RefreshOverrideList();
            _onChanged?.Invoke();
        }

        private void RefreshOverrideList()
        {
            _listHost.Clear();
            var known = new HashSet<string>((_graph?.Nodes ?? new List<TryllNodeDescription>()).Select(n => n.Name));
            foreach (var o in _variant.Overrides.ToList())
            {
                string problem = null;
                if (!known.Contains(o.NodeName))
                    problem = "not in this agent's workflow";
                else
                {
                    var node = _graph.Nodes.First(n => n.Name == o.NodeName);
                    if (node.Params == null || !TryllNodeFieldReflection.TryGetField(node.Params.GetType(), o.FieldPath, out var field))
                        problem = "field not overridable on this node type";
                    else
                        problem = TryllNodeFieldReflection.ValidateValue(field, o.Value);
                }

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
                var label = new Label(problem == null
                    ? $"{o.NodeName} · {o.FieldPath} = {o.Value}"
                    : $"{o.NodeName} · {o.FieldPath} = {o.Value}  {TryllEditorUiContract.IconWarning} {problem}");
                label.style.flexGrow = 1;
                label.style.overflow = Overflow.Hidden;
                label.style.color = problem == null
                    ? StyleKeyword.Null
                    : TryllEditorTheme.ColWarn;
                row.Add(label);
                var remove = new Button(() =>
                {
                    _variant.Overrides.Remove(o);
                    RefreshOverrideList();
                    _onChanged?.Invoke();
                })
                { text = "✕" };
                row.Add(remove);
                _listHost.Add(row);
            }
        }
    }
}
