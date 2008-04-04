namespace Castle.MonoRail.ExtJSDemo.Controllers
{
	using System;
	using Castle.MonoRail.Framework;
	using Castle.MonoRail.Framework.Helpers;

	[Layout("default2")]
	[Helper(typeof(ExtJSHelper), "ExtJS")]
	public class Home2Controller : SmartDispatcherController
	{
		public void Index()
		{
			PropertyBag["AccessDate"] = DateTime.Now;
		}

		public void BlowItAway()
		{
			throw new Exception("Exception thrown from a MonoRail action");
		}
	}
}
