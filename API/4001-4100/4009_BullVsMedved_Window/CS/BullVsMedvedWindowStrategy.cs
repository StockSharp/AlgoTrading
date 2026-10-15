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
/// Bull vs Medved strategy converted from MetaTrader 4.
/// Enters market orders during predefined intraday windows when multi-candle patterns appear.
/// Exits on candle-based stop-loss / take-profit levels.
/// </summary>
public class BullVsMedvedWindowStrategy : Strategy
{
	private readonly StrategyParam<decimal> _candleSizePoints;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<int> _entryWindowMinutes;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _startTime0;
	private readonly StrategyParam<TimeSpan> _startTime1;
	private readonly StrategyParam<TimeSpan> _startTime2;
	private readonly StrategyParam<TimeSpan> _startTime3;
	private readonly StrategyParam<TimeSpan> _startTime4;
	private readonly StrategyParam<TimeSpan> _startTime5;

	private decimal _pointValue;
	private decimal _candleSizeThreshold;
	private decimal _bodyMinSize;
	private decimal _pullbackSize;

	private ICandleMessage _previousCandle1;
	private ICandleMessage _previousCandle2;

	private TimeSpan[] _entryTimes = Array.Empty<TimeSpan>();
	private TimeSpan _entryWindow = TimeSpan.Zero;
	private bool _orderPlacedInWindow;

