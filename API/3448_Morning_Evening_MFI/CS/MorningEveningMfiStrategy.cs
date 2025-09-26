using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Morning and Evening Star strategy with MFI confirmation.
/// Converted from the MetaTrader expert Expert_AMS_ES_MFI.
/// </summary>
public class MorningEveningMfiStrategy : Strategy
{
	private const decimal Tolerance = 0.0000001m;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _bullishMfiThreshold;
	private readonly StrategyParam<decimal> _bearishMfiThreshold;
	private readonly StrategyParam<decimal> _upperExitLevel;
	private readonly StrategyParam<decimal> _lowerExitLevel;

	private ICandleMessage _previousCandle;
	private ICandleMessage _secondPreviousCandle;
	private decimal? _previousMfi;
	private decimal? _secondPreviousMfi;

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the Money Flow Index.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Bullish MFI confirmation threshold.
	/// </summary>
	public decimal BullishMfiThreshold
	{
		get => _bullishMfiThreshold.Value;
		set => _bullishMfiThreshold.Value = value;
	}

	/// <summary>
	/// Bearish MFI confirmation threshold.
	/// </summary>
	public decimal BearishMfiThreshold
	{
		get => _bearishMfiThreshold.Value;
		set => _bearishMfiThreshold.Value = value;
	}

	/// <summary>
	/// Upper exit level for MFI.
	/// </summary>
	public decimal UpperExitLevel
	{
		get => _upperExitLevel.Value;
		set => _upperExitLevel.Value = value;
	}

