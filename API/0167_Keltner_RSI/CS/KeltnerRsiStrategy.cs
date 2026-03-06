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
/// Strategy combining Keltner Channels and RSI indicators.
/// Looks for mean reversion opportunities when price touches channel boundaries
/// and RSI confirms oversold/overbought conditions.
/// </summary>
public class KeltnerRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverboughtLevel;
	private readonly StrategyParam<decimal> _rsiOversoldLevel;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// EMA period for Keltner Channels.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for Keltner Channels.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for Keltner Channels width.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Period for RSI calculation.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverboughtLevel
	{
		get => _rsiOverboughtLevel.Value;
		set => _rsiOverboughtLevel.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversoldLevel
	{
		get => _rsiOversoldLevel.Value;
		set => _rsiOversoldLevel.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	// Fields for indicators
	private ExponentialMovingAverage _ema;
	private ATR _atr;
	private RSI _rsi;
	private int _cooldown;

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public KeltnerRsiStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA in Keltner Channels", "Indicators")
			
			.SetOptimize(10, 30, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR in Keltner Channels", "Indicators")
			
			.SetOptimize(7, 21, 7);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicators")
			
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
			
			.SetOptimize(7, 21, 7);

		_rsiOverboughtLevel = Param(nameof(RsiOverboughtLevel), 60m)
			.SetRange(50, 90)
			.SetDisplay("RSI Overbought", "RSI level considered overbought", "Trading Levels")
			
			.SetOptimize(65, 80, 5);

		_rsiOversoldLevel = Param(nameof(RsiOversoldLevel), 40m)
			.SetRange(10, 50)
			.SetDisplay("RSI Oversold", "RSI level considered oversold", "Trading Levels")
			
			.SetOptimize(20, 35, 5);

		_cooldownBars = Param(nameof(CooldownBars), 120)
			.SetRange(5, 500)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
			
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_ema = null;
		_atr = null;
		_rsi = null;
		_cooldown = 0;
	}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
	base.OnStarted2(time);

		// Create indicators
		_ema = new EMA { Length = EmaPeriod };
		_atr = new ATR { Length = AtrPeriod };
		_rsi = new RSI { Length = RsiPeriod };

		// Create subscription
		var subscription = SubscribeCandles(CandleType);

		// Use WhenCandlesFinished to process candles manually
		subscription
			.Bind(_ema, _atr, _rsi, ProcessCandle)
			.Start();

		// Setup chart if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			
			// Add indicators to chart
			DrawIndicator(area, _ema);
			
			// Create second area for RSI
			var rsiArea = CreateChartArea();
			DrawIndicator(rsiArea, _rsi);
			
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue, decimal rsiValue)
	{
		// Skip if indicators are not formed yet
		if (!_ema.IsFormed || !_atr.IsFormed || !_rsi.IsFormed)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate Keltner Channels
		var upperBand = emaValue + (atrValue * AtrMultiplier);
		var lowerBand = emaValue - (atrValue * AtrMultiplier);

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Trading logic
		if (candle.ClosePrice < emaValue && rsiValue < 45m && Position == 0)
		{
			// Mean-reversion long in lower zone.
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (candle.ClosePrice > emaValue && rsiValue > 55m && Position == 0)
		{
			// Mean-reversion short in upper zone.
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && candle.ClosePrice >= emaValue && rsiValue > 50)
		{
			// Exit long position when price crosses above EMA (middle band)
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice <= emaValue && rsiValue < 50)
		{
			// Exit short position when price crosses below EMA (middle band)
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
