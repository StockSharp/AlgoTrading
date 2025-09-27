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
/// Strategy that trades whenever the Parabolic SAR indicator switches sides relative to the close price.
/// The idea mirrors the MQL alert by entering long when SAR drops below price and short when SAR rises above price.
/// </summary>
public class ParabolicSarAlertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accelerationFactor;
	private readonly StrategyParam<decimal> _maxAccelerationFactor;
	private readonly StrategyParam<DataType> _candleType;

	// Previous candle context for detecting a new Parabolic SAR crossover.
	private decimal _previousSar;
	private decimal _previousClose;
	private bool _hasPrevious;

	/// <summary>
	/// Initial acceleration factor for the Parabolic SAR calculation.
	/// </summary>
	public decimal AccelerationFactor
	{
		get => _accelerationFactor.Value;
		set => _accelerationFactor.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for the Parabolic SAR calculation.
	/// </summary>
	public decimal MaxAccelerationFactor
	{
		get => _maxAccelerationFactor.Value;
		set => _maxAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Creates a new instance of <see cref="ParabolicSarAlertStrategy"/>.
	/// </summary>
	public ParabolicSarAlertStrategy()
	{
		_accelerationFactor = Param(nameof(AccelerationFactor), 0.02m)
			.SetDisplay("Acceleration Factor", "Initial step for the Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_maxAccelerationFactor = Param(nameof(MaxAccelerationFactor), 0.2m)
			.SetDisplay("Max Acceleration", "Upper bound for the Parabolic SAR acceleration", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Market data type that feeds the indicator", "General");
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

		_previousSar = 0m;
		_previousClose = 0m;
		_hasPrevious = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var parabolicSar = new ParabolicSar
		{
			Acceleration = AccelerationFactor,
			AccelerationMax = MaxAccelerationFactor
		};

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
		// Process only finished candles to avoid premature signals.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is allowed to trade and all required data is available.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var closePrice = candle.ClosePrice;

		// Capture the very first data point to initialize crossover tracking.
		if (!_hasPrevious)
		{
			_previousSar = sarValue;
			_previousClose = closePrice;
			_hasPrevious = true;
			return;
		}

		// Determine the relative position of the SAR for the current and previous candles.
		var wasSarAbove = _previousSar > _previousClose;
		var isSarAbove = sarValue > closePrice;

		if (wasSarAbove && !isSarAbove && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: SAR {sarValue} switched below close {closePrice}.");
		}
		else if (!wasSarAbove && isSarAbove && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: SAR {sarValue} switched above close {closePrice}.");
		}

		// Store current values for the next candle to evaluate future crossovers.
		_previousSar = sarValue;
		_previousClose = closePrice;
	}
}
