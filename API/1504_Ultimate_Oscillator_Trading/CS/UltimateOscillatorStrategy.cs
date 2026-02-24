using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy based on the Ultimate Oscillator.
/// Buys when the oscillator drops below a threshold and exits when price breaks the previous high.
/// </summary>
public class UltimateOscillatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousHigh;

	// Manual UO computation buffers
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();

	public decimal BuyThreshold { get => _buyThreshold.Value; set => _buyThreshold.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UltimateOscillatorStrategy()
	{
		_buyThreshold = Param(nameof(BuyThreshold), 30m)
			.SetNotNegative()
			.SetDisplay("Buy Threshold", "Buy when oscillator below this", "Trading");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI confirmation length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousHigh = 0;
		_highs.Clear();
		_lows.Clear();
		_closes.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		_previousHigh = 0;
		_highs.Clear();
		_lows.Clear();
		_closes.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		_closes.Add(candle.ClosePrice);

		// Keep buffer for longest period (28) + 1
		while (_highs.Count > 30)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
			_closes.RemoveAt(0);
		}

		if (_closes.Count < 29)
		{
			_previousHigh = candle.HighPrice;
			return;
		}

		// Calculate Ultimate Oscillator manually
		// BP = Close - Min(Low, PrevClose)
		// TR = Max(High, PrevClose) - Min(Low, PrevClose)
		var uo = CalculateUO(7, 14, 28);

		if (uo < BuyThreshold && Position <= 0)
		{
			BuyMarket();
		}
		else if (Position > 0 && _previousHigh > 0 && candle.ClosePrice > _previousHigh)
		{
			SellMarket();
		}

		_previousHigh = candle.HighPrice;
	}

	private decimal CalculateUO(int short1, int mid, int long1)
	{
		var idx = _closes.Count - 1;

		decimal bpSumS = 0, trSumS = 0;
		decimal bpSumM = 0, trSumM = 0;
		decimal bpSumL = 0, trSumL = 0;

		for (int i = 0; i < long1; i++)
		{
			var ci = idx - i;
			if (ci < 1) break;

			var prevClose = _closes[ci - 1];
			var bp = _closes[ci] - Math.Min(_lows[ci], prevClose);
			var tr = Math.Max(_highs[ci], prevClose) - Math.Min(_lows[ci], prevClose);

			if (i < short1) { bpSumS += bp; trSumS += tr; }
			if (i < mid) { bpSumM += bp; trSumM += tr; }
			bpSumL += bp; trSumL += tr;
		}

		var avg7 = trSumS != 0 ? bpSumS / trSumS : 0;
		var avg14 = trSumM != 0 ? bpSumM / trSumM : 0;
		var avg28 = trSumL != 0 ? bpSumL / trSumL : 0;

		return 100m * (4m * avg7 + 2m * avg14 + avg28) / 7m;
	}
}
