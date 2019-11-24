using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
using UnityEngine;

using Object = UnityEngine.Object;
#endif

namespace Capstones.UnityEngineEx
{
    public static class LanguageConverter
    {
        public class CapsLangFormatter : IFormatProvider, ICustomFormatter
        {
            public object GetFormat(Type format)
            {
                if (format == typeof(ICustomFormatter))
                    return this;
                return null;
            }

            private static readonly char[] _WordSplitChars = new[] { '/' };

            public string Format(string format, object arg, IFormatProvider provider)
            {
                if (format == null)
                {
                    if (arg is IFormattable)
                        return ((IFormattable)arg).ToString(format, provider);
                    return arg.ToString();
                }
                else
                {
                    if (format.StartsWith("`cnt`"))
                    {
                        // Examples:
                        // string.Format(Instance, "Look! {1} {0:`cnt`is/are} {0:`cnt`a} {0:`cnt`child/children}", 1, "There")
                        // string.Format(Instance, "Look! {1} {0:`cnt`is/are} {0:`cnt`a} {0:`cnt`child/children}", 10, "There")
                        string sub = format.Substring("`cnt`".Length);
                        var words = sub.Split(_WordSplitChars, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length > 0)
                        {
                            double cnt = 0;
                            try
                            {
                                cnt = Convert.ToDouble(arg);
                                if (Math.Abs(cnt) > 1)
                                {
                                    if (words.Length > 1)
                                    {
                                        return words[1];
                                    }
                                }
                                else
                                {
                                    return words[0];
                                }
                            }
                            catch (Exception e)
                            {
                                PlatDependant.LogError(e);
                            }
                        }
                        if (arg is IFormattable)
                            return ((IFormattable)arg).ToString("", provider); // invalid format. so we set format to "" and call default formatter.
                        return arg.ToString();
                    }
                    else
                    {
                        if (arg is IFormattable)
                            return ((IFormattable)arg).ToString(format, provider);
                        return arg.ToString();
                    }
                }
            }

            public static readonly CapsLangFormatter Instance = new CapsLangFormatter();
        }

        public static string JSONPATH = "config/language.json";
        private static Dictionary<string, string> _LangDict;

        public static void Init()
        {
            _LangDict = ResManager.TryLoadConfig(JSONPATH);
        }

        public static bool ContainsKey(string key)
        {
            return _LangDict != null && _LangDict.ContainsKey(key);
        }

        public static string GetLangValue(string key, params object[] args)
        {
            string format = null, result = null;
            if (key == null)
            {
                PlatDependant.LogError("Language Converter - cannot convert null key.");
            }
            else
            {
                if (_LangDict == null)
                {
                    PlatDependant.LogWarning("Language Converter - not initialized.");
                }
                else
                {
                    if (!_LangDict.TryGetValue(key, out format))
                    {
                        PlatDependant.LogError("Language Converter - cannot find key: " + key);
                    }
                    else
                    {
                        if (format == null)
                        {
                            PlatDependant.LogError("Language Converter - null record for key: " + key);
                        }
                        else
                        {
                            if (args != null && args.Length > 0)
                            {
                                try
                                {
                                    result = string.Format(CapsLangFormatter.Instance, format, args);
                                }
                                catch (Exception e)
                                {
                                    PlatDependant.LogError(e);
                                    System.Text.StringBuilder sbmess = new System.Text.StringBuilder();
                                    sbmess.AppendLine("Language Converter - format failed.");
                                    sbmess.Append("key: ");
                                    sbmess.AppendLine(key);
                                    sbmess.Append("format: ");
                                    sbmess.AppendLine(format);
                                    sbmess.Append("args: cnt ");
                                    sbmess.AppendLine(args.Length.ToString());
                                    for (int i = 0; i < args.Length; ++i)
                                    {
                                        sbmess.AppendLine((args[i] ?? "null").ToString());
                                    }
                                    PlatDependant.LogError(sbmess);
                                }
                            }
                        }
                    }
                }
            }
            return result ?? format ?? key;
        }

#if UNITY_ENGINE || UNITY_5_3_OR_NEWER
        public static void IterateText(Transform trans)
        {
            UnityEngine.UI.Text[] textArr = trans.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var item in textArr)
            {
                string txt = item.text;
                int posIndex = txt.IndexOf('@');
                if (posIndex >= 0)
                {
                    string langValue = GetLangValue(txt.Substring(posIndex + 1));
                    item.text = txt.Substring(0, posIndex) + (langValue ?? "");
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnUnityStart()
        {
            ResManager.AddInitItem(ResManager.LifetimeOrders.PostResLoader - 5, Init);
        }
#endif

#if UNITY_EDITOR || !UNITY_ENGINE && !UNITY_5_3_OR_NEWER
        static LanguageConverter()
        {
            Init();
        }
#endif
    }
}