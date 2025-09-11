using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy with early entry and MA-based exit.
/// Buys or sells when SAR switches sides relative to price.
/// Exits long positions when price falls below moving average while SAR is above price.
/// </summary>
public class ParabolicSarEarlyBuyMaBasedExitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _accelerationStep;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevIsSarAbovePrice;

	/// <summary>
	/// Initial acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal Acceleration
	{
		get => _acceleration.Value;
		set => _acceleration.Value = value;
	}

	/// <summary>
	/// Increment step for acceleration factor.
	/// </summary>
	public decimal AccelerationStep
	{
		get => _accelerationStep.Value;
		set => _accelerationStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal MaxAcceleration
	{
		get => _maxAcceleration.Value;
		set => _maxAcceleration.Value = value;
	}

	/// <summary>
	/// Period for exit moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicSarEarlyBuyMaBasedExitStrategy"/>.
	/// </summary>
	public ParabolicSarEarlyBuyMaBasedExitStrategy()
	{
		_acceleration = Param(nameof(Acceleration), 0.02m)
			.SetDisplay("Acceleration", "Initial acceleration factor for Parabolic SAR", "SAR Settings")
			.SetRange(0.01m, 0.05m)
			.SetCanOptimize(true);

		_accelerationStep = Param(nameof(AccelerationStep), 0.02m)
			.SetDisplay("Acceleration Step", "Increment step for acceleration factor", "SAR Settings")
			.SetRange(0.01m, 0.05m)
			.SetCanOptimize(true);

		_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
			.SetDisplay("Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "SAR Settings")
			.SetRange(0.1m, 0.5m)
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for exit moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

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
		_prevIsSarAbovePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var parabolicSar = new ParabolicSar
		{
			Acceleration = Acceleration,
			AccelerationStep = AccelerationStep,
			AccelerationMax = MaxAcceleration
		};

		var sma = new SMA { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isSarAbovePrice = sarValue > candle.ClosePrice;

		if (_prevIsSarAbovePrice == null)
		{
			_prevIsSarAbovePrice = isSarAbovePrice;
			return;
		}

		var sarSwitchedBelow = _prevIsSarAbovePrice.Value && !isSarAbovePrice;
		var sarSwitchedAbove = !_prevIsSarAbovePrice.Value && isSarAbovePrice;

		if (sarSwitchedBelow && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Long entry: SAR {sarValue} switched below price {candle.ClosePrice}");
		}
		else if (sarSwitchedAbove && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Short entry: SAR {sarValue} switched above price {candle.ClosePrice}");
		}

		if (sarValue > candle.ClosePrice && candle.ClosePrice < smaValue && Position > 0)
		{
			ClosePosition();
			LogInfo($"Exit long: SAR {sarValue} above price {candle.ClosePrice} and price below MA {smaValue}");
		}

		_prevIsSarAbovePrice = isSarAbovePrice;
	}
}
