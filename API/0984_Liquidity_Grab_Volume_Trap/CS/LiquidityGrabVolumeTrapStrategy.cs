using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Grab Strategy (Volume Trap).
/// Detects liquidity grabs where price sweeps beyond recent range 
/// then reverses back, indicating a trap.
/// </summary>
public class LiquidityGrabVolumeTrapStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private readonly System.Collections.Generic.List<decimal> _highs = new();
	private readonly System.Collections.Generic.List<decimal> _lows = new();

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LiquidityGrabVolumeTrapStrategy()
	{
		_lookback = Param(nameof(Lookback), 10)
			.SetDisplay("Lookback", "Bars for range", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumeSma = new SimpleMovingAverage { Length = 20 };
		_highs.Clear();
		_lows.Clear();


		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_volumeSma.Process(new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.ServerTime));

		// Track highs/lows for rolling range
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		if (_highs.Count > Lookback + 1) _highs.RemoveAt(0);
		if (_lows.Count > Lookback + 1) _lows.RemoveAt(0);

		if (_highs.Count <= Lookback)
			return;

		// Range from the PREVIOUS lookback bars (excluding current candle)
		var rangeHigh = decimal.MinValue;
		var rangeLow = decimal.MaxValue;
		for (int i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > rangeHigh) rangeHigh = _highs[i];
			if (_lows[i] < rangeLow) rangeLow = _lows[i];
		}

		// Bullish grab: wick swept below prior range low but closed back inside
		var bullGrab = candle.LowPrice < rangeLow && candle.ClosePrice > rangeLow;

		// Bearish grab: wick swept above prior range high but closed back inside
		var bearGrab = candle.HighPrice > rangeHigh && candle.ClosePrice < rangeHigh;

		if (bullGrab && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (bearGrab && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		// Exit: close beyond range on wrong side
		if (Position > 0 && candle.ClosePrice < rangeLow)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && candle.ClosePrice > rangeHigh)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
