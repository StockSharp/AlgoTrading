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
/// Bill Williams Alligator breakout strategy converted from MetaTrader 5 expert "Bill Williams.mq5".
/// The system looks for the most recent confirmed fractals and opens trades when the price breaks them while staying outside the shifted Alligator lines.
/// Optional parameters mirror the original robot: stop-loss, take-profit, trailing stop, signal reversal, and forced closing of opposite positions.
/// </summary>
public class BillWilliamsAlligatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<int> _fractalsLookback;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw = null!;
	private SmoothedMovingAverage _teeth = null!;
	private SmoothedMovingAverage _lips = null!;

	private decimal[] _highHistory = Array.Empty<decimal>();
	private decimal[] _lowHistory = Array.Empty<decimal>();
	private decimal?[] _jawHistory = Array.Empty<decimal?>();
	private decimal?[] _teethHistory = Array.Empty<decimal?>();
	private decimal?[] _lipsHistory = Array.Empty<decimal?>();

	private int _priceCount;
	private int _jawCount;
	private int _teethCount;
	private int _lipsCount;

	private decimal _pipSize;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Initializes <see cref="BillWilliamsAlligatorStrategy"/> parameters.
	/// </summary>
	public BillWilliamsAlligatorStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Trade size in lots or contracts.", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance converted with price step.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Optional take-profit distance in pips.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance once enabled.", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Additional progress required before trailing moves.", "Risk");

		_jawPeriod = Param(nameof(JawPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Period", "Smoothed moving average length for the jaw line.", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
		.SetNotNegative()
		.SetDisplay("Jaw Shift", "Forward shift (in bars) applied to the jaw.", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Teeth Period", "Smoothed moving average length for the teeth line.", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
		.SetNotNegative()
		.SetDisplay("Teeth Shift", "Forward shift (in bars) applied to the teeth.", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lips Period", "Smoothed moving average length for the lips line.", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
		.SetNotNegative()
		.SetDisplay("Lips Shift", "Forward shift (in bars) applied to the lips.", "Alligator");

		_fractalsLookback = Param(nameof(FractalsLookback), 100)
		.SetGreaterOrEqual(5)
		.SetDisplay("Fractal Lookback", "Number of finished candles scanned for confirmed fractals.", "Fractals");

		_reverseSignals = Param(nameof(ReverseSignals), false)
		.SetDisplay("Reverse Signals", "Invert breakout directions (buy on down fractal, sell on up fractal).", "Trading");

		_closeOpposite = Param(nameof(CloseOppositePositions), false)
		.SetDisplay("Close Opposite", "Close opposite exposure before opening a new trade.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series used for signals.", "Data");
	}

	/// <summary>
	/// Order volume expressed in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips; zero disables the hard stop.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips; zero disables the target.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips; zero disables trailing.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional pip offset required before the trailing stop moves.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Period of the jaw smoothed moving average.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw values.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Period of the teeth smoothed moving average.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth values.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Period of the lips smoothed moving average.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips values.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Maximum number of candles scanned for the latest fractals.
	/// </summary>
	public int FractalsLookback
	{
		get => _fractalsLookback.Value;
		set => _fractalsLookback.Value = value;
	}

	/// <summary>
	/// Invert breakout directions.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Close opposite exposure before opening a new trade.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Candle series used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_highHistory = Array.Empty<decimal>();
		_lowHistory = Array.Empty<decimal>();
		_jawHistory = Array.Empty<decimal?>();
		_teethHistory = Array.Empty<decimal?>();
		_lipsHistory = Array.Empty<decimal?>();

		_priceCount = 0;
		_jawCount = 0;
		_teethCount = 0;
		_lipsCount = 0;

		_pipSize = 0m;

		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
		throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		_jaw = new SmoothedMovingAverage { Length = JawPeriod };
		_teeth = new SmoothedMovingAverage { Length = TeethPeriod };
		_lips = new SmoothedMovingAverage { Length = LipsPeriod };

		var historySize = CalculateHistorySize();

		_highHistory = new decimal[historySize];
		_lowHistory = new decimal[historySize];
		_jawHistory = new decimal?[historySize];
		_teethHistory = new decimal?[historySize];
		_lipsHistory = new decimal?[historySize];

		_priceCount = 0;
		_jawCount = 0;
		_teethCount = 0;
		_lipsCount = 0;

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			ResetPositionState();
			return;
		}

		var entryPrice = PositionPrice;

		if (Position > 0m && delta > 0m)
		{
			_longStopPrice = StopLossPips > 0 ? entryPrice - StopLossPips * _pipSize : (decimal?)null;
			_longTakePrice = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * _pipSize : (decimal?)null;
			_longExitRequested = false;

			_shortStopPrice = null;
			_shortTakePrice = null;
			_shortExitRequested = false;
		}
		else if (Position < 0m && delta < 0m)
		{
			_shortStopPrice = StopLossPips > 0 ? entryPrice + StopLossPips * _pipSize : (decimal?)null;
			_shortTakePrice = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * _pipSize : (decimal?)null;
			_shortExitRequested = false;

			_longStopPrice = null;
			_longTakePrice = null;
			_longExitRequested = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (Position > 0m)
		{
			ManageLong(candle);
		}
		else if (Position < 0m)
		{
			ManageShort(candle);
		}

		UpdatePriceHistory(candle);

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawValue = _jaw.Process(median, candle.OpenTime, true);
		var teethValue = _teeth.Process(median, candle.OpenTime, true);
		var lipsValue = _lips.Process(median, candle.OpenTime, true);

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal)
		return;

		ShiftIndicatorHistory(_jawHistory, ref _jawCount, jawValue.ToDecimal());
		ShiftIndicatorHistory(_teethHistory, ref _teethCount, teethValue.ToDecimal());
		ShiftIndicatorHistory(_lipsHistory, ref _lipsCount, lipsValue.ToDecimal());

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!TryFindFractals(out var upperFractal, out var upperIndex, out var lowerFractal, out var lowerIndex))
		return;

		if (!TryGetShiftedIndicatorValue(_jawHistory, _jawCount, JawShift, upperIndex, out var jawAtUpper) ||
		!TryGetShiftedIndicatorValue(_teethHistory, _teethCount, TeethShift, upperIndex, out var teethAtUpper) ||
		!TryGetShiftedIndicatorValue(_lipsHistory, _lipsCount, LipsShift, upperIndex, out var lipsAtUpper))
		{
			return;
		}

		if (!TryGetShiftedIndicatorValue(_jawHistory, _jawCount, JawShift, lowerIndex, out var jawAtLower) ||
		!TryGetShiftedIndicatorValue(_teethHistory, _teethCount, TeethShift, lowerIndex, out var teethAtLower) ||
		!TryGetShiftedIndicatorValue(_lipsHistory, _lipsCount, LipsShift, lowerIndex, out var lipsAtLower))
		{
			return;
		}

		var upperBreakout = upperFractal > jawAtUpper && upperFractal > teethAtUpper && upperFractal > lipsAtUpper && candle.ClosePrice > upperFractal;
		var lowerBreakout = lowerFractal < jawAtLower && lowerFractal < teethAtLower && lowerFractal < lipsAtLower && candle.ClosePrice < lowerFractal;

		if (ReverseSignals)
		{
			(upperBreakout, lowerBreakout) = (lowerBreakout, upperBreakout);
		}

		if (upperBreakout && OrderVolume > 0m)
		{
			if (Position < 0m)
			{
				if (CloseOppositePositions)
				{
					TryCloseShort();
					return;
				}

				return;
			}

			if (Position == 0m)
			{
				BuyMarket(volume: OrderVolume);
			}
		}

		if (lowerBreakout && OrderVolume > 0m)
		{
			if (Position > 0m)
			{
				if (CloseOppositePositions)
				{
					TryCloseLong();
					return;
				}

				return;
			}

			if (Position == 0m)
			{
				SellMarket(volume: OrderVolume);
			}
		}
	}

	private void ManageLong(ICandleMessage candle)
	{
		if (_longTakePrice is decimal take && candle.HighPrice >= take)
		{
			TryCloseLong();
			return;
		}

		UpdateTrailingStopForLong(candle);

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			TryCloseLong();
		}
	}

	private void ManageShort(ICandleMessage candle)
	{
		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			TryCloseShort();
			return;
		}

		UpdateTrailingStopForShort(candle);

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			TryCloseShort();
		}
	}

	private void UpdateTrailingStopForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _pipSize <= 0m)
		return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;
		var referencePrice = Math.Max(candle.HighPrice, candle.ClosePrice);

		if (referencePrice - PositionPrice <= trailingDistance + stepDistance)
		return;

		var desiredStop = referencePrice - trailingDistance;
		var threshold = stepDistance > 0m ? desiredStop - stepDistance : desiredStop;

		if (_longStopPrice is not decimal current || current < threshold)
		{
			_longStopPrice = desiredStop;
		}
	}

	private void UpdateTrailingStopForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _pipSize <= 0m)
		return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;
		var referencePrice = Math.Min(candle.LowPrice, candle.ClosePrice);

		if (PositionPrice - referencePrice <= trailingDistance + stepDistance)
		return;

		var desiredStop = referencePrice + trailingDistance;
		var threshold = stepDistance > 0m ? desiredStop + stepDistance : desiredStop;

		if (_shortStopPrice is not decimal current || current > threshold)
		{
			_shortStopPrice = desiredStop;
		}
	}

	private void TryCloseLong()
	{
		if (_longExitRequested)
		return;

		_longExitRequested = true;
		SellMarket(volume: Position);
	}

	private void TryCloseShort()
	{
		if (_shortExitRequested)
		return;

		_shortExitRequested = true;
		BuyMarket(volume: Math.Abs(Position));
	}

	private void ResetPositionState()
	{
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	private void UpdatePriceHistory(ICandleMessage candle)
	{
		if (_highHistory.Length == 0 || _lowHistory.Length == 0)
		return;

		var maxIndex = Math.Min(_priceCount, _highHistory.Length - 1);

		ShiftPriceHistory(_highHistory, candle.HighPrice, maxIndex);
		ShiftPriceHistory(_lowHistory, candle.LowPrice, maxIndex);

		if (_priceCount < _highHistory.Length)
		_priceCount++;
	}

	private static void ShiftPriceHistory(decimal[] buffer, decimal value, int maxIndex)
	{
		if (buffer.Length == 0)
		return;

		for (var i = maxIndex; i > 0; i--)
		{
			buffer[i] = buffer[i - 1];
		}

		buffer[0] = value;
	}

	private static void ShiftIndicatorHistory(decimal?[] buffer, ref int count, decimal value)
	{
		if (buffer.Length == 0)
		return;

		var maxIndex = Math.Min(count, buffer.Length - 1);

		for (var i = maxIndex; i > 0; i--)
		{
			buffer[i] = buffer[i - 1];
		}

		buffer[0] = value;

		if (count < buffer.Length)
		count++;
	}

	private bool TryFindFractals(out decimal upper, out int upperIndex, out decimal lower, out int lowerIndex)
	{
		upper = 0m;
		lower = 0m;
		upperIndex = -1;
		lowerIndex = -1;

		if (_priceCount < 5)
		return false;

		var limit = Math.Min(_priceCount, FractalsLookback);

		for (var i = 2; i + 2 < limit; i++)
		{
			var high = _highHistory[i];
			var low = _lowHistory[i];

			var isUpper = high > _highHistory[i - 1] && high > _highHistory[i - 2] && high > _highHistory[i + 1] && high > _highHistory[i + 2];
			var isLower = low < _lowHistory[i - 1] && low < _lowHistory[i - 2] && low < _lowHistory[i + 1] && low < _lowHistory[i + 2];

			if (isUpper && upperIndex == -1)
			{
				upper = high;
				upperIndex = i;
			}

			if (isLower && lowerIndex == -1)
			{
				lower = low;
				lowerIndex = i;
			}

			if (upperIndex != -1 && lowerIndex != -1)
			break;
		}

		return upperIndex != -1 && lowerIndex != -1;
	}

	private static bool TryGetShiftedIndicatorValue(decimal?[] buffer, int count, int shift, int barsAgo, out decimal value)
	{
		value = 0m;

		var index = shift + barsAgo;
		if (index >= count)
		return false;

		if (buffer[index] is not decimal stored)
		return false;

		value = stored;
		return true;
	}

	private int CalculateHistorySize()
	{
		var maxShift = Math.Max(JawShift, Math.Max(TeethShift, LipsShift));
		return Math.Max(FractalsLookback + maxShift + 10, 32);
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}
}

