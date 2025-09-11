using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy that compares the last two daily candles.
/// Closes existing position at the start of a new day and enters according to breakout conditions.
/// </summary>
public class DailyBreakoutDailyShadowStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prev1;
	private ICandleMessage _prev2;

	/// <summary>
	/// Type of candles for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public DailyBreakoutDailyShadowStrategy()
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
	protected override void OnReseted()
	{
		base.OnReseted();

		_prev1 = null;
		_prev2 = null;
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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prev1 != null && _prev2 != null)
		{
			var b1High = Math.Max(_prev1.OpenPrice, _prev1.ClosePrice);
			var b1Low = Math.Min(_prev1.OpenPrice, _prev1.ClosePrice);
			var b2High = Math.Max(_prev2.OpenPrice, _prev2.ClosePrice);
			var b2Low = Math.Min(_prev2.OpenPrice, _prev2.ClosePrice);

			if (Position != 0)
				ClosePosition();

			if (Position == 0)
			{
				var longCond = _prev1.ClosePrice > b2High && _prev1.OpenPrice < b2High;
				var shortCond = _prev1.ClosePrice < b2Low && _prev1.OpenPrice > b2Low;

				if (longCond)
					BuyMarket();
				else if (shortCond)
					SellMarket();
			}
		}

		_prev2 = _prev1;
		_prev1 = candle;
	}
}
