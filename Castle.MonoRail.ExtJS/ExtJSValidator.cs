#region License
// Copyright (c) 2007, Ricardo Stuven.
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

namespace Castle.MonoRail.Framework.Helpers.ValidationStrategy
{
	using System;
	using System.Collections;
	using Castle.Components.Validator;
	using Newtonsoft.Json;

	public class ExtJSValidator : IBrowserValidatorProvider
	{
		#region IBrowserValidatorProvider Members

		public BrowserValidationConfiguration CreateConfiguration(IDictionary parameters)
		{
			BrowserValidationConfiguration config = new ExtJSValidationConfiguration();
			config.Configure(parameters);
			return config;
		}

		public IBrowserValidationGenerator CreateGenerator(BrowserValidationConfiguration config, InputElementType inputType, IDictionary attributes)
		{
			return new ExtJSValidationGenerator((ExtJSValidationConfiguration)config, inputType, attributes);
		}

		#endregion
	}

	public class ExtJSValidationConfiguration : BrowserValidationConfiguration
	{
		private IDictionary parameters;
		private String currentFormId;

		public IDictionary Parameters
		{
			get { return this.parameters; }
		}

		public String CurrentFormId
		{
			get { return this.currentFormId; }
		}

		public override void Configure(IDictionary parameters)
		{
			this.parameters = parameters;
		}

		public override string CreateAfterFormOpened(string formId)
		{
			this.currentFormId = formId;
			return null;
		}

		public override string CreateBeforeFormClosed(string formId)
		{
//            String script = String.Format(@"
//Ext.getCmp({0}).on('clientvalidation', function(valid) {
//	var submit = Ext.getCmp({1});
//	submit.
//});
//"
//, JavaScriptConvert.ToString(formId)
//, JavaScriptConvert.ToString(formId + "-submit-button")
//);
//            return script;
			return null;
		}

	}

	public class ExtJSValidationGenerator : IBrowserValidationGenerator
	{
		private readonly ExtJSValidationConfiguration config; 
		private readonly InputElementType inputType;
		private readonly IDictionary attributes;

		public ExtJSValidationGenerator(ExtJSValidationConfiguration config, InputElementType inputType, IDictionary attributes)
		{
			this.config = config;
			this.inputType = inputType;
			this.attributes = attributes;
		}

		#region IBrowserValidationGenerator Members

		public void SetAsRequired(string target, string violationMessage)
		{
			this.attributes["allowBlank"] = false;
			this.attributes["blankText"] = violationMessage;
		}

		public void SetAsSameAs(string target, string comparisonFieldName, string violationMessage)
		{
			SetAsComparedAs(target, "==", comparisonFieldName, violationMessage);
		}

		public void SetAsNotSameAs(string target, string comparisonFieldName, string violationMessage)
		{
			SetAsComparedAs(target, "!=", comparisonFieldName, violationMessage);
		}

		private void SetAsComparedAs(string target, String comparisonOperator, string comparisonFieldName, string violationMessage)
		{
			String[] parts = target.Split('.');
			comparisonFieldName = String.Join(".", parts, 0, parts.Length - 1)
				+ "." + comparisonFieldName;

			String validator = String.Format(@"
function(value) {{
	var otherField = Ext.getCmp({0}).form.findField({1});
	if (!otherField) return false;
	return (value {2} otherField.getValue()) || {3};
}}"
, JavaScriptConvert.ToString(this.config.CurrentFormId)
, JavaScriptConvert.ToString(comparisonFieldName)
, comparisonOperator
, JavaScriptConvert.ToString(violationMessage)
);
			SetValidator(validator);
		}

		public void SetDate(string target, string violationMessage)
		{
			// Use a Ext.form.DateField
		}

		public void SetDigitsOnly(string target, string violationMessage)
		{
			// Use a Ext.form.NumberField
		}

		public void SetEmail(string target, string violationMessage)
		{
			this.attributes["vtype"] = "email";
			this.attributes["vtypeText"] = violationMessage;
		}

		public void SetExactLength(string target, int length, string violationMessage)
		{
			SetMinLength(target, length, violationMessage);
			SetMaxLength(target, length, violationMessage);
		}

		public void SetExactLength(string target, int length)
		{
			SetMinLength(target, length);
			SetMaxLength(target, length);
		}

		public void SetLengthRange(string target, int minLength, int maxLength, string violationMessage)
		{
			SetMinLength(target, minLength, violationMessage);
			SetMaxLength(target, maxLength, violationMessage);
		}

		public void SetLengthRange(string target, int minLength, int maxLength)
		{
			SetMinLength(target, minLength);
			SetMaxLength(target, maxLength);
		}

		public void SetMaxLength(string target, int maxLength, string violationMessage)
		{
			this.attributes["maxLength"] = maxLength;
			this.attributes["maxLengthText"] = violationMessage;
		}

		public void SetMaxLength(string target, int maxLength)
		{
			this.attributes["maxLength"] = maxLength;
		}

		public void SetMinLength(string target, int minLength, string violationMessage)
		{
			this.attributes["minLength"] = minLength;
			this.attributes["minLengthText"] = violationMessage;
		}

		public void SetMinLength(string target, int minLength)
		{
			this.attributes["minLength"] = minLength;
		}

		public void SetNumberOnly(string target, string violationMessage)
		{
			// Use a Ext.form.NumberField
		}

		public void SetRegExp(string target, string regExp, string violationMessage)
		{
			this.attributes["regex"] = regExp;
			this.attributes["regexText"] = violationMessage;
		}

		public void SetValueRange(string target, string minValue, string maxValue, string violationMessage)
		{
			SetValueRange(minValue, maxValue, violationMessage);
		}

		public void SetValueRange(string target, DateTime minValue, DateTime maxValue, string violationMessage)
		{
			SetValueRange(minValue, maxValue, violationMessage);
		}

		public void SetValueRange(string target, decimal minValue, decimal maxValue, string violationMessage)
		{
			SetValueRange(minValue, maxValue, violationMessage);
		}

		public void SetValueRange(string target, int minValue, int maxValue, string violationMessage)
		{
			SetValueRange(minValue, maxValue, violationMessage);
		}

		#endregion

		private void SetValueRange(Object minValue, Object maxValue, string violationMessage)
		{
			String validator = String.Format(@"
function(value) {{
	return (value >= {0} && value <= {1}) || {2};
}}"
, JavaScriptConvert.ToString(minValue)
, JavaScriptConvert.ToString(maxValue)
, JavaScriptConvert.ToString(violationMessage)
);
			SetValidator(validator);
		}

		private void SetValidator(String validator)
		{
			// Remove some characters to keep the script in one line
			// (and easily comment the line that uses ExtJSHelper)
			validator = validator.Replace("\n", "").Replace("\r", "");

			this.attributes["validator"] = new JavaScriptLiteral(validator);
		}
	}
}
