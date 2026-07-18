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
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_period = Param(nameof(Period), 26)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Kijun-Sen lookback period", "Indicators");
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
		_prevClose = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMid = null;
		_prevClose = null;

		var middle = new SimpleMovingAverage { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(middle, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, middle);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mid)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMid = mid;
			_prevClose = candle.ClosePrice;
			return;
		}

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
