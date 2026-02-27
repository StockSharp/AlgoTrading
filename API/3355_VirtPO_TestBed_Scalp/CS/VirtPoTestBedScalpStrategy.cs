namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Virtual Pending Order Scalp strategy: ATR-based breakout scalper.
/// Buys when price breaks above recent high + ATR offset. Sells on break below low - ATR.
/// </summary>
public class VirtPoTestBedScalpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public VirtPoTestBedScalpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for breakout offset", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// Breakout above previous high + small ATR offset
		if (close > _prevHigh + atr * 0.1m && Position <= 0)
			BuyMarket();
		// Breakout below previous low - small ATR offset
		else if (close < _prevLow - atr * 0.1m && Position >= 0)
			SellMarket();

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