	private decimal _entryPrice;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private bool _exitRequested;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public BullVsMedvedWindowStrategy()
	{
		_candleSizePoints = Param(nameof(CandleSizePoints), 75m)
			.SetDisplay("Body Size (points)", "Minimum body size for the latest candle", "Filters")
			.SetGreaterThanZero();

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 0.8m)
			.SetDisplay("Stop Multiplier", "Coefficient applied to the candle body for stop-loss", "Risk")
			.SetGreaterThanZero();

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 0.8m)
			.SetDisplay("Take Profit Multiplier", "Coefficient applied to the candle body for take-profit", "Risk")
			.SetGreaterThanZero();

		_entryWindowMinutes = Param(nameof(EntryWindowMinutes), 10)
			.SetDisplay("Entry Window", "Duration of each trading window in minutes", "Timing")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for pattern detection", "Data");

		_startTime0 = Param(nameof(StartTime0), new TimeSpan(0, 5, 0))
			.SetDisplay("Start Time #1", "First trading window start", "Timing");

		_startTime1 = Param(nameof(StartTime1), new TimeSpan(4, 5, 0))
			.SetDisplay("Start Time #2", "Second trading window start", "Timing");

		_startTime2 = Param(nameof(StartTime2), new TimeSpan(8, 5, 0))
			.SetDisplay("Start Time #3", "Third trading window start", "Timing");

		_startTime3 = Param(nameof(StartTime3), new TimeSpan(12, 5, 0))
			.SetDisplay("Start Time #4", "Fourth trading window start", "Timing");

		_startTime4 = Param(nameof(StartTime4), new TimeSpan(16, 5, 0))
			.SetDisplay("Start Time #5", "Fifth trading window start", "Timing");

		_startTime5 = Param(nameof(StartTime5), new TimeSpan(20, 5, 0))
			.SetDisplay("Start Time #6", "Sixth trading window start", "Timing");
	}

	/// <summary>
	/// Minimum bullish or bearish body size in broker points.
	/// </summary>
	public decimal CandleSizePoints
	{
		get => _candleSizePoints.Value;
		set => _candleSizePoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the signal candle body to calculate the stop-loss distance.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the signal candle body to calculate the take-profit distance.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Duration of each trading window in minutes.
	/// </summary>
	public int EntryWindowMinutes
	{
		get => _entryWindowMinutes.Value;
		set => _entryWindowMinutes.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate price patterns.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// First trading window start time.
	/// </summary>
	public TimeSpan StartTime0
	{
		get => _startTime0.Value;
		set => _startTime0.Value = value;
	}

	/// <summary>
	/// Second trading window start time.
	/// </summary>
	public TimeSpan StartTime1
	{
		get => _startTime1.Value;
		set => _startTime1.Value = value;
	}

	/// <summary>
	/// Third trading window start time.
	/// </summary>
	public TimeSpan StartTime2
	{
		get => _startTime2.Value;
		set => _startTime2.Value = value;
	}

	/// <summary>
	/// Fourth trading window start time.
	/// </summary>
	public TimeSpan StartTime3
	{
		get => _startTime3.Value;
		set => _startTime3.Value = value;
	}

	/// <summary>
	/// Fifth trading window start time.
	/// </summary>
	public TimeSpan StartTime4
	{
		get => _startTime4.Value;
		set => _startTime4.Value = value;
	}

	/// <summary>
	/// Sixth trading window start time.
	/// </summary>
	public TimeSpan StartTime5
	{
		get => _startTime5.Value;
		set => _startTime5.Value = value;
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

		_pointValue = 0m;
		_candleSizeThreshold = 0m;
		_bodyMinSize = 0m;
		_pullbackSize = 0m;
		_entryWindow = TimeSpan.Zero;

		_previousCandle1 = null;
		_previousCandle2 = null;
		_entryTimes = Array.Empty<TimeSpan>();
		_orderPlacedInWindow = false;

		_entryPrice = 0m;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_exitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pointValue = Security?.PriceStep ?? 1m;
		_candleSizeThreshold = CandleSizePoints * _pointValue;
		_bodyMinSize = 10m * _pointValue;
		_pullbackSize = 20m * _pointValue;
		_entryWindow = TimeSpan.FromMinutes(EntryWindowMinutes);

		_entryTimes = new[]
		{
			StartTime0,
			StartTime1,
			StartTime2,
			StartTime3,
			StartTime4,
			StartTime5,
		};

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (HandlePositionExits(candle))
		{
			ShiftHistory(candle);
			return;
		}

		var inWindow = IsWithinEntryWindow(candle.CloseTime);
		if (!inWindow)
		{
			_orderPlacedInWindow = false;
			ShiftHistory(candle);
			return;
		}

		if (_orderPlacedInWindow || Position != 0m)
		{
			ShiftHistory(candle);
			return;
		}

		if (_previousCandle1 is null || _previousCandle2 is null)
		{
			ShiftHistory(candle);
			return;
		}

		var shift1 = candle;
		var shift2 = _previousCandle1;
		var shift3 = _previousCandle2;

		var placedOrder = false;

		var isBull = IsBull(shift3, shift2, shift1);
		var isBadBull = IsBadBull(shift3, shift2, shift1);
		var isCoolBull = IsCoolBull(shift2, shift1);
		var isBear = IsBear(shift1);

		if (isBull && !isBadBull)
			placedOrder = TryBuyMarket(shift1);
		else if (isCoolBull)
			placedOrder = TryBuyMarket(shift1);
		else if (isBear)
			placedOrder = TrySellMarket(shift1);

		if (placedOrder)
			_orderPlacedInWindow = true;

		ShiftHistory(candle);
	}

	private bool HandlePositionExits(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (!_exitRequested && _longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				_exitRequested = true;
				SellMarket();
				ResetProtectionLevels();
				return true;
			}

			if (!_exitRequested && _longTakePrice is decimal take && candle.HighPrice >= take)
			{
				_exitRequested = true;
				SellMarket();
				ResetProtectionLevels();
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (!_exitRequested && _shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				_exitRequested = true;
				BuyMarket();
				ResetProtectionLevels();
				return true;
			}

			if (!_exitRequested && _shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				_exitRequested = true;
				BuyMarket();
				ResetProtectionLevels();
				return true;
			}
		}

		return false;
	}

	private bool TryBuyMarket(ICandleMessage referenceCandle)
	{
		var body = (referenceCandle.ClosePrice - referenceCandle.OpenPrice).Abs();
		var stopDistance = RoundToPoint(body * StopLossMultiplier);
		var takeDistance = RoundToPoint(body * TakeProfitMultiplier);

		var price = referenceCandle.ClosePrice;

		BuyMarket();

		_entryPrice = price;
		_longStopPrice = stopDistance > 0m ? NormalizePrice(price - stopDistance) : null;
		_longTakePrice = takeDistance > 0m ? NormalizePrice(price + takeDistance) : null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_exitRequested = false;

		return true;
	}

	private bool TrySellMarket(ICandleMessage referenceCandle)
	{
		var body = (referenceCandle.ClosePrice - referenceCandle.OpenPrice).Abs();
		var stopDistance = RoundToPoint(body * StopLossMultiplier);
		var takeDistance = RoundToPoint(body * TakeProfitMultiplier);

		var price = referenceCandle.ClosePrice;

		SellMarket();

		_entryPrice = price;
		_shortStopPrice = stopDistance > 0m ? NormalizePrice(price + stopDistance) : null;
		_shortTakePrice = takeDistance > 0m ? NormalizePrice(price - takeDistance) : null;
		_longStopPrice = null;
		_longTakePrice = null;
		_exitRequested = false;

		return true;
	}

	private bool IsWithinEntryWindow(DateTimeOffset time)
	{
		if (_entryWindow <= TimeSpan.Zero)
			return false;

		var tod = time.TimeOfDay;

		for (var i = 0; i < _entryTimes.Length; i++)
		{
			var start = _entryTimes[i];
			var end = start + _entryWindow;

			if (tod >= start && tod <= end)
				return true;
		}

		return false;
	}

	private void ShiftHistory(ICandleMessage candle)
	{
		_previousCandle2 = _previousCandle1;
		_previousCandle1 = candle;
	}

	private bool IsBull(ICandleMessage shift3, ICandleMessage shift2, ICandleMessage shift1)
	{
		return shift3.ClosePrice > shift2.OpenPrice &&
			(shift2.ClosePrice - shift2.OpenPrice) >= _bodyMinSize &&
			(shift1.ClosePrice - shift1.OpenPrice) >= _candleSizeThreshold;
	}

	private bool IsBadBull(ICandleMessage shift3, ICandleMessage shift2, ICandleMessage shift1)
	{
		return (shift3.ClosePrice - shift3.OpenPrice) >= _bodyMinSize &&
			(shift2.ClosePrice - shift2.OpenPrice) >= _bodyMinSize &&
			(shift1.ClosePrice - shift1.OpenPrice) >= _candleSizeThreshold;
	}

	private bool IsCoolBull(ICandleMessage shift2, ICandleMessage shift1)
	{
		return (shift2.OpenPrice - shift2.ClosePrice) >= _pullbackSize &&
			shift2.ClosePrice <= shift1.OpenPrice &&
			shift1.ClosePrice > shift2.OpenPrice &&
			(shift1.ClosePrice - shift1.OpenPrice) >= 0.4m * _candleSizeThreshold;
	}

	private bool IsBear(ICandleMessage shift1)
	{
		return (shift1.OpenPrice - shift1.ClosePrice) >= _candleSizeThreshold;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_pointValue <= 0m)
			return price;

		var steps = price / _pointValue;
		var roundedSteps = decimal.Round(steps, MidpointRounding.AwayFromZero);
		return roundedSteps * _pointValue;
	}

	private decimal RoundToPoint(decimal value)
	{
		if (_pointValue <= 0m)
			return value;

		var steps = value / _pointValue;
		var roundedSteps = decimal.Round(steps, MidpointRounding.AwayFromZero);
		return roundedSteps * _pointValue;
	}

	private void ResetProtectionLevels()
	{
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}
}
