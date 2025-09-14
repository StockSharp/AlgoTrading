namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Stochastic-based strategy with level and crossover modes.
/// Generates contrarian signals when the oscillator leaves extreme zones or crosses its signal line.
/// </summary>
public class StochasticHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<TrendMode> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;
	private bool _initialized;

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
	/// Smoothing value for %K line.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Upper threshold for level-based mode.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for level-based mode.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Signal generation mode.
	/// </summary>
	public TrendMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters with default values.
	/// </summary>
	public StochasticHistogramStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Stochastic %K period", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Stochastic %D period", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Smoothing for %K line", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetDisplay("High Level", "Upper threshold", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(55m, 80m, 5m);

		_lowLevel = Param(nameof(LowLevel), 40m)
			.SetDisplay("Low Level", "Lower threshold", "Stochastic")
			.SetCanOptimize(true)
			.SetOptimize(20m, 45m, 5m);

		_mode = Param(nameof(Mode), TrendMode.Cross)
			.SetDisplay("Mode", "Signal mode", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculation", "General");
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
		_prevK = 0m;
		_prevD = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stoch = new StochasticOscillator
		{
			Length = KPeriod,
			K = { Length = Slowing },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stoch);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (!_initialized)
		{
			_prevK = k;
			_prevD = d;
			_initialized = true;
			return;
		}

		var buySignal = false;
		var sellSignal = false;

		if (Mode == TrendMode.Levels)
		{
			// Buy when %K leaves overbought zone, sell when leaving oversold zone.
			buySignal = _prevK > HighLevel && k <= HighLevel;
			sellSignal = _prevK < LowLevel && k >= LowLevel;
		}
		else
		{
			// Contrarian trades on crossovers between %K and %D lines.
			buySignal = _prevK > _prevD && k <= d;
			sellSignal = _prevK < _prevD && k >= d;
		}

		if (buySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevK = k;
		_prevD = d;
	}
}

/// <summary>
/// Modes of signal generation for the Stochastic strategy.
/// </summary>
public enum TrendMode
{
	/// <summary>
	/// Signals are generated when %K leaves extreme levels.
	/// </summary>
	Levels,

	/// <summary>
	/// Signals are generated on crossovers between %K and %D.
	/// </summary>
	Cross
}
