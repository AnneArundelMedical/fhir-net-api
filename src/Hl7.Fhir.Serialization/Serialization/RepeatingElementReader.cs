﻿using Hl7.Fhir.Introspection;
using Hl7.Fhir.Properties;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Hl7.Fhir.Serialization
{
    public class RepeatingElementReader
    {
        private IFhirReader _current;
        private ModelInspector _inspector;

        public RepeatingElementReader(IFhirReader reader)
        {
            _current = reader;
            _inspector = SerializationConfig.Inspector;
        }

        public object Deserialize(PropertyMapping prop, string memberName, object existing=null)
        {
            if (prop == null) throw Error.ArgumentNull("prop");

            bool overwriteMode;
            IEnumerable<IFhirReader> elements;

            if(_current.CurrentToken == TokenType.Array)        // Json has members that are arrays, if we encounter multiple, update the old values of the array
            {
                overwriteMode = existing != null;
                elements = _current.GetArrayElements();
            }
            else if(_current.CurrentToken == TokenType.Object)  // Xml has repeating members, if we encounter multiple, add them to the array
            {
                overwriteMode = false;
                elements = new List<IFhirReader>() { _current };
            }
            else
                throw Error.InvalidOperation("Expecting to be either at a repeating complex element or an array when parsing a repeating member.");

            IList result;

            if (existing == null) existing = ReflectionHelper.CreateGenericList(prop.ElementType);

            result = existing as IList;                       
            if(result == null) throw Error.Argument("existing", "Can only read repeating elements into a type implementing IList");

            var position = 0;
            foreach(var element in elements)
            {
                var reader = new DispatchingReader(element, arrayMode: true);

                if(overwriteMode)
                {
                    if (position >= result.Count)
                        throw Error.InvalidOperation("The value and extension array are not well-aligned");

                    // Arrays may contain null values as placeholders
                    if(element.CurrentToken != TokenType.Null)
                        result[position] = reader.Deserialize(prop, memberName, existing: result[position]);
                }
                else
                {                   
                    object item = null;
                    if (element.CurrentToken != TokenType.Null)
                        item = reader.Deserialize(prop, memberName);
                    else
                        item = null;  // Arrays may contain null values as placeholders
                    
                    result.Add(item);
                }

                position++;
            }

            return result;
        }
    }
}