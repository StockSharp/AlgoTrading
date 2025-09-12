using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum TimezoneOption
{
	Utc,
	NewYork,
	London,
	Tokyo
}

/// <summary>
/// Demonstrates timezone conversion for candle times.
/// </summary>
public class TimezoneStrategy : Strategy
{
	private readonly StrategyParam<TimezoneOption> _timezone;
	private readonly StrategyParam<DataType> _candleType;

	public TimezoneOption Timezone { get => _timezone.Value; set => _timezone.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimezoneStrategy()
	{
		_timezone = Param(nameof(Timezone), TimezoneOption.Utc)
			.SetDisplay("Timezone", "Target timezone", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var tz = TimeZoneMap[Timezone];
		var local = TimeZoneInfo.ConvertTime(candle.CloseTime.UtcDateTime, tz);
		LogInfo($"Close time in {tz.Id}: {local:yyyy-MM-dd HH:mm}");
	}

	private static readonly Dictionary<TimezoneOption, TimeZoneInfo> TimeZoneMap = new()
	{
		[TimezoneOption.Utc] = TimeZoneInfo.Utc,
		[TimezoneOption.NewYork] = TimeZoneInfo.FindSystemTimeZoneById("America/New_York"),
		[TimezoneOption.London] = TimeZoneInfo.FindSystemTimeZoneById("Europe/London"),
		[TimezoneOption.Tokyo] = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"),
	};
}
