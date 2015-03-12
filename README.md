# MonoRail ExtJS Helper

Provides utility classes to work with [ExtJS](http://www.sencha.com/products/extjs/) library from [Castle MonoRail](http://www.castleproject.org/projects/monorail/) MVC web framework.

As `FormHelper` does, `ExtJSHelper` mainly takes care of data binding and validation at the browser side
based on server side declarations. But also there are methods that provide foundation for advanced applications
(see `Container` method and `Ext.ux.MonoRail.Container.js` script).

Let's start from an ExtJS-only sample code that is not enjoying MonoRail? databinding and validation:

        var f = new Ext.FormPanel({
                id:"contact-form",
                url: '/Contact/Save.castle',
                items:[
                        new Ext.form.TextField({
                                name: "contact.FullName",
                                fieldLabel: "Full Name"
                        })
                ]
        });

Adding `ExtJSHelper` the code looks like this (using Brail view engine):

        ${ExtJSHelper.BeginForm({@id:"contact-form"})}

        var f = new Ext.FormPanel({
                id:"contact-form",
                url: '${Url.For({@action: "Save"})}',
                items:[
                        ${ExtJSHelper.TextField("contact.FullName", {
                                @fieldLabel: "Full Name"
                        })}
                ]
        });

        ${ExtJSHelper.EndForm()}

Even we can make it fully configured by `ExtJSHelper` (note how Brail hash notation is everywhere):

        ${ExtJSHelper.BeginForm({@id:"contact-form"})}

        ${ExtJSHelper.FormPanel({
                @assignTo: "contactForm",
                @url: Url.For({@action: "Save"}),
                @items:[
                        ExtJSHelper.TextField("contact.FullName", {
                                @fieldLabel: "Full Name"
                        })}
                ]
        })}

        ${ExtJSHelper.EndForm()}

Also you can configure some fields after they are rendered. For example:

        var f = new Ext.FormPanel({
                id:"contact-form",
                url: '${Url.For({@action: "Save"})}',
                items:[
                        new Ext.form.TextField({
                                name: "contact.FullName",
                                fieldLabel: "Full Name"
                        })
                ]
        });
        f.render("container");

        // Some code here...

        ${ExtJSHelper.BeginForm({@id:"contact-form"})}
        ${ExtJSHelper.ApplyToField("contact.FullName")}
        ${ExtJSHelper.EndForm()}

See the demo app for more examples and the XML documentation in ExtJSHelper class.

---

### Issues to work on:
* ExtJSAjaxProxyGenerator has not been tested yet.
* Demo application is working but it needs more work. The idea is to show how to convert to ExJS the template MonoRail? app.
* Generated Javascript needs line feeds for better debugging experience.
* Find a better place for `Ext.ux.MonoRail.Container.js` (it's in the demo project today)
* ...and other design issues I'm not sure about. Here I need your feedback.

### Notes on the demo:
* The first thing you see is an Ext.ux.MonoRail?.Panel with the Contact2/Index view.
* To open an Ext.ux.MonoRail?.Window, click on "A simple use of parameters" link.
* The "refresh" tool in the title bar is defined in Ext.ux.MonoRail?.Container.
* The "gear" tool in the title bar is defined in Contact2/Index
* See validation rules in Models/ContactInfo?.cs
* The view in the window has client validation disabled, just to contrast it to the view in the panel with client validation enabled.
* The Message field always has client validation disabled.
* To try redirection in the same container, fill a form according to validation rules and press "Save Contact".
