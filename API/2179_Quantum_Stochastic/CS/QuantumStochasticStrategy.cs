using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Quantum Stochastic strategy based on Stochastic oscillator thresholds.
/// Buys when %K leaves oversold zone and sells when %K leaves overbought zone.
/// Positions are closed when %K reaches extreme closing levels.
/// </summary>
public class QuantumStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<decimal> _highCloseLevel;
	private readonly StrategyParam<decimal> _lowCloseLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousK;

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing factor.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Lower boundary of overbought zone.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Upper boundary of oversold zone.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Level to close long positions.
	/// </summary>
	public decimal HighCloseLevel
	{
		get => _highCloseLevel.Value;
		set => _highCloseLevel.Value = value;
	}

	/// <summary>
	/// Level to close short positions.
	/// </summary>
	public decimal LowCloseLevel
	{
		get => _lowCloseLevel.Value;
		set => _lowCloseLevel.Value = value;
	}

	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="QuantumStochasticStrategy"/>.
	/// </summary>
	public QuantumStochasticStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Period of %K line", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(50, 150, 10);

		_dPeriod = Param(nameof(DPeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "Period of %D line", "Stochastic")
		.SetCanOptimize(true)
		.SetOptimize(50, 150, 10);

		_slowing = Param(nameof(Slowing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Slowing", "Slowing factor", "Stochastic");

		_highLevel = Param(nameof(HighLevel), 80m)
		.SetDisplay("High Level", "Bottom of overbought zone", "Levels");

		_lowLevel = Param(nameof(LowLevel), 20m)
		.SetDisplay("Low Level", "Top of oversold zone", "Levels");

		_highCloseLevel = Param(nameof(HighCloseLevel), 90m)
		.SetDisplay("High Close Level", "Level to close long", "Levels");

		_lowCloseLevel = Param(nameof(LowCloseLevel), 10m)
		.SetDisplay("Low Close Level", "Level to close short", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_previousK = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			KPeriod = KPeriod,
			DPeriod = DPeriod,
			Slowing = Slowing
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var stoch = (StochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal kValue)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousK = kValue;
			return;
		}

		// Entry conditions
		if (_previousK < LowLevel && kValue >= LowLevel && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (_previousK > HighLevel && kValue <= HighLevel && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		// Exit conditions
		if (Position > 0 && kValue >= HighCloseLevel)
			SellMarket(Position);

		if (Position < 0 && kValue <= LowCloseLevel)
			BuyMarket(-Position);

		_previousK = kValue;
	}
}
