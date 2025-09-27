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
/// Strategy that trades hammer and hanging man candlestick patterns confirmed by the Money Flow Index.
/// </summary>
public class AhHmMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _hammerEntryThreshold;
	private readonly StrategyParam<decimal> _hangingEntryThreshold;
	private readonly StrategyParam<decimal> _mfiUpperExitLevel;
	private readonly StrategyParam<decimal> _mfiLowerExitLevel;

	private decimal? _previousMfi;
	private decimal? _previousSma;
	private ICandleMessage _previousCandle;

	/// <summary>
	/// Initializes a new instance of the <see cref="AhHmMfiStrategy"/> class.
	/// </summary>
	public AhHmMfiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for pattern detection", "General");

		_mfiPeriod = Param(nameof(MfiPeriod), 47)
			.SetDisplay("MFI Period", "Lookback period for the Money Flow Index", "Indicator")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 5)
			.SetDisplay("MA Period", "Length of the moving average used for trend detection", "Indicator")
			.SetCanOptimize(true);

		_hammerEntryThreshold = Param(nameof(HammerEntryThreshold), 40m)
			.SetDisplay("Hammer MFI Threshold", "Maximum MFI value allowed to buy after a hammer", "Signals")
			.SetRange(10m, 60m)
			.SetCanOptimize(true);

		_hangingEntryThreshold = Param(nameof(HangingEntryThreshold), 60m)
			.SetDisplay("Hanging Man MFI Threshold", "Minimum MFI value required to sell after a hanging man", "Signals")
			.SetRange(40m, 90m)
			.SetCanOptimize(true);

		_mfiUpperExitLevel = Param(nameof(MfiUpperExitLevel), 70m)
			.SetDisplay("Upper Exit Level", "MFI level that triggers exits when crossed upward", "Risk")
			.SetRange(50m, 90m)
			.SetCanOptimize(true);

		_mfiLowerExitLevel = Param(nameof(MfiLowerExitLevel), 30m)
			.SetDisplay("Lower Exit Level", "MFI level that triggers exits when crossed downward", "Risk")
			.SetRange(10m, 50m)
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Type of candles to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period for the Money Flow Index indicator.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Length of the moving average used to approximate the original trend filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Maximum MFI value allowed to confirm hammer patterns.
	/// </summary>
	public decimal HammerEntryThreshold
	{
		get => _hammerEntryThreshold.Value;
		set => _hammerEntryThreshold.Value = value;
	}

	/// <summary>
	/// Minimum MFI value required to confirm hanging man patterns.
	/// </summary>
	public decimal HangingEntryThreshold
	{
		get => _hangingEntryThreshold.Value;
		set => _hangingEntryThreshold.Value = value;
	}

	/// <summary>
	/// Upper MFI level that forces the strategy to exit positions.
	/// </summary>
	public decimal MfiUpperExitLevel
	{
		get => _mfiUpperExitLevel.Value;
		set => _mfiUpperExitLevel.Value = value;
	}

	/// <summary>
	/// Lower MFI level that forces the strategy to exit positions.
	/// </summary>
	public decimal MfiLowerExitLevel
	{
		get => _mfiLowerExitLevel.Value;
		set => _mfiLowerExitLevel.Value = value;
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

		_previousMfi = null;
		_previousSma = null;
		_previousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var mfi = new MoneyFlowIndex
		{
			Length = MfiPeriod
		};

		var average = new SimpleMovingAverage
		{
			Length = MaPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(mfi, average, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue mfiValue, IIndicatorValue averageValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!mfiValue.IsFinal || !averageValue.IsFinal)
			return;

		var currentMfi = mfiValue.GetValue<decimal>();
		var currentSma = averageValue.GetValue<decimal>();

		if (_previousMfi is decimal previousMfi)
		{
			var crossedAboveLower = previousMfi < MfiLowerExitLevel && currentMfi > MfiLowerExitLevel;
			var crossedAboveUpper = previousMfi < MfiUpperExitLevel && currentMfi > MfiUpperExitLevel;
			var crossedBelowLower = previousMfi > MfiLowerExitLevel && currentMfi < MfiLowerExitLevel;

			if (Position < 0 && (crossedAboveLower || crossedAboveUpper))
			{
				ClosePosition();
			}
			else if (Position > 0 && (crossedAboveUpper || crossedBelowLower))
			{
				ClosePosition();
			}
			else if (Position == 0 && _previousCandle is not null && _previousSma is decimal previousSma)
			{
				var hammer = IsHammer(candle, _previousCandle, previousSma);
				var hangingMan = IsHangingMan(candle, _previousCandle, previousSma);

				if (hammer && currentMfi <= HammerEntryThreshold)
				{
					BuyMarket();
				}
				else if (hangingMan && currentMfi >= HangingEntryThreshold)
				{
					SellMarket();
				}
			}
		}

		_previousMfi = currentMfi;
		_previousSma = currentSma;
		_previousCandle = candle;
	}

	private static bool IsHammer(ICandleMessage current, ICandleMessage previous, decimal previousSma)
	{
		var range = current.HighPrice - current.LowPrice;
		if (range <= 0m)
			return false;

		var bodyTop = Math.Max(current.OpenPrice, current.ClosePrice);
		var bodyBottom = Math.Min(current.OpenPrice, current.ClosePrice);
		var body = bodyTop - bodyBottom;

		var bodyInUpperThird = bodyBottom >= current.HighPrice - range / 3m;
		var lowerShadow = bodyBottom - current.LowPrice;
		var hasLongLowerShadow = lowerShadow >= body;

		var previousMidPoint = (previous.HighPrice + previous.LowPrice) / 2m;
		var downTrend = previousMidPoint < previousSma;
		var gapDown = current.ClosePrice < previous.ClosePrice && current.OpenPrice < previous.OpenPrice;
		var bullishClose = current.ClosePrice >= current.OpenPrice;

		return downTrend && bodyInUpperThird && hasLongLowerShadow && gapDown && bullishClose;
	}

	private static bool IsHangingMan(ICandleMessage current, ICandleMessage previous, decimal previousSma)
	{
		var range = current.HighPrice - current.LowPrice;
		if (range <= 0m)
			return false;

		var bodyTop = Math.Max(current.OpenPrice, current.ClosePrice);
		var bodyBottom = Math.Min(current.OpenPrice, current.ClosePrice);
		var body = bodyTop - bodyBottom;

		var bodyInUpperThird = bodyBottom >= current.HighPrice - range / 3m;
		var lowerShadow = bodyBottom - current.LowPrice;
		var hasLongLowerShadow = lowerShadow >= body;

		var previousMidPoint = (previous.HighPrice + previous.LowPrice) / 2m;
		var upTrend = previousMidPoint > previousSma;
		var gapUp = current.ClosePrice > previous.ClosePrice && current.OpenPrice > previous.OpenPrice;
		var bearishClose = current.ClosePrice <= current.OpenPrice;

		return upTrend && bodyInUpperThird && hasLongLowerShadow && gapUp && bearishClose;
	}
}

