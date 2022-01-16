using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Utilities;

namespace EddiSpeechResponder.AvalonEdit
{
    internal class TextCompletion
    {
        public TextCompletion(Type reflectionObjectType, object reflectionObject = null)
        {
            throw new NotImplementedException("TODO: Generate `TextCompletionItem` values via reflection");

            if (reflectionObjectType is null)
            {
                Results = new List<TextCompletionItem>();
            }
            else
            {
                Results = GetVariables(reflectionObjectType, reflectionObject);
            }
        }

        public List<TextCompletionItem> Results { get; private set; }

        // Some types don't need to be decomposed further - we'll stop reflecting when we hit these types
        private static readonly Type[] undecomposedTypes = { typeof(string), typeof(DateTime), typeof(TimeSpan) };

        /// <summary> Walk an object and write out all of the possible fields </summary>
        /// <param name="reflectionObjectType">The Type property of the object that we're walking, specified independent from the actual object in case the actual object value is null</param>
        /// <param name="reflectionObject">(Optional) The object that we're walking to obtain values. At the top level, this should be an `Event` class object</param>
        /// <param name="keysPath">(Used internally, do not set) The path to the specific key</param>
        private List<TextCompletionItem> GetVariables(Type reflectionObjectType, object reflectionObject = null, List<string> keysPath = null)
        {
            if (keysPath is null) { keysPath = new List<string>(); }
            if (Results is null) { Results = new List<TextCompletionItem>(); }

            // Some types don't need to be decomposed further.
            if (undecomposedTypes.Contains(reflectionObjectType))
            {
//                GetVariable(keysPath.Copy(), "", reflectionObjectType, string.Empty, reflectionObject);
                return Results;
            }

            var objectProperties = reflectionObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var objectFields = reflectionObjectType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var eventProperty in objectProperties)
            {
                // We ignore some keys which we've marked in advance
                bool passProperty = false;
                foreach (var attribute in eventProperty.GetCustomAttributes())
                {
                    if (attribute is PublicAPIAttribute publicAPIAttribute)
                    {
                        passProperty = true;
//                        GetVariable(keysPath.Copy(), eventProperty.Name, eventProperty.PropertyType, publicAPIAttribute.Description, eventProperty.CanRead && reflectionObject != null ? eventProperty.GetValue(reflectionObject) : null);
                        break;
                    }
                }
                if (!passProperty)
                {
                    Logging.Debug("Ignoring key " + eventProperty.Name);
                }
            }

            foreach (var eventField in objectFields)
            {
                // We ignore some keys which we've marked in advance
                bool passField = false;
                foreach (var attribute in eventField.GetCustomAttributes())
                {
                    if (attribute is PublicAPIAttribute publicAPIAttribute)
                    {
                        passField = true;
//                        GetVariable(keysPath.Copy(), eventField.Name, eventField.FieldType, publicAPIAttribute.Description, reflectionObject != null ? eventField.GetValue(reflectionObject) : null);
                        break;
                    }
                }
                if (!passField)
                {
                    Logging.Debug("Ignoring key " + eventField.Name);
                }
            }

            return Results;
        }
    }
}
