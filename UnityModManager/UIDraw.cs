﻿
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityModManagerNet
{
    /// <summary>
    /// [0.18.0]
    /// </summary>
    public enum DrawType { Auto, Ignore, Field, Slider, Toggle, ToggleGroup, /*MultiToggle, */PopupList };

    /// <summary>
    /// [0.18.0]
    /// </summary>
    [Flags]
    public enum DrawFieldMask { Any = 0, Public = 1, Serialized = 2, SkipNotSerialized = 4, OnlyDrawAttr = 8 };

    /// <summary>
    /// [0.18.0]
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        /// Called when values change. For sliders it is called too often.
        /// </summary>
        void OnChange();
    }

    /// <summary>
    /// [0.18.0]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field, AllowMultiple = false)]
    public class DrawFieldsAttribute : Attribute
    {
        public DrawFieldMask Mask;

        public DrawFieldsAttribute(DrawFieldMask Mask)
        {
            this.Mask = Mask;
        }
    }

    /// <summary>
    /// [0.18.0]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DrawAttribute : Attribute
    {
        public DrawType Type = DrawType.Auto;
        public string Label;
        public int Width = 0;
        public int Height = 0;
        public double Min = double.MinValue;
        public double Max = double.MaxValue;
        /// <summary>
        /// Rounds a double-precision floating-point value to a specified number of fractional digits, and rounds midpoint values to the nearest even number. 
        /// Default 2
        /// </summary>
        public int Precision = 2;
        public int MaxLength = int.MaxValue;
        //public string DependsOn;

        public DrawAttribute()
        {
        }

        public DrawAttribute(string Label)
        {
            this.Label = Label;
        }

        public DrawAttribute(string Label, DrawType Type)
        {
            this.Label = Label;
            this.Type = Type;
        }

        public DrawAttribute(DrawType Type)
        {
            this.Type = Type;
        }
    }

    /// <summary>
    /// [0.18.0]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class HorizontalAttribute : Attribute
    {
    }

    public partial class UnityModManager
    {
        public partial class UI : MonoBehaviour
        {
            static Type[] fieldTypes = new[] { typeof(int), typeof(long), typeof(float), typeof(double), typeof(string), typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Color) };
            static Type[] sliderTypes = new[] { typeof(int), typeof(long), typeof(float), typeof(double) };
            static Type[] toggleTypes = new[] { typeof(bool) };
            static Type[] specialTypes = new[] { typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Color) };
            static float drawHeight = 24;

            /// <summary>
            /// [0.18.0]
            /// </summary>
            /// <returns>
            /// Returns true if the value has changed.
            /// </returns>
            public static bool DrawVector(ref Vector2 vec, GUIStyle style = null, params GUILayoutOption[] option)
            {
                var values = new float[2] { vec.x, vec.y };
                var labels = new string[2] { "x", "y" };
                if(DrawFloatMultiField(ref values, labels, style, option))
                {
                    vec = new Vector2(values[0], values[1]);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// [0.18.0]
            /// </summary>
            public static void DrawVector(Vector2 vec, Action<Vector2> onChange, GUIStyle style = null, params GUILayoutOption[] option)
            {
                if (onChange == null)
                {
                    throw new ArgumentNullException("onChange");
                }
                if (DrawVector(ref vec, style, option))
                {
                    onChange(vec);
                }
            }

            /// <summary>
            /// [0.18.0]
            /// </summary>
            /// <returns>
            /// Returns true if the value has changed.
            /// </returns>
            public static bool DrawVector(ref Vector3 vec, GUIStyle style = null, params GUILayoutOption[] option)
            {
                var values = new float[3] { vec.x, vec.y, vec.z };
                var labels = new string[3] { "x", "y", "z" };
                if (DrawFloatMultiField(ref values, labels, style, option))
                {
                    vec = new Vector3(values[0], values[1], values[2]);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// [0.18.0]
            /// </summary>
            public static void DrawVector(Vector3 vec, Action<Vector3> onChange, GUIStyle style = null, params GUILayoutOption[] option)
            {
                if (onChange == null)
                {
                    throw new ArgumentNullException("onChange");
                }
                if (DrawVector(ref vec, style, option))
                {
                    onChange(vec);
                }
            }

            /// <summary>
            /// [0.18.0]
            /// </summary>
            /// <returns>
            /// Returns true if the value has changed.
            /// </returns>
            public static bool DrawVector(ref Vector4 vec, GUIStyle style = null, params GUILayoutOption[] option)
            {
                var values = new float[4] { vec.x, vec.y, vec.z, vec.w };
                var labels = new string[4] { "x", "y", "z", "w" };
                if (DrawFloatMultiField(ref values, labels, style, option))
                {
                    vec = new Vector4(values[0], values[1], values[2], values[3]);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// [0.18.0]
            /// </summary>
            public static void DrawVector(Vector4 vec, Action<Vector4> onChange, GUIStyle style = null, params GUILayoutOption[] option)
            {
                if (onChange == null)
                {
                    throw new ArgumentNullException("onChange");
                }
                if (DrawVector(ref vec, style, option))
                {
                    onChange(vec);
                }
            }

            /// <summary>
            /// [0.18.0]
            /// </summary>
            /// <returns>
            /// Returns true if the value has changed.
            /// </returns>
            public static bool DrawColor(ref Color vec, GUIStyle style = null, params GUILayoutOption[] option)
            {
                var values = new float[4] { vec.r, vec.g, vec.b, vec.a };
                var labels = new string[4] { "r", "g", "b", "a" };
                if (DrawFloatMultiField(ref values, labels, style, option))
                {
                    vec = new Color(values[0], values[1], values[2], values[3]);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// [0.18.0]
            /// </summary>
            public static void DrawVector(Color vec, Action<Color> onChange, GUIStyle style = null, params GUILayoutOption[] option)
            {
                if (onChange == null)
                {
                    throw new ArgumentNullException("onChange");
                }
                if (DrawColor(ref vec, style, option))
                {
                    onChange(vec);
                }
            }

            /// <summary>
            /// [0.18.0]
            /// </summary>
            /// <returns>
            /// Returns true if the value has changed.
            /// </returns>
            public static bool DrawFloatMultiField(ref float[] values, string[] labels, GUIStyle style = null, params GUILayoutOption[] option)
            {
                if (values == null || values.Length == 0)
                    throw new ArgumentNullException(nameof(values));
                if (labels == null || labels.Length == 0)
                    throw new ArgumentNullException(nameof(labels));
                if(values.Length != labels.Length)
                    throw new ArgumentOutOfRangeException(nameof(labels));

                var changed = false;
                var result = new float[values.Length];
                
                for (int i = 0; i < values.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(labels[i], GUILayout.ExpandWidth(false));
                    var str = GUILayout.TextField(values[i].ToString("f6"), style ?? GUI.skin.textField, option);
                    GUILayout.EndHorizontal();
                    if (string.IsNullOrEmpty(str))
                    {
                        result[i] = 0;
                    }
                    else
                    {
                        if (float.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out var num))
                        {
                            result[i] = num;
                        }
                        else
                        {
                            result[i] = 0;
                        }
                    }
                    if (result[i] != values[i])
                    {
                        changed = true;
                    }
                }
                
                values = result;
                return changed;
            }

            private static bool Draw(object container, Type type, ModEntry mod, DrawFieldMask defaultMask)
            {
                bool changed = false;
                var options = new List<GUILayoutOption>();
                DrawFieldMask mask = defaultMask;
                foreach(DrawFieldsAttribute attr in type.GetCustomAttributes(typeof(DrawFieldsAttribute), false))
                {
                    mask = attr.Mask;
                }
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var f in fields)
                {
                    DrawAttribute a = new DrawAttribute();
                    var attributes = f.GetCustomAttributes(typeof(DrawAttribute), false);
                    if (attributes.Length > 0)
                    {
                        foreach (DrawAttribute a_ in attributes)
                        {
                            a = a_;
                            a.Width = a.Width != 0 ? Scale(a.Width) : 0;
                            a.Height = a.Height != 0 ? Scale(a.Height) : 0;
                        }

                        if (a.Type == DrawType.Ignore)
                            continue;

                        //if (!string.IsNullOrEmpty(a.DependsOn))
                        //{
                        //    var field = type.GetField(a.DependsOn, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        //    if (field == null)
                        //    {

                        //    }
                        //    else
                        //    {
                        //        var value = field.GetValue(container);
                        //    }

                        //}
                    }
                    else
                    {
                        if ((mask & DrawFieldMask.OnlyDrawAttr) == 0 && ((mask & DrawFieldMask.SkipNotSerialized) == 0 || !f.IsNotSerialized)
                            && ((mask & DrawFieldMask.Public) > 0 && f.IsPublic 
                            || (mask & DrawFieldMask.Serialized) > 0 && f.GetCustomAttributes(typeof(SerializeField), false).Length > 0
                            || (mask & DrawFieldMask.Public) == 0 && (mask & DrawFieldMask.Serialized) == 0))
                        {
                            foreach (RangeAttribute a_ in f.GetCustomAttributes(typeof(RangeAttribute), false))
                            {
                                a.Type = DrawType.Slider;
                                a.Min = a_.min;
                                a.Max = a_.max;
                                break;
                            }
                            foreach (HeaderAttribute a_ in f.GetCustomAttributes(typeof(HeaderAttribute), false))
                            {
                                a.Label = a_.header;
                                break;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    foreach (SpaceAttribute a_ in f.GetCustomAttributes(typeof(SpaceAttribute), false))
                    {
                        GUILayout.Space(Scale((int)a_.height));
                    }

                    var fieldName = string.IsNullOrEmpty(a.Label) ? f.Name : a.Label;

                    if (f.FieldType.IsClass || f.FieldType.IsValueType && !f.FieldType.IsPrimitive && !f.FieldType.IsEnum && !Array.Exists(specialTypes, x => x == f.FieldType))
                    {
                        defaultMask = mask;
                        foreach (DrawFieldsAttribute attr in f.GetCustomAttributes(typeof(DrawFieldsAttribute), false))
                        {
                            defaultMask = attr.Mask;
                        }
                        var horizontal = f.GetCustomAttributes(typeof(HorizontalAttribute), false).Length > 0;
                        if (horizontal)
                            GUILayout.BeginHorizontal();
                        GUILayout.Label($"{fieldName}", GUILayout.ExpandWidth(false));
                        var val = f.GetValue(container);
                        if (Draw(val, f.FieldType, mod, defaultMask))
                        {
                            changed = true;
                            f.SetValue(container, val);
                        }
                        if (horizontal)
                            GUILayout.EndHorizontal();
                        continue;
                    }

                    options.Clear();
                    if (a.Type == DrawType.Auto)
                    {
                        if (Array.Exists(fieldTypes, x => x == f.FieldType))
                        {
                            a.Type = DrawType.Field;
                        }
                        else if (Array.Exists(toggleTypes, x => x == f.FieldType))
                        {
                            a.Type = DrawType.Toggle;
                        }
                        else if (f.FieldType.IsEnum)
                        {
                            if (f.GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
                                a.Type = DrawType.PopupList;
                        }
                    }

                    if (a.Type == DrawType.Field)
                    {
                        if (!Array.Exists(fieldTypes, x => x == f.FieldType))
                        {
                            mod.Logger.Error($"Type {f.FieldType} can't be drawed as {DrawType.Field}");
                            break;
                        }

                        options.Add(a.Width != 0 ? GUILayout.Width(a.Width) : GUILayout.Width(Scale(100)));
                        options.Add(a.Height != 0 ? GUILayout.Height(a.Height) : GUILayout.Height(Scale((int)drawHeight)));
                        if (f.FieldType == typeof(Vector2))
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            GUILayout.Space(Scale(5));
                            var vec = (Vector2)f.GetValue(container);
                            if (DrawVector(ref vec, null, options.ToArray()))
                            {
                                f.SetValue(container, vec);
                                changed = true;
                            }
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        else if (f.FieldType == typeof(Vector3))
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            GUILayout.Space(Scale(5));
                            var vec = (Vector3)f.GetValue(container);
                            if (DrawVector(ref vec, null, options.ToArray()))
                            {
                                f.SetValue(container, vec);
                                changed = true;
                            }
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        else if (f.FieldType == typeof(Vector4))
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            GUILayout.Space(Scale(5));
                            var vec = (Vector4)f.GetValue(container);
                            if (DrawVector(ref vec, null, options.ToArray()))
                            {
                                f.SetValue(container, vec);
                                changed = true;
                            }
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        else if (f.FieldType == typeof(Color))
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            GUILayout.Space(Scale(5));
                            var vec = (Color)f.GetValue(container);
                            if (DrawColor(ref vec, null, options.ToArray()))
                            {
                                f.SetValue(container, vec);
                                changed = true;
                            }
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            GUILayout.Space(Scale(5));
                            var val = f.GetValue(container).ToString();
                            if (a.Precision >= 0 && (f.FieldType == typeof(float) || f.FieldType == typeof(double)))
                            {
                                if (Double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out var num))
                                {
                                    val = num.ToString($"f{a.Precision}");
                                }
                            }
                            var result = f.FieldType == typeof(string) ? GUILayout.TextField(val, a.MaxLength, options.ToArray()) : GUILayout.TextField(val, options.ToArray());
                            GUILayout.EndHorizontal();
                            if (result != val)
                            {
                                if (string.IsNullOrEmpty(result))
                                {
                                    if (f.FieldType != typeof(string))
                                        result = "0";
                                }
                                else
                                {
                                    if (Double.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out var num))
                                    {
                                        num = Math.Max(num, a.Min);
                                        num = Math.Min(num, a.Max);
                                        result = num.ToString();
                                    }
                                    else
                                    {
                                        result = "0";
                                    }
                                }
                                f.SetValue(container, Convert.ChangeType(result, f.FieldType));
                                changed = true;
                            }
                        }
                    }
                    else if (a.Type == DrawType.Slider)
                    {
                        if (!Array.Exists(sliderTypes, x => x == f.FieldType))
                        {
                            mod.Logger.Error($"Type {f.FieldType} can't be drawed as {DrawType.Slider}");
                            break;
                        }

                        options.Add(a.Width != 0 ? GUILayout.Width(a.Width) : GUILayout.Width(Scale(200)));
                        options.Add(a.Height != 0 ? GUILayout.Height(a.Height) : GUILayout.Height(Scale((int)drawHeight)));
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                        GUILayout.Space(Scale(5));
                        var val = f.GetValue(container).ToString();
                        if (!Double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out var num))
                        {
                            mod.Logger.Error($"Field {fieldName} can't be parsed as number");
                            break;
                        }
                        var fnum = (float)num;
                        var result = GUILayout.HorizontalSlider(fnum, (float)a.Min, (float)a.Max, options.ToArray());
                        GUILayout.Space(Scale(5));
                        GUILayout.Label(result.ToString(), GUILayout.ExpandWidth(false), GUILayout.Height(Scale((int)drawHeight)));
                        GUILayout.EndHorizontal();
                        if (result != fnum)
                        {
                            if ((f.FieldType == typeof(float) || f.FieldType == typeof(double)) && a.Precision >= 0)
                                result = (float)Math.Round(result, a.Precision);
                            f.SetValue(container, Convert.ChangeType(result, f.FieldType));
                            changed = true;
                        }
                    }
                    else if (a.Type == DrawType.Toggle)
                    {
                        if (!Array.Exists(toggleTypes, x => x == f.FieldType))
                        {
                            mod.Logger.Error($"Type {f.FieldType} can't be drawed as {DrawType.Toggle}");
                            break;
                        }

                        options.Add(GUILayout.ExpandWidth(false));
                        options.Add(a.Height != 0 ? GUILayout.Height(a.Height) : GUILayout.Height(Scale((int)drawHeight)));
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                        var val = (bool)f.GetValue(container);
                        var result = GUILayout.Toggle(val, "", options.ToArray());
                        GUILayout.EndHorizontal();
                        if (result != val)
                        {
                            f.SetValue(container, Convert.ChangeType(result, f.FieldType));
                            changed = true;
                        }
                    }
                    else if (a.Type == DrawType.ToggleGroup)
                    {
                        if (!f.FieldType.IsEnum || f.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0)
                        {
                            mod.Logger.Error($"Type {f.FieldType} can't be drawed as {DrawType.ToggleGroup}");
                            break;
                        }

                        options.Add(GUILayout.ExpandWidth(false));
                        options.Add(a.Height != 0 ? GUILayout.Height(a.Height) : GUILayout.Height(Scale((int)drawHeight)));
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                        GUILayout.Space(Scale(5));
                        var values = Enum.GetNames(f.FieldType);
                        var val = (int)f.GetValue(container);

                        if (ToggleGroup(ref val, values, null, options.ToArray()))
                        {
                            var v = Enum.Parse(f.FieldType, values[val]);
                            f.SetValue(container, v);
                            changed = true;
                        }
                        GUILayout.EndHorizontal();
                    }
                    else if (a.Type == DrawType.PopupList)
                    {
                        if (!f.FieldType.IsEnum || f.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0)
                        {
                            mod.Logger.Error($"Type {f.FieldType} can't be drawed as {DrawType.PopupList}");
                            break;
                        }

                        options.Add(GUILayout.ExpandWidth(false));
                        options.Add(a.Height != 0 ? GUILayout.Height(a.Height) : GUILayout.Height(Scale((int)drawHeight)));
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                        GUILayout.Space(Scale(5));
                        var values = Enum.GetNames(f.FieldType);
                        var val = (int)f.GetValue(container);
                        if (PopupToggleGroup(ref val, values, null, options.ToArray()))
                        {
                            var v = Enum.Parse(f.FieldType, values[val]);
                            f.SetValue(container, v);
                            changed = true;
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                return changed;
            }

            /// <summary>
            /// [0.18.0]
            /// </summary>
            public static void DrawFields<T>(ref T container, ModEntry mod, DrawFieldMask defaultMask, Action onChange = null) where T : new()
            {
                object obj = container;
                var changed = Draw(obj, typeof(T), mod, defaultMask);
                if (changed)
                {
                    container = (T)obj;
                    if (onChange != null)
                    {
                        try
                        {
                            onChange();
                        }
                        catch (Exception e)
                        {
                            mod.Logger.LogException(e);
                        }
                    }
                }
            }
        }
    }

    public static partial class Extensions
    {
        /// <summary>
        /// [0.18.0]
        /// </summary>
        public static void Draw<T>(this T instance, UnityModManager.ModEntry mod) where T : class, IDrawable, new()
        {
            UnityModManager.UI.DrawFields(ref instance, mod, DrawFieldMask.OnlyDrawAttr, instance.OnChange);
        }
    }
}
