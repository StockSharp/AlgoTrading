namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Direct Williams %R histogram strategy with volume-weighted smoothing.
/// Trades only on strong bullish and bearish zone flips.
/// </summary>
public class ExpXwprHistogramVolDirectStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<int> _highLevel1;
	private readonly StrategyParam<int> _lowLevel1;
	private readonly StrategyParam<MovingAverageKinds> _smoothingType;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<VolumeSources> _volumeSource;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _williams;
	private DecimalLengthIndicator _valueSmoother;
	private DecimalLengthIndicator _volumeSmoother;
	private int? _previousZone;
	private int _cooldownRemaining;

	public int WilliamsPeriod { get => _williamsPeriod.Value; set => _williamsPeriod.Value = value; }
	public int HighLevel1 { get => _highLevel1.Value; set => _highLevel1.Value = value; }
	public int LowLevel1 { get => _lowLevel1.Value; set => _lowLevel1.Value = value; }
	public MovingAverageKinds SmoothingType { get => _smoothingType.Value; set => _smoothingType.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public bool EnableLongEntries { get => _enableLongEntries.Value; set => _enableLongEntries.Value = value; }
	public bool EnableShortEntries { get => _enableShortEntries.Value; set => _enableShortEntries.Value = value; }
	public bool EnableLongExits { get => _enableLongExits.Value; set => _enableLongExits.Value = value; }
	public bool EnableShortExits { get => _enableShortExits.Value; set => _enableShortExits.Value = value; }
	public VolumeSources VolumeSource { get => _volumeSource.Value; set => _volumeSource.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpXwprHistogramVolDirectStrategy()
	{
		_williamsPeriod = Param(nameof(WilliamsPeriod), 14)
			.SetRange(5, 200)
			.SetDisplay("Williams %R Period", "Lookback for the Williams %R oscillator", "Indicator");

		_highLevel1 = Param(nameof(HighLevel1), 1)
			.SetRange(-200, 200)
			.SetDisplay("High Level 1", "Bullish threshold", "Indicator");

		_lowLevel1 = Param(nameof(LowLevel1), -1)
			.SetRange(-200, 200)
			.SetDisplay("Low Level 1", "Bearish threshold", "Indicator");

		_smoothingType = Param(nameof(SmoothingType), MovingAverageKinds.Simple)
			.SetDisplay("Smoothing Type", "Moving average type used for smoothing", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 12)
			.SetRange(2, 200)
			.SetDisplay("Smoothing Length", "Moving average length", "Indicator");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow the strategy to open long positions", "Trading Rules");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow the strategy to open short positions", "Trading Rules");

		_enableLongExits = Param(nameof(EnableLongExits), true)
			.SetDisplay("Enable Long Exits", "Allow the strategy to close long positions", "Trading Rules");

		_enableShortExits = Param(nameof(EnableShortExits), true)
			.SetDisplay("Enable Short Exits", "Allow the strategy to close short positions", "Trading Rules");

		_volumeSource = Param(nameof(VolumeSource), VolumeSources.Tick)
			.SetDisplay("Volume Source", "Type of volume used for weighting", "Indicator");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 5)
			.SetRange(1, 200)
			.SetDisplay("Signal Cooldown", "Bars to wait between new entries", "Trading Rules");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetRange(0, 10000)
			.SetDisplay("Stop Loss (ticks)", "Protective stop distance in price steps", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetRange(0, 10000)
			.SetDisplay("Take Profit (ticks)", "Profit target distance in price steps", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for analysis", "General");
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
		_williams = null;
		_valueSmoother = null;
		_volumeSmoother = null;
		_previousZone = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_williams = new WilliamsR { Length = WilliamsPeriod };
		_valueSmoother = CreateMovingAverage(SmoothingType, SmoothingLength);
		_volumeSmoother = CreateMovingAverage(SmoothingType, SmoothingLength);
		_previousZone = null;
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var williamsValue = _williams.Process(candle);
		if (!_williams.IsFormed)
			return;

		var wprValue = williamsValue.ToDecimal();

		// Williams %R ranges from -100 to 0; shift to 0..100
		var normalized = wprValue + 100m;
		var bullishLevel = 80m;
		var bearishLevel = 20m;
		var zone = normalized >= bullishLevel ? 1 : normalized <= bearishLevel ? -1 : 0;

		if (_previousZone == null)
		{
			_previousZone = zone;
			return;
		}

		if (_previousZone.Value != zone && _cooldownRemaining == 0 && Position == 0)
		{
			if (zone > 0 && EnableLongEntries)
			{
				BuyMarket();
				_cooldownRemaining = SignalCooldownBars;
			}
			else if (zone < 0 && EnableShortEntries)
			{
				SellMarket();
				_cooldownRemaining = SignalCooldownBars;
			}
		}

		_previousZone = zone;
	}

	private decimal GetWeightedVolume(ICandleMessage candle)
	{
		if (VolumeSource == VolumeSources.Tick && candle.TotalTicks is int ticks && ticks > 0)
			return ticks;

		return candle.TotalVolume > 0m ? candle.TotalVolume : 1m;
	}

	private static DecimalLengthIndicator CreateMovingAverage(MovingAverageKinds type, int length)
	{
		return type switch
		{
			MovingAverageKinds.Simple => new SMA { Length = length },
			MovingAverageKinds.Exponential => new EMA { Length = length },
			MovingAverageKinds.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageKinds.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageKinds.Hull => new HullMovingAverage { Length = length },
			MovingAverageKinds.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			MovingAverageKinds.DoubleExponential => new DoubleExponentialMovingAverage { Length = length },
			MovingAverageKinds.TripleExponential => new TripleExponentialMovingAverage { Length = length },
			_ => new SMA { Length = length },
		};
	}

	public enum MovingAverageKinds
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
		Hull,
		VolumeWeighted,
		DoubleExponential,
		TripleExponential,
	}

	public enum VolumeSources
	{
		Tick,
		Real,
	}
}
