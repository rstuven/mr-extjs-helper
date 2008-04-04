#region License
// Copyright 2007 Ricardo Stuven.
// Copyright 2004-2007 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

namespace Castle.MonoRail.Framework
{
	using System;
	using System.Collections;
	using System.IO;
	using System.Text;
	using Newtonsoft.Json;

	internal static class JavaScriptUtils
	{
		/// <summary>
		/// 
		/// </summary>
		private static readonly JsonConverter[] attributeJsonConverters =
			new JsonConverter[] { 
				new JavaScriptLiteralConverter() 
			};

		/// <summary>
		/// Serializes a value or object to JavaScript
		/// </summary>
		/// <param name="attribute"></param>
		/// <returns>A value or object serialized to JavaScript.</returns>

		public static String Serialize(Object value)
		{
			if (JavaScriptUtils.HasToStringConversion(value))
			{
				return JavaScriptConvert.ToString(value);
			}
			else
			{
				Type type = value.GetType();
				JsonConverter foundConverter =
					Array.Find<JsonConverter>(attributeJsonConverters,
						delegate(JsonConverter converter)
						{
							return converter.CanConvert(type);
						});

				if (foundConverter == null)
				{
					return JavaScriptConvert.SerializeObject(value, attributeJsonConverters);
				}
				else
				{
					StringBuilder sb = new StringBuilder();
					using (StringWriter sw = new StringWriter(sb))
					{
						using (JsonWriter jw = new JsonWriter(sw))
						{
							jw.WriteStartArray();
							foundConverter.WriteJson(jw, value);
							jw.WriteEndArray();
						}
					}
					sb.Remove(0, 1);
					sb.Remove(sb.Length - 1, 1);
					return sb.ToString();
				}
			}
		}

		public static Boolean HasBuiltinConversion(Object value)
		{
			IConvertible convertible = value as IConvertible;

			if (convertible == null)
			{
				return (value is IList ||
						value is IDictionary ||
						value is ICollection ||
						value is Identifier);
			}
			else
			{
				return HasToStringConversion(value);
			}
		}

		public static Boolean HasToStringConversion(Object value)
		{
			IConvertible convertible = value as IConvertible;

			if (convertible != null)
			{
				switch (convertible.GetTypeCode())
				{
					case TypeCode.Boolean:
					case TypeCode.Char:
					case TypeCode.SByte:
					case TypeCode.Byte:
					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Int32:
					case TypeCode.UInt32:
					case TypeCode.Int64:
					case TypeCode.UInt64:
					case TypeCode.Single:
					case TypeCode.Double:
					case TypeCode.Decimal:
					case TypeCode.DateTime:
					case TypeCode.String:
						return true;
				}
			}
			return false;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <example>
		/// Suppose we have an object value such as 
		/// <c>JavaScriptUtils.Serialze(value)</c> returns:
		/// <code>
		/// {"Name":"Bob","Age":21,"Address":{"Street":"123 Broadway Ave.","State": "NY"}}
		/// </code>
		/// If we call
		/// <c>FlattenObjectByKey(parameters, "contact", value)</c>,
		/// then we get the following "flattened" form:
		/// <code>
		/// {"contact.Name":"Bob","contact.Age":21,"contact.Address.Street":"123 Broadway Ave.","contact.Address.State":"NY"}
		/// </code>
		/// </example>
		/// <param name="parameters"></param>
		/// <param name="rootKey"></param>
		/// <param name="value"></param>
		public static void FlattenObjectByKey(JavaScriptObject parameters, String rootKey, Object value)
		{
			if (JavaScriptUtils.HasBuiltinConversion(value))
			{
				parameters[rootKey] = value;
			}
			else
			{
				JavaScriptObject jso =
					JavaScriptConvert.DeserializeObject<JavaScriptObject>(
						JavaScriptConvert.SerializeObject(value));

				FlattenObjectByKey(parameters, rootKey, jso);
			}
		}

		private static void FlattenObjectByKey(JavaScriptObject parameters, String rootKey, JavaScriptObject jso)
		{
			rootKey = rootKey + ".";
			foreach (String key in jso.Keys)
			{
				Object value = jso[key];
				FlattenObjectItemByKey(parameters, rootKey + key, value);
			}
		}

		private static void FlattenObjectItemByKey(JavaScriptObject parameters, String key, Object value)
		{
			if (value is JavaScriptObject)
			{
				FlattenObjectByKey(parameters, key, (JavaScriptObject)value);
			}
			else if (value is JavaScriptArray)
			{
				int index = 0;
				(value as JavaScriptArray).ForEach(delegate(Object item)
				{
					FlattenObjectItemByKey(parameters, key + "." + (index++), item);
				});
			}
			else
			{
				parameters[key] = value;
			}
		}

	
	}

	/// <summary>
	/// Writes and reads raw values for "non-pure" JSON serialization.
	/// </summary>
	/// <remarks>
	/// This can be useful to serialize function declarations:
	/// <example>
	/// <code>
	///		JavaScriptObject button = new JavaScriptObject();
	///		button["text"] = "Click me";
	///		button["handler"] = new JavaScriptLiteral("function(){alet('hi!');}");
	///		String json = JavaScriptConvert.SerializeObject(button, 
	///			new JsonConverter[]{new JavaScriptLiteralConverter()});
	///		Console.Write(json);
	/// </code>
	/// Output:
	/// <code>
	/// {"text":"Click me","handler":function(){alet('hi!');}}
	/// </code>
	/// </example>
	/// </remarks>
	internal class JavaScriptLiteralConverter : JsonConverter
	{
		public override Boolean CanConvert(Type objectType)
		{
			return objectType == typeof(JavaScriptLiteral);
		}

		public override void WriteJson(JsonWriter writer, object value)
		{
			writer.WriteRaw(value.ToString());
		}

		public override object ReadJson(JsonReader reader, Type objectType)
		{
			return new JavaScriptLiteral(base.ReadJson(reader, objectType).ToString());
		}
	}

	/// <summary>
	/// Stores a raw value to be serialized by <see cref="JavaScriptLiteralConverter"/>.
	/// </summary>
	/// <remarks>
	/// Implementation note:
	/// Later I found I could be simple have used <c>Newtonsoft.Json.Identifier</c> 
	/// to get the same results without implementing a converter either,
	/// but the meanings are so different that I decided not to confuse the terms
	/// and explicitly mean you can use this for any JavaScript literal
	/// (eg. function declarations or object creation), not just identifiers.
	/// </remarks>
	public class JavaScriptLiteral
	{
		private readonly String literal;
		public JavaScriptLiteral(String literal)
		{
			this.literal = literal;

		}

		public override String ToString()
		{
			return this.literal;
		}
	}

}
