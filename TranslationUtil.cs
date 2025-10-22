using HarmonyLib;
using MonoMod.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.Assertions.Must;
using System.Linq;
using UnityEngine.SceneManagement;
using static CimdyTranslationUtil.TranslationUtil;

namespace CimdyTranslationUtil
{
    /// <summary>
    /// Utilities I made for translations, allows loading translation data from .json files
    /// 
    /// <para>You MUST call <see cref="SetHarmony(Harmony)"/> in your mod and provide your mods Harmony so that it is capable of doing the patches it needs to</para>
    /// 
    /// <para>Example JSON:</para>
    /// <code>
    /// {
    ///     "SHEET1": {
    ///         "KEY1": "Example Key Data",
    ///         "KEY2": "Another Key Data"
    ///     },
    ///         "SHEET2": {
    ///         "KEY": "Key Data in another Sheet"
    ///     }
    /// }
    /// </code>
    /// <para>
    /// You can load the json in multiple ways:
    /// <list type="number">
    ///     <item>
    ///         <term><see cref="LoadJson(string, string)"/></term>
    ///         <description>For loading from a json string</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="LoadJsonFile(string, string)"/></term>
    ///         <description>For loading from a json from file</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="LoadJsonFromStream(string, Stream)"/></term>
    ///         <description>For loading from a json from a stream</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="LoadJsonFromAssemblyEmbedded(string, string, Assembly)"/></term>
    ///         <description>For loading from a json from embedded resources in a specified assembly</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="LoadJsonFromEmbedded(string, string)"/></term>
    ///         <description>For loading from a json from embedded resources in the calling assembly</description>
    ///     </item>
    /// </list>
    /// </para>
    /// </summary>
    public class TranslationUtil
    {
        /// <summary>
        /// The currently loaded data for translations
        /// </summary>
        public static readonly TranslationData data = new TranslationData();
        static Harmony harmony;
        static TranslationUtil()
        {
            SceneManager.sceneLoaded += OnLoadScene;
        }
        /// <summary>
        /// Provides the <paramref name="harmony"/> that this will use for patches
        /// </summary>
        /// <param name="harmony"></param>
        public static void SetHarmony(Harmony harmony)
        {
            TranslationUtil.harmony = harmony;
        }
        private static bool patched = false;
        private static void OnLoadScene(Scene scene, LoadSceneMode mode)
        {
            if (!patched)
            {
                TranslationPatches.Patch(harmony);
                patched = true;
            }
        }
        /// <summary>
        /// Loads translation data json from an embedded resource in the calling assembly
        /// </summary>
        /// <param name="language"></param>
        /// <param name="name"></param>
        public static void LoadJsonFromEmbedded(string language, string name)
        {
            LoadJsonFromAssemblyEmbedded(language, name, Assembly.GetCallingAssembly());
        }
        /// <summary>
        /// Loads translation data json from an embedded resource in the specified <paramref name="assembly"/>
        /// </summary>
        /// <param name="language"></param>
        /// <param name="name"></param>
        /// <param name="assembly"></param>
        public static void LoadJsonFromAssemblyEmbedded(string language, string name, Assembly assembly)
        {
            if (!assembly.GetManifestResourceNames().Contains(name))
            {
                throw new Exception("No json with the name of \"" + name + "\" in assembly \"" + assembly.GetName().Name + "\"");
            }
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                LoadJsonFromStream(language, stream);
            }
        }
        /// <summary>
        /// Loads translation data json from the specified <paramref name="stream"/>
        /// </summary>
        /// <param name="language"></param>
        /// <param name="stream"></param>
        public static void LoadJsonFromStream(string language, Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                LoadJson(language, json);
            }
        }
        /// <summary>
        /// Loads translation data json from the specified <paramref name="filepath"/>
        /// </summary>
        /// <param name="language"></param>
        /// <param name="filepath"></param>
        public static void LoadJsonFile(string language, string filepath)
        {
            LoadJson(language, File.ReadAllText(filepath));
        }
        /// <summary>
        /// Loads translation data from the specified <paramref name="json"/> data
        /// </summary>
        /// <param name="language"></param>
        /// <param name="json"></param>
        public static void LoadJson(string language, string json)
        {
            TranslationData.Language lang = data.AddLanguage(new TranslationData.Language(language));
            JObject jsonObj = JObject.Parse(json);
            foreach (KeyValuePair<string, JToken?> i in jsonObj)
            {
                string sheetName = i.Key;
                JToken? jToken = i.Value;
                if (jToken != null && jToken.Type == JTokenType.Object)
                {
                    TranslationData.Sheet sheet = new TranslationData.Sheet(sheetName);
                    JObject obj = (JObject)jToken;
                    foreach (KeyValuePair<string, JToken?> j in obj)
                    {
                        string key = j.Key;
                        if (j.Value != null && j.Value.Type == JTokenType.String)
                        {
                            string value = j.Value.ToString();
                            sheet.entries.Add(key, value);
                        }
                    }
                    if (sheet.entries.Count > 0)
                    {
                        lang.AddSheet(sheet);
                    }
                }
            }
        }
        public class TranslationData
        {
            public Dictionary<string, Language> languages = new Dictionary<string, Language>();
            internal TranslationData() { }
            public class Sheet
            {
                public string name;
                internal Sheet(string name)
                {
                    this.name = name;
                }
                public Dictionary<string, string> entries = new Dictionary<string, string>();
            }
            public class Language
            {
                public string name;
                internal Language(string name)
                {
                    this.name = name;
                }
                public Dictionary<string, Sheet> sheets = new Dictionary<string, Sheet>();
                public Sheet AddSheet(Sheet sheet)
                {
                    Sheet usedSheet = sheet;
                    if (sheets.ContainsKey(sheet.name))
                    {
                        foreach (KeyValuePair<string, string> pair in sheet.entries)
                        {
                            if (!usedSheet.entries.TryAdd(pair.Key, pair.Value))
                            {
                                usedSheet.entries[pair.Key] = pair.Value;
                            }
                        }
                    }
                    else
                    {
                        sheets.Add(sheet.name, sheet);
                    }
                    return usedSheet;
                }
                public Dictionary<string, Dictionary<string, string>> Build()
                {
                    Dictionary<string, Dictionary<string, string>> data = new Dictionary<string, Dictionary<string, string>>();
                    foreach (KeyValuePair<string, Sheet> sheet in sheets)
                    {
                        Dictionary<string, string> values = new Dictionary<string, string>();
                        values.AddRange(sheet.Value.entries);
                        data.Add(sheet.Key, values);
                    }
                    return data;
                }
                public void BuildInto(ref Dictionary<string, Dictionary<string, string>> other)
                {
                    Dictionary<string, Dictionary<string, string>> built = Build();
                    foreach (KeyValuePair<string, Dictionary<string, string>> sheet in built)
                    {
                        if (other.ContainsKey(sheet.Key))
                        {
                            foreach (KeyValuePair<string, string> pair in sheet.Value) 
                            {
                                if (!other[sheet.Key].TryAdd(pair.Key, pair.Value))
                                {
                                    other[sheet.Key][pair.Key] = pair.Value;
                                }
                            }
                        } else
                        {
                            other.Add(sheet.Key, sheet.Value);
                        }
                    }
                }
            }
            public Language AddLanguage(Language language)
            {
                Language usedLanguage = language;
                if (languages.ContainsKey(language.name))
                {
                    usedLanguage = languages[language.name];
                    foreach (KeyValuePair<string, Sheet> pair in language.sheets)
                    {
                        if (!usedLanguage.sheets.TryAdd(pair.Key, pair.Value))
                        {
                            usedLanguage.sheets[pair.Key] = pair.Value;
                        }
                    }
                }
                else
                {
                    languages.Add(language.name, language);
                }
                return usedLanguage;
            }
        }
    }
    internal class TranslationPatches
    {
        public static void LoadForLanguage(LanguageCode code)
        {
            Dictionary<string, Dictionary<string, string>> entrySheets = Language._currentEntrySheets;

            string defaultLangCode = Language.Settings.defaultLangCode;
            if (data.languages.ContainsKey(defaultLangCode))
            {
                TranslationData.Language defaultLang = data.languages[defaultLangCode];
                defaultLang.BuildInto(ref entrySheets);
            }
            string currentLangCode = code.ToString();
            if (data.languages.ContainsKey(currentLangCode))
            {
                TranslationData.Language currentLang = data.languages[currentLangCode];
                currentLang.BuildInto(ref entrySheets);
            }

            Language._currentEntrySheets = entrySheets;
        }
        public static void Patch(Harmony harmony)
        {
            harmony.Patch(typeof(Language).GetMethod("SwitchLanguage", [typeof(LanguageCode)]), null, new HarmonyMethod(typeof(TranslationPatches).GetMethod("LoadForLanguage")));
        }
    }
}
