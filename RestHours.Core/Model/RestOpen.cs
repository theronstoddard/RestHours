using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestHours.Core.Model
{
	public class RestOpen
	{
		public const int CalendarYear = 2019;
		public const int CalendarMonth = 7;
		public const int CalendarFirstDayOfWeek = 1;

		public string Name { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
	}
}
