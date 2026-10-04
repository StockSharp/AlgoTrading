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
/// Strategy based on Parabolic SAR indicator.
/// It enters long position when price is above SAR and short position when price is below SAR.
/// </summary>
public class ParabolicSarTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accelerationFactor;
	private readonly StrategyParam<decimal> _maxAccelerationFactor;
	private readonly StrategyParam<DataType> _candleType;

	// Current state
	private decimal _prevSarValue;
	private bool _prevIsPriceAboveSar;

	/// <summary>
	/// Initial acceleration factor for SAR.
	/// </summary>
	public decimal AccelerationFactor
	{
		get => _accelerationFactor.Value;
		set => _accelerationFactor.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for SAR.
	/// </summary>
	public decimal MaxAccelerationFactor
	{
		get => _maxAccelerationFactor.Value;
		set => _maxAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Parabolic SAR Trend strategy.
	/// </summary>
	public ParabolicSarTrendStrategy()
	{
		_accelerationFactor = Param(nameof(AccelerationFactor), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Acceleration Factor", "Initial acceleration factor for SAR calculation", "Indicators")

			.SetOptimize(0.01m, 0.05m, 0.01m);

		_maxAccelerationFactor = Param(nameof(MaxAccelerationFactor), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Max Acceleration Factor", "Maximum acceleration factor for SAR calculation", "Indicators")

			.SetOptimize(0.1m, 0.5m, 0.1m);

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
		_prevSarValue = 0;
		_prevIsPriceAboveSar = false;

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create Parabolic SAR indicator
		var parabolicSar = new ParabolicSar
		{
			Acceleration = AccelerationFactor,
			AccelerationMax = MaxAccelerationFactor
		};

		// Create subscription and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		// Setup chart visualization if available
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
		if (candle.State != CandleStates.Finished || sarValue <= 0)
			return;

		var isPriceAboveSar = candle.ClosePrice > sarValue;

		// A flip is a change of side between two consecutive finished candles.
		var flipped = _prevSarValue > 0 && isPriceAboveSar != _prevIsPriceAboveSar;

		_prevSarValue = sarValue;
		_prevIsPriceAboveSar = isPriceAboveSar;

		if (!flipped || !IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Volume + Math.Abs(Position);

		if (isPriceAboveSar && Position <= 0)
		{
			BuyMarket(volume);
			LogInfo($"Buy signal: Price {candle.ClosePrice} crossed above SAR {sarValue}");
		}
		else if (!isPriceAboveSar && Position >= 0)
		{
			SellMarket(volume);
			LogInfo($"Sell signal: Price {candle.ClosePrice} crossed below SAR {sarValue}");
		}
	}
}
