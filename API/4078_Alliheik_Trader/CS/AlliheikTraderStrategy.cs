namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Conversion of the MetaTrader "Alliheik" expert advisor.
/// Trades Heiken Ashi Smoothed candles with an Alligator jaw filter and price-based trailing.
/// </summary>
public class AlliheikTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jawsPeriod;
	private readonly StrategyParam<int> _jawsShift;
	private readonly StrategyParam<MovingAverageType> _jawsMethod;
	private readonly StrategyParam<AppliedPriceType> _jawsPrice;
	private readonly StrategyParam<MovingAverageType> _preSmoothMethod;
	private readonly StrategyParam<int> _preSmoothPeriod;
	private readonly StrategyParam<MovingAverageType> _postSmoothMethod;
	private readonly StrategyParam<int> _postSmoothPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _orderVolume;

	private LengthIndicator<decimal> _jaws;
	private LengthIndicator<decimal> _openPreSmooth;
	private LengthIndicator<decimal> _closePreSmooth;
	private LengthIndicator<decimal> _highPreSmooth;
	private LengthIndicator<decimal> _lowPreSmooth;
	private LengthIndicator<decimal> _lowerPostSmooth;
	private LengthIndicator<decimal> _upperPostSmooth;

	private decimal?[] _closeHistory = Array.Empty<decimal?>();
	private decimal?[] _jawsHistory = Array.Empty<decimal?>();

	private decimal? _previousLower;
	private decimal? _previousUpper;
	private decimal? _prevRawHaOpen;
	private decimal? _prevRawHaClose;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private DateTimeOffset? _lastEntryBarTime;

	private bool _closeAllowed;
	private decimal _pointSize;

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the Alligator jaw moving average.
	/// </summary>
	public int JawsPeriod
	{
		get => _jawsPeriod.Value;
		set => _jawsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw moving average.
	/// </summary>
	public int JawsShift
	{
		get => _jawsShift.Value;
		set => _jawsShift.Value = value;
	}

	/// <summary>
	/// Moving average type used to build the jaw line.
	/// </summary>
	public MovingAverageType JawsMethod
	{
		get => _jawsMethod.Value;
		set => _jawsMethod.Value = value;
	}

	/// <summary>
	/// Price component supplied to the jaw moving average.
	/// </summary>
	public AppliedPriceType JawsPrice
	{
		get => _jawsPrice.Value;
		set => _jawsPrice.Value = value;
	}

	/// <summary>
	/// Moving average used to pre-smooth raw OHLC prices for Heiken Ashi.
	/// </summary>
	public MovingAverageType PreSmoothMethod
	{
		get => _preSmoothMethod.Value;
		set => _preSmoothMethod.Value = value;
	}

	/// <summary>
	/// Period of the pre-smoothing moving averages.
	/// </summary>
	public int PreSmoothPeriod
	{
		get => _preSmoothPeriod.Value;
		set => _preSmoothPeriod.Value = value;
	}

	/// <summary>
	/// Moving average used to smooth Heiken Ashi derived buffers.
	/// </summary>
	public MovingAverageType PostSmoothMethod
	{
		get => _postSmoothMethod.Value;
		set => _postSmoothMethod.Value = value;
	}

	/// <summary>
	/// Period of the post-smoothing moving averages.
	/// </summary>
	public int PostSmoothPeriod
	{
		get => _postSmoothPeriod.Value;
		set => _postSmoothPeriod.Value = value;
	}

	/// <summary>
	/// Fixed stop-loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Order volume expressed in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AlliheikTraderStrategy"/> class.
	/// </summary>
	public AlliheikTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for calculations.", "General");

		_jawsPeriod = Param(nameof(JawsPeriod), 144)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Period", "Length of the Alligator jaw moving average.", "Alligator");

		_jawsShift = Param(nameof(JawsShift), 8)
		.SetNotNegative()
		.SetDisplay("Jaw Shift", "Forward shift applied to the jaw moving average.", "Alligator");

		_jawsMethod = Param(nameof(JawsMethod), MovingAverageType.Simple)
		.SetDisplay("Jaw MA Type", "Moving average type used by the jaw.", "Alligator");

		_jawsPrice = Param(nameof(JawsPrice), AppliedPriceType.Close)
		.SetDisplay("Jaw Applied Price", "Price component passed into the jaw moving average.", "Alligator");

		_preSmoothMethod = Param(nameof(PreSmoothMethod), MovingAverageType.Exponential)
		.SetDisplay("Pre-smooth MA", "Moving average for smoothing OHLC prices.", "Heiken Ashi");

		_preSmoothPeriod = Param(nameof(PreSmoothPeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("Pre-smooth Period", "Length used by the OHLC smoothers.", "Heiken Ashi");

		_postSmoothMethod = Param(nameof(PostSmoothMethod), MovingAverageType.Weighted)
		.SetDisplay("Post-smooth MA", "Moving average used on Heiken Ashi buffers.", "Heiken Ashi");

		_postSmoothPeriod = Param(nameof(PostSmoothPeriod), 1)
		.SetGreaterThanZero()
		.SetDisplay("Post-smooth Period", "Length used by the final smoothers.", "Heiken Ashi");

		_stopLossPoints = Param(nameof(StopLossPoints), 0)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pts)", "Fixed protective stop in points.", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pts)", "Trailing distance applied once price clears the jaw.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 225)
		.SetNotNegative()
		.SetDisplay("Take Profit (pts)", "Fixed take-profit distance in points.", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Lot size used for entries.", "General");
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

		_jaws = default!;
		_openPreSmooth = default!;
		_closePreSmooth = default!;
		_highPreSmooth = default!;
		_lowPreSmooth = default!;
		_lowerPostSmooth = default!;
		_upperPostSmooth = default!;

		_closeHistory = Array.Empty<decimal?>();
		_jawsHistory = Array.Empty<decimal?>();

		_previousLower = null;
		_previousUpper = null;
		_prevRawHaOpen = null;
		_prevRawHaClose = null;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		_lastEntryBarTime = null;

		_closeAllowed = false;
		_pointSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = Security?.PriceStep ?? 0m;
		if (_pointSize <= 0m)
		{
			var decimals = Security?.Decimals;
			if (decimals is not null && decimals.Value > 0)
			{
				_pointSize = (decimal)Math.Pow(10, -decimals.Value);
			}
		}

		if (_pointSize <= 0m)
		_pointSize = 0.0001m;

		_jaws = CreateMovingAverage(JawsMethod, JawsPeriod);
		_openPreSmooth = CreateMovingAverage(PreSmoothMethod, PreSmoothPeriod);
		_closePreSmooth = CreateMovingAverage(PreSmoothMethod, PreSmoothPeriod);
		_highPreSmooth = CreateMovingAverage(PreSmoothMethod, PreSmoothPeriod);
		_lowPreSmooth = CreateMovingAverage(PreSmoothMethod, PreSmoothPeriod);
		_lowerPostSmooth = CreateMovingAverage(PostSmoothMethod, PostSmoothPeriod);
		_upperPostSmooth = CreateMovingAverage(PostSmoothMethod, PostSmoothPeriod);

		var historyLength = Math.Max(JawsShift + 8, 16);
		_closeHistory = new decimal?[historyLength];
		_jawsHistory = new decimal?[historyLength];

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_closeAllowed = false;
			return;
		}

		var entryPrice = PositionPrice;
		if (Position > 0m && delta > 0m)
		{
			_longStopPrice = StopLossPoints > 0 ? entryPrice - StopLossPoints * _pointSize : (decimal?)null;
			_longTakePrice = TakeProfitPoints > 0 ? entryPrice + TakeProfitPoints * _pointSize : (decimal?)null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_closeAllowed = false;
		}
		else if (Position < 0m && delta < 0m)
		{
			_shortStopPrice = StopLossPoints > 0 ? entryPrice + StopLossPoints * _pointSize : (decimal?)null;
			_shortTakePrice = TakeProfitPoints > 0 ? entryPrice - TakeProfitPoints * _pointSize : (decimal?)null;
			_longStopPrice = null;
			_longTakePrice = null;
			_closeAllowed = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateHistory(_closeHistory, candle.ClosePrice);

		var jawPrice = GetAppliedPrice(candle, JawsPrice);
		var jawValue = _jaws.Process(jawPrice, candle.OpenTime, true);
		UpdateHistory(_jawsHistory, jawValue.IsFinal ? jawValue.ToDecimal() : (decimal?)null);

		var openValue = _openPreSmooth.Process(candle.OpenPrice, candle.OpenTime, true);
		var closeValue = _closePreSmooth.Process(candle.ClosePrice, candle.OpenTime, true);
		var highValue = _highPreSmooth.Process(candle.HighPrice, candle.OpenTime, true);
		var lowValue = _lowPreSmooth.Process(candle.LowPrice, candle.OpenTime, true);

		if (!openValue.IsFinal || !closeValue.IsFinal || !highValue.IsFinal || !lowValue.IsFinal)
		return;

		var maOpen = openValue.ToDecimal();
		var maClose = closeValue.ToDecimal();
		var maHigh = highValue.ToDecimal();
		var maLow = lowValue.ToDecimal();

		var haOpen = _prevRawHaOpen is not null && _prevRawHaClose is not null
		? (_prevRawHaOpen.Value + _prevRawHaClose.Value) / 2m
		: (maOpen + maClose) / 2m;

		var haClose = (maOpen + maHigh + maLow + maClose) / 4m;
		var haHigh = Math.Max(maHigh, Math.Max(haOpen, haClose));
		var haLow = Math.Min(maLow, Math.Min(haOpen, haClose));

		decimal lowerSource;
		decimal upperSource;

		if (haOpen < haClose)
		{
			lowerSource = haLow;
			upperSource = haHigh;
		}
		else
		{
			lowerSource = haHigh;
			upperSource = haLow;
		}

		_prevRawHaOpen = haOpen;
		_prevRawHaClose = haClose;

		var lowerValue = _lowerPostSmooth.Process(lowerSource, candle.OpenTime, true);
		var upperValue = _upperPostSmooth.Process(upperSource, candle.OpenTime, true);

		if (!lowerValue.IsFinal || !upperValue.IsFinal)
		return;

		var currentLower = lowerValue.ToDecimal();
		var currentUpper = upperValue.ToDecimal();
		var previousLower = _previousLower;
		var previousUpper = _previousUpper;

		_previousLower = currentLower;
		_previousUpper = currentUpper;

		ManageOpenPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0m)
		return;

		if (previousLower is null || previousUpper is null)
		return;

		var goLong = currentLower < currentUpper && previousLower >= previousUpper;
		var goShort = currentLower > currentUpper && previousLower <= previousUpper;

		if (OrderVolume <= 0m)
		return;

		if (goLong && CanEnter(candle))
		{
			BuyMarket(OrderVolume);
			_closeAllowed = false;
			_lastEntryBarTime = candle.OpenTime;
		}
		else if (goShort && CanEnter(candle))
		{
			SellMarket(OrderVolume);
			_closeAllowed = false;
			_lastEntryBarTime = candle.OpenTime;
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longTakePrice is decimal longTake && candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				return;
			}

			if (_longStopPrice is decimal longStop && candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				return;
			}

			if (!TryGetHistoryValue(_closeHistory, 6, out var closeSix))
			return;

			if (!TryGetHistoryValue(_jawsHistory, JawsShift, out var jaw))
			return;

			if (closeSix > jaw)
			_closeAllowed = true;

			if (_pointSize > 0m)
			{
				var distance = Math.Abs(closeSix - jaw) / _pointSize;
				if (_closeAllowed && distance >= 8m && closeSix < jaw)
				{
					SellMarket(Position);
					return;
				}
			}

			if (TrailingStopPoints > 0 && _pointSize > 0m && closeSix > jaw)
			{
				var candidate = closeSix - TrailingStopPoints * _pointSize;
				if (_longStopPrice is decimal existing)
				{
					if (candidate > existing)
					_longStopPrice = candidate;
				}
				else
				{
					_longStopPrice = candidate;
				}
			}
		}
		else if (Position < 0m)
		{
			if (_shortTakePrice is decimal shortTake && candle.LowPrice <= shortTake)
			{
				BuyMarket(-Position);
				return;
			}

			if (_shortStopPrice is decimal shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(-Position);
				return;
			}

			if (!TryGetHistoryValue(_closeHistory, 6, out var closeSix))
			return;

			if (!TryGetHistoryValue(_jawsHistory, JawsShift, out var jaw))
			return;

			if (closeSix < jaw)
			_closeAllowed = true;

			if (_pointSize > 0m)
			{
				var distance = Math.Abs(closeSix - jaw) / _pointSize;
				if (_closeAllowed && distance >= 8m && closeSix > jaw)
				{
					BuyMarket(-Position);
					return;
				}
			}

			if (TrailingStopPoints > 0 && _pointSize > 0m && closeSix < jaw)
			{
				var candidate = closeSix + TrailingStopPoints * _pointSize;
				if (_shortStopPrice is decimal existing)
				{
					if (candidate < existing)
					_shortStopPrice = candidate;
				}
				else
				{
					_shortStopPrice = candidate;
				}
			}
		}
	}

	private static void UpdateHistory(decimal?[] buffer, decimal? value)
	{
		if (buffer.Length == 0)
		return;

		Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
		buffer[^1] = value;
	}

	private static bool TryGetHistoryValue(decimal?[] buffer, int shift, out decimal value)
	{
		value = 0m;

		var offsetFromEnd = shift + 1;
		if (buffer.Length < offsetFromEnd)
		return false;

		var index = buffer.Length - offsetFromEnd;
		if (index < 0)
		return false;

		if (buffer[index] is not decimal stored)
		return false;

		value = stored;
		return true;
	}

	private bool CanEnter(ICandleMessage candle)
	{
		return _lastEntryBarTime != candle.OpenTime;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageType type, int length)
	{
		return type switch
		{
			MovingAverageType.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageType.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageType.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType type)
	{
		return type switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	/// <summary>
	/// Moving average families supported by the conversion.
	/// </summary>
	public enum MovingAverageType
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
	}

	/// <summary>
	/// Price sources that can be supplied to the jaw moving average.
	/// </summary>
	public enum AppliedPriceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
	}
}
