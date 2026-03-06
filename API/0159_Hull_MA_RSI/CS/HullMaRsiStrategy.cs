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
/// Implementation of strategy - Hull Moving Average + RSI.
/// Buy when HMA is rising and RSI is below 30 (oversold).
/// Sell when HMA is falling and RSI is above 70 (overbought).
/// </summary>
public class HullMaRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _hmaValue;
	private decimal _prevHmaValue;
	private int _cooldown;

	/// <summary>
	/// Hull Moving Average period.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
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
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initialize <see cref="HullMaRsiStrategy"/>.
	/// </summary>
	public HullMaRsiStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Period for Hull Moving Average", "HMA Parameters");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for Relative Strength Index", "RSI Parameters");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetRange(1, 100)
			.SetDisplay("RSI Oversold", "RSI level to consider market oversold", "RSI Parameters");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetRange(1, 100)
			.SetDisplay("RSI Overbought", "RSI level to consider market overbought", "RSI Parameters");

		_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Absolute))
			.SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 130)
			.SetRange(5, 500)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");

		_prevHmaValue = 0;
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

			_hmaValue = 0;
			_prevHmaValue = 0;
			_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
			base.OnStarted2(time);

			// Create indicators
			var hma = new HullMovingAverage { Length = HmaPeriod };
			var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

			// Setup candle subscription
			var subscription = SubscribeCandles(CandleType);
		
		// Store HMA value in field, process logic on RSI callback.
		subscription.Bind(hma, OnHma);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			
			// Create separate area for RSI
			var rsiArea = CreateChartArea();
			if (rsiArea != null)
			{
				DrawIndicator(rsiArea, rsi);
			}
			
			DrawOwnTrades(area);
		}

	}

	private void OnHma(ICandleMessage candle, decimal hmaValue)
	{
		_hmaValue = hmaValue;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_hmaValue == 0)
			return;

		if (_prevHmaValue == 0)
		{
			_prevHmaValue = _hmaValue;
			return;
		}

		// Determine if HMA is rising or falling
		var isHmaRising = _prevHmaValue != 0 && _hmaValue > _prevHmaValue;
		var isHmaFalling = _prevHmaValue != 0 && _hmaValue < _prevHmaValue;

		LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, " +
			   $"HMA: {_hmaValue}, Previous HMA: {_prevHmaValue}, " +
			   $"HMA Rising: {isHmaRising}, HMA Falling: {isHmaFalling}, " +
			   $"RSI: {rsiValue}");

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevHmaValue = _hmaValue;
			return;
		}

		// Trading rules
		if (isHmaRising && rsiValue < RsiOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
			LogInfo($"Buy signal: HMA rising and RSI oversold ({rsiValue} < {RsiOversold}).");
		}
		else if (isHmaFalling && rsiValue > RsiOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
			LogInfo($"Sell signal: HMA falling and RSI overbought ({rsiValue} > {RsiOverbought}).");
		}
		// Exit conditions based on HMA direction change
		else if (isHmaFalling && Position > 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
			LogInfo($"Exit long: HMA started falling. Position: {Position}");
		}
		else if (isHmaRising && Position < 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
			LogInfo($"Exit short: HMA started rising. Position: {Position}");
		}

		// Update HMA value for next iteration
		_prevHmaValue = _hmaValue;
	}
}
