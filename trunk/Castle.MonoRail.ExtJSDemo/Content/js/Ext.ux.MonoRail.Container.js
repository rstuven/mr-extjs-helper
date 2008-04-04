Ext.namespace("Ext.ux.MonoRail");

Ext.ux.MonoRail.Container = {

	disableCaching: false,
	
	scripts: true,
	
	initMonoRailContainer: function() {
		var _this = this;

		var loadConfig = {
			url: _this.url,
			disableCaching: _this.disableCaching,
			params: {
				"ext-ux-monorail-container-id": _this.id
			},
			scripts: _this.scripts
		};
		this.autoLoad = loadConfig;
		
		this.width = 0;
		this.height = 0;
		
		if (!this.tools)
			this.tools = [];
		
		this.tools.unshift({
			id:'refresh',
			handler:function(){
				_this.load(loadConfig);
			}
        });

		// "Backup" tools
		this.baseTools = this.tools;
	}
};

////////////////////////////////////////////////////////////////////////////////

Ext.ux.MonoRail.Window = Ext.extend(Ext.Window, {
	
	initComponent: function() {
		
		this.initMonoRailContainer();
		
		Ext.ux.MonoRail.Window.superclass.initComponent.call(this);
	}
});

Ext.override(Ext.ux.MonoRail.Window, Ext.ux.MonoRail.Container);

Ext.reg("Ext.ux.MonoRail.Window", Ext.ux.MonoRail.Window);

////////////////////////////////////////////////////////////////////////////////

Ext.ux.MonoRail.Panel = Ext.extend(Ext.Panel, {
	
	initComponent: function() {
		
		this.initMonoRailContainer();
		
		Ext.ux.MonoRail.Panel.superclass.initComponent.call(this);
	}
});

Ext.override(Ext.ux.MonoRail.Panel, Ext.ux.MonoRail.Container);

Ext.reg("Ext.ux.MonoRail.Panel", Ext.ux.MonoRail.Panel);
