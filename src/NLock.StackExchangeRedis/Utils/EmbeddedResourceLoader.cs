using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;

namespace NLock.StackExchangeRedis.Utils
{
	internal static class LockScript
	{
		internal const string MUTEX_LOCK_ACQUIRE = "NLock.StackExchangeRedis.Lua.MutexLock_Acquire.lua";
		internal const string MUTEX_LOCK_RELEASE = "NLock.StackExchangeRedis.Lua.MutexLock_Release.lua";
		internal const string MUTEX_LOCK_RENEW = "NLock.StackExchangeRedis.Lua.MutexLock_Renew.lua";

		internal const string READ_LOCK_ACQUIRE = "NLock.StackExchangeRedis.Lua.ReadLock_Acquire.lua";
		internal const string READ_LOCK_RELEASE = "NLock.StackExchangeRedis.Lua.ReadLock_Release.lua";
		internal const string READ_LOCK_RENEW = "NLock.StackExchangeRedis.Lua.ReadLock_Renew.lua";

		internal const string WRITE_LOCK_ACQUIRE = "NLock.StackExchangeRedis.Lua.WriteLock_Acquire.lua";
		internal const string WRITE_LOCK_RELEASE = "NLock.StackExchangeRedis.Lua.WriteLock_Release.lua";
		
		internal const string SEMAPHORE_ACQUIRE = "NLock.StackExchangeRedis.Lua.Semaphore_Acquire.lua";
		internal const string SEMAPHORE_RELEASE = "NLock.StackExchangeRedis.Lua.Semaphore_Release.lua";
		internal const string SEMAPHORE_RENEW = "NLock.StackExchangeRedis.Lua.Semaphore_Renew.lua";

	}

	internal static class LuaScriptLoader
	{
		internal static LuaScript GetScript(string name)
		{
			var scriptString = GetResource(name);
			return LuaScript.Prepare(scriptString);
		}

		private static string GetResource(string resourceName)
		{
			var assembly = typeof(LuaScriptLoader).GetTypeInfo().Assembly;

			using (var stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null) 
					throw new MissingManifestResourceException($"Cannot find resource {resourceName}");
				
				using (var streamReader = new StreamReader(stream))
				{
					return streamReader.ReadToEnd();
				}
			}
			
		}
	}
}
