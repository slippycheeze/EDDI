﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Utilities;

namespace EddiVoiceAttackResponder
{
    public partial class VoiceAttackVariables
    {
        // Event keys that shall not be written to VoiceAttack
        private static readonly string[] ignoredKeys = { "type", "raw", "fromLoad", "edName", "baseName" };
        // Some types don't need to be decomposed further
        private static readonly Type[] undecomposedTypes = { typeof(string), typeof(DateTime), typeof(TimeSpan) };

        /// <summary> Walk an object and write out all of the possible fields </summary>
        /// <param name="prefix">The prefix to add in front of the property name</param>
        /// <param name="reflectionObjectType">The Type property of the object that we're walking, specified independent from the actual object in case the actual object value is null</param>
        /// <param name="setVars">The list of values that we're preparing to set within VoiceAttack</param>
        /// <param name="isTopLevel">(Optional) Whether we're looking at the top level of the object that we're walking to obtain values/param>
        /// <param name="reflectionObject">(Optional) The object that we're walking to obtain values. At the top level, this should be an `Event` class object</param>
        public static void PrepareEventVariables(string prefix, Type reflectionObjectType, ref List<VoiceAttackVariable> setVars, bool isTopLevel = true, object reflectionObject = null)
        {
            // Some types don't need to be decomposed further.
            if (undecomposedTypes.Contains(reflectionObjectType)) 
            {
                PrepareEventVariable(ref setVars, prefix, "", reflectionObjectType, reflectionObject, isTopLevel);
                return; 
            }

            var objectProperties = reflectionObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var objectFields = reflectionObjectType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var eventProperty in objectProperties)
            {
                PrepareEventVariable(ref setVars, prefix, eventProperty.Name, eventProperty.PropertyType, eventProperty.CanRead && reflectionObject != null ? eventProperty.GetValue(reflectionObject) : null, isTopLevel);
            }

            foreach (var eventField in objectFields)
            {
                PrepareEventVariable(ref setVars, prefix, eventField.Name, eventField.FieldType, reflectionObject != null ? eventField.GetValue(reflectionObject) : null, isTopLevel);
            }
        }

