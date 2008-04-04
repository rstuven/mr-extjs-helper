namespace Castle.MonoRail.ExtJSDemo.Models
{
	using Castle.Components.Validator;

	public class ContactInfo
	{
		private string name, email, message;
		private Country country;

		[ValidateNonEmpty]
		//[ValidateNotSameAs("Email", "aaaa {0} bbbb")]
		//[ValidateSameAs("Email")]
		[ValidateRange("x", "z")]
		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		[ValidateNonEmpty, ValidateEmail]
		public string Email
		{
			get { return email; }
			set { email = value; }
		}

		[ValidateNonEmpty, ValidateLength(3, 5)]
		public string Message
		{
			get { return message; }
			set { message = value; }
		}

		[ValidateNonEmpty]
		public Country Country
		{
			get { return country; }
			set { country = value; }
		}
	}
}

