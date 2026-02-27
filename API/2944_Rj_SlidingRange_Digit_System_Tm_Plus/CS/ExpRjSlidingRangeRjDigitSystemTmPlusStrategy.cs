using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sliding range breakout strategy using Highest/Lowest channel.
/// Enters on breakout above/below the channel, exits on opposite breakout.
/// </summary>
public class ExpRjSlidingRangeRjDigitSystemTmPlusStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal? _prevUpper;
	private decimal? _prevLower;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public ExpRjSlidingRangeRjDigitSystemTmPlusStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Channel lookback", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpper = null;
		_prevLower = null;

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_prevUpper == null || _prevLower == null)
		{
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		// Breakout above previous upper → buy
		if (close > _prevUpper.Value && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Breakdown below previous lower → sell
		else if (close < _prevLower.Value && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevUpper = upper;
		_prevLower = lower;
	}
}
