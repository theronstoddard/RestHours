using Ninject;
using System;
using System.Reflection;

namespace RestHours.ConsoleApp
{
	class Program
	{
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		static void Main(string[] args)
		{
			Log.Debug("Starting Restaurant Locator.");
			try
			{
				var kernel = new StandardKernel();
				kernel.Load(Assembly.GetExecutingAssembly());
				var ui = kernel.Get<UserInteraction>();

				ui.Run();
			}
			catch (Exception e)
			{
				Log.Fatal($"Failed with exception: '{e.Message}'");
				System.Console.WriteLine("Press enter to quit.");
				System.Console.ReadLine();
			}
			Log.Debug("Stopping Restaurant Locator.");
		}
	}
}
