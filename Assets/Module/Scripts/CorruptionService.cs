using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class CorruptionService : MonoBehaviour
{
    public bool SettingsLoaded = false;

    private string _settingsFile;
    private CorruptionSettings _settings;

    void Start()
    {
        name = "Corruption Service";

        _settingsFile = Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), "CorruptionSettings.json");

        if (!File.Exists(_settingsFile))
            _settings = new CorruptionSettings();
        else
        {
            try
            {
                _settings = JsonConvert.DeserializeObject<CorruptionSettings>(File.ReadAllText(_settingsFile), new StringEnumConverter());
                if (_settings == null)
                    throw new Exception("Settings could not be read. Creating new Settings...");
                SettingsLoaded = true;
                Debug.LogFormat(@"[Corruption Service] Settings successfully loaded");
            }
            catch (Exception e)
            {
                Debug.LogFormat(@"[Corruption Service] Error loading settings file:");
                Debug.LogException(e);
                _settings = new CorruptionSettings();
            }
        }

        Debug.LogFormat(@"[Corruption Service] Service is active");
        if (_settings.AutomaticUpdate || _settings.Version != 0)
            StartCoroutine(GetData());
        else
            Debug.LogFormat(@"[Corruption Service] Automatic Update is disabled!");
    }

    public bool MustAutoSolve(string moduleId)
    {
        string setting;
        return _settings.RememberedCompatibilities.TryGetValue(moduleId, out setting) && setting == "RequiresAutoSolve";
    }

    public bool MustNotBeInfected(string moduleId)
    {
        string setting;
        return (_settings.RememberedCompatibilities.TryGetValue(moduleId, out setting) && (setting == "MustNotBeInfected"));
    }

    public string GetModuleType(string moduleId)
    {
        string setting;
        return _settings.RememberedCompatibilities.TryGetValue(moduleId, out setting) ? setting : "Unexamined";
    }

    IEnumerator GetData()
    {
        using (var http = UnityWebRequest.Get(_settings.SiteUrl))
        {
            // Request and wait for the desired page.
            yield return http.SendWebRequest();

            if (http.isNetworkError)
            {
                Debug.LogFormat(@"[Corruption Service] Website {0} responded with error: {1}", _settings.SiteUrl, http.error);
                yield break;
            }

            if (http.responseCode != 200)
            {
                Debug.LogFormat(@"[Corruption Service] Website {0} responded with code: {1}", _settings.SiteUrl, http.responseCode);
                yield break;
            }

            var allModules = JObject.Parse(http.downloadHandler.text)["KtaneModules"] as JArray;
            if (allModules == null)
            {
                Debug.LogFormat(@"[Corruption Service] Website {0} did not respond with a JSON array at “KtaneModules” key.", _settings.SiteUrl, http.responseCode);
                yield break;
            }

            var compatibilities = new Dictionary<string, string>();

            foreach (JObject module in allModules)
            {
                var id = module["ModuleID"] as JValue;
                if (id == null || !(id.Value is string))
                    continue;
                var compatibility = module["Corruption"] as JValue;
                if (compatibility == null || !(compatibility.Value is string))
                    continue;
                compatibilities[(string)id.Value] = (string)compatibility.Value;
            }

            Debug.LogFormat(@"[Corruption Service] List successfully loaded:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, compatibilities.Select(kvp => string.Format("[Corruption Service] {0} => {1}", kvp.Key, kvp.Value)).ToArray()));
            _settings.RememberedCompatibilities = compatibilities;
            _settings.Version = 0;
            SettingsLoaded = true;

            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(_settingsFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile));
                File.WriteAllText(_settingsFile, JsonConvert.SerializeObject(_settings, Formatting.Indented, new StringEnumConverter()));
            }
            catch (Exception e)
            {
                Debug.LogFormat("[Corruption Service] Failed to save settings file:");
                Debug.LogException(e);
            }
        }
    }
}