using log4net;
using Newtonsoft.Json;
using RestHours.Core.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RestHours.Core
{
	public interface IRestaurantRepo
	{
		void LoadFromFile(string fullPath);
		void LoadFromJson(string json);
		IList<RestOpen> Available(ShortDayOfWeek day, int hour, int minute);
	}

	public class RestaurantRepository : IRestaurantRepo
	{
		private readonly ILog _log;

		public RestaurantRepository(ILog log)
		{
			_log = log;
		}

		private IList<RestOpen> _schedule;
		public void LoadFromFile(string fullPath)
		{
			if (!File.Exists(fullPath))
				throw new Exception($"File not found: '{fullPath}'");
			_log.Debug($"Loading restaurants from file {fullPath}");

			LoadFromJson(File.ReadAllText(fullPath));
		}

		public void LoadFromJson(string json)
		{
			var rawItems = JsonConvert.DeserializeObject<List<FileItem>>(json);
			_schedule = rawItems.SelectMany(FileItemToScheduleEntries).ToList();
		}

		public IList<RestOpen> Available(ShortDayOfWeek day, int hour, int minute)
		{
			if (_schedule == null)
				throw new Exception($"No restaurant schedule loaded. Call one of the load methods before calling {nameof(Available)}.");

			var requestDateTime = new DateTime(RestOpen.CalendarYear, RestOpen.CalendarMonth, 
				RestOpen.CalendarFirstDayOfWeek + (int)day, hour, minute, 0);
			var available = _schedule
				.Where(s => s.Start <= requestDateTime && s.End > requestDateTime)
				.OrderBy(s => s.Name)
				.ToArray();
			return available;
		}

		private IEnumerable<RestOpen> FileItemToScheduleEntries(FileItem item)
		{
			var restaurantOpenTimes = new List<RestOpen>();
			foreach (var timeStr in item.Times)
			{
				var match = Regex.Match(timeStr, @"\d");
				if (!match.Success)
					throw new Exception($"Restaurant '{item.Name}' missing time in '{timeStr}'.");

				var timesPart = timeStr.Substring(match.Index, timeStr.Length - match.Index);
				var daysPart = timeStr.Substring(0, match.Index);

				var days = GetScheduleItemDays(item.Name, daysPart, timeStr);
				var startEndTimes = GetScheduleItemStartAndEndTimes(item.Name, timesPart, timeStr);

				foreach (var day in days)
				{
					AddScheduleEntry(restaurantOpenTimes, day, item.Name, startEndTimes.StartTime, startEndTimes.EndTime, startEndTimes.AddDayToEnd);
				}
			}
			return restaurantOpenTimes;
		}

		private IEnumerable<ShortDayOfWeek> GetScheduleItemDays(string restaurantName, string daysPart, string scheduleStr)
		{
			var dayRanges = Regex.Split(daysPart, ",");
			if (dayRanges == null || dayRanges.Length == 0)
				throw new Exception($"Restaurant '{restaurantName}' missing day of week in '{scheduleStr}'");

			var days = new List<ShortDayOfWeek>();
			foreach (var dayRange in dayRanges)
			{
				var range = Regex.Split(dayRange, "-");
				var startDay = (ShortDayOfWeek)Enum.Parse(typeof(ShortDayOfWeek), range[0].Trim());
				days.Add(startDay);
				if (range.Length > 1)
				{
					var endDay = (ShortDayOfWeek)Enum.Parse(typeof(ShortDayOfWeek), range[1].Trim());
					if (startDay >= endDay)
						throw new Exception($"Restaurant '{restaurantName}' has invalid day range in '{scheduleStr}'.");
					for (var dayEnum = (ShortDayOfWeek)((int)startDay + 1); dayEnum < endDay; dayEnum = (ShortDayOfWeek)((int)dayEnum + 1))
						days.Add(dayEnum);
					days.Add(endDay);
				}
			}
			return days;
		}

		private (DateTime StartTime, DateTime EndTime, int AddDayToEnd) GetScheduleItemStartAndEndTimes(string restaurantName, string timesPart, string scheduleStr)
		{
			var startEndTimes = Regex.Split(timesPart, "-");
			if (startEndTimes == null || startEndTimes.Length != 2)
				throw new Exception($"Restaurant '{restaurantName}' missing start or end time in '{scheduleStr}'");

			var startTime = DateTime.Parse(startEndTimes[0], CultureInfo.InvariantCulture);
			var endTime = DateTime.Parse(startEndTimes[1], CultureInfo.InvariantCulture);
			var addDayToEnd = 0;
			if (startTime > endTime)
			{
				_log.Debug($"**** Start > end {restaurantName} {startTime:h:mm tt} > {endTime:h:mm tt}, adjusting end to be on next day.");
				addDayToEnd = 1;
			}

			return (StartTime : startTime, EndTime: endTime, AddDayToEnd: addDayToEnd);
		}

		private void AddScheduleEntry(IList<RestOpen> openTimes, ShortDayOfWeek day, string restaurantName,
			DateTime startTime, DateTime endTime, int addDayToEnd)
		{
			var start = new DateTime(
					RestOpen.CalendarYear,
					RestOpen.CalendarMonth,
					RestOpen.CalendarFirstDayOfWeek + (int)day,
					startTime.Hour,
					startTime.Minute,
					0,
					DateTimeKind.Local);
			var end = new DateTime(
					RestOpen.CalendarYear,
					RestOpen.CalendarMonth,
					RestOpen.CalendarFirstDayOfWeek + (int)day + addDayToEnd,
					endTime.Hour,
					endTime.Minute,
					0,
					DateTimeKind.Local);

			// Handle edge case where date range on last day of the week rolls over into next week
			// Split schedule entry that spans week to last-day entry and first-day entry
			if (end > new DateTime(RestOpen.CalendarYear, RestOpen.CalendarMonth, RestOpen.CalendarFirstDayOfWeek) + TimeSpan.FromDays(7))
			{
				var rollOverStart = new DateTime(
						RestOpen.CalendarYear,
						RestOpen.CalendarMonth,
						RestOpen.CalendarFirstDayOfWeek,
						0,
						0,
						0,
						DateTimeKind.Local);

				var rollOverEnd = new DateTime(
						RestOpen.CalendarYear,
						RestOpen.CalendarMonth,
						RestOpen.CalendarFirstDayOfWeek,
						endTime.Hour,
						endTime.Minute,
						0,
						DateTimeKind.Local);

				openTimes.Add(new RestOpen
				{
					Name = restaurantName,
					Start = rollOverStart,
					End = rollOverEnd
				});
				LogEntry(openTimes.Last());

				// Adjust end to stop at the end of the week
				end = new DateTime(
						RestOpen.CalendarYear,
						RestOpen.CalendarMonth,
						RestOpen.CalendarFirstDayOfWeek + (int)day,
						23,
						59,
						59,
						DateTimeKind.Local);
			}

			openTimes.Add(new RestOpen
			{
				Name = restaurantName,
				Start = start,
				End = end
			});
			LogEntry(openTimes.Last());
		}

		private void LogEntry(RestOpen entry)
		{
			var startDayOfWeek = entry.Start.DayOfWeek.ToString();
			var endDayOfWeek = entry.End.DayOfWeek.ToString();
			_log.Debug($"'{entry.Name}', {startDayOfWeek} {entry.Start:h:mm tt} -- {endDayOfWeek} {entry.End:h:mm tt}");
		}
	}
}
