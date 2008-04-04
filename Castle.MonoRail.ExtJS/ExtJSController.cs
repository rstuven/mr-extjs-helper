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
	using System.Reflection;
	using Castle.Components.Binder;
	using Castle.MonoRail.Framework.Helpers;
	using Newtonsoft.Json;

	[Layout("blank")]
	[Helper(typeof(ExtJSHelper), "ExtJS")]
	public class ExtJSController : SmartDispatcherController
	{
		/// <summary>
		/// Gets the form response.
		/// </summary>
		/// <returns></returns>
		public FormResponse GetFormResponse()
		{
			FormResponse response = new FormResponse(this);

			// Load errors of bound instances
			foreach (Object instance in this.BoundInstanceErrors.Keys)
			{
				ErrorList errors = this.BoundInstanceErrors[instance];
				foreach (DataBindError error in errors)
				{
					response.Errors[error.Key] = error.ErrorMessage;
				}
			}
			return response;
		}
		
		/// <summary>
		/// Based on code from 
		/// <see href="http://using.castleproject.org/display/MR/UsingActionReturnValuesInJS"/>
		/// </summary>
		/// <param name="method"></param>
		/// <param name="request"></param>
		/// <param name="actionArgs"></param>
		protected override void InvokeMethod(MethodInfo method, IRequest request, IDictionary actionArgs)
		{
			ParameterInfo[] parameters = method.GetParameters();
			object[] methodArgs = BuildMethodArguments(parameters, request, actionArgs);
			object result = method.Invoke(this, methodArgs);
			bool isAjaxAction = method.GetCustomAttributes(typeof(AjaxActionAttribute), false).Length > 0;

            string text = null;
			if (isAjaxAction)
			{
				if (result == null || JavaScriptUtils.HasToStringConversion(result))
				{
					text = JavaScriptConvert.ToString(result);
				}
				else if (result is FormResponse)
				{
					text = (result as FormResponse).ToJson();
				}
				else
				{
					text = JavaScriptConvert.SerializeObject(result);
				}
			}
			else if (result != null)
			{
				text = result.ToString();
			}
			if (text != null)
			{
				RenderText(text);
			}
		}

		/// <summary>
		/// Generates a response to be processed by <c>success</c> and 
		/// <c>failure</c> callbacks configured in subclasses of <c>Ext.form.Action</c>
		/// </summary>
		/// <seealso href="http://extjs.com/deploy/dev/docs/output/Ext.form.Action.html">Ext.form.Action</seealso>
		public class FormResponse
		{
			public readonly JavaScriptObject Errors = new JavaScriptObject();
			public Object Data;
			public String RedirectContainerUrl;
			public IDictionary RedirectContainerParams;

			private readonly Controller controller;

			public FormResponse(Controller controller)
			{
				this.controller = controller;
			}

			#region Serialization

			/// <summary>
			/// Serializes to the format expected by <c>Ext.form.Action</c>
			/// </summary>
			/// <returns></returns>
			public String ToJson()
			{
				JavaScriptObject response = new JavaScriptObject();
				ToJsonBasic(response);
				ToJsonRedirect(response);
				return JavaScriptConvert.SerializeObject(response);
			}

			private void ToJsonBasic(JavaScriptObject response)
			{
				Boolean success = (this.Errors.Count == 0);
				response["success"] = success;
				if (success)
				{
					if (this.Data != null)
					{
						response["data"] = this.Data;
					}
				}
				else
				{
					response["errors"] = this.Errors;
				}
			}

			private void ToJsonRedirect(JavaScriptObject response)
			{
				if (!String.IsNullOrEmpty(this.RedirectContainerUrl))
				{
					JavaScriptObject redirect = new JavaScriptObject();
					redirect["url"] = this.RedirectContainerUrl;
					if (this.RedirectContainerParams != null)
					{
						JavaScriptObject parameters = new JavaScriptObject();
						foreach (String key in this.RedirectContainerParams.Keys)
						{
							Object value = this.RedirectContainerParams[key];
							JavaScriptUtils.FlattenObjectByKey(parameters, key, value);
						}
						redirect["params"] = parameters;
					}

					response["redirect"] = redirect;
				}
			}

			#endregion

			#region Fluent interfaces

			/// <summary>
			/// 
			/// </summary>
			/// <param name="data"></param>
			/// <returns></returns>
			public FormResponse LoadData(Object data)
			{
				this.Data = data;
				return this;
			}

			public FormResponse RedirectContainer(String url)
			{
				return RedirectContainer(url, (IDictionary)null);
			}

			public FormResponse RedirectContainer(String url, params String[] parameters)
			{
				return RedirectContainer(url, DictHelper.Create(parameters));
			}

			public FormResponse RedirectContainer(String url, IDictionary parameters)
			{
				this.RedirectContainerUrl = url;
				this.RedirectContainerParams = parameters;
				return this;
			}

			public FormResponse RedirectContainerToAction(String action)
			{
				return RedirectContainerToAction(action, (IDictionary)null);
			}

			public FormResponse RedirectContainerToAction(String action, params String[] parameters)
			{
				return RedirectContainerToAction(action, DictHelper.Create(parameters));
			}

			public FormResponse RedirectContainerToAction(String action, IDictionary parameters)
			{
				String url = controller.UrlBuilder.BuildUrl(
					controller.Context.UrlInfo, 
					controller.AreaName, 
					controller.Name, 
					action);

				this.RedirectContainerUrl = url;
				this.RedirectContainerParams = parameters;
				return this;
			}

			#endregion
		}

	}
}
