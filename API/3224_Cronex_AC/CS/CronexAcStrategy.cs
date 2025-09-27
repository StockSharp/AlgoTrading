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
/// Strategy that smooths the Accelerator Oscillator with two moving averages.
/// A bullish crossover opens long positions while bearish crossover opens shorts.
/// </summary>
public class CronexAcStrategy : Strategy
{
	private readonly StrategyParam<CronexMovingAverageTypes> _smoothingType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;

	private decimal?[] _fastHistory = Array.Empty<decimal?>();
	private decimal?[] _slowHistory = Array.Empty<decimal?>();

	/// <summary>
	/// Type of smoothing applied to the Accelerator Oscillator.
	/// </summary>
	public CronexMovingAverageTypes SmoothingType
	{
		get => _smoothingType.Value;
		set => _smoothingType.Value = value;
	}

	/// <summary>
	/// Fast smoothing period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothing period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Shift in bars used to evaluate the signal.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions when bearish signal appears.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions when bullish signal appears.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CronexAcStrategy"/>.
	/// </summary>
	public CronexAcStrategy()
	{
		_smoothingType = Param(nameof(SmoothingType), CronexMovingAverageTypes.Simple)
			.SetDisplay("Smoothing", "Moving average type for smoothing", "Indicators");

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetRange(2, 100)
			.SetDisplay("Fast Period", "Fast smoothing period", "Indicators")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 25)
			.SetRange(2, 150)
			.SetDisplay("Slow Period", "Slow smoothing period", "Indicators")
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetRange(0, 10)
			.SetDisplay("Signal Bar", "Bar shift for signal evaluation", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
			.SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
			.SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading");
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

		_fastHistory = Array.Empty<decimal?>();
		_slowHistory = Array.Empty<decimal?>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bufferSize = Math.Max(2, SignalBar + 2);
		_fastHistory = new decimal?[bufferSize];
		_slowHistory = new decimal?[bufferSize];

		var accelerator = new AcceleratorOscillator();
		var fastMa = CreateMovingAverage(SmoothingType, FastPeriod);
		var slowMa = CreateMovingAverage(SmoothingType, SlowPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(accelerator, fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, accelerator);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal acceleratorValue, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateHistory(fastValue, slowValue);

		var signalIndex = Math.Min(SignalBar, _fastHistory.Length - 2);
		var prevIndex = signalIndex + 1;

		var fastCurrent = _fastHistory[signalIndex];
		var slowCurrent = _slowHistory[signalIndex];
		var fastPrev = _fastHistory[prevIndex];
		var slowPrev = _slowHistory[prevIndex];

		if (fastCurrent is null || slowCurrent is null || fastPrev is null || slowPrev is null)
			return;

		var fastNow = fastCurrent.Value;
		var slowNow = slowCurrent.Value;
		var fastOld = fastPrev.Value;
		var slowOld = slowPrev.Value;

		if (fastNow > slowNow)
		{
			if (EnableLongEntry && fastOld <= slowOld && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				if (volume > 0)
					BuyMarket(volume);
			}

			if (EnableShortExit && Position < 0)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
					BuyMarket(volume);
			}
		}
		else if (fastNow < slowNow)
		{
			if (EnableShortEntry && fastOld >= slowOld && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				if (volume > 0)
					SellMarket(volume);
			}

			if (EnableLongExit && Position > 0)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
					SellMarket(volume);
			}
		}
	}

	private void UpdateHistory(decimal fastValue, decimal slowValue)
	{
		for (var i = _fastHistory.Length - 1; i > 0; i--)
		{
			_fastHistory[i] = _fastHistory[i - 1];
			_slowHistory[i] = _slowHistory[i - 1];
		}

		_fastHistory[0] = fastValue;
		_slowHistory[0] = slowValue;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(CronexMovingAverageTypes type, int length)
	{
		return type switch
		{
			CronexMovingAverageTypes.Simple => new SimpleMovingAverage { Length = length },
			CronexMovingAverageTypes.Exponential => new ExponentialMovingAverage { Length = length },
			CronexMovingAverageTypes.Smoothed => new SmoothedMovingAverage { Length = length },
			CronexMovingAverageTypes.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	public enum CronexMovingAverageTypes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted
	}
}
