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

namespace Castle.MonoRail.Framework.Helpers
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Reflection;
	using System.Text;
	using Castle.Components.Validator;
	using Castle.MonoRail.Framework.Helpers.ValidationStrategy;
	using Castle.MonoRail.Framework.Services.AjaxProxyGenerator;
	using Newtonsoft.Json;

	/// <summary>
	/// Provides utility methods to work with
	/// <see href="http://extjs.com">ExtJS 2.0 library</see>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// As <see cref="FormHelper"/>, this class mainly takes care of data binding 
	/// and validation at the broswer side based on server side declarations.
	/// </para>
	/// <para>
	/// But also there are methods that provide 
	/// a foundation for advanced applications.
	/// For example, the <see cref="Container"/> method in combination with 
	/// <c>Ext.ux.MonoRail.Container.js</c> script.
	/// </para>
	/// </remarks>
	/// 
	/// <example>
	/// <para>
	/// Let's start from an ExtJS-only sample code 
	/// that is not enjoying MonoRail databinding nor validation logic:
	/// <code>
	///		var f = new Ext.FormPanel({
	///			id:"contact-form",
	///			url: '/Contact/Save.castle',
	///			items:[
	///				new Ext.form.TextField({
	///					name: "contact.FullName",
	///					fieldLabel: "Full Name"
	///				})
	///			]
	///		});
	/// </code>
	/// </para>
	/// 
	/// <para>
	/// Adding ExtJSHelper the code looks like this (on Brail view engine):
	/// <code>
	///		${ExtJSHelper.BeginForm({@id:"contact-form"})}
	/// 
	///		var f = new Ext.FormPanel({
	///			id:"contact-form",
	///			url: '${Url.For({@action: "Save"})}',
	///			items:[
	///				${ExtJSHelper.TextField("contact.FullName", {
	///					@fieldLabel: "Full Name"
	///				})}
	///			]
	///		});
	///
	///		${ExtJSHelper.EndForm()}
	/// </code>
	/// </para>
	/// 
	/// Even we can make it fully configured by ExtJSHelper
	/// (note how Brail hash notation is everywhere):
	/// <code>
	///		${ExtJSHelper.BeginForm({@id:"contact-form"})}
	/// 
	///		${ExtJSHelper.FormPanel({
	///			@assignTo: "f",
	///			@url: Url.For({@action: "Save"}),
	///			@items:[
	///				ExtJSHelper.TextField("contact.FullName", {
	///					@fieldLabel: "Full Name"
	///				})}
	///			]
	///		})}
	///
	///		${ExtJSHelper.EndForm()}
	/// </code>
	/// 
	/// Also you can configure some fields after they are rendered.
	/// For example:
	/// <code>
	///		var f = new Ext.FormPanel({
	///			id:"contact-form",
	///			url: '${Url.For({@action: "Save"})}',
	///			items:[
	///				new Ext.form.TextField({
	///					name: "contact.FullName",
	///					fieldLabel: "Full Name"
	///				})
	///			]
	///		});
	///		f.render("container");
	/// 
	///		// Some code here...
	/// 
	///		${ExtJSHelper.BeginForm({@id:"contact-form"})}
	///		${ExtJSHelper.ApplyToField("contact.FullName")}
	///		${ExtJSHelper.EndForm()}
	/// </code>
	/// </example>
	/// <seealso href="http://extjs.com">ExtJS website</seealso>.
	public class ExtJSHelper : AbstractHelper //, IServiceEnabledComponent
	{
		/// <summary>
		/// This class just exposes some protected or private methods of FormHelper 
		/// </summary>
		private class MyFormHelper : FormHelper
		{
			public IValidator[] CollectValidators(RequestContext requestContext, String target)
			{
				List<IValidator> validators = new List<IValidator>();

				ObtainTargetProperty(requestContext, target, delegate(PropertyInfo property)
				{
					validators.AddRange(Controller.Validator.GetValidators(property.DeclaringType, property));
				});

				return validators.ToArray();
			}

			new public object ObtainValue(String target)
			{
				return base.ObtainValue(target);
			}

			/// <summary>
			/// Rewrites the target if within object scope.
			/// </summary>
			/// <param name="target">The target.</param>
			/// <returns></returns>
			new public String RewriteTargetIfWithinObjectScope(String target)
			{
				return base.RewriteTargetIfWithinObjectScope(target);
			}
		}

		private IBrowserValidatorProvider validatorProvider = new ExtJSValidator();
		private BrowserValidationConfiguration validationConfig;
		private int formCount;
		private String currentFormId;
		private Boolean isValidationDisabled;
		private String focusedFieldName;
		private readonly MyFormHelper myFormHelper = new MyFormHelper();

		public override void SetController(Controller controller)
		{
			base.SetController(controller);
			myFormHelper.SetController(controller);
			myFormHelper.UseWebValidatorProvider(validatorProvider);
		}

		#region Form related

		public String BeginForm()
		{
			return BeginForm(null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// <para>
		/// Use it always in cobination with <see cref="EndForm"/>
		/// </para>
		/// <para>
		/// If <c>id</c> config option is not provided, 
		/// a value is automatically generated.
		/// </para>
		/// </remarks>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public String BeginForm(IDictionary parameters)
		{
			currentFormId = CommonUtils.ObtainEntryAndRemove(parameters, "id", 
				this.ContainerId + "-form" + ++formCount);

			validationConfig = validatorProvider.CreateConfiguration(parameters);

			String afterBeginForm = IsValidationEnabled ?
				validationConfig.CreateAfterFormOpened(currentFormId) :
				String.Empty;

			String formContent = String.Empty;

			//formContent = ???

			return formContent + afterBeginForm;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// <para>
		/// Use it always in cobination with <see cref="BeginForm"/>
		/// </para>
		/// </remarks>
		/// <returns></returns>
		public String EndForm()
		{
			String beforeEndForm = String.Empty;

			if (validationConfig != null)
			{
				beforeEndForm = IsValidationEnabled ?
					validationConfig.CreateBeforeFormClosed(currentFormId) :
					String.Empty;
			}

			if (!String.IsNullOrEmpty(focusedFieldName))
			{
				//currentForm.form.findField("contact.Name").focus();
				beforeEndForm += String.Format(
					"Ext.getCmp({0}).form.findField({1}).focus();",
					JavaScriptUtils.Serialize(currentFormId),
					JavaScriptUtils.Serialize(focusedFieldName));
			}

			currentFormId = null;
			validationConfig = null;
			focusedFieldName = null;

			return beforeEndForm;
		}

		#endregion

		#region Validation

		private Boolean IsValidationEnabled
		{
			get
			{
				if (isValidationDisabled) return false;

				//if (objectStack.Count == 0) return true;

				//return ((FormScopeInfo)objectStack.Peek()).IsValidationEnabled;

				return true;
			}
		}

		/// <summary>
		/// Disables the browser side validation.
		/// </summary>
		public void DisableValidation()
		{
			isValidationDisabled = true;
		}

		/// <summary>
		/// Applies the browser side validation.
		/// </summary>
		/// <param name="inputType"></param>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		protected virtual void ApplyValidation(InputElementType inputType, String target, ref IDictionary attributes)
		{
			Boolean disableValidation = CommonUtils.ObtainEntryAndRemove(attributes, "disableValidation", "false") == "true";

			// TODO: Is a MonoRail bug?
//			if (!IsValidationEnabled && disableValidation)
			if (!IsValidationEnabled || disableValidation)
			{
				return;
			}

			if (Controller.Validator == null || validationConfig == null)
			{
				return;
			}

			if (attributes == null)
			{
				attributes = new HybridDictionary(true);
			}

			IValidator[] validators = myFormHelper.CollectValidators(RequestContext.All, target);

			IBrowserValidationGenerator generator = validatorProvider.CreateGenerator(validationConfig, inputType, attributes);

			foreach (IValidator validator in validators)
			{
				if (validator.SupportsBrowserValidation)
				{
					validator.ApplyBrowserValidation(validationConfig, inputType, generator, attributes, target);
				}
			}
		}

		#endregion

		#region Object scope related

		/// <summary>
		/// Pushes the specified target. Experimental.
		/// </summary>
		/// <param name="target">The target.</param>
		public void Push(String target)
		{
			myFormHelper.Push(target);
		}

		/// <summary>
		/// Pushes the specified target. Experimental.
		/// </summary>
		/// <param name="target">The target.</param>
		/// <param name="parameters">The parameters.</param>
		public void Push(String target, IDictionary parameters)
		{
			myFormHelper.Push(target, parameters);
		}

		/// <summary>
		/// Pops this instance. Experimental.
		/// </summary>
		public void Pop()
		{
			myFormHelper.Pop();
		}

		#endregion

		#region Field

		/// <summary>
		/// Apply configurations to field.
		/// </summary>
		/// <remarks>
		/// <para>
		/// BeginForm method must be called before applying field configurations.
		/// </para>
		/// <para>
		/// The config options <c>name</c> and <c>xtype</c> are not allowed
		/// since we are applying configurations over an existing field.
		/// Other config options, including <c>value</c>, are allowed.
		/// </para>
		/// </remarks>
		/// <exception cref="FormIdNotAvailableException"></exception>
		/// <param name="target"></param>
		/// <returns>A script that configures the field.</returns>
		public String ApplyToField(String target)
		{
			if (String.IsNullOrEmpty(currentFormId))
			{
				throw new FormIdNotAvailableException();
			}
			target = myFormHelper.RewriteTargetIfWithinObjectScope(target);

			IDictionary attributes = new HybridDictionary(true);

			ApplyValidation(InputElementType.Text, target, ref attributes);

			Object value = myFormHelper.ObtainValue(target);
			if (value != null)
			{
				attributes["value"] = value;
			}

			if (attributes.Count == 0)
			{
				return String.Empty;
			}

			// Generate an anonymous function to configure the field
			StringBuilder sb = new StringBuilder();

			sb.Append("(function(){");
			sb.AppendFormat("var f = Ext.getCmp({0}).form.findField({1});"
				, JavaScriptConvert.ToString(currentFormId)
				, JavaScriptConvert.ToString(target)
				);

			foreach (String key in attributes.Keys)
			{
				if (key == "value")
				{
					sb.AppendFormat("f.setValue({0});"
						, JavaScriptUtils.Serialize(attributes[key])
						);
				}
				else
				{
					sb.AppendFormat("f.{0}={1};"
						, key
						, JavaScriptUtils.Serialize(attributes[key])
						);
				}
			}
			sb.Append("})();");

			return sb.ToString();
		}

		/// <summary>
		/// Generates an <c>Ext.form.Field</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <param name="xtype"></param>
		/// <returns>A <see cref="JavaScriptLiteral"/> object.</returns>
		private JavaScriptLiteral Field(String target, IDictionary attributes, String xtype)
		{
			if (attributes == null)
				attributes = new HybridDictionary(true);
			attributes["xtype"] = xtype;
			return Field(target, attributes);
		}

		/// <summary>
		/// Generates an <c>Ext.form.Field</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <param name="xtype"></param>
		/// <returns>An Ext config object.</returns>
		private IDictionary FieldConfig(String target, IDictionary attributes, String xtype)
		{
			if (attributes == null)
				attributes = new HybridDictionary(true);
			attributes["xtype"] = xtype;
			return FieldConfig(target, attributes);
		}

		/// <summary>
		/// Generates an <c>Ext.form.Field</c> config object.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Use the same config options documented for a given 
		/// <see href="http://extjs.com/deploy/dev/docs/output/Ext.form.Field.html">Ext.form.Field</see>
		/// or derived component.
		/// </para>
		/// <para>
		/// The <c>name</c> config option will be automatically set 
		/// according to the <paramref name="target"/> argument.
		/// The final value of <c>name</c> could be different from 
		/// <paramref name="target"/>.
		/// </para>
		/// <para>
		/// The <c>value</c> config option will be automatically loaded
		/// from the <paramref name="target"/> value in the request scope,
		/// but can be overriden by a <c>value</c> set in <paramref name="config"/>.
		/// </para>
		/// <para>
		/// In addition, this method provides extra config options:
		/// <list type="table">
		/// <listheader>
		///		<term>Config option</term>
		///		<description>Description</description>
		/// </listheader>
		/// <item>
		///		<term><c>disableValidation</c></term>
		///		<description>(Boolean) Avoids the generation of 
		///		browser side validation. Defaults to <c>false</c></description>
		/// </item>
		/// <item>
		///		<term><c>focused</c></term>
		///		<description>(Boolean) Calls the <c>Ext.Field#focus</c> method
		///		on <see cref="EndForm"/> script.
		///		Defaults to <c>false</c></description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <exception cref="FormIdNotAvailableException"></exception>
		/// <param name="target"></param>
		/// <param name="config">A dictionary of
		/// <see href="http://extjs.com/deploy/dev/docs/output/Ext.form.Field.html">Ext.form.Field</see>
		/// config options</param>
		/// <returns>A <see cref="JavaScriptLiteral"/> object.</returns>
		/// <seealso href="http://extjs.com/deploy/dev/docs/output/Ext.form.Field.html">Ext.form.Field</seealso>
		public JavaScriptLiteral Field(String target, IDictionary config)
		{
			return Literal(Config(FieldConfig(target, config)));
		}

		/// <summary>
		/// Generates an <c>Ext.form.Field</c> config object.
		/// </summary>
		/// <exception cref="FormIdNotAvailableException"></exception>
		/// <param name="target"></param>
		/// <param name="config"></param>
		/// <returns>An Ext config object.</returns>
		public IDictionary FieldConfig(String target, IDictionary config)
		{
			if (String.IsNullOrEmpty(currentFormId))
			{
				throw new FormIdNotAvailableException();
			}
			target = myFormHelper.RewriteTargetIfWithinObjectScope(target);

			Boolean focused = CommonUtils.ObtainEntryAndRemove(config, "focused", "false").ToLower() == "true";
			if (focused)
			{
				focusedFieldName = target;
			}

			ApplyValidation(InputElementType.Text, target, ref config);

			if (!config.Contains("value"))
			{
				Object value = myFormHelper.ObtainValue(target);
				if (value != null)
				{
					config["value"] = value;
				}
			}

			config["name"] = target;

			return config;
		}

		#endregion

		#region TextField

		/// <summary>
		/// Generates an <c>Ext.form.TextField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral TextField(String target)
		{
			return TextField(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.TextField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public IDictionary TextFieldNode(String target)
		{
			return TextFieldNode(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.TextField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral TextField(String target, IDictionary attributes)
		{
			return Field(target, attributes, "textfield");
		}

		/// <summary>
		/// Generates an <c>Ext.form.TextField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object.</returns>
		public IDictionary TextFieldNode(String target, IDictionary attributes)
		{
			return FieldConfig(target, attributes, "textfield");
		}

		#endregion

		#region Checkbox

		/// <summary>
		/// Generates an <c>Ext.form.Checkbox</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral Checkbox(String target)
		{
			return Checkbox(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.Checkbox</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral Checkbox(String target, IDictionary attributes)
		{
			return Field(target, attributes, "checkbox");
		}

		#endregion

		#region DateField

		/// <summary>
		/// Generates an <c>Ext.form.DateField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral DateField(String target)
		{
			return DateField(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.DateField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral DateField(String target, IDictionary attributes)
		{
			return Field(target, attributes, "datefield");
		}

		#endregion

		#region Hidden

		/// <summary>
		/// Generates an <c>Ext.form.Hidden</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral Hidden(String target)
		{
			return Hidden(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.Hidden</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral Hidden(String target, IDictionary attributes)
		{
			return Field(target, attributes, "hidden");
		}

		#endregion

		#region HtmlEditor

		/// <summary>
		/// Generates an <c>Ext.form.HtmlEditor</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral HtmlEditor(String target)
		{
			return HtmlEditor(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.HtmlEditor</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral HtmlEditor(String target, IDictionary attributes)
		{
			return Field(target, attributes, "htmleditor");
		}

		#endregion

		#region NumberField

		/// <summary>
		/// Generates an <c>Ext.form.NumberField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral NumberField(String target)
		{
			return NumberField(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.NumberField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral NumberField(String target, IDictionary attributes)
		{
			return Field(target, attributes, "numberfield");
		}

		#endregion

		#region Radio

		/// <summary>
		/// Generates an <c>Ext.form.Radio</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral Radio(String target)
		{
			return Radio(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.Radio</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral Radio(String target, IDictionary attributes)
		{
			return Field(target, attributes, "radio");
		}

		#endregion

		#region TextArea

		/// <summary>
		/// Generates an <c>Ext.form.TextArea</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral TextArea(String target)
		{
			return TextArea(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.TextArea</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public IDictionary TextAreaNode(String target)
		{
			return TextAreaNode(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.TextArea</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral TextArea(String target, IDictionary attributes)
		{
			return Field(target, attributes, "textarea");
		}

		/// <summary>
		/// Generates an <c>Ext.form.TextArea</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object.</returns>
		public IDictionary TextAreaNode(String target, IDictionary attributes)
		{
			return FieldConfig(target, attributes, "textarea");
		}

		#endregion

		#region TimeField

		/// <summary>
		/// Generates an <c>Ext.form.TimeField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral TimeField(String target)
		{
			return TimeField(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.TimeField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral TimeField(String target, IDictionary attributes)
		{
			return Field(target, attributes, "timefield");
		}

		#endregion

		#region TriggerField

		/// <summary>
		/// Generates an <c>Ext.form.TriggerField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral TriggerField(String target)
		{
			return TriggerField(target, null);
		}

		/// <summary>
		/// Generates an <c>Ext.form.TriggerField</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="attributes"></param>
		/// <returns>An Ext config object serialized to Javascript.</returns>
		public JavaScriptLiteral TriggerField(String target, IDictionary attributes)
		{
			return Field(target, attributes, "triggerfield");
		}

		#endregion

		#region ComboBox

		/// <summary>
		/// Generates an <c>Ext.form.ComboBox</c> config object.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="dataSource"></param>
		/// <returns>An Ext config object serialized to JavaScript.</returns>
		public JavaScriptLiteral ComboBox(String target, IEnumerable dataSource)
		{
			return ComboBox(target, dataSource, null);
		}

		public JavaScriptLiteral ComboBox(String target, IEnumerable dataSource, IDictionary attributes)
		{
			return Literal(Config(ComboBoxConfig(target, dataSource, attributes)));
		}

		/// <summary>
		/// Generates an <c>Ext.form.ComboBox</c> config object.
		/// </summary>
		/// <exception cref="FormIdNotAvailableException"></exception>
		/// <param name="target"></param>
		/// <param name="dataSource"></param>
		/// <param name="attributes"></param>
		/// <returns></returns>
		public IDictionary ComboBoxConfig(String target, IEnumerable dataSource, IDictionary attributes)
		{
			if (String.IsNullOrEmpty(currentFormId))
			{
				throw new FormIdNotAvailableException();
			}

			if (attributes == null)
				attributes = new HybridDictionary(true);

			target = myFormHelper.RewriteTargetIfWithinObjectScope(target);

			Object selectedValue = CommonUtils.ObtainEntry(attributes, "value");
			if (selectedValue == null)
			{
				selectedValue = myFormHelper.ObtainValue(target);
				if (selectedValue != null)
				{
					attributes["value"] = JavaScriptConvert.ToString(selectedValue);
				}
			}

			String valueProperty = CommonUtils.ObtainEntry(attributes, "valueField", "value");
			String textProperty = CommonUtils.ObtainEntry(attributes, "displayField", "text");

			// Set default value if no value was set before
			attributes["valueField"] = valueProperty;
			attributes["displayField"] = textProperty;

			JavaScriptArray items = GetComboBoxItems(dataSource, attributes, selectedValue, valueProperty, textProperty);
			JavaScriptLiteral store = GetComboBoxStore(valueProperty, textProperty, items);

			ApplyValidation(InputElementType.Text, target, ref attributes);

			// hiddenName == true is replaced by target field name (by default).
			// hiddenName == false is ignored (ie. removed)
			// hiddenName == "..." uses the specified string value.
			// TODO: Add the above to method documentation.
			Object hiddenName = CommonUtils.ObtainEntry(attributes, "hiddenName");
			if (hiddenName == null)
			{
				hiddenName = true;
			}
			if (hiddenName != null && hiddenName is Boolean)
			{
				if ((Boolean)hiddenName)
				{
					attributes["hiddenName"] = target;
				}
				else
				{
					attributes.Remove("hiddenName");
				}
			}

			attributes["xtype"] = "combo";
			attributes["name"] = target;
			attributes["store"] = store;
			attributes["mode"] = "local";
			attributes["triggerAction"] = "all";

			return attributes;
		}

		private static JavaScriptArray GetComboBoxItems(IEnumerable dataSource, IDictionary attributes, Object selectedValue, String valueProperty, String textProperty)
		{
			// Save original "value" attribute
			String originalValue = CommonUtils.ObtainEntry(attributes, "value");

			// Set attributes used by SetOperation.IterateOnDataSource.
			// Note that these attributes will be overwritten and removed.
			attributes["value"] = valueProperty;
			attributes["text"] = textProperty;
			OperationState state = SetOperation.IterateOnDataSource(selectedValue, dataSource, attributes);
			JavaScriptArray rows = new JavaScriptArray();
			foreach (SetItem item in state)
			{
				JavaScriptArray row = new JavaScriptArray();
				row.Add(item.Value);
				row.Add(item.Text);
				rows.Add(row);
			}

			// Restore original "value" attribute
			if (originalValue != null)
			{
				attributes["value"] = originalValue;
			}

			return rows;
		}

		private static JavaScriptLiteral GetComboBoxStore(String valueProperty, String textProperty, JavaScriptArray items)
		{
			IDictionary config = new HybridDictionary();
			config["fields"] = new String[] { valueProperty, textProperty };
			config["data"] = items;
			String store =
				"new Ext.data.SimpleStore(" +
				JavaScriptUtils.Serialize(config) +
				")";
			return new JavaScriptLiteral(store);
		}

		#endregion

		#region Common Utils

		/// <summary>
		/// Code shared by Helpers/Controllers/Others
		/// (Copied from Castle.MonoRail.Framework.Internal)
		/// </summary>
		private static class CommonUtils
		{
			/// <summary>
			/// Obtains the entry.
			/// </summary>
			/// <param name="attributes">The attributes.</param>
			/// <param name="key">The key.</param>
			/// <returns>The generated form element</returns>
			internal static String ObtainEntry(IDictionary attributes, String key)
			{
				if (attributes != null && attributes.Contains(key))
				{
					//return (String)attributes[key];
					return attributes[key].ToString(); // rstuven: A bit less strict than casting
				}

				return null;
			}

			/// <summary>
			/// Obtains the entry.
			/// </summary>
			/// <param name="attributes">The attributes.</param>
			/// <param name="key">The key.</param>
			/// <param name="defaultValue">The default value.</param>
			/// <returns>the entry value or the default value</returns>
			internal static String ObtainEntry(IDictionary attributes, String key, String defaultValue)
			{
				String value = ObtainEntry(attributes, key);

				return value ?? defaultValue;
			}

			/// <summary>
			/// Obtains the entry and remove it if found.
			/// </summary>
			/// <param name="attributes">The attributes.</param>
			/// <param name="key">The key.</param>
			/// <param name="defaultValue">The default value.</param>
			/// <returns>the entry value or the default value</returns>
			internal static String ObtainEntryAndRemove(IDictionary attributes, String key, String defaultValue)
			{
				String value = ObtainEntryAndRemove(attributes, key);

				return value ?? defaultValue;
			}

			/// <summary>
			/// Obtains the entry and remove it if found.
			/// </summary>
			/// <param name="attributes">The attributes.</param>
			/// <param name="key">The key.</param>
			/// <returns>the entry value or null</returns>
			internal static String ObtainEntryAndRemove(IDictionary attributes, String key)
			{
				String value = null;

				if (attributes != null && attributes.Contains(key))
				{
					//value = (String)attributes[key];
					value = attributes[key].ToString(); // rstuven: A bit less strict than casting

					attributes.Remove(key);
				}

				return value;
			}
		}

		#endregion	

		#region Script generators

		/// <summary>
		/// Serializes an Ext config object to JavaScript.
		/// </summary>
		/// <param name="config">A dictionary of config options</param>
		/// <returns>An Ext config object serialized to JavaScript.</returns>
		public String Config(IDictionary config)
		{
			return JavaScriptUtils.Serialize(config);
		}

		/// <summary>
		/// Creates a <see cref="JavaScriptLiteral"/> instance for a value.
		/// </summary>
		/// <param name="literal"></param>
		/// <returns>A <see cref="JavaScriptLiteral"/> object.</returns>
		public JavaScriptLiteral Literal(String literal)
		{
			return new JavaScriptLiteral(literal);
		}

		/// <summary>
		/// Generates a script that initializes an Ext component.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Use the same config options documented for a given Ext component.
		/// </para>
		/// <para>
		/// In addition, this method provides extra config options:
		/// <list type="table">
		/// <listheader>
		///		<term>Config option</term>
		///		<description>Description</description>
		/// </listheader>
		/// <item>
		///		<term><c>assignTo</c></term>
		///		<description>(String) Assigns the component to the specified local variable.</description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <param name="type">An Ext component class name.</param>
		/// <param name="config">A dictionary of config options.</param>
		/// <returns>JavaScript code that initializes an Ext component.</returns>
		public String Component(String type, IDictionary config)
		{
			String variable = CommonUtils.ObtainEntryAndRemove(config, "assignTo");

			return 
				(String.IsNullOrEmpty(variable) ? "" : 
				"var " + variable + " = ") +
				"new " + type + "(" + Config(config) + ");";
		}

		/// <summary>
		/// Generates JavaScript code to create an
		/// <see href="http://extjs.com/deploy/dev/docs/output/Ext.form.FormPanel.html">Ext.form.FormPanel</see>
		/// component.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Requires to be enclosed by calls to 
		/// <see cref="BeginForm"/> and <see cref="EndForm"/>
		/// </para>
		/// <para>
		/// Use the same config options documented for <c>Ext.form.FormPanel</c>.
		/// Some config options are set by default:
		/// <list type="table">
		/// <listheader>
		///		<term>Config option</term>
		///		<description>Value</description>
		/// </listheader>
		/// <item>
		///		<term><c>monitorValid</c></term>
		///		<description><c>true</c></description>
		/// </item>
		/// <item>
		///		<term><c>waitMsgTarget</c></term>
		///		<description><c>true</c></description>
		/// </item>
		/// <item>
		///		<term><c>buttons</c></term>
		///		<description>A button that performs an
		///		<see href="http://extjs.com/deploy/dev/docs/output/Ext.form.Action.Submit.html">Ext.form.Action.Submit</see>
		///		action.</description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// In addition, this method provides extra config options:
		/// <list type="table">
		/// <listheader>
		///		<term>Config option</term>
		///		<description>Description</description>
		/// </listheader>
		/// <item>
		///		<term><c>assignTo</c></term>
		///		<description>(String) Assigns the component to a local variable. 
		///		If not specified, it will be automatically generated
		///		since it is required for internal use of the form.</description>
		/// </item>
		/// <item>
		///		<term><c>submitText</c></term>
		///		<description>(String) Text of the submit action button.
		///		Defaults to <c>"Send"</c></description>
		/// </item>
		/// <item>
		///		<term><c>waitMsg</c></term>
		///		<description>(String) The message to be displayed during the time 
		///		the action is being processed.
		///		Defaults to <c>"Sending..."</c></description>
		/// </item>
		/// <item>
		///		<term><c>waitTitle</c></term>
		///		<description>(String) The title to be displayed by a call to 
		///		<c>Ext.MessageBox.wait</c> during the time the action
		///		is being processed.
		///		Defaults to <c>"Please wait"</c></description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentException"><c>id</c> was specified in config options.
		/// It should be provided in <see cref="BeginForm(IDictionary)"/> method instead 
		/// or not provided at all for this form.</exception>
		/// <param name="config">A dictionary of config options</param>
		/// <returns>JavaScript code that initializes a form panel component.</returns>
		/// <seealso href="http://extjs.com/deploy/dev/docs/output/Ext.form.FormPanel.html">Ext.form.FormPanel</seealso>
		public String FormPanel(IDictionary config)
		{
			String variable = CommonUtils.ObtainEntry(config, 
				"assignTo", 
				"variable_" + currentFormId.Replace("-", "_"));

			config["assignTo"] = variable;

			String id = CommonUtils.ObtainEntry(config, "id");
			if (!String.IsNullOrEmpty(id))
			{
				throw new RailsException("'id' config option " + 
					"should be provided in BeginForm method instead " +
					"or should be not provided at all for this form", "config");
			}

			config["id"] = currentFormId;
			SetIfNotExists(config, "monitorValid", true);
			SetIfNotExists(config, "waitMsgTarget", true);
			SetIfNotExists(config, "buttons", GetFormPanelButtons(variable, config));

			String component = Component("Ext.FormPanel", config);

			StringBuilder sb = new StringBuilder(component);

			String fixIERefresingBug = String.Format(@"
if (Ext.isIE) {{
	{0}.hide();
	(function(){{
		{0}.show();
		{0}.doLayout();
	}}).defer(1);
}}
"
, variable
);
			sb.AppendFormat(@"
{0}_on_afteraction = function(form, action) {{
	{1}
	var redirect = action.result.redirect;
	if (redirect) {{
		var container = Ext.getCmp({2});
		var loadConfig = container.autoLoad;
		loadConfig.url = redirect.url;
		Ext.apply(loadConfig.params, redirect.params);
		container.load(loadConfig);
	}}
}};
{0}.form.on('actioncomplete', {0}_on_afteraction);
{0}.form.on('actionfailed', {0}_on_afteraction);
"
, variable
, fixIERefresingBug
, JavaScriptConvert.ToString(this.ContainerId)
);

			return sb.ToString();
		}

		/// <summary>
		/// Builds a JavaScript array of buttons for a form panel.
		/// </summary>
		/// <param name="variable">A JavaScript variable name.</param>
		/// <param name="config">A dictionary of config options.</param>
		/// <returns></returns>
		private JavaScriptArray GetFormPanelButtons(String variable, IDictionary config)
		{
			String submitText = CommonUtils.ObtainEntry(config, "submitText", "Send");
			String waitMsg = CommonUtils.ObtainEntry(config, "waitMsg", "Sending...");
			String waitTitle = CommonUtils.ObtainEntry(config, "waitTitle", "Please wait");

			JavaScriptObject submit = new JavaScriptObject();
			// Use formBind=true combined with monitorValid=true to automatically disable this button
			submit["formBind"] = true;
			submit["text"] = submitText;
			submit["handler"] = new JavaScriptLiteral(String.Format(@"
function(btn, e){{
	if ({0}.form.isValid()) {{
		{0}.form.submit({{
			waitMsg: {1},
			waitTitle: {2}
		}});
	}}
}}"
, variable
, JavaScriptConvert.ToString(waitMsg)
, JavaScriptConvert.ToString(waitTitle)
));

			JavaScriptArray buttons = new JavaScriptArray();
			buttons.Add(submit);

			return buttons;
		}

		private static void SetIfNotExists(IDictionary config, String key, Object value)
		{
			if (!config.Contains(key))
			{
				config[key] = value;
			}
		}

		/// <summary>
		/// Returns the identifier of the Ext container component.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When the view is dinamically loaded using an
		/// <c>Ext.ux.MonoRail.Container</c> object, the container identifier
		/// is sent as part of the request.
		/// </para>
		/// </remarks>
		public String ContainerId
		{
			get
			{
				return Controller.Request["ext-ux-monorail-container-id"];
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>A <see cref="JavaScriptLiteral"/> object.</returns>
		public JavaScriptLiteral ContainerCmp()
		{
			return new JavaScriptLiteral("Ext.getCmp('" + ContainerId + "')");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="member"></param>
		/// <returns>A <see cref="JavaScriptLiteral"/> object.</returns>
		public JavaScriptLiteral ContainerMember(String member)
		{
			return new JavaScriptLiteral(ContainerCmp() + "." + member);
		}

		/// <summary>
		/// Configures the container component
		/// </summary>
		/// <remarks>
		/// <para>
		/// Use the same config options documented for a given Ext component.
		/// </para>
		/// <para>
		/// In addition, this method provides extra config options:
		/// <list type="table">
		/// <listheader>
		///		<term>Config option</term>
		///		<description>Description</description>
		/// </listheader>
		/// <item>
		///		<term><c>centered</c></term>
		///		<description>(Boolean) Calls the <c>center</c> method
		///		of an <c>Ext.Window</c>.
		///		Defaults to <c>false</c></description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <param name="config"></param>
		/// <returns></returns>
		/// <seealso href="http://extjs.com/deploy/dev/docs/output/Ext.Component.html">Ext.Component</seealso>
		/// <seealso href="http://extjs.com/deploy/dev/docs/output/Ext.Panel.html">Ext.Panel</seealso>
		/// <seealso href="http://extjs.com/deploy/dev/docs/output/Ext.Window.html">Ext.Window</seealso>
		public String Container(IDictionary config)
		{
			String componentId = this.ContainerId;

			if (String.IsNullOrEmpty(componentId))
			{
				return String.Empty;
			}

			SetIfNotExists(config, "tools", new Object[] { });

			String[] settableValues = new String[] { 
				"disabled", 
				"iconClass",
				"title",
				"visible",
				"width",
				"height"
			//  setPagePosition( Number x, Number y ) : Ext.BoxComponent 
			//  setPosition( Number left, Number top ) : Ext.BoxComponent 
			//  setSize( Number/Object width, Number height ) : Ext.BoxComponent 
			};

			String[] toolOptions = new String[] {
				"tools",
				"minimizable",
				"maximizable",
				"resizable"
			};

			Boolean doInitTools = false;
			Boolean doCenter = (CommonUtils.ObtainEntryAndRemove(config, "centered", "false").ToLower() == "true");

			String componentIdJS = JavaScriptConvert.ToString(componentId);

			StringBuilder sb = new StringBuilder();
			sb.Append(@"
(function(){
");

			sb.AppendFormat(@"
var w = Ext.getCmp({0});
"
, componentIdJS
);
			foreach (String key in config.Keys)
			{
				String value = JavaScriptUtils.Serialize(config[key]);
				if (key == "tools")
				{
					sb.AppendFormat(@"
Ext.select('#' + {0} + ' .x-tool').each(function(el){{
	el.remove();
}});

w.tools = {{}};
if(w.baseTools){{
	w.addTool.apply(w, w.baseTools);
}}

w.addTool.apply(w, {1});
"
, componentIdJS
, value
);
				}
				else if (key == "resizable")
				{
					sb.Append(@"
Ext.destroy(w.resizer);
if(w instanceof Ext.Window && " + value + @"){
    w.resizer = new Ext.Resizable(w.el, {
        minWidth: w.minWidth,
        minHeight:w.minHeight,
        handles: w.resizeHandles || ""all"",
        pinned: true,
        resizeElement : w.resizerAction
    });
    w.resizer.window = w;
    w.resizer.on(""beforeresize"", w.beforeResize, w);
}
");
				}
				else if (Array.IndexOf<String>(settableValues, key) != -1)
				{
					sb.AppendFormat("w.set{0}({1});\n"
						, key.Substring(0, 1).ToUpper() + key.Substring(1)
						, value
						);
				}
				else
				{
					sb.AppendFormat("w.{0} = {1};\n"
						, key
						, value
						);
				}
				if (Array.IndexOf<String>(toolOptions,key) != -1){
					doInitTools = true;
				}
			}

			const String ifWindow = "if (w instanceof Ext.Window) ";

			if (doInitTools)
			{
				sb.Append(ifWindow + "w.initTools();\n");
			}
			if (doCenter)
			{
				sb.Append(ifWindow + "w.center();\n");
			}

			sb.Append(@"
})();
");
			return sb.ToString();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public String GetRemoteValue(IDictionary parameters, String callback)
		{
			String url = UrlHelper.For(parameters);

			return String.Format(@"
Ext.Ajax.Request({{
	url: '{0}',
	params: '{1}',
	success: function(params, xhr) {{
		{2}(Ext.decode(xhr.responseText));
	}}
}});"
, url
, BuildQueryString(parameters)
, callback
);
		}

		#endregion


		private IAjaxProxyGenerator ajaxProxyGenerator = new ExtJSAjaxProxyGenerator();

		/// <summary>
		/// Generates an AJAX JavaScript proxy for the current controller.
		/// </summary>
		/// <param name="proxyName">Name of the javascript proxy object</param>
		public String GenerateJSProxy(string proxyName)
		{
			return GenerateJSProxy(proxyName, Controller.AreaName, Controller.Name);
		}

		/// <summary>
		/// Generates an AJAX JavaScript proxy for a given controller.
		/// </summary>
		/// <param name="proxyName">Name of the javascript proxy object</param>
		/// <param name="controller">Controller which will be target of the proxy</param>
		public String GenerateJSProxy(string proxyName, string controller)
		{
			return GenerateJSProxy(proxyName, String.Empty, controller);
		}

		/// <summary>
		/// Generates an AJAX JavaScript proxy for a given controller.
		/// </summary>
		/// <param name="proxyName">Name of the javascript proxy object</param>
		/// <param name="controller">Controller which will be target of the proxy</param>
		/// <param name="area">area which the controller belongs to</param>
		public String GenerateJSProxy(string proxyName, string area, string controller)
		{
			return ajaxProxyGenerator.GenerateJSProxy(CurrentContext, proxyName, area, controller);
		}

	}
}
