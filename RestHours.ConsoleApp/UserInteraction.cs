using log4net;
using RestHours.Core;
using RestHours.Core.Model;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RestHours.ConsoleApp
{
	public class UserInteraction
	{
		private readonly IRestaurantRepo _restaurantRepo;
		private readonly ILog _log;

		public UserInteraction(IRestaurantRepo restaurantRepo, ILog log)
		{
			_restaurantRepo = restaurantRepo;
			_log = log;
		}

		public void Run()
		{
			// Json file is included in project to be copied to executable destination directory
			_restaurantRepo.LoadFromFile("rest_hours.json");

			System.Console.WriteLine();
			System.Console.WriteLine("********** Welcome to the Restaurant Locator! **********");
			System.Console.WriteLine();

			while (true)
			{
				if (!GetDay(out ShortDayOfWeek validDay))
					return;

				if (!GetTime(out DateTime hourMin))
					return;

				_log.Debug($"User entered: Day: {validDay}, Hour: {hourMin.Hour}, Minute: {hourMin.Minute}");
				var validDate = new DateTime(RestOpen.CalendarYear, RestOpen.CalendarMonth, RestOpen.CalendarFirstDayOfWeek + (int)validDay, hourMin.Hour, hourMin.Minute, 0);

				var available = _restaurantRepo.Available(validDay, hourMin.Hour, hourMin.Minute);
				if (!available.Any())
				{
					_log.Info($"No restaurants are available at {validDay} {validDate:MM/dd/yyyy h:mm tt}.");
					System.Console.WriteLine();
					continue;
				}

				System.Console.WriteLine();
				_log.Info($"These {available.Count} restaurants are available on {validDay} at {validDate:h:mm tt}:");
				foreach (var restaurant in available)
				{
					System.Console.WriteLine($"{restaurant.Name}");
					_log.Debug($"{restaurant.Name} ({restaurant.Start:MM/dd/yyyy h:mm tt} - {restaurant.End:MM/dd/yyyy h:mm tt})");
				}
				System.Console.WriteLine();
			}
		}

		private bool GetDay(out ShortDayOfWeek validDay)
		{
			validDay = ShortDayOfWeek.Mon;
			for (var parseSuccess = false; !parseSuccess;)
			{
				_log.Info("Enter a day (Mon, Tue, etc.) or q to quit:");
				var dayStr = System.Console.ReadLine();
				if (dayStr == "q" || dayStr == null)
				{
					_log.Info("Terminating at user's request.");
					return false;
				}
				if (Regex.IsMatch(dayStr, "\\d"))
					dayStr = string.Empty;
				parseSuccess = Enum.TryParse(dayStr, true, out ShortDayOfWeek day);
				if (parseSuccess)
					validDay = day;
				else
					_log.Info("Invalid day. Please try again.");
			}

			return true;
		}

		private bool GetTime(out DateTime validHourMin)
		{
			validHourMin = DateTime.MinValue;
			for (var parseSuccess = false; !parseSuccess;)
			{
				_log.Info("Enter hour and minute (hh:mm am/pm) or q to quit:");
				var hourMinStr = System.Console.ReadLine();
				if (hourMinStr == "q")
				{
					_log.Info("Terminating at user's request.");
					return false;
				}
				parseSuccess = DateTime.TryParse(hourMinStr, out var hourMin);
				if (parseSuccess)
					validHourMin = hourMin;
				else
					_log.Info("Invalid hour or minute. Please try again.");
			}

			return true;
		}
	}
}
