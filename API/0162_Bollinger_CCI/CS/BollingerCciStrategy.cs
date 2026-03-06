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
/// Implementation of strategy - Bollinger Bands + CCI.
/// Buy when price is below lower Bollinger Band and CCI is below -100 (oversold).
/// Sell when price is above upper Bollinger Band and CCI is above 100 (overbought).
/// </summary>
public class BollingerCciStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciOversold;
	private readonly StrategyParam<decimal> _cciOverbought;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private int _cooldown;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
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
	/// Initialize <see cref="BollingerCciStrategy"/>.
	/// </summary>
	public BollingerCciStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Bollinger Parameters");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Bollinger Parameters");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters");

		_cciOversold = Param(nameof(CciOversold), -100m)
			.SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters");

		_cciOverbought = Param(nameof(CciOverbought), 100m)
			.SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 80)
			.SetRange(5, 500)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Absolute))
			.SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management");

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
		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		// Setup candle subscription
		var subscription = SubscribeCandles(CandleType);
		
		// Bind indicators to candles
		subscription
			.BindEx(bollinger, cci, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			
			// Create separate area for CCI
			var cciArea = CreateChartArea();
			if (cciArea != null)
			{
				DrawIndicator(cciArea, cci);
			}
			
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!bollingerValue.IsFormed || !cciValue.IsFormed)
			return;

		// In this function we receive only the middle band value from the Bollinger Bands indicator
		// We need to calculate the upper and lower bands ourselves or get them directly from the indicator

		// Get Bollinger Bands values from the indicator
		var bb = (BollingerBandsValue)bollingerValue;
		var middleBand = bb.MovingAverage;
		var upperBand = bb.UpBand;
		var lowerBand = bb.LowBand;
		var cciTyped = cciValue.ToDecimal();

		// Current price
		var price = candle.ClosePrice;

		LogInfo($"Candle: {candle.OpenTime}, Close: {price}, " +
			$"Upper Band: {upperBand}, Middle Band: {middleBand}, Lower Band: {lowerBand}, " +
			$"CCI: {cciTyped}");

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Trading rules
		var lowerTouch = price <= lowerBand * 1.002m;
		var upperTouch = price >= upperBand * 0.998m;

		if (lowerTouch && cciTyped < CciOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
			
			LogInfo($"Buy signal: Price below lower Bollinger Band and CCI oversold ({cciTyped} < {CciOversold}).");
		}
		else if (upperTouch && cciTyped > CciOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
			
			LogInfo($"Sell signal: Price above upper Bollinger Band and CCI overbought ({cciTyped} > {CciOverbought}).");
		}
		// Exit conditions
		else if (price > middleBand && Position > 0)
		{
			// Exit long position when price returns to the middle band
			SellMarket();
			_cooldown = CooldownBars;
			LogInfo($"Exit long: Price returned to middle band. Position: {Position}");
		}
		else if (price < middleBand && Position < 0)
		{
			// Exit short position when price returns to the middle band
			BuyMarket();
			_cooldown = CooldownBars;
			LogInfo($"Exit short: Price returned to middle band. Position: {Position}");
		}
	}
}
