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
/// Implementation of strategy - VWAP + CCI.
/// Buy when price is below VWAP and CCI is below -100 (oversold).
/// Sell when price is above VWAP and CCI is above 100 (overbought).
/// </summary>
public class VwapCciStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciOversold;
	private readonly StrategyParam<decimal> _cciOverbought;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private int _cooldown;

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
	/// Initialize <see cref="VwapCciStrategy"/>.
	/// </summary>
	public VwapCciStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters");

		_cciOversold = Param(nameof(CciOversold), -20m)
			.SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters");

		_cciOverbought = Param(nameof(CciOverbought), 20m)
			.SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 120)
			.SetRange(5, 500)
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
	}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
	base.OnStarted2(time);

		// Create indicators
		var vwap = new VolumeWeightedMovingAverage();
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		// Setup candle subscription
		var subscription = SubscribeCandles(CandleType);
		
		// Bind indicators to candles
		subscription
			.Bind(vwap, cci, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			
			// Create separate area for CCI
			var cciArea = CreateChartArea();
			if (cciArea != null)
			{
				DrawIndicator(cciArea, cci);
			}
			
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(ICandleMessage candle, decimal vwapValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Current price
		var price = candle.ClosePrice;
		
		// Determine if price is above or below VWAP
		var isPriceAboveVWAP = price > vwapValue;

		LogInfo($"Candle: {candle.OpenTime}, Close: {price}, " +
			$"VWAP: {vwapValue}, Price > VWAP: {isPriceAboveVWAP}, " +
			$"CCI: {cciValue}");

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Trading rules
		var belowVwap = price <= vwapValue * 1.001m;
		var aboveVwap = price >= vwapValue * 0.999m;

		if (belowVwap && cciValue <= CciOversold && Position == 0)
		{
			// Buy signal - price below VWAP and CCI oversold
			BuyMarket();
			_cooldown = CooldownBars;
			
			LogInfo($"Buy signal: Price below VWAP and CCI oversold ({cciValue} <= {CciOversold}).");
		}
		else if (aboveVwap && cciValue >= CciOverbought && Position == 0)
		{
			// Sell signal - price above VWAP and CCI overbought
			SellMarket();
			_cooldown = CooldownBars;
			
			LogInfo($"Sell signal: Price above VWAP and CCI overbought ({cciValue} >= {CciOverbought}).");
		}
		// Exit conditions
		else if (isPriceAboveVWAP && Position > 0)
		{
			// Exit long position when price crosses above VWAP
			SellMarket();
			_cooldown = CooldownBars;
			LogInfo($"Exit long: Price crossed above VWAP. Position: {Position}");
		}
		else if (!isPriceAboveVWAP && Position < 0)
		{
			// Exit short position when price crosses below VWAP
			BuyMarket();
			_cooldown = CooldownBars;
			LogInfo($"Exit short: Price crossed below VWAP. Position: {Position}");
		}
	}
}
