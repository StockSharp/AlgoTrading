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

using StockSharp.Algo;
using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of strategy - MACD + CCI.
/// Buy when MACD is above Signal line and CCI is below -100 (oversold).
/// Sell when MACD is below Signal line and CCI is above 100 (overbought).
/// </summary>
public class MacdCciStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciOversold;
	private readonly StrategyParam<decimal> _cciOverbought;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private int _cooldown;
	private bool _hasPrevMacdState;
	private bool _prevMacdAboveSignal;

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// CCI oversold level.
	/// </summary>
	public decimal CciOversold
	{
		get => _cciOversold.Value;
		set => _cciOversold.Value = value;
	}

	/// <summary>
	/// CCI overbought level.
	/// </summary>
	public decimal CciOverbought
	{
		get => _cciOverbought.Value;
		set => _cciOverbought.Value = value;
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
	/// Stop-loss value.
	/// </summary>
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type used for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MacdCciStrategy"/>.
	/// </summary>
	public MacdCciStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD Parameters");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD Parameters");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line period for MACD", "MACD Parameters");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters");

		_cciOversold = Param(nameof(CciOversold), -100m)
			.SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters");

		_cciOverbought = Param(nameof(CciOverbought), 100m)
			.SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 320)
			.SetRange(5, 1000)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
			.SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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

		_cooldown = 0;
		_hasPrevMacdState = false;
		_prevMacdAboveSignal = false;
	}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
	base.OnStarted2(time);

		// Create indicators

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		// Setup candle subscription
		var subscription = SubscribeCandles(CandleType);
		
		// Bind indicators to candles
		subscription
			.BindEx(macd, cci, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			
			// Create separate area for CCI
			var cciArea = CreateChartArea();
			if (cciArea != null)
			{
				DrawIndicator(cciArea, cci);
			}
			
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!macdValue.IsFormed || !cciValue.IsFormed)
			return;

		// Note: In this implementation, the MACD and signal values are obtained separately.
		// We need to extract both MACD and signal values to determine crossovers.
		// For demonstration, we'll access these values through a direct call to the indicator.
		// In a proper implementation, we should find a way to get these values through Bind parameter values.
		
		// Get MACD line and Signal line values
		// This approach is not ideal - in a proper implementation, these values should come from the Bind parameters
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdTyped.Macd; // The main MACD line
		var signalLine = macdTyped.Signal; // Signal line
		
		// Determine if MACD is above or below signal line
		var isMacdAboveSignal = macdLine > signalLine;
		var cciDec = cciValue.ToDecimal();

		if (!_hasPrevMacdState)
		{
			_hasPrevMacdState = true;
			_prevMacdAboveSignal = isMacdAboveSignal;
			return;
		}

		LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, " +
			$"MACD: {macdLine}, Signal: {signalLine}, " +
			$"MACD > Signal: {isMacdAboveSignal}, CCI: {cciDec}");

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMacdAboveSignal = isMacdAboveSignal;
			return;
		}

		var crossedUp = !_prevMacdAboveSignal && isMacdAboveSignal;
		var crossedDown = _prevMacdAboveSignal && !isMacdAboveSignal;

		// Trading rules
		if (crossedUp && cciDec < CciOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
			
			LogInfo($"Buy signal: MACD crossed above Signal and CCI oversold ({cciDec} < {CciOversold}).");
		}
		else if (crossedDown && cciDec > CciOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
			
			LogInfo($"Sell signal: MACD crossed below Signal and CCI overbought ({cciDec} > {CciOverbought}).");
		}
		// Exit conditions based on MACD crossovers
		else if (crossedDown && Position > 0)
		{
			// Exit long position when MACD crosses below signal
			SellMarket();
			_cooldown = CooldownBars;
			LogInfo($"Exit long: MACD crossed below Signal. Position: {Position}");
		}
		else if (crossedUp && Position < 0)
		{
			// Exit short position when MACD crosses above signal
			BuyMarket();
			_cooldown = CooldownBars;
			LogInfo($"Exit short: MACD crossed above Signal. Position: {Position}");
		}

		_prevMacdAboveSignal = isMacdAboveSignal;
	}
}
