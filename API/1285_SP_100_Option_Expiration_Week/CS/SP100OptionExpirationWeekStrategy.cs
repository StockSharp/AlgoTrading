using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enters long on Monday of option expiration week and exits on the third Friday of the month.
/// </summary>
public class SP100OptionExpirationWeekStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Candle type used for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref=\"SP100OptionExpirationWeekStrategy\"/> class.
	/// </summary>
	public SP100OptionExpirationWeekStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private static bool IsOptionExpirationWeek(DateTimeOffset time)
	{
		return time.Day >= 15 && time.Day <= 21 && time.DayOfWeek >= DayOfWeek.Monday && time.DayOfWeek <= DayOfWeek.Friday;
	}

	private static bool IsThirdFriday(DateTimeOffset time)
	{
		return time.DayOfWeek == DayOfWeek.Friday && time.Day >= 15 && time.Day <= 21;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime;

		if (time.DayOfWeek == DayOfWeek.Monday && IsOptionExpirationWeek(time) && Position <= 0)
		{
			BuyMarket();
		}
		else if (IsThirdFriday(time) && Position > 0)
		{
			ClosePosition();
		}
	}
}
