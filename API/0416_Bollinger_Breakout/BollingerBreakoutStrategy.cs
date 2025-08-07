namespace StockSharp.Samples.Strategies;

using System;
using System.Drawing;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bollinger Breakout Strategy
/// </summary>
public class BollingerBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _aroonLength;
	private readonly StrategyParam<int> _aroonConfirmation;
	private readonly StrategyParam<int> _aroonStop;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _useMA;
	private readonly StrategyParam<bool> _useRSI;
	private readonly StrategyParam<bool> _useAroon;
	private readonly StrategyParam<decimal> _candlePercent;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<bool> _closeEarly;
	private readonly StrategyParam<bool> _useSL;
	private readonly StrategyParam<Unit> _stopValue;

	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;
	private Aroon _aroon;
	private ExponentialMovingAverage _ma;
	
	private decimal? _entryPrice;

	public BollingerBreakoutStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		// Bollinger Bands parameters
		_bbLength = Param(nameof(BBLength), 20)
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands");
		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetDisplay("BB StdDev", "Standard deviation multiplier", "Bollinger Bands");

		// RSI parameters
		_useRSI = Param(nameof(UseRSI), true)
			.SetDisplay("Use RSI", "Enable RSI filter", "RSI Filter");
		_rsiLength = Param(nameof(RSILength), 14)
			.SetDisplay("RSI Length", "RSI period", "RSI Filter");
		_rsiOversold = Param(nameof(RSIOversold), 45)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI Filter");
		_rsiOverbought = Param(nameof(RSIOverbought), 55)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI Filter");

		// Aroon parameters
		_useAroon = Param(nameof(UseAroon), false)
			.SetDisplay("Use Aroon", "Enable Aroon filter", "Aroon Filter");
		_aroonLength = Param(nameof(AroonLength), 288)
			.SetDisplay("Aroon Periods", "Aroon indicator period", "Aroon Filter");
		_aroonConfirmation = Param(nameof(AroonConfirmation), 90)
			.SetDisplay("Aroon Confirmation", "Aroon confirmation level", "Aroon Filter");
		_aroonStop = Param(nameof(AroonStop), 70)
			.SetDisplay("Aroon Stop", "Aroon stop level", "Aroon Filter");

		// Moving Average parameters
		_useMA = Param(nameof(UseMA), true)
			.SetDisplay("Use MA", "Enable Moving Average filter", "Moving Average");
		_maLength = Param(nameof(MALength), 200)
			.SetDisplay("MA Length", "Moving Average period", "Moving Average");

		// Strategy parameters
		_candlePercent = Param(nameof(CandlePercent), 0.3m)
			.SetDisplay("Candle %", "Candle body penetration percentage", "Strategy");
		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long entries", "Enable long positions", "Strategy");
		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short entries", "Enable short positions", "Strategy");
		_closeEarly = Param(nameof(CloseEarly), true)
			.SetDisplay("Close early", "Close position when price touches opposite BB", "Strategy");

		// Stop Loss parameters
		_useSL = Param(nameof(UseSL), true)
			.SetDisplay("Enable SL", "Enable Stop Loss", "Stop Loss");
		_stopValue = Param(nameof(StopValue), new Unit(2, UnitTypes.Percent))
			.SetDisplay("Stop Loss", "Stop loss value", "Stop Loss");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	public int RSILength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int RSIOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public int RSIOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public bool UseRSI
	{
		get => _useRSI.Value;
		set => _useRSI.Value = value;
	}

	public int AroonLength
	{
		get => _aroonLength.Value;
		set => _aroonLength.Value = value;
	}

	public int AroonConfirmation
	{
		get => _aroonConfirmation.Value;
		set => _aroonConfirmation.Value = value;
	}

	public int AroonStop
	{
		get => _aroonStop.Value;
		set => _aroonStop.Value = value;
	}

	public bool UseAroon
	{
		get => _useAroon.Value;
		set => _useAroon.Value = value;
	}

	public int MALength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public bool UseMA
	{
		get => _useMA.Value;
		set => _useMA.Value = value;
	}

	public decimal CandlePercent
	{
		get => _candlePercent.Value;
		set => _candlePercent.Value = value;
	}

	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	public bool CloseEarly
	{
		get => _closeEarly.Value;
		set => _closeEarly.Value = value;
	}

	public bool UseSL
	{
		get => _useSL.Value;
		set => _useSL.Value = value;
	}

	public Unit StopValue
	{
		get => _stopValue.Value;
		set => _stopValue.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[] { (Security, CandleType) };

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		if (UseRSI)
		{
			_rsi = new RelativeStrengthIndex
			{
				Length = RSILength
			};
		}

		if (UseAroon)
		{
			_aroon = new Aroon
			{
				Length = AroonLength
			};
		}

		if (UseMA)
		{
			_ma = new ExponentialMovingAverage
			{
				Length = MALength
			};
		}

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);

		// Bind indicators and start processing
		if (UseRSI && UseAroon && UseMA)
		{
			subscription
				.BindEx(_bollinger, _rsi, _aroon, _ma, OnProcessWithAllIndicators)
				.Start();
		}
		else if (UseRSI && UseMA)
		{
			subscription
				.BindEx(_bollinger, _rsi, _ma, OnProcessWithRsiAndMa)
				.Start();
		}
		else if (UseRSI)
		{
			subscription
				.BindEx(_bollinger, _rsi, OnProcessWithRsi)
				.Start();
		}
		else
		{
			subscription
				.BindEx(_bollinger, OnProcessWithBollingerOnly)
				.Start();
		}

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			if (UseMA)
				DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}

		// Enable protection if Stop Loss is enabled
		if (UseSL)
		{
			StartProtection(new(), StopValue);
		}
	}

	private void OnProcessWithAllIndicators(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue aroonValue, IIndicatorValue maValue)
	{
		ProcessCandle(candle, bollingerValue, rsiValue, aroonValue, maValue);
	}

	private void OnProcessWithRsiAndMa(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue maValue)
	{
		ProcessCandle(candle, bollingerValue, rsiValue, null, maValue);
	}

	private void OnProcessWithRsi(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue)
	{
		ProcessCandle(candle, bollingerValue, rsiValue, null, null);
	}

	private void OnProcessWithBollingerOnly(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		ProcessCandle(candle, bollingerValue, null, null, null);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue = null, IIndicatorValue aroonValue = null, IIndicatorValue maValue = null)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_bollinger.IsFormed)
			return;

		if (UseRSI && !_rsi.IsFormed)
			return;

		if (UseAroon && !_aroon.IsFormed)
			return;

		if (UseMA && !_ma.IsFormed)
			return;

		// Get indicator values
		var bollingerTyped = (BollingerBandsValue)bollingerValue;
		var upper = bollingerTyped.UpBand;
		var lower = bollingerTyped.LowBand;
		var basis = bollingerTyped.MovingAverage;

		var rsiValueDecimal = UseRSI ? rsiValue.ToDecimal() : 50m;
		var aroonUp = UseAroon ? ((AroonValue)aroonValue).Up.Value : 100m;
		var maValueDecimal = UseMA ? maValue.ToDecimal() : candle.ClosePrice;

		// Calculate candle metrics
		var candleSize = candle.HighPrice - candle.LowPrice;
		var buyZone = candleSize * CandlePercent + candle.LowPrice;
		var sellZone = candle.HighPrice - candleSize * CandlePercent;

		// Check filters
		var buyRSIFilter = !UseRSI || rsiValueDecimal < RSIOversold;
		var sellRSIFilter = !UseRSI || rsiValueDecimal > RSIOverbought;
		var buyAroonFilter = !UseAroon || aroonUp > AroonConfirmation;
		var sellAroonFilter = !UseAroon || aroonUp < AroonStop;
		var buyMAFilter = !UseMA || candle.ClosePrice > maValueDecimal;
		var sellMAFilter = !UseMA || candle.ClosePrice < maValueDecimal;

		// Entry conditions
		var buySignal = buyZone < lower && 
						candle.ClosePrice < candle.OpenPrice && 
						buyRSIFilter && 
						buyAroonFilter && 
						buyMAFilter;

		var sellSignal = sellZone > upper && 
						 candle.ClosePrice > candle.OpenPrice && 
						 sellRSIFilter && 
						 sellAroonFilter && 
						 sellMAFilter;

		// Early exit conditions
		if (CloseEarly && Position != 0)
		{
			if (Position > 0 && _entryPrice.HasValue && candle.ClosePrice > _entryPrice.Value)
			{
				// Close long position early if price touches upper band
				if (candle.HighPrice >= upper)
				{
					ClosePosition();
					return;
				}
			}
			else if (Position < 0 && _entryPrice.HasValue && candle.ClosePrice < _entryPrice.Value)
			{
				// Close short position early if price touches lower band
				if (candle.LowPrice <= lower)
				{
					ClosePosition();
					return;
				}
			}
		}

		// Execute trades
		if (ShowLong && buySignal)
		{
			if (Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (ShowShort && sellSignal)
		{
			if (Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_entryPrice = candle.ClosePrice;
			}
		}
	}
}