using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic Heat Map Strategy.
/// Averages multiple Stochastic oscillators and trades on fast/slow crossovers.
/// </summary>
public class StochasticHeatMapStrategy : Strategy
{
	private readonly StrategyParam<int> _increment;
	private readonly StrategyParam<int> _smoothFast;
	private readonly StrategyParam<int> _smoothSlow;
	private readonly StrategyParam<int> _plotNumber;
	private readonly StrategyParam<bool> _useWaves;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>
	/// Increment between periods for Stochastic calculations.
	/// </summary>
	public int Increment
	{
		get => _increment.Value;
		set => _increment.Value = value;
	}

	/// <summary>
	/// Smoothing length for individual Stochastics.
	/// </summary>
	public int SmoothFast
	{
		get => _smoothFast.Value;
		set => _smoothFast.Value = value;
	}

	/// <summary>
	/// Smoothing length for signal line.
	/// </summary>
	public int SmoothSlow
	{
		get => _smoothSlow.Value;
		set => _smoothSlow.Value = value;
	}

	/// <summary>
	/// Number of Stochastic calculations to average.
	/// </summary>
	public int PlotNumber
	{
		get => _plotNumber.Value;
		set => _plotNumber.Value = value;
	}

	/// <summary>
	/// Increase smoothing for each Stochastic.
	/// </summary>
	public bool UseWaves
	{
		get => _useWaves.Value;
		set => _useWaves.Value = value;
	}

	/// <summary>
	/// Moving average type used for smoothing.
	/// </summary>
	public MaType MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
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
	/// Initializes a new instance of the <see cref="StochasticHeatMapStrategy"/>.
	/// </summary>
	public StochasticHeatMapStrategy()
	{
		_increment = Param(nameof(Increment), 10)
			.SetDisplay("Increment", "Period increment between Stochastic calculations", "Parameters")
			.SetRange(5, 20)
			.SetCanOptimize(true);

		_smoothFast = Param(nameof(SmoothFast), 2)
			.SetDisplay("Smooth Fast", "Smoothing length for individual Stochastics", "Parameters")
			.SetRange(1, 5)
			.SetCanOptimize(true);

		_smoothSlow = Param(nameof(SmoothSlow), 21)
			.SetDisplay("Smooth Slow", "Smoothing length for signal line", "Parameters")
			.SetRange(10, 50)
			.SetCanOptimize(true);

		_plotNumber = Param(nameof(PlotNumber), 28)
			.SetDisplay("Plot Number", "Number of Stochastic calculations to average", "Parameters")
			.SetRange(5, 28)
			.SetCanOptimize(true);

		_useWaves = Param(nameof(UseWaves), false)
			.SetDisplay("Waves", "Increase smoothing for each Stochastic", "Parameters");

		_maType = Param(nameof(MaType), MaType.EMA)
			.SetDisplay("MA Type", "Type of moving average for smoothing", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

		_prevFast = 0;
		_prevSlow = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var heatMap = new StochasticHeatMapIndicator
		{
			Increment = Increment,
			SmoothFast = SmoothFast,
			SmoothSlow = SmoothSlow,
			PlotNumber = PlotNumber,
			UseWaves = UseWaves,
			MaType = MaType
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(heatMap, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, heatMap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hm = (StochasticHeatMapValue)value;

		var fast = hm.Fast;
		var slow = hm.Slow;

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}

/// <summary>
/// Custom indicator calculating Stochastic Heat Map.
/// </summary>
public class StochasticHeatMapIndicator : BaseIndicator<decimal>
{
	public int Increment { get; set; } = 10;
	public int SmoothFast { get; set; } = 2;
	public int SmoothSlow { get; set; } = 21;
	public int PlotNumber { get; set; } = 28;
	public bool UseWaves { get; set; }
	public MaType MaType { get; set; } = MaType.EMA;

	private readonly List<(StochasticOscillator stoch, IIndicator ma)> _items = new();
	private IIndicator _slowMa;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DecimalIndicatorValue(this, default, input.Time);

		if (_items.Count == 0)
		{
			for (var i = 1; i <= PlotNumber; i++)
			{
				var length = i * Increment;
				var smooth = SmoothFast + (UseWaves ? i : 0);

				var stoch = new StochasticOscillator
				{
					K = { Length = length },
					D = { Length = 1 },
				};

				IIndicator ma = MaType switch
				{
					MaType.SMA => new SimpleMovingAverage { Length = smooth },
					MaType.WMA => new WeightedMovingAverage { Length = smooth },
					_ => new ExponentialMovingAverage { Length = smooth },
				};

				_items.Add((stoch, ma));
			}

			_slowMa = MaType switch
			{
				MaType.SMA => new SimpleMovingAverage { Length = SmoothSlow },
				MaType.WMA => new WeightedMovingAverage { Length = SmoothSlow },
				_ => new ExponentialMovingAverage { Length = SmoothSlow },
			};
		}

		decimal sum = 0;

		foreach (var (stoch, ma) in _items)
		{
			var stochVal = stoch.Process(input);
			var stochTyped = (StochasticOscillatorValue)stochVal;
			if (stochTyped.K is not decimal k)
				continue;

			var smoothVal = ma.Process(new DecimalIndicatorValue(ma, k, input.Time));
			sum += smoothVal.ToDecimal();
		}

		var fast = sum / 100m;
		var slowVal = _slowMa.Process(new DecimalIndicatorValue(_slowMa, fast, input.Time));
		var slow = slowVal.ToDecimal();

		return new StochasticHeatMapValue(this, input, fast, slow);
	}
}

/// <summary>
/// Indicator value for <see cref="StochasticHeatMapIndicator"/>.
/// </summary>
public class StochasticHeatMapValue : ComplexIndicatorValue
{
	public StochasticHeatMapValue(IIndicator indicator, IIndicatorValue input, decimal fast, decimal slow)
		: base(indicator, input, (nameof(Fast), fast), (nameof(Slow), slow))
	{
	}

	/// <summary>
	/// Fast line value.
	/// </summary>
	public decimal Fast => (decimal)GetValue(nameof(Fast));

	/// <summary>
	/// Slow line value.
	/// </summary>
	public decimal Slow => (decimal)GetValue(nameof(Slow));
}

/// <summary>
/// Moving average type.
/// </summary>
public enum MaType
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	SMA,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	EMA,

	/// <summary>
	/// Weighted moving average.
	/// </summary>
	WMA
}
