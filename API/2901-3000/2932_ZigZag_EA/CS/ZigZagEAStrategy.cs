using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ZigZag breakout strategy using highest/lowest channels.
/// Buys on breakout above recent high, sells on breakdown below recent low.
/// </summary>
public class ZigZagEAStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _depth;

	private decimal? _prevHigh;
	private decimal? _prevLow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Depth
	{
		get => _depth.Value;
		set => _depth.Value = value;
	}

	public ZigZagEAStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_depth = Param(nameof(Depth), 20)
			.SetGreaterThanZero()
			.SetDisplay("Depth", "Channel lookback period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = null;
		_prevLow = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = null;
		_prevLow = null;

		var highest = new Highest { Length = Depth };
		var lowest = new Lowest { Length = Depth };

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
		{
			_prevHigh = high;
			_prevLow = low;
			return;
		}

		if (_prevHigh == null || _prevLow == null)
		{
			_prevHigh = high;
			_prevLow = low;
			return;
		}

		var close = candle.ClosePrice;

		// Breakout above channel high
		if (close > _prevHigh.Value && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Breakdown below channel low
		else if (close < _prevLow.Value && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevHigh = high;
		_prevLow = low;
	}
}
