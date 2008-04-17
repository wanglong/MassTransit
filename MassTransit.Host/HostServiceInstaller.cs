/// Copyright 2007-2008 The Apache Software Foundation.
/// 
/// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
/// this file except in compliance with the License. You may obtain a copy of the 
/// License at 
/// 
///   http://www.apache.org/licenses/LICENSE-2.0 
/// 
/// Unless required by applicable law or agreed to in writing, software distributed 
/// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
/// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
/// specific language governing permissions and limitations under the License.
namespace MassTransit.Host
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Configuration.Install;
	using System.Reflection;
	using System.ServiceProcess;
	using log4net;
	using MassTransit.Host.Config.Util.Arguments;
	using Microsoft.Win32;

	public class HostServiceInstaller :
		Installer
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (HostServiceInstaller));
		private readonly List<Assembly> _assemblies = new List<Assembly>();
		private readonly ServiceInstaller _serviceInstaller = new ServiceInstaller();
		private readonly ServiceProcessInstaller _serviceProcessInstaller = new ServiceProcessInstaller();

		private object _configurator;

		public HostServiceInstaller(HostServiceInstallerArgs args)
		{
			_serviceInstaller.ServiceName = args.Name;
			_serviceInstaller.Description = args.Description;
			_serviceInstaller.DisplayName = args.DisplayName;

			if (!string.IsNullOrEmpty(args.Username) && !string.IsNullOrEmpty(args.Password))
			{
				_serviceProcessInstaller.Username = args.Username;
				_serviceProcessInstaller.Account = ServiceAccount.User;
				_serviceProcessInstaller.Password = args.Password;
			}
			else
			{
				_serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
				_serviceProcessInstaller.Username = null;
				_serviceProcessInstaller.Password = null;
			}

			_serviceInstaller.StartType = ServiceStartMode.Automatic;

			Installers.AddRange(new Installer[] {_serviceProcessInstaller, _serviceInstaller});
		}

		public bool IsInstalled()
		{
			foreach (ServiceController service in ServiceController.GetServices())
			{
				if (service.ServiceName == _serviceInstaller.ServiceName)
					return true;
			}

			return false;
		}

		public override void Install(IDictionary stateSaver)
		{
			if (_log.IsInfoEnabled)
				_log.InfoFormat("Installing Service {0}", _serviceInstaller.ServiceName);

			base.Install(stateSaver);

			if (_log.IsInfoEnabled)
				_log.InfoFormat("Opening Registry");

			using (RegistryKey system = Registry.LocalMachine.OpenSubKey("System"))
			using (RegistryKey currentControlSet = system.OpenSubKey("CurrentControlSet"))
			using (RegistryKey services = currentControlSet.OpenSubKey("Services"))
			using (RegistryKey service = services.OpenSubKey(_serviceInstaller.ServiceName, true))
			{
				service.SetValue("Description", _serviceInstaller.Description);

				string imagePath = (string) service.GetValue("ImagePath");

				_log.InfoFormat("Service Path {0}", imagePath);

				imagePath += string.Format(" -service -config:{0}", _configurator.GetType().Assembly.GetName().Name);

				using (RegistryKey configuration = service.CreateSubKey("Configuration"))
				{
					string name = _configurator.GetType().AssemblyQualifiedName;

					_log.InfoFormat("Configuration Type {0}", name);
					configuration.SetValue("Type", name);

					PropertyInfo[] properties = _configurator.GetType().GetProperties();
					foreach (PropertyInfo property in properties)
					{
						object[] attributes = property.GetCustomAttributes(typeof (ArgumentAttribute), true);
						if (attributes != null && attributes.Length > 0)
						{
							ArgumentAttribute attribute = (ArgumentAttribute) attributes[0];

							string value = property.GetValue(_configurator, null).ToString();

							_log.InfoFormat("Configuration Value {0} = {1}", property.Name, value);

							configuration.SetValue(property.Name, value);

							imagePath += string.Format(" -{0}:{1}", attribute.Key, value);
						}
					}

					service.SetValue("ImagePath", imagePath);
				}

				service.Close();
				services.Close();
				currentControlSet.Close();
				system.Close();
			}
		}

		public void Unregister()
		{
			if (IsInstalled())
			{
				using (TransactedInstaller ti = new TransactedInstaller())
				{
					ti.Installers.Add(this);

					string path = string.Format("/assemblypath={0}", Assembly.GetExecutingAssembly().Location);
					string[] commandLine = {path};

					InstallContext context = new InstallContext(null, commandLine);
					ti.Context = context;

					ti.Uninstall(null);
				}
			}
			else
			{
				Console.WriteLine("Service is not installed");
				if (_log.IsInfoEnabled)
					_log.Info("Service is not installed");
			}
		}

		public void Register(Assembly configuratorAssembly, object configurator)
		{
			if (!IsInstalled())
			{
				_assemblies.Add(configuratorAssembly);
				_configurator = configurator;

				using (TransactedInstaller ti = new TransactedInstaller())
				{
					ti.Installers.Add(this);

					string path = string.Format("/assemblypath={0}", Assembly.GetExecutingAssembly().Location);
					string[] commandLine = {path};

					InstallContext context = new InstallContext(null, commandLine);
					ti.Context = context;

					Hashtable savedState = new Hashtable();

					ti.Install(savedState);
				}
			}
			else
			{
				Console.WriteLine("Service is already installed");
				if (_log.IsInfoEnabled)
					_log.Info("Service is already installed");
			}
		}

		public class HostServiceInstallerArgs
		{
			private string _name;
			private string _description;
			private string _displayName;
			private string _userName;
			private string _password;


			public HostServiceInstallerArgs(string name, string description, string displayName) : 
				this(name, description, displayName, null, null)
			{
			}

			public HostServiceInstallerArgs(string name, string description, string displayName, string userName, string password)
			{
				_name = name ?? "MassTransitHost";
				_description = description ?? "MassTransit Host";
				_displayName = displayName ?? "Mass Transit Host";
				_userName = userName;
				_password = password;
			}


			[Argument(Key = "name", Description = "The name for the service", Required = false)]
			public string Name
			{
				get { return _name; }
				set { _name = value; }
			}

			[Argument(Key = "description", Description = "The description for the service", Required = false)]
			public string Description
			{
				get { return _description; }
				set { _description = value; }
			}

			[Argument(Key = "displayname", Description = "The name to display for the service", Required = false)]
			public string DisplayName
			{
				get { return _displayName; }
				set { _displayName = value; }
			}

			[Argument(Key = "username", Description = "Username for service account", Required = false)]
			public string Username
			{
				get { return _userName; }
				set { _userName = value; }
			}

			[Argument(Key = "password", Description = "Password for service account", Required = false)]
			public string Password
			{
				get { return _password; }
				set { _password = value; }
			}
		}
	}
}