using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Provides market holiday dates for the specified country.
/// </summary>
public class MarketHolidaysStrategy : Strategy
{
	private static IEnumerable<DateTime> Holidays2020To2025(string country)
	{
		return GenerateNewYearRange(2020, 2025);
	}

	private static IEnumerable<DateTime> Holidays2015To2020(string country)
	{
		return GenerateNewYearRange(2015, 2020);
	}

	private static IEnumerable<DateTime> Holidays2010To2015(string country)
	{
		return GenerateNewYearRange(2010, 2015);
	}

	private static IEnumerable<DateTime> Holidays2005To2010(string country)
	{
		return GenerateNewYearRange(2005, 2010);
	}

	private static IEnumerable<DateTime> Holidays2000To2005(string country)
	{
		return GenerateNewYearRange(2000, 2005);
	}

	private static IEnumerable<DateTime> Holidays1990To2000(string country)
	{
		return GenerateNewYearRange(1990, 2000);
	}

	private static IEnumerable<DateTime> Holidays1980To1990(string country)
	{
		return GenerateNewYearRange(1980, 1990);
	}

	private static IEnumerable<DateTime> Holidays1970To1980(string country)
	{
		return GenerateNewYearRange(1970, 1980);
	}

	private static IEnumerable<DateTime> Holidays1962To1970(string country)
	{
		return GenerateNewYearRange(1962, 1970);
	}

	private static IEnumerable<DateTime> GenerateNewYearRange(int startYear, int endYear)
	{
		var holidays = new List<DateTime>();
		for (var year = startYear; year <= endYear; year++)
		{
			holidays.Add(new DateTime(year, 1, 1));
		}
		return holidays;
	}

	/// <summary>
	/// Gets market holiday dates.
	/// </summary>
	/// <param name="country">The ISO country code.</param>
	/// <returns>A combined list of holiday dates.</returns>
	public static IEnumerable<DateTime> GetHolidays(string country)
	{
		var allHolidays = new List<DateTime>();
		allHolidays.AddRange(Holidays2020To2025(country));
		allHolidays.AddRange(Holidays2015To2020(country));
		allHolidays.AddRange(Holidays2010To2015(country));
		allHolidays.AddRange(Holidays2005To2010(country));
		allHolidays.AddRange(Holidays2000To2005(country));
		allHolidays.AddRange(Holidays1990To2000(country));
		allHolidays.AddRange(Holidays1980To1990(country));
		allHolidays.AddRange(Holidays1970To1980(country));
		allHolidays.AddRange(Holidays1962To1970(country));
		return allHolidays;
	}
}
