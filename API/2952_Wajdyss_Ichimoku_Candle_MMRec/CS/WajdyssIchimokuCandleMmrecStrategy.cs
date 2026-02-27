using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku-inspired strategy using Highest/Lowest midline (Kijun-Sen concept).
/// Trades when price crosses above/below the midline.
/// </summary>
public class WajdyssIchimokuCandleMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal? _prevMid;
	private decimal? _prevClose;

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

	public WajdyssIchimokuCandleMmrecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_period = Param(nameof(Period), 26)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Kijun-Sen lookback period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMid = null;
		_prevClose = null;

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

		var mid = (high + low) / 2;
		var close = candle.ClosePrice;

		if (_prevMid == null || _prevClose == null)
		{
			_prevMid = mid;
			_prevClose = close;
			return;
		}

		// Price crosses above midline → buy
		if (_prevClose.Value <= _prevMid.Value && close > mid && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Price crosses below midline → sell
		else if (_prevClose.Value >= _prevMid.Value && close < mid && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevMid = mid;
		_prevClose = close;
	}
}
