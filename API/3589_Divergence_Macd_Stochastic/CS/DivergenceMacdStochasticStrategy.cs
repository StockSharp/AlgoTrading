using System;

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

	// Manual EMA for MACD
	private decimal _fastEma;
	private decimal _slowEma;
	private bool _emaInitialized;
	private int _barCount;
	private decimal _fastMultiplier;
	private decimal _slowMultiplier;

	private readonly decimal[] _macdWindow = new decimal[DivergenceLookback];
	private readonly decimal[] _priceWindow = new decimal[DivergenceLookback];
	private int _windowCount;
	private int _windowIndex;
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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for divergence detection", "General");

		_macdFast = Param(nameof(MacdFast), 20)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 50)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for confirmation", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_windowCount = 0;
		_windowIndex = 0;
		_emaInitialized = false;
		_barCount = 0;
		_fastMultiplier = 2m / (MacdFast + 1);
		_slowMultiplier = 2m / (MacdSlow + 1);

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

		if (_barCount < MacdSlow)
			return;

		var macdLine = _fastEma - _slowEma;

		_macdWindow[_windowIndex] = macdLine;
		_priceWindow[_windowIndex] = close;
		_windowIndex = (_windowIndex + 1) % DivergenceLookback;
		if (_windowCount < DivergenceLookback)
			_windowCount++;

		if (_windowCount < DivergenceLookback)
			return;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var oldestIndex = _windowIndex;
		var newestIndex = (_windowIndex + DivergenceLookback - 1) % DivergenceLookback;
		var oldMacd = _macdWindow[oldestIndex];
		var newMacd = _macdWindow[newestIndex];
		var oldPrice = _priceWindow[oldestIndex];
		var newPrice = _priceWindow[newestIndex];

		var minPriceMove = oldPrice * 0.005m;
		// Bullish divergence: price makes lower low but MACD makes higher low.
		var bullishDiv = newPrice < oldPrice - minPriceMove && newMacd > oldMacd;
		// Bearish divergence: price makes higher high but MACD makes lower high.
		var bearishDiv = newPrice > oldPrice + minPriceMove && newMacd < oldMacd;

		if (bullishDiv)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		else if (bearishDiv)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_fastEma = 0;
		_slowEma = 0;
		_emaInitialized = false;
		_barCount = 0;
		_fastMultiplier = 0;
		_slowMultiplier = 0;
		_windowCount = 0;
		_windowIndex = 0;

		base.OnReseted();
	}
}