        private static void PrepareEventVariable(ref List<VoiceAttackVariable> setVars, string prefix, string key, Type type, object value, bool isTopLevel = true)
        {
            try
            {
                // We ignore some keys that are maintained for internal use only
                if (ignoredKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase))
                {
                    Logging.Debug("Ignoring key " + key);
                    return;
                }

                // Only append the child name to the current prefix if if does not repeat the prior word
                string childKey = AddSpacesToTitleCasedName(key).Replace("_", " ").ToLowerInvariant();
                string name;
                if (Regex.Match(prefix, @"(\w+)$").Value == childKey)
                {
                    name = prefix;
                }
                else
                {
                    name = prefix + " " + childKey;
                }

                // We also ignore any keys that we have already set elsewhere
                if (setVars.FirstOrDefault(v => v.Key == name) != null)
                {
                    Logging.Debug("Skipping already-set key " + name);
                    return;
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // Get the underlying type for nullable types
                    type = Nullable.GetUnderlyingType(type);
                }

                // Doubles, floats, and longs are all stored as decimals in VoiceAttack
                if (type == typeof(double) || type == typeof(float) || type == typeof(long))
                {
                    type = typeof(decimal);
                }

                Logging.Debug("Setting values for " + name);

                if (type == typeof(bool))
                {
                    setVars.Add(new VoiceAttackVariable(name, typeof(bool), (bool?)value, isTopLevel));
                }
                else if (type == typeof(string))
                {
                    setVars.Add(new VoiceAttackVariable(name, typeof(string), (string)value, isTopLevel));
                }
                else if (type == typeof(int))
                {
                    setVars.Add(new VoiceAttackVariable(name, typeof(int), (int?)value, isTopLevel));
                }
                else if (type == typeof(decimal))
                {
                    setVars.Add(new VoiceAttackVariable(name, typeof(decimal), value is null ? null : (decimal?)Convert.ToDecimal(value), isTopLevel));
                }
                else if (type == typeof(DateTime))
                {
                    setVars.Add(new VoiceAttackVariable(name, typeof(DateTime), (DateTime?)value, isTopLevel));
                }
                else if (type is null)
                {
                    setVars.Add(new VoiceAttackVariable(name, null, null, isTopLevel));
                }
                else if (!type.IsGenericType && type.IsEnum)
                {
                    var fieldsArray = type?.GetFields(BindingFlags.Public | BindingFlags.Static);
                    var enumName = value != null ? fieldsArray[(int)value].Name : null;
                    setVars.Add(new VoiceAttackVariable(name, typeof(string), enumName, isTopLevel));
                }
                else
                {
                    if (undecomposedTypes.Contains(type)) { return; }
                    else if (type.GetInterfaces().Contains(typeof(IDictionary)))
                    {
                        if (value != null)
                        {
                            foreach (DictionaryEntry kvp in (IDictionary)value)
                            {
                                PrepareEventVariables(name + " " + kvp.Key, kvp.Value.GetType(), ref setVars, false, kvp.Value);
                            }
                        }
                    }
                    else if (type.GetInterfaces().Contains(typeof(IEnumerable)))
                    {
                        // The object is an enumerable collection. A list, array, or similar.

                        // Get the underlying type
                        var underlyingType = type.GetGenericArguments()[0];

                        int? i = 0;
                        if (value != null)
                        {
                            foreach (object item in (IEnumerable)value)
                            {
                                // Handle filled collections
                                Logging.Debug("Handling element " + i);
                                PrepareEventVariables(name + " " + i, underlyingType, ref setVars, false, item);
                                i++;
                            }
                        }
                        if (i == 0)
                        {
                            // Handle empty collections (for when we're generating wiki documentation)
                            PrepareEventVariables(name + " n", underlyingType, ref setVars, false, null);
                            // Set i to null so that no value is written to the wiki documentation when i is zero
                            i = null;
                        }
                        setVars.Add(new VoiceAttackVariable(name + " entries", typeof(int), i, isTopLevel));
                    }
                    else if ((type.IsClass || type.IsInterface) && !type.IsGenericType)
                    {
                        Logging.Debug($"Found object '{type.Name}'");
                        PrepareEventVariables(name, type, ref setVars, false, value);
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected type '{type.FullName}' cannot be set as a VoiceAttack variable.");
                    }
                }
            }
            catch (Exception ex)
            {
                var data = new Dictionary<string, object>()
                    {
                        { "Prefix", prefix },
                        { "Key", key },
                        { "Type", type },
                        { "Value", value },
                        { "Exception", ex }
                    };
                Logging.Error($"Failed to prepare VoiceAttack value.", data);
            }
        }

        private static string AddSpacesToTitleCasedName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ' && !char.IsUpper(text[i - 1]))
                {
                    newText.Append(' ');
                }
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        public static void SetEventVariables(dynamic vaProxy, List<VoiceAttackVariable> variables)
        {
            foreach (var variable in variables)
            {
                try
                {
                    if (variable.Type is null)
                    {
                        // No idea what it might have been so reset everything
                        Logging.Debug($"'{variable.Key}' type is null; Unset all possible values");
                        vaProxy.SetText(variable.Key, null);
                        vaProxy.SetInt(variable.Key, null);
                        vaProxy.SetDecimal(variable.Key, null);
                        vaProxy.SetBoolean(variable.Key, null);
                        vaProxy.SetDate(variable.Key, null);
                    }
                    else if (variable.Type == typeof(string))
                    {
                        Logging.Debug($"Setting string value '{variable.Key}' to: {(string)variable.Value}");
                        vaProxy.SetText(variable.Key, (string)variable.Value);
                    }
                    else if (variable.Type == typeof(int))
                    {
                        Logging.Debug($"Setting integer value '{variable.Key}' to: {(int?)variable.Value}");
                        vaProxy.SetInt(variable.Key, (int?)variable.Value);
                    }
                    else if (variable.Type == typeof(bool))
                    {
                        Logging.Debug($"Setting boolean value '{variable.Key}' to: {(bool?)variable.Value}");
                        vaProxy.SetBoolean(variable.Key, (bool?)variable.Value);
                    }
                    else if (variable.Type == typeof(decimal))
                    {
                        Logging.Debug($"Setting decimal value '{variable.Key}' to: {(decimal?)variable.Value}");
                        vaProxy.SetDecimal(variable.Key, (decimal?)variable.Value);
                    }
                    else if (variable.Type == typeof(DateTime))
                    {
                        Logging.Debug($"Setting date value '{variable.Key} to {(DateTime?)variable.Value}");
                        vaProxy.SetDate(variable.Key, (DateTime?)variable.Value);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid type");
                    }
                }
                catch (Exception ex)
                {
                    var data = new Dictionary<string, object>()
                    {
                        { "Value", variable },
                        { "Exception", ex }
                    };
                    Logging.Error($"Failed to write VoiceAttack value for key '{variable.Key}'", data);
                }
            }
        }
    }

    public class VoiceAttackVariable
    {
        /// <summary> The full key used to access the variable in VoiceAttack, including any applicable prefix </summary>
        public string Key { get; private set; }

        /// <summary> One of "string", "int", "bool", "decimal", "double", "long", or "DateTime" </summary>
        public Type Type { get; private set; }

        /// <summary> The value to write (if any) </summary>
        public object Value { get; set; }

        /// <summary> Whether this is a top level variable which might have a description </summary>
        public bool IsTopLevel { get; private set; }

        public VoiceAttackVariable(string key, Type type, object value = null, bool isTopLevel = true)
        {
            this.Key = key;
            this.Type = type;
            this.Value = value;
            this.IsTopLevel = isTopLevel;
        }
    }
}