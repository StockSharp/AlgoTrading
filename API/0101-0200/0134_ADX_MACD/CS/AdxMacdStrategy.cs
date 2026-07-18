using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining ADX and MACD indicators.
/// Enters on MACD crossover when ADX indicates strong trend.
/// </summary>
public class AdxMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _adxValue;
	private int _cooldown;

	/// <summary>
	/// Data type for candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for ADX calculation.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for trend strength.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
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
	/// Initializes a new instance of the <see cref="AdxMacdStrategy"/>.
	/// </summary>
	public AdxMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("ADX Period", "Period for ADX calculation", "ADX Settings");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX threshold for trend strength", "ADX Settings");

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
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(CandleType);

		// Bind ADX with BindEx (composite indicator)
		subscription.BindEx(adx, OnAdx);

		// Bind MACD for main logic
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var adxArea = CreateChartArea();
			if (adxArea != null)
				DrawIndicator(adxArea, adx);

			var macdArea = CreateChartArea();
			if (macdArea != null)
				DrawIndicator(macdArea, macd);
		}
	}

	private void OnAdx(ICandleMessage candle, IIndicatorValue adxValue)
	{
		var typed = (AverageDirectionalIndexValue)adxValue;
		if (typed.MovingAverage is decimal adx)
			_adxValue = adx;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var strongTrend = _adxValue > AdxThreshold;

		// Entry: strong trend + MACD bullish = buy
		if (strongTrend && macdLine > signalLine && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Entry: strong trend + MACD bearish = sell
		else if (strongTrend && macdLine < signalLine && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit on MACD crossover against position
		if (Position > 0 && macdLine < signalLine)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && macdLine > signalLine)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
