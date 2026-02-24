using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Divergence MACD Stochastic" MetaTrader expert.
/// Uses MACD histogram divergence (price vs histogram) with RSI confirmation.
/// MACD is computed manually from two EMAs.
/// </summary>
public class DivergenceMacdStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _rsiPeriod;

	private RelativeStrengthIndex _rsi;

	// Manual EMA for MACD
	private decimal _fastEma;
	private decimal _slowEma;
	private bool _emaInitialized;
	private int _barCount;
	private decimal _fastMultiplier;
	private decimal _slowMultiplier;

	private readonly Queue<decimal> _macdHistory = new();
	private readonly Queue<decimal> _priceHistory = new();
	private const int DivergenceLookback = 10;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public DivergenceMacdStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for divergence detection", "General");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for confirmation", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_macdHistory.Clear();
		_priceHistory.Clear();
		_emaInitialized = false;
		_barCount = 0;
		_fastMultiplier = 2m / (MacdFast + 1);
		_slowMultiplier = 2m / (MacdSlow + 1);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		_barCount++;

		// Manual EMA computation
		if (!_emaInitialized)
		{
			_fastEma = close;
			_slowEma = close;
			_emaInitialized = true;
		}
		else
		{
			_fastEma = close * _fastMultiplier + _fastEma * (1 - _fastMultiplier);
			_slowEma = close * _slowMultiplier + _slowEma * (1 - _slowMultiplier);
		}

		if (_barCount < MacdSlow || !_rsi.IsFormed)
			return;

		var macdLine = _fastEma - _slowEma;

		_macdHistory.Enqueue(macdLine);
		_priceHistory.Enqueue(close);
		while (_macdHistory.Count > DivergenceLookback)
		{
			_macdHistory.Dequeue();
			_priceHistory.Dequeue();
		}

		if (_macdHistory.Count < DivergenceLookback)
			return;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var macdArr = _macdHistory.ToArray();
		var priceArr = _priceHistory.ToArray();
		var oldMacd = macdArr[0];
		var newMacd = macdArr[macdArr.Length - 1];
		var oldPrice = priceArr[0];
		var newPrice = priceArr[priceArr.Length - 1];

		// Bullish divergence: price makes lower low but MACD makes higher low + RSI oversold
		var bullishDiv = newPrice < oldPrice && newMacd > oldMacd && rsiValue < 40;
		// Bearish divergence: price makes higher high but MACD makes lower high + RSI overbought
		var bearishDiv = newPrice > oldPrice && newMacd < oldMacd && rsiValue > 60;

		if (bullishDiv)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (bearishDiv)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}
	}
}
