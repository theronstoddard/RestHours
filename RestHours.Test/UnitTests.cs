using NSubstitute;
using RestHours.Core;
using RestHours.Core.Model;
using System;
using System.Linq;
using log4net;
using Xunit;

namespace RestHours.Test
{
	public class UnitTests
    {
	    [Fact]
	    public void AllDaysInWeek()
	    {
		    var restRepo = new RestaurantRepository(Substitute.For<ILog>());
		    var restJason = "  [ { \"name\": \"Test Restaurant\", \"times\": [\"Mon-Sun 11 am - 10 pm\"]} ]";
		    restRepo.LoadFromJson(restJason);
		    foreach (var day in Enum.GetValues(typeof(ShortDayOfWeek)).Cast<ShortDayOfWeek>())
		    {
			    var schedule = restRepo.Available(day, 12, 0);
				Assert.Equal(1, schedule.Count);
		    }
	    }

	    [Fact]
	    public void RestaurantHasMultipleSchedules()
	    {
		    var restRepo = new RestaurantRepository(Substitute.For<ILog>());
		    var restJason = "[ {\"name\": \"Test Restaurant\", \"times\": [\"Mon-Thu 11 am - 10:30 pm\", \"Fri 11 am - 11 pm\", \"Sat 11:30 am - 11 pm\", \"Sun 4:30 pm - 10:30 pm\"]} ]";
		    restRepo.LoadFromJson(restJason);
		    foreach (var day in new [] {ShortDayOfWeek.Mon, ShortDayOfWeek.Fri, ShortDayOfWeek.Sat, ShortDayOfWeek.Sun})
		    {
			    var schedule = restRepo.Available(day, 17, 0);
			    Assert.Equal(1, schedule.Count);
		    }
	    }

	    [Fact]
	    public void MultipleRestaurantsOverlappingSchedules()
	    {
		    var restRepo = new RestaurantRepository(Substitute.For<ILog>());
		    var restJason = "[" +
		                    "{ \"name\": \"Test Restaurant1\", \"times\": [\"Mon-Tue 5:30 pm - 1 am\"]}," +
							"{ \"name\": \"Test Restaurant2\", \"times\": [\"Tue-Sat 10:00 am - 5:45 pm\"]}" +
		                    "]";
		    restRepo.LoadFromJson(restJason);

		    var schedule = restRepo.Available(ShortDayOfWeek.Tue, 17, 30);
		    Assert.Equal(2, schedule.Count);

		    schedule = restRepo.Available(ShortDayOfWeek.Sun, 17, 30);
		    Assert.Equal(0, schedule.Count);
	    }

		[Fact]
	    public void IncludeMinBeforeEndHour()
	    {
		    var restRepo = new RestaurantRepository(Substitute.For<ILog>());
		    var restJason = "  [ { \"name\": \"Test Restaurant\", \"times\": [\"Mon 11 am - 10 pm\"]} ]";
		    restRepo.LoadFromJson(restJason);

		    var schedule = restRepo.Available(ShortDayOfWeek.Mon, 21, 59);
		    Assert.Equal(1, schedule.Count);
	    }

	    [Fact]
	    public void InclusiveStartHour()
	    {
		    var restRepo = new RestaurantRepository(Substitute.For<ILog>());
		    var restJason = "  [ { \"name\": \"Test Restaurant\", \"times\": [\"Mon 11 am - 10 pm\"]} ]";
		    restRepo.LoadFromJson(restJason);

		    var schedule = restRepo.Available(ShortDayOfWeek.Mon, 11, 0);
			Assert.Equal(1, schedule.Count);
		    Assert.Equal(11, schedule.First().Start.Hour);
	    }

	    [Fact]
	    public void ExclusiveEndHour()
	    {
		    var restRepo = new RestaurantRepository(Substitute.For<ILog>());
		    var restJason = "  [ { \"name\": \"Test Restaurant\", \"times\": [\"Mon 11 am - 10 pm\"]} ]";
		    restRepo.LoadFromJson(restJason);

		    var schedule = restRepo.Available(ShortDayOfWeek.Mon, 22, 0);
		    Assert.Equal(0, schedule.Count);
	    }

	    [Fact]
	    public void WrapToNextDay()
	    {
		    var restRepo = new RestaurantRepository(Substitute.For<ILog>());
		    var restJason = "  [ { \"name\": \"Test Restaurant\", \"times\": [\"Mon-Sun 5:30 pm - 2 am\"]} ]";
		    restRepo.LoadFromJson(restJason);

		    var schedule = restRepo.Available(ShortDayOfWeek.Sun, 1, 0);
		    Assert.Equal(1, schedule.Count);

		    schedule = restRepo.Available(ShortDayOfWeek.Sat, 23, 59);
		    Assert.Equal(1, schedule.Count);
	    }

	    [Fact]
	    public void ScheduleEndsOnDayBoundary()
	    {
		    var restRepo = new RestaurantRepository(Substitute.For<ILog>());
		    var restJason = "  [ { \"name\": \"Test Restaurant\", \"times\": [\"Mon-Sun 5:30 pm - 12 am\"]} ]";
		    restRepo.LoadFromJson(restJason);

		    var schedule = restRepo.Available(ShortDayOfWeek.Sun, 23, 59);
		    Assert.Equal(1, schedule.Count);

		    schedule = restRepo.Available(ShortDayOfWeek.Mon, 0, 0);
		    Assert.Equal(0, schedule.Count);
	    }
    }
}
