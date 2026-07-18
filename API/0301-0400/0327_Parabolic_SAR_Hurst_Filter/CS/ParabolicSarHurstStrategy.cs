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
/// Parabolic SAR with Hurst Filter Strategy.
/// Enters a position when price crosses SAR and Hurst exponent indicates a persistent trend.
/// </summary>
public class ParabolicSarHurstStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarAccelerationFactor;
	private readonly StrategyParam<decimal> _sarMaxAccelerationFactor;
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalCooldownBars;

	private ParabolicSar _parabolicSar = null!;
	private HurstExponent _hurstIndicator = null!;
	private decimal _prevSarValue;
	private decimal _hurstValue;
	private bool? _prevPriceAboveSar;
	private int _cooldownRemaining;

	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarAccelerationFactor
	{
		get => _sarAccelerationFactor.Value;
		set => _sarAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMaxAccelerationFactor
	{
		get => _sarMaxAccelerationFactor.Value;
		set => _sarMaxAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Hurst exponent calculation period.
	/// </summary>
	public int HurstPeriod
	{
		get => _hurstPeriod.Value;
		set => _hurstPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of closed candles to wait before a new SAR crossover entry.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public ParabolicSarHurstStrategy()
	{
		_sarAccelerationFactor = Param(nameof(SarAccelerationFactor), 0.02m)
			.SetRange(0.01m, 0.2m)
			.SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "SAR Settings")
			
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_sarMaxAccelerationFactor = Param(nameof(SarMaxAccelerationFactor), 0.2m)
			.SetRange(0.05m, 0.5m)
			.SetDisplay("SAR Max Acceleration Factor", "Maximum acceleration factor for Parabolic SAR", "SAR Settings")
			
			.SetOptimize(0.1m, 0.3m, 0.05m);

		_hurstPeriod = Param(nameof(HurstPeriod), 100)
			.SetRange(20, 200)
			.SetDisplay("Hurst Period", "Period for Hurst exponent calculation", "Hurst Settings")
			
			.SetOptimize(50, 150, 25);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 4)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new SAR crossover entry", "General");
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

		_parabolicSar = null!;
		_hurstIndicator = null!;
		_prevSarValue = 0;
		_hurstValue = 0.5m;
		_prevPriceAboveSar = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicators
		_parabolicSar = new ParabolicSar
		{
			Acceleration = SarAccelerationFactor,
			AccelerationMax = SarMaxAccelerationFactor
		};

		_hurstIndicator = new HurstExponent
		{
			Length = HurstPeriod
		};

		// Create subscription for candles
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		// Start position protection
		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			DrawIndicator(area, _hurstIndicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var sarValue = _parabolicSar.Process(new CandleIndicatorValue(_parabolicSar, candle));
		var hurstValue = _hurstIndicator.Process(new CandleIndicatorValue(_hurstIndicator, candle));

		if (!_parabolicSar.IsFormed || !_hurstIndicator.IsFormed || sarValue.IsEmpty || hurstValue.IsEmpty)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sarPrice = sarValue.ToDecimal();
		_hurstValue = hurstValue.ToDecimal();
		var currentSarValue = sarPrice;
		var priceAboveSar = candle.ClosePrice > sarPrice;

		if (_prevPriceAboveSar is null || _prevSarValue == 0)
		{
			_prevSarValue = currentSarValue;
			_prevPriceAboveSar = priceAboveSar;
			return;
		}

		if (_hurstValue > 0.55m)
		{
			var bullishCross = !_prevPriceAboveSar.Value && priceAboveSar;
			var bearishCross = _prevPriceAboveSar.Value && !priceAboveSar;

			if (_cooldownRemaining == 0 && bullishCross && Position <= 0)
			{
				BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (_cooldownRemaining == 0 && bearishCross && Position >= 0)
			{
				SellMarket(Volume + (Position > 0 ? Math.Abs(Position) : 0m));
				_cooldownRemaining = SignalCooldownBars;
			}
		}
		else
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}

		_prevSarValue = currentSarValue;
		_prevPriceAboveSar = priceAboveSar;
	}
}
