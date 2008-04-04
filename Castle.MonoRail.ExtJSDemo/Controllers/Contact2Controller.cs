namespace Castle.MonoRail.ExtJSDemo.Controllers
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using Castle.MonoRail.Framework;
	using Castle.MonoRail.Framework.Helpers;
	using Castle.MonoRail.ExtJSDemo.Models;

	public class Contact2Controller : ExtJSController
	{
		public void Desktop()
		{
		}

		public void Index()
		{
			// Here you could, for example, query the database for some
			// information about your company, and make it available
			// to the template using the "PropertyBag" property. 
			AddCountriesToPropertyBag();

			// The following line is required to allow automatic validation
			PropertyBag["contacttype"] = typeof(ContactInfo);
		}

		[AccessibleThrough(Verb.Post)]
		[AjaxAction]
		public FormResponse SendContact([DataBind("contact", Validate = true)] ContactInfo info)
		{
			//PropertyBag["contact"] = info;
			//RenderView("Confirmation");

			if (!this.HasValidationError(info))
			{
				// We could save, send through email or something else. 
				// For now, we just show the data back

				PropertyBag["contact"] = info;

				return this.GetFormResponse()
					.RedirectContainerToAction("Confirmation", PropertyBag);
			}
			return this.GetFormResponse();
		}

		public void Confirmation([DataBind("contact")] ContactInfo contact)
			
		{
			PropertyBag["contact"] = contact;
		}


		[AccessibleThrough(Verb.Post)]
		[AjaxAction]
		public FormResponse Load()
		{
			IDictionary data = new Hashtable();
			data["contact.Name"] = "jajaja!";
			data["contact.Message"] = "!!!!";

			return this.GetFormResponse()
				.LoadData(data);
		}

		private void AddCountriesToPropertyBag()
		{
			List<Country> countries = new List<Country>();

			countries.Add(new Country(1, "Brazil"));
			countries.Add(new Country(2, "Canada"));
			countries.Add(new Country(3, "United States"));
			countries.Add(new Country(4, "Russia"));

			PropertyBag["countries"] = countries;
		}
	}
}
