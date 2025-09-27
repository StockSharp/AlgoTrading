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
/// High-level StockSharp port of the Manual EA stochastic crossover.
/// </summary>
public class ManualEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<decimal> _volume;

	private StochasticOscillator _stochastic = null!;
	private decimal? _previousSignal;

	/// <summary>
	/// Candle type used to calculate the oscillator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback length for the %K line.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length for the %D signal line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to the %K line.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Overbought threshold for the stochastic %D line.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the stochastic %D line.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points (0 disables protection).
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in points (0 disables protection).
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Order volume that will be sent with each signal.
	/// </summary>
	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initialize default parameters.
	/// </summary>
	public ManualEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for oscillator", "General");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "%K lookback length", "Indicator");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "%D smoothing length", "Indicator");

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Additional smoothing for %K", "Indicator");

		_overboughtLevel = Param(nameof(OverboughtLevel), 90m)
			.SetRange(0m, 100m)
			.SetDisplay("Overbought Level", "Upper threshold for stochastic %D", "Indicator");

		_oversoldLevel = Param(nameof(OversoldLevel), 10m)
			.SetRange(0m, 100m)
			.SetDisplay("Oversold Level", "Lower threshold for stochastic %D", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 100)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 100)
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk Management");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order size for entries", "Trading");
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
		_previousSignal = null;
		_stochastic?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			KPeriod = KPeriod,
			DPeriod = DPeriod,
			Slowing = Slowing
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var stopLossUnit = StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Point) : null;
		var takeProfitUnit = TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Point) : null;

		StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		// Work only with completed candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Ignore partial indicator values.
		if (!indicatorValue.IsFinal)
			return;

		var value = (StochasticOscillatorValue)indicatorValue;
		if (value.D is not decimal currentSignal)
			return;

		var previousSignal = _previousSignal;
		_previousSignal = currentSignal;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (previousSignal is null)
			return;

		var crossedAboveOversold = previousSignal <= OversoldLevel && currentSignal > OversoldLevel;
		var crossedBelowOverbought = previousSignal >= OverboughtLevel && currentSignal < OverboughtLevel;

		if (crossedAboveOversold && Volume > 0m)
		{
			var volume = Volume + Math.Max(0m, -Position);
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (crossedBelowOverbought && Volume > 0m)
		{
			var volume = Volume + Math.Max(0m, Position);
			if (volume > 0m)
				SellMarket(volume);
		}
	}
}

