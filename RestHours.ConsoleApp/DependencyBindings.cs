using log4net;
using RestHours.Core;

namespace RestHours.ConsoleApp
{
	public class DependencyBindings : Ninject.Modules.NinjectModule
	{
		public override void Load()
		{
			Bind<ILog>().ToMethod(ctx => LogManager.GetLogger(ctx.Request.Target.Member.ReflectedType));
			Bind<IRestaurantRepo>().To<RestaurantRepository>().InSingletonScope();
		}
	}
}
