using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace RegistryUtils
{
	
	public class RegistryMonitor : IDisposable
	{
		#region P/Invoke

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint options, int samDesired,
		                                       out IntPtr phkResult);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree,
		                                                  RegChangeNotifyFilter dwNotifyFilter, IntPtr hEvent,
		                                                  bool fAsynchronous);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern int RegCloseKey(IntPtr hKey);

		private const int KEY_QUERY_VALUE = 0x0001;
		private const int KEY_NOTIFY = 0x0010;
		private const int STANDARD_RIGHTS_READ = 0x00020000;

		private static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int) 0x80000000));
		private static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int) 0x80000001));
		private static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int) 0x80000002));
		private static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int) 0x80000003));
		private static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked((int) 0x80000004));
		private static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int) 0x80000005));
		private static readonly IntPtr HKEY_DYN_DATA = new IntPtr(unchecked((int) 0x80000006));

		#endregion

		#region Event handling

		
		public event EventHandler RegChanged;
		
		
		protected virtual void OnRegChanged()
		{
			EventHandler handler = RegChanged;
			if (handler != null)
				handler(this, null);
		}

		
		public event ErrorEventHandler Error;
		
		
		protected virtual void OnError(Exception e)
		{
			ErrorEventHandler handler = Error;
			if (handler != null)
				handler(this, new ErrorEventArgs(e));
		}

		#endregion

		#region Private member variables

		private IntPtr _registryHive;
		private string _registrySubName;
		private object _threadLock = new object();
		private Thread _thread;
		private bool _disposed = false;
		private ManualResetEvent _eventTerminate = new ManualResetEvent(false);

		private RegChangeNotifyFilter _regFilter = RegChangeNotifyFilter.Key | RegChangeNotifyFilter.Attribute |
		                                           RegChangeNotifyFilter.Value | RegChangeNotifyFilter.Security;

		#endregion

		
		public RegistryMonitor(RegistryKey registryKey)
		{
			InitRegistryKey(registryKey.Name);
		}

		
		public RegistryMonitor(string name)
		{
			if (name == null || name.Length == 0)
				throw new ArgumentNullException("name");

			InitRegistryKey(name);
		}
		
		
		public RegistryMonitor(RegistryHive registryHive, string subKey)
		{
			InitRegistryKey(registryHive, subKey);
		}

		
		public void Dispose()
		{
			Stop();
			_disposed = true;
			GC.SuppressFinalize(this);
		}

		
		public RegChangeNotifyFilter RegChangeNotifyFilter
		{
			get { return _regFilter; }
			set
			{
				lock (_threadLock)
				{
					if (IsMonitoring)
						throw new InvalidOperationException("Monitoring thread is already running");

					_regFilter = value;
				}
			}
		}
		
		#region Initialization

		private void InitRegistryKey(RegistryHive hive, string name)
		{
			switch (hive)
			{
				case RegistryHive.ClassesRoot:
					_registryHive = HKEY_CLASSES_ROOT;
					break;

				case RegistryHive.CurrentConfig:
					_registryHive = HKEY_CURRENT_CONFIG;
					break;

				case RegistryHive.CurrentUser:
					_registryHive = HKEY_CURRENT_USER;
					break;

				case RegistryHive.DynData:
					_registryHive = HKEY_DYN_DATA;
					break;

				case RegistryHive.LocalMachine:
					_registryHive = HKEY_LOCAL_MACHINE;
					break;

				case RegistryHive.PerformanceData:
					_registryHive = HKEY_PERFORMANCE_DATA;
					break;

				case RegistryHive.Users:
					_registryHive = HKEY_USERS;
					break;

				default:
					throw new InvalidEnumArgumentException("hive", (int)hive, typeof (RegistryHive));
			}
			_registrySubName = name;
		}

		private void InitRegistryKey(string name)
		{
			string[] nameParts = name.Split('\\');

			switch (nameParts[0])
			{
				case "HKEY_CLASSES_ROOT":
				case "HKCR":
					_registryHive = HKEY_CLASSES_ROOT;
					break;

				case "HKEY_CURRENT_USER":
				case "HKCU":
					_registryHive = HKEY_CURRENT_USER;
					break;

				case "HKEY_LOCAL_MACHINE":
				case "HKLM":
					_registryHive = HKEY_LOCAL_MACHINE;
					break;

				case "HKEY_USERS":
					_registryHive = HKEY_USERS;
					break;

				case "HKEY_CURRENT_CONFIG":
					_registryHive = HKEY_CURRENT_CONFIG;
					break;

				default:
					_registryHive = IntPtr.Zero;
					throw new ArgumentException("The registry hive '" + nameParts[0] + "' is not supported", "value");
			}

			_registrySubName = String.Join("\\", nameParts, 1, nameParts.Length - 1);
		}
		
		#endregion

		
		public bool IsMonitoring
		{
			get { return _thread != null; }
		}

		
		public void Start()
		{
			if (_disposed)
				throw new ObjectDisposedException(null, "This instance is already disposed");
			
			lock (_threadLock)
			{
				if (!IsMonitoring)
				{
					_eventTerminate.Reset();
					_thread = new Thread(new ThreadStart(MonitorThread));
					_thread.IsBackground = true;
					_thread.Start();
				}
			}
		}

		
		public void Stop()
		{
			if (_disposed)
				throw new ObjectDisposedException(null, "This instance is already disposed");
			
			lock (_threadLock)
			{
				Thread thread = _thread;
				if (thread != null)
				{
					_eventTerminate.Set();
					thread.Join();
				}
			}
		}

		private void MonitorThread()
		{
			try
			{
				ThreadLoop();
			}
			catch (Exception e)
			{
				OnError(e);
			}
			_thread = null;
		}

		private void ThreadLoop()
		{
			IntPtr registryKey;
			int result = RegOpenKeyEx(_registryHive, _registrySubName, 0, STANDARD_RIGHTS_READ | KEY_QUERY_VALUE | KEY_NOTIFY,
			                          out registryKey);
			if (result != 0)
				throw new Win32Exception(result);

			try
			{
				AutoResetEvent _eventNotify = new AutoResetEvent(false);
				WaitHandle[] waitHandles = new WaitHandle[] {_eventNotify, _eventTerminate};
				while (!_eventTerminate.WaitOne(0, true))
				{
					result = RegNotifyChangeKeyValue(registryKey, true, _regFilter, _eventNotify.Handle, true);
					if (result != 0)
						throw new Win32Exception(result);

					if (WaitHandle.WaitAny(waitHandles) == 0)
					{
						OnRegChanged();
					}
				}
			}
			finally
			{
				if (registryKey != IntPtr.Zero)
				{
					RegCloseKey(registryKey);
				}
			}
		}
	}
	
	
	[Flags]
	public enum RegChangeNotifyFilter
	{
		
		Key = 1,
		
		Attribute = 2,
		
		Value = 4,
		
		Security = 8,
	}
}