	/// <summary>
	/// Lower exit level for MFI.
	/// </summary>
	public decimal LowerExitLevel
	{
		get => _lowerExitLevel.Value;
		set => _lowerExitLevel.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MorningEveningMfiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for pattern detection", "General");

		_mfiPeriod = Param(nameof(MfiPeriod), 49)
			.SetNotLess(2)
			.SetDisplay("MFI Period", "Length of the Money Flow Index", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 5);

		_bullishMfiThreshold = Param(nameof(BullishMfiThreshold), 40m)
			.SetDisplay("Bullish MFI", "Threshold that confirms Morning Star entries", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(20m, 50m, 5m);

		_bearishMfiThreshold = Param(nameof(BearishMfiThreshold), 60m)
			.SetDisplay("Bearish MFI", "Threshold that confirms Evening Star entries", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(50m, 80m, 5m);

		_upperExitLevel = Param(nameof(UpperExitLevel), 70m)
			.SetDisplay("Upper Exit", "MFI level used to close overbought positions", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_lowerExitLevel = Param(nameof(LowerExitLevel), 30m)
			.SetDisplay("Lower Exit", "MFI level used to close oversold positions", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);
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

		_previousCandle = null;
		_secondPreviousCandle = null;
		_previousMfi = null;
		_secondPreviousMfi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		var mfi = new MoneyFlowIndex
		{
			Length = MfiPeriod
		};

		subscription
			.Bind(mfi, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(0m, UnitTypes.Absolute),
			stopLoss: new Unit(0m, UnitTypes.Absolute),
			isStopTrailing: false
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		var hasPreviousMfi = _previousMfi.HasValue;
		var hasTwoMfi = hasPreviousMfi && _secondPreviousMfi.HasValue;
		var hasPatternCandles = _previousCandle != null && _secondPreviousCandle != null;

		if (canTrade && hasTwoMfi)
		{
			var prev = _secondPreviousMfi!.Value;
			var last = _previousMfi!.Value;

			if (Position > 0 && (CrossAbove(prev, last, UpperExitLevel) || CrossBelow(prev, last, LowerExitLevel)))
			{
				CloseLong();
			}

			if (Position < 0 && (CrossAbove(prev, last, LowerExitLevel) || CrossAbove(prev, last, UpperExitLevel)))
			{
				CloseShort();
			}
		}

		if (canTrade && hasPatternCandles && hasPreviousMfi)
		{
			var older = _secondPreviousCandle!;
			var middle = _previousCandle!;
			var mfiPrevious = _previousMfi!.Value;

			if (Position <= 0 && IsMorningStar(older, middle, candle) && mfiPrevious < BullishMfiThreshold)
			{
				OpenLong();
			}
			else if (Position >= 0 && IsEveningStar(older, middle, candle) && mfiPrevious > BearishMfiThreshold)
			{
				OpenShort();
			}
		}

		_secondPreviousCandle = _previousCandle;
		_previousCandle = candle;

		_secondPreviousMfi = _previousMfi;
		_previousMfi = mfiValue;
	}

	private void OpenLong()
	{
		CancelActiveOrders();

		var volume = Volume + Math.Abs(Position);

		if (volume > 0)
			BuyMarket(volume);
	}

	private void OpenShort()
	{
		CancelActiveOrders();

		var volume = Volume + Math.Abs(Position);

		if (volume > 0)
			SellMarket(volume);
	}

	private void CloseLong()
	{
		if (Position <= 0)
			return;

		CancelActiveOrders();
		SellMarket(Position);
	}

	private void CloseShort()
	{
		if (Position >= 0)
			return;

		CancelActiveOrders();
		BuyMarket(-Position);
	}

	private static bool CrossAbove(decimal previous, decimal current, decimal level)
	{
		return previous < level - Tolerance && current >= level - Tolerance;
	}

	private static bool CrossBelow(decimal previous, decimal current, decimal level)
	{
		return previous > level + Tolerance && current <= level + Tolerance;
	}

	private static bool IsMorningStar(ICandleMessage older, ICandleMessage middle, ICandleMessage current)
	{
		if (older.ClosePrice >= older.OpenPrice)
			return false;

		if (current.ClosePrice <= current.OpenPrice)
			return false;

		var olderRange = older.HighPrice - older.LowPrice;
		var middleRange = middle.HighPrice - middle.LowPrice;
		var currentRange = current.HighPrice - current.LowPrice;

		if (olderRange <= 0m || middleRange <= 0m || currentRange <= 0m)
			return false;

		var olderBody = Math.Abs(older.OpenPrice - older.ClosePrice);
		var middleBody = Math.Abs(middle.OpenPrice - middle.ClosePrice);
		var currentBody = Math.Abs(current.OpenPrice - current.ClosePrice);

		if (olderBody / olderRange < 0.6m)
			return false;

		if (middleBody / middleRange > 0.4m)
			return false;

		if (currentBody / currentRange < 0.6m)
			return false;

		if (middle.HighPrice >= older.ClosePrice)
			return false;

		if (current.OpenPrice <= middle.ClosePrice)
			return false;

		var midpoint = (older.OpenPrice + older.ClosePrice) / 2m;

		return current.ClosePrice > midpoint;
	}

	private static bool IsEveningStar(ICandleMessage older, ICandleMessage middle, ICandleMessage current)
	{
		if (older.ClosePrice <= older.OpenPrice)
			return false;

		if (current.ClosePrice >= current.OpenPrice)
			return false;

		var olderRange = older.HighPrice - older.LowPrice;
		var middleRange = middle.HighPrice - middle.LowPrice;
		var currentRange = current.HighPrice - current.LowPrice;

		if (olderRange <= 0m || middleRange <= 0m || currentRange <= 0m)
			return false;

		var olderBody = Math.Abs(older.OpenPrice - older.ClosePrice);
		var middleBody = Math.Abs(middle.OpenPrice - middle.ClosePrice);
		var currentBody = Math.Abs(current.OpenPrice - current.ClosePrice);

		if (olderBody / olderRange < 0.6m)
			return false;

		if (middleBody / middleRange > 0.4m)
			return false;

		if (currentBody / currentRange < 0.6m)
			return false;

		if (middle.LowPrice <= older.ClosePrice)
			return false;

		if (current.OpenPrice >= middle.ClosePrice)
			return false;

		var midpoint = (older.OpenPrice + older.ClosePrice) / 2m;

		return current.ClosePrice < midpoint;
	}
}
