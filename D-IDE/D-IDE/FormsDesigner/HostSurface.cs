using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using D_IDE;

namespace D_IDE.FormsDesigner
{
	/// <summary>
	/// Inherits from DesignSurface and hosts the RootComponent and 
	/// all other designers. It also uses loaders (BasicDesignerLoader
	/// or CodeDomDesignerLoader) when required. It also provides various
	/// services to the designers. Adds MenuCommandService which is used
	/// for Cut, Copy, Paste, etc.
	/// </summary>
	public class HostSurface : DesignSurface
	{
		private BasicDesignerLoader _loader;
		private ISelectionService _selectionService;

		public HostSurface()
			: base()
		{
			this.AddService(typeof(IMenuCommandService), new MenuCommandService(this));
		}
		public HostSurface(IServiceProvider parentProvider)
			: base(parentProvider)
		{
			this.AddService(typeof(IMenuCommandService), new MenuCommandService(this));
		}

		internal void Initialize()
		{

			Control control = null;
			IDesignerHost host = (IDesignerHost)this.GetService(typeof(IDesignerHost));

			if(host == null)
				return;
			
			try
			{
				// Set the backcolor
				Type hostType = host.RootComponent.GetType();
				if(hostType == typeof(Form))
				{
					control = this.View as Control;
					control.BackColor = Color.White;
				}
				else if(hostType == typeof(UserControl))
				{
					control = this.View as Control;
					control.BackColor = Color.White;
				}
				else if(hostType == typeof(Component))
				{
					control = this.View as Control;
					control.BackColor = Color.FloralWhite;
				}
				else
				{
					throw new Exception("Undefined Host Type: " + hostType.ToString());
				}

				// Set SelectionService - SelectionChanged event handler
				_selectionService = (ISelectionService)(this.ServiceContainer.GetService(typeof(ISelectionService)));
				_selectionService.SelectionChanged += new EventHandler(selectionService_SelectionChanged);
			}
			catch(Exception ex)
			{
				Trace.WriteLine(ex.ToString());
			}
		}

		public BasicDesignerLoader Loader
		{
			get
			{
				return _loader;
			}
			set
			{
				_loader = value;
			}
		}

		/// <summary>
		/// When the selection changes this sets the PropertyGrid's selected component 
		/// </summary>
		private void selectionService_SelectionChanged(object sender, EventArgs e)
		{
			if(_selectionService != null)
			{
				ICollection selectedComponents = _selectionService.GetSelectedComponents();
				PropertyGrid propertyGrid = (PropertyGrid)this.GetService(typeof(PropertyGrid));

				object[] comps = new object[selectedComponents.Count];
				int i = 0;

				foreach(Object o in selectedComponents)
				{
					//MessageBox.Show((o as Control).Text);
					comps[i] = o;
					i++;
				}

				propertyGrid.SelectedObjects = comps;
			}
		}

		public void AddService(Type type, object serviceInstance)
		{
			this.ServiceContainer.AddService(type, serviceInstance);
		}
	}











	public class HostSurfaceManager : DesignSurfaceManager
	{
		public HostSurfaceManager()
			: base()
		{
			this.AddService(typeof(INameCreationService), new NameCreationService());
		}

		protected override DesignSurface CreateDesignSurfaceCore(IServiceProvider parentProvider)
		{
			return new HostSurface(parentProvider);
		}

		/// <summary>
		/// Gets a new HostSurface and loads it with the appropriate type of
		/// root component. 
		/// </summary>
		public Control GetNewHost(Type rootComponentType)
		{
			HostSurface hostSurface = (HostSurface)this.CreateDesignSurface(this.ServiceContainer);

			if(rootComponentType == typeof(Form))
				hostSurface.BeginLoad(typeof(Form));
			else if(rootComponentType == typeof(UserControl))
				hostSurface.BeginLoad(typeof(UserControl));
			else if(rootComponentType == typeof(Component))
				hostSurface.BeginLoad(typeof(Component));
			else
				throw new Exception("Undefined Host Type: " + rootComponentType.ToString());

			hostSurface.Initialize();
			this.ActiveDesignSurface = hostSurface;
			return hostSurface.View as Control;
		}

		public void AddService(Type type, object serviceInstance)
		{
			this.ServiceContainer.AddService(type, serviceInstance);
		}
	}
}
