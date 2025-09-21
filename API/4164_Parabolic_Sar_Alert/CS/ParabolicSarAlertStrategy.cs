namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Parabolic SAR alert strategy converted from the MetaTrader expert advisor "pSAR_alert2".
/// Generates informational alerts when the indicator flips relative to the close price and can optionally auto-trade the signals.
/// </summary>
public class ParabolicSarAlertStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _sarAccelerationStep;
	private readonly StrategyParam<decimal> _sarAccelerationMax;
	private readonly StrategyParam<bool> _enableAutoTrading;
	private readonly StrategyParam<decimal> _tradeVolume;

	private bool? _wasSarAbovePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicSarAlertStrategy"/> class.
	/// </summary>
	public ParabolicSarAlertStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate Parabolic SAR reversals.", "General");

		_sarAccelerationStep = Param(nameof(SarAccelerationStep), 0.02m)
			.SetDisplay("SAR Step", "Initial acceleration factor for Parabolic SAR.", "Indicator")
			.SetRange(0.01m, 0.2m)
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.2m, 0.01m);

		_sarAccelerationMax = Param(nameof(SarAccelerationMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration factor for Parabolic SAR.", "Indicator")
			.SetRange(0.05m, 0.6m)
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.6m, 0.05m);

		_enableAutoTrading = Param(nameof(EnableAutoTrading), false)
			.SetDisplay("Enable Auto Trading", "Place market orders when a Parabolic SAR alert appears.", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order volume submitted when auto trading is enabled.", "Trading")
			.SetGreaterThanZero();

		Volume = _tradeVolume.Value;
	}

	/// <summary>
	/// Candle type used for indicator calculations and signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initial acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarAccelerationStep
	{
		get => _sarAccelerationStep.Value;
		set => _sarAccelerationStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarAccelerationMax
	{
		get => _sarAccelerationMax.Value;
		set => _sarAccelerationMax.Value = value;
	}

	/// <summary>
	/// Enables market order execution when Parabolic SAR flips relative to the close price.
	/// </summary>
	public bool EnableAutoTrading
	{
		get => _enableAutoTrading.Value;
		set => _enableAutoTrading.Value = value;
	}

	/// <summary>
	/// Default trading volume for generated market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
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

		_wasSarAbovePrice = null;
		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Keep helper order methods synchronized with the configured trade size.

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarAccelerationStep,
			AccelerationMax = SarAccelerationMax
		};

		_wasSarAbovePrice = null;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		// Work strictly with finished candles to reproduce the MetaTrader timing.
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;

		// Determine whether the Parabolic SAR is above or below the close.
		var isSarAbovePrice = sarValue > closePrice;

		if (_wasSarAbovePrice is null)
		{
			// Store the initial indicator position without producing a signal.
			_wasSarAbovePrice = isSarAbovePrice;
			return;
		}

		if (_wasSarAbovePrice == true && !isSarAbovePrice)
		{
			HandleSignal(Sides.Buy, candle, sarValue);
		}
		else if (_wasSarAbovePrice == false && isSarAbovePrice)
		{
			HandleSignal(Sides.Sell, candle, sarValue);
		}

		// Persist the current SAR position for the next candle.
		_wasSarAbovePrice = isSarAbovePrice;
	}

	private void HandleSignal(Sides signal, ICandleMessage candle, decimal sarValue)
	{
		var directionText = signal == Sides.Buy ? "bullish" : "bearish";

		// Inform the user about the detected Parabolic SAR flip.
		LogInfo("Parabolic SAR flip detected ({0}) on {1:O}. Close={2}, SAR={3}.", directionText, candle.OpenTime, candle.ClosePrice, sarValue);

		if (!EnableAutoTrading)
			return; // Alerts without auto execution reproduce the original expert advisor behavior.

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var tradeVolume = TradeVolume;
		if (tradeVolume <= 0m)
			return;

		if (signal == Sides.Buy)
		{
			var volumeToBuy = tradeVolume;
			if (Position < 0m)
				volumeToBuy += Math.Abs(Position); // Close existing shorts before opening a new long position.

			if (volumeToBuy > 0m)
				BuyMarket(volumeToBuy);
		}
		else
		{
			var volumeToSell = tradeVolume;
			if (Position > 0m)
				volumeToSell += Position; // Close existing longs before opening a new short position.

			if (volumeToSell > 0m)
				SellMarket(volumeToSell);
		}
	}
}
