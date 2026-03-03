using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining ADX for trend strength and manual Stochastic %K for entry timing.
/// Enters when ADX shows strong trend and Stochastic is oversold/overbought.
/// </summary>
public class AdxStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _adxValue;
	private int _cooldown;
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private const int StochPeriod = 14;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for strong trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public AdxStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("ADX Period", "Period of the ADX indicator", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level for strong trend", "Indicators");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stochastic Oversold", "Level considered oversold", "Indicators");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stochastic Overbought", "Level considered overbought", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_adxValue = 0;
		_cooldown = 0;
		_highs.Clear();
		_lows.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var adxArea = CreateChartArea();
			if (adxArea != null)
				DrawIndicator(adxArea, adx);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Extract ADX value
		if (adxValue is AverageDirectionalIndexValue typedAdx && typedAdx.MovingAverage is decimal adx)
			_adxValue = adx;

		if (_adxValue == 0)
			return;

		// Track highs/lows for manual stochastic
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var maxBuf = StochPeriod * 2;
		if (_highs.Count > maxBuf)
		{
			_highs.RemoveRange(0, _highs.Count - maxBuf);
			_lows.RemoveRange(0, _lows.Count - maxBuf);
		}

		if (_highs.Count < StochPeriod)
			return;

		// Manual Stochastic %K
		var start = _highs.Count - StochPeriod;
		var highestHigh = decimal.MinValue;
		var lowestLow = decimal.MaxValue;
		for (var i = start; i < _highs.Count; i++)
		{
			if (_highs[i] > highestHigh) highestHigh = _highs[i];
			if (_lows[i] < lowestLow) lowestLow = _lows[i];
		}
		var diff = highestHigh - lowestLow;
		if (diff == 0) return;
		var stochK = 100m * (candle.ClosePrice - lowestLow) / diff;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var strongTrend = _adxValue > AdxThreshold;

		// Long: strong trend + stochastic oversold
		if (strongTrend && stochK < StochOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Short: strong trend + stochastic overbought
		else if (strongTrend && stochK > StochOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit when trend weakens
		if (!strongTrend && Position > 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (!strongTrend && Position < 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
