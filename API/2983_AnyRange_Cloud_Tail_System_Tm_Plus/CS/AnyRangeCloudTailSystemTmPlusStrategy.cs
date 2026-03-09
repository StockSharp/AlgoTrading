using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AnyRange Cloud Tail System TM Plus strategy. Uses Highest/Lowest channel midline crossover.
/// </summary>
public class AnyRangeCloudTailSystemTmPlusStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal? _prevMid;

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

	public AnyRangeCloudTailSystemTmPlusStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Channel lookback period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMid = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMid = null;

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

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var mid = (high + low) / 2m;
		var close = candle.ClosePrice;

		if (_prevMid == null)
		{
			_prevMid = mid;
			return;
		}

		// Close crosses above midline → buy
		if (close > mid && candle.OpenPrice <= _prevMid.Value && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Close crosses below midline → sell
		else if (close < mid && candle.OpenPrice >= _prevMid.Value && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevMid = mid;
	}
}
