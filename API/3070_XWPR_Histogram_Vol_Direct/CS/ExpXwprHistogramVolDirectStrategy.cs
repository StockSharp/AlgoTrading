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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "Exp_XWPR_Histogram_Vol_Direct" expert advisor.
/// Combines Williams %R with volume-weighted smoothing to trade color flips of the histogram slope.
/// </summary>
public class ExpXwprHistogramVolDirectStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<int> _highLevel2;
	private readonly StrategyParam<int> _highLevel1;
	private readonly StrategyParam<int> _lowLevel1;
	private readonly StrategyParam<int> _lowLevel2;
	private readonly StrategyParam<MovingAverageKinds> _smoothingType;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<VolumeSources> _volumeSource;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _williams = null!;
	private LengthIndicator<decimal> _valueSmoother = null!;
	private LengthIndicator<decimal> _volumeSmoother = null!;

	private readonly int?[] _directionBuffer = new int?[10];
	private int _directionCount;
	private decimal? _previousSmoothedValue;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Upper multiplier for the extreme bullish band.
	/// </summary>
	public int HighLevel2
	{
		get => _highLevel2.Value;
		set => _highLevel2.Value = value;
	}

	/// <summary>
	/// Upper multiplier for the neutral bullish band.
	/// </summary>
	public int HighLevel1
	{
		get => _highLevel1.Value;
		set => _highLevel1.Value = value;
	}

	/// <summary>
	/// Lower multiplier for the neutral bearish band.
	/// </summary>
	public int LowLevel1
	{
		get => _lowLevel1.Value;
		set => _lowLevel1.Value = value;
	}

	/// <summary>
	/// Lower multiplier for the extreme bearish band.
	/// </summary>
	public int LowLevel2
	{
		get => _lowLevel2.Value;
		set => _lowLevel2.Value = value;
	}

	/// <summary>
	/// Moving average smoothing type.
	/// </summary>
	public MovingAverageKinds SmoothingType
	{
		get => _smoothingType.Value;
		set => _smoothingType.Value = value;
	}

	/// <summary>
	/// Moving average length used for both weighted value and volume streams.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Bar shift used for signal evaluation (1 means the last finished bar).
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Enable entering long positions.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _enableLongEntries.Value;
		set => _enableLongEntries.Value = value;
	}

	/// <summary>
	/// Enable entering short positions.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _enableShortEntries.Value;
		set => _enableShortEntries.Value = value;
	}

	/// <summary>
	/// Enable closing existing long positions.
	/// </summary>
	public bool EnableLongExits
	{
		get => _enableLongExits.Value;
		set => _enableLongExits.Value = value;
	}

	/// <summary>
	/// Enable closing existing short positions.
	/// </summary>
	public bool EnableShortExits
	{
		get => _enableShortExits.Value;
		set => _enableShortExits.Value = value;
	}

	/// <summary>
	/// Volume source used to weight Williams %R values.
	/// </summary>
	public VolumeSources VolumeSources
	{
		get => _volumeSource.Value;
		set => _volumeSource.Value = value;
	}

	/// <summary>
	/// Stop-loss in price steps (0 disables risk management).
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit in price steps (0 disables take-profit).
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations and trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpXwprHistogramVolDirectStrategy"/> class.
	/// </summary>
	public ExpXwprHistogramVolDirectStrategy()
	{
		_williamsPeriod = Param(nameof(WilliamsPeriod), 14)
		.SetRange(5, 200)
		.SetDisplay("Williams %R Period", "Lookback for the Williams %R oscillator", "Indicator")
		.SetCanOptimize(true);

		_highLevel2 = Param(nameof(HighLevel2), 20)
		.SetRange(-200, 200)
		.SetDisplay("High Level 2", "Extreme bullish multiplier", "Indicator");

		_highLevel1 = Param(nameof(HighLevel1), 15)
		.SetRange(-200, 200)
		.SetDisplay("High Level 1", "Neutral bullish multiplier", "Indicator");

		_lowLevel1 = Param(nameof(LowLevel1), -15)
		.SetRange(-200, 200)
		.SetDisplay("Low Level 1", "Neutral bearish multiplier", "Indicator");

		_lowLevel2 = Param(nameof(LowLevel2), -20)
		.SetRange(-200, 200)
		.SetDisplay("Low Level 2", "Extreme bearish multiplier", "Indicator");

		_smoothingType = Param(nameof(SmoothingType), MovingAverageKinds.Simple)
		.SetDisplay("Smoothing Type", "Moving average type used for smoothing", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 12)
		.SetRange(2, 200)
		.SetDisplay("Smoothing Length", "Moving average length", "Indicator")
		.SetCanOptimize(true);

		_signalShift = Param(nameof(SignalShift), 1)
		.SetRange(0, 5)
		.SetDisplay("Signal Shift", "Bar shift used for signal evaluation", "Trading Rules");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Enable Long Entries", "Allow the strategy to open long positions", "Trading Rules");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Enable Short Entries", "Allow the strategy to open short positions", "Trading Rules");

		_enableLongExits = Param(nameof(EnableLongExits), true)
		.SetDisplay("Enable Long Exits", "Allow the strategy to close long positions", "Trading Rules");

		_enableShortExits = Param(nameof(EnableShortExits), true)
		.SetDisplay("Enable Short Exits", "Allow the strategy to close short positions", "Trading Rules");

		_volumeSource = Param(nameof(VolumeSources), VolumeSources.Tick)
		.SetDisplay("Volume Source", "Type of volume used for weighting", "Indicator");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetRange(0, 10000)
		.SetDisplay("Stop Loss (ticks)", "Protective stop distance in price steps", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetRange(0, 10000)
		.SetDisplay("Take Profit (ticks)", "Profit target distance in price steps", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

		Array.Clear(_directionBuffer);
		_directionCount = 0;
		_previousSmoothedValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_williams = new WilliamsR { Length = WilliamsPeriod };
		_valueSmoother = CreateMovingAverage(SmoothingType, SmoothingLength);
		_volumeSmoother = CreateMovingAverage(SmoothingType, SmoothingLength);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_williams, ProcessCandle)
		.Start();

		var stopLossUnit = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Step) : null;
		var takeProfitUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Step) : null;
		if (stopLossUnit != null || takeProfitUnit != null)
		{
			StartProtection(
			stopLoss: stopLossUnit,
			takeProfit: takeProfitUnit);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _williams);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal williamsValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var time = candle.OpenTime;
		decimal volume = VolumeSources == VolumeSources.Tick ? candle.TotalTicks : candle.TotalVolume;
		var weightedValue = (williamsValue + 50m) * volume;

		var valueResult = _valueSmoother.Process(weightedValue, time, true);
		var volumeResult = _volumeSmoother.Process(volume, time, true);

		if (!_valueSmoother.IsFormed || !_volumeSmoother.IsFormed)
		{
			_previousSmoothedValue = null;
			UpdateHistory(null);
			return;
		}

		var smoothedValue = valueResult.ToDecimal();
		var smoothedVolume = volumeResult.ToDecimal();

		var previousSmoothed = _previousSmoothedValue;
		var previousDirection = _directionCount > 0 ? _directionBuffer[0] : (int?)null;

		var direction = DetermineDirection(smoothedValue, previousSmoothed, previousDirection);
		_previousSmoothedValue = smoothedValue;

		UpdateHistory(direction);

		if (!TryGetColors(out var recentColor, out var olderColor))
		return;

		var shouldOpenLong = false;
		var shouldOpenShort = false;
		var shouldCloseLong = false;
		var shouldCloseShort = false;

		if (olderColor == 0)
		{
			if (EnableLongEntries && recentColor == 1)
			{
				shouldOpenLong = true;
			}

			if (EnableShortExits)
			{
				shouldCloseShort = true;
			}
		}
		else if (olderColor == 1)
		{
			if (EnableShortEntries && recentColor == 0)
			{
				shouldOpenShort = true;
			}

			if (EnableLongExits)
			{
				shouldCloseLong = true;
			}
		}

		if (!shouldOpenLong && !shouldOpenShort && !shouldCloseLong && !shouldCloseShort)
		return;

		var high2 = HighLevel2 * smoothedVolume;
		var high1 = HighLevel1 * smoothedVolume;
		var low1 = LowLevel1 * smoothedVolume;
		var low2 = LowLevel2 * smoothedVolume;

		var zone = ClassifyZone(smoothedValue, high2, high1, low1, low2);

		if (shouldCloseLong && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Closing long position because histogram color reverted. Zone={zone}, value={smoothedValue:F2}");
		}

		if (shouldCloseShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Closing short position because histogram color reverted. Zone={zone}, value={smoothedValue:F2}");
		}

		var volumeToTrade = Volume + Math.Abs(Position);

		if (shouldOpenLong && Position <= 0)
		{
			BuyMarket(volumeToTrade);
			LogInfo($"Opening long position on bearish-to-bullish color flip. Zone={zone}, value={smoothedValue:F2}");
		}

		if (shouldOpenShort && Position >= 0)
		{
			SellMarket(volumeToTrade);
			LogInfo($"Opening short position on bullish-to-bearish color flip. Zone={zone}, value={smoothedValue:F2}");
		}
	}

	private void UpdateHistory(int? direction)
	{
		if (direction is null)
		return;

		var shift = SignalShift;
		var required = shift + 2;

		var newCount = Math.Min(_directionCount + 1, _directionBuffer.Length);
		for (var i = newCount - 1; i > 0; i--)
		{
			_directionBuffer[i] = _directionBuffer[i - 1];
		}
		_directionBuffer[0] = direction.Value;
		_directionCount = newCount;
		var maxAllowed = Math.Max(required, 2);
		if (_directionCount > maxAllowed)
		{
			for (var i = maxAllowed; i < _directionCount; i++)
			{
				_directionBuffer[i] = null;
			}
			_directionCount = maxAllowed;
		}
	}

	private bool TryGetColors(out int recentColor, out int olderColor)
	{
		recentColor = default;
		olderColor = default;

		var shift = SignalShift;
		if (_directionCount <= shift + 1)
		{
			return false;
		}

		recentColor = _directionBuffer[shift] ?? 0;
		olderColor = _directionBuffer[shift + 1] ?? 0;
		return true;
	}

	private static int DetermineDirection(decimal current, decimal? previousValue, int? previousDirection)
	{
		if (previousValue is null)
		return previousDirection ?? 0;

		if (current > previousValue.Value)
		return 0;

		if (current < previousValue.Value)
		return 1;

		return previousDirection ?? 0;
	}

	private static string ClassifyZone(decimal value, decimal high2, decimal high1, decimal low1, decimal low2)
	{
		if (value > high2)
		return "Extreme Bullish";

		if (value > high1)
		return "Bullish";

		if (value < low2)
		return "Extreme Bearish";

		if (value < low1)
		return "Bearish";

		return "Neutral";
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageKinds type, int length)
	{
		return type switch
		{
			MovingAverageKinds.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageKinds.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageKinds.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageKinds.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageKinds.Hull => new HullMovingAverage { Length = length },
			MovingAverageKinds.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			MovingAverageKinds.DoubleExponential => new DoubleExponentialMovingAverage { Length = length },
			MovingAverageKinds.TripleExponential => new TripleExponentialMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Supported moving average types.
	/// </summary>
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

	/// <summary>
	/// Volume source used to weight Williams %R values.
	/// </summary>
	public enum VolumeSources
	{
		Tick,
		Real,
	}
}

