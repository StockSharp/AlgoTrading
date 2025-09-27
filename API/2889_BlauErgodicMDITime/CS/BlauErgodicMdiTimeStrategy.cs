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
/// Trading strategy based on the Blau Ergodic MDI oscillator with three signal modes.
/// The strategy replicates the behaviour of the original MetaTrader expert advisor
/// by evaluating the oscillator on a higher timeframe and optionally restricting
/// trading to a custom time window.
/// </summary>
public enum BlauErgodicMdiModes
{
	/// <summary>
	/// Generates entries when the histogram crosses the zero line.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Generates entries when the histogram twists and changes slope.
	/// </summary>
	Twist,

	/// <summary>
	/// Generates entries when the histogram and its smoothed copy cross.
	/// </summary>
	CloudTwist
}

/// <summary>
/// Price source used to feed the Blau Ergodic MDI calculation.
/// </summary>
public enum PriceInputModes
{
	/// <summary>
	/// Candle close price.
	/// </summary>
	Close,

	/// <summary>
	/// Candle open price.
	/// </summary>
	Open,

	/// <summary>
	/// Candle high price.
	/// </summary>
	High,

	/// <summary>
	/// Candle low price.
	/// </summary>
	Low,

	/// <summary>
	/// Median price (high + low) / 2.
	/// </summary>
	Median,

	/// <summary>
	/// Typical price (high + low + close) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted close price (high + low + 2 * close) / 4.
	/// </summary>
	Weighted,

	/// <summary>
	/// Simplified price (open + close) / 2.
	/// </summary>
	Simple,

	/// <summary>
	/// Quarter price (open + high + low + close) / 4.
	/// </summary>
	Quarter,

	/// <summary>
	/// Trend follow price - picks the high on bullish candles and the low on bearish candles.
	/// </summary>
	TrendFollow0,

	/// <summary>
	/// Half trend follow price - averages close with the extreme price of the candle.
	/// </summary>
	TrendFollow1,

	/// <summary>
	/// Demark price calculation.
	/// </summary>
	Demark
}

/// <summary>
/// Blau Ergodic MDI strategy converted from MetaTrader version 21013.
/// </summary>
public class BlauErgodicMdiTimeStrategy : Strategy
{
	private readonly StrategyParam<BlauErgodicMdiModes> _mode;
	private readonly StrategyParam<PriceInputModes> _priceMode;
	private readonly StrategyParam<int> _baseLength;
	private readonly StrategyParam<int> _firstSmoothingLength;
	private readonly StrategyParam<int> _secondSmoothingLength;
	private readonly StrategyParam<int> _thirdSmoothingLength;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;

	private decimal? _priceEma;
	private decimal? _diffEma1;
	private decimal? _diffEma2;
	private decimal? _diffEma3;

	private decimal[] _histBuffer = Array.Empty<decimal>();
	private decimal[] _signalBuffer = Array.Empty<decimal>();
	private DateTimeOffset[] _timeBuffer = Array.Empty<DateTimeOffset>();
	private int _bufferCount;

	private decimal? _entryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private TimeSpan _candleSpan;
	private int _barsProcessed;

	/// <summary>
	/// Selected signal mode.
	/// </summary>
	public BlauErgodicMdiModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Selected price source.
	/// </summary>
	public PriceInputModes PriceMode
	{
		get => _priceMode.Value;
		set => _priceMode.Value = value;
	}

	/// <summary>
	/// Base smoothing length applied to the price series.
	/// </summary>
	public int BaseLength
	{
		get => _baseLength.Value;
		set => _baseLength.Value = value;
	}

	/// <summary>
	/// Length of the first smoothing of the price difference.
	/// </summary>
	public int FirstSmoothingLength
	{
		get => _firstSmoothingLength.Value;
		set => _firstSmoothingLength.Value = value;
	}

	/// <summary>
	/// Length of the second smoothing stage.
	/// </summary>
	public int SecondSmoothingLength
	{
		get => _secondSmoothingLength.Value;
		set => _secondSmoothingLength.Value = value;
	}

	/// <summary>
	/// Length of the third smoothing stage.
	/// </summary>
	public int ThirdSmoothingLength
	{
		get => _thirdSmoothingLength.Value;
		set => _thirdSmoothingLength.Value = value;
	}

	/// <summary>
	/// Number of bars back used for signal evaluation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Enables exits from long positions.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Enables exits from short positions.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Enables trading within the configured time range only.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Start hour of the trading window.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Start minute of the trading window.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// End hour of the trading window.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// End minute of the trading window.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points (price steps).
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Take-profit distance expressed in points (price steps).
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = Math.Max(0, value);
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
	/// Constructor.
	/// </summary>
	public BlauErgodicMdiTimeStrategy()
	{
		_mode = Param(nameof(Mode), BlauErgodicMdiModes.Twist)
		.SetDisplay("Mode", "Signal mode", "General");

		_priceMode = Param(nameof(PriceMode), PriceInputModes.Close)
		.SetDisplay("Price Mode", "Price input used for the oscillator", "General");

		_baseLength = Param(nameof(BaseLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Base Length", "Length of the base EMA", "Indicator")
		.SetCanOptimize(true);

		_firstSmoothingLength = Param(nameof(FirstSmoothingLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("First Smooth", "Length of the first smoothing", "Indicator")
		.SetCanOptimize(true);

		_secondSmoothingLength = Param(nameof(SecondSmoothingLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Second Smooth", "Length of the second smoothing", "Indicator")
		.SetCanOptimize(true);

		_thirdSmoothingLength = Param(nameof(ThirdSmoothingLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Third Smooth", "Length of the third smoothing", "Indicator")
		.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of bars back used for the signal", "Indicator");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
		.SetDisplay("Allow Long Entry", "Enable long entries", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
		.SetDisplay("Allow Short Entry", "Enable short entries", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
		.SetDisplay("Allow Long Exit", "Enable exits from long positions", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
		.SetDisplay("Allow Short Exit", "Enable exits from short positions", "Trading");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
		.SetDisplay("Use Time Filter", "Restrict trading to the configured session", "Time Filter");

		_startHour = Param(nameof(StartHour), 0)
		.SetDisplay("Start Hour", "Hour when trading can start", "Time Filter");

		_startMinute = Param(nameof(StartMinute), 0)
		.SetDisplay("Start Minute", "Minute when trading can start", "Time Filter");

		_endHour = Param(nameof(EndHour), 23)
		.SetDisplay("End Hour", "Hour when trading stops", "Time Filter");

		_endMinute = Param(nameof(EndMinute), 59)
		.SetDisplay("End Minute", "Minute when trading stops", "Time Filter");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss", "Protective stop distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit", "Target distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for calculations", "General");

		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");
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

		_priceEma = null;
		_diffEma1 = null;
		_diffEma2 = null;
		_diffEma3 = null;

		_histBuffer = Array.Empty<decimal>();
		_signalBuffer = Array.Empty<decimal>();
		_timeBuffer = Array.Empty<DateTimeOffset>();
		_bufferCount = 0;

		_entryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		_candleSpan = TimeSpan.Zero;
		_barsProcessed = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_candleSpan = GetCandleSpan();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (CheckRiskManagement(candle))
		{
			return;
		}

		var price = GetAppliedPrice(candle);

		var baseLength = Math.Max(1, BaseLength);
		var firstLength = Math.Max(1, FirstSmoothingLength);
		var secondLength = Math.Max(1, SecondSmoothingLength);
		var thirdLength = Math.Max(1, ThirdSmoothingLength);

		var baseSmoothed = UpdateEma(ref _priceEma, price, baseLength);
		var diff = price - baseSmoothed;
		var diffSmoothed1 = UpdateEma(ref _diffEma1, diff, firstLength);
		var diffSmoothed2 = UpdateEma(ref _diffEma2, diffSmoothed1, secondLength);
		var diffSmoothed3 = UpdateEma(ref _diffEma3, diffSmoothed2, thirdLength);

		var point = GetPointValue();
		var histValue = diffSmoothed2 / point;
		var signalValue = diffSmoothed3 / point;
		var closeTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime + _candleSpan;

		_barsProcessed++;
		var minimumBars = GetMinimumBars();
		var requiredLength = GetRequiredBufferLength();

		PushValues(histValue, signalValue, closeTime, requiredLength);

		if (_barsProcessed < minimumBars)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		var tradeWindow = !UseTimeFilter || InTradeWindow(closeTime);

		if (UseTimeFilter && !tradeWindow && Position != 0)
		{
			CloseAllPositions();
			return;
		}

		var signalBar = Math.Max(0, SignalBar);

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;
		DateTimeOffset? upSignalTime = null;
		DateTimeOffset? downSignalTime = null;

		switch (Mode)
		{
			case BlauErgodicMdiModes.Breakdown:
			{
				if (!HasSufficientData(signalBar + 1))
				{
					break;
				}

				var current = _histBuffer[signalBar];
				var previous = _histBuffer[signalBar + 1];
				var signalTime = _timeBuffer[signalBar];

				if (previous > 0m)
				{
					if (AllowLongEntry && current <= 0m)
					{
						buyOpen = true;
					}

					if (AllowShortExit)
					{
						sellClose = true;
					}

					upSignalTime = signalTime;
				}

				if (previous < 0m)
				{
					if (AllowShortEntry && current >= 0m)
					{
						sellOpen = true;
					}

					if (AllowLongExit)
					{
						buyClose = true;
					}

					downSignalTime = signalTime;
				}

				break;
			}

			case BlauErgodicMdiModes.Twist:
			{
				if (!HasSufficientData(signalBar + 2))
				{
					break;
				}

				var current = _histBuffer[signalBar];
				var prev1 = _histBuffer[signalBar + 1];
				var prev2 = _histBuffer[signalBar + 2];
				var signalTime = _timeBuffer[signalBar];

				if (prev1 < prev2)
				{
					if (AllowLongEntry && current > prev1)
					{
						buyOpen = true;
					}

					if (AllowShortExit)
					{
						sellClose = true;
					}

					upSignalTime = signalTime;
				}

				if (prev1 > prev2)
				{
					if (AllowShortEntry && current < prev1)
					{
						sellOpen = true;
					}

					if (AllowLongExit)
					{
						buyClose = true;
					}

					downSignalTime = signalTime;
				}

				break;
			}

			case BlauErgodicMdiModes.CloudTwist:
			{
				if (!HasSufficientData(signalBar + 1))
				{
					break;
				}

				var currentUp = _histBuffer[signalBar];
				var currentDown = _signalBuffer[signalBar];
				var prevUp = _histBuffer[signalBar + 1];
				var prevDown = _signalBuffer[signalBar + 1];
				var signalTime = _timeBuffer[signalBar];

				if (prevUp > prevDown)
				{
					if (AllowLongEntry && currentUp <= currentDown)
					{
						buyOpen = true;
					}

					if (AllowShortExit)
					{
						sellClose = true;
					}

					upSignalTime = signalTime;
				}

				if (prevUp < prevDown)
				{
					if (AllowShortEntry && currentUp >= currentDown)
					{
						sellOpen = true;
					}

					if (AllowLongExit)
					{
						buyClose = true;
					}

					downSignalTime = signalTime;
				}

				break;
			}
		}

		if (buyClose && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			ResetRiskLevels();
		}

		if (sellClose && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskLevels();
		}

		if (!tradeWindow)
		{
			return;
		}

		if (buyOpen && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			SetRiskForLong(candle.ClosePrice);
		}

		if (sellOpen && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			SetRiskForShort(candle.ClosePrice);
		}
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		return PriceMode switch
		{
			PriceInputModes.Open => open,
			PriceInputModes.High => high,
			PriceInputModes.Low => low,
			PriceInputModes.Median => (high + low) / 2m,
			PriceInputModes.Typical => (close + high + low) / 3m,
			PriceInputModes.Weighted => (2m * close + high + low) / 4m,
			PriceInputModes.Simple => (open + close) / 2m,
			PriceInputModes.Quarter => (open + high + low + close) / 4m,
			PriceInputModes.TrendFollow0 => close > open ? high : close < open ? low : close,
			PriceInputModes.TrendFollow1 => close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close,
			PriceInputModes.Demark => CalculateDemarkPrice(open, high, low, close),
			_ => close,
		};
	}

	private static decimal CalculateDemarkPrice(decimal open, decimal high, decimal low, decimal close)
	{
		var res = high + low + close;

		if (close < open)
		{
			res = (res + low) / 2m;
		}
		else if (close > open)
		{
			res = (res + high) / 2m;
		}
		else
		{
			res = (res + close) / 2m;
		}

		return ((res - low) + (res - high)) / 2m;
	}

	private static decimal UpdateEma(ref decimal? previous, decimal value, int length)
	{
		if (length <= 1)
		{
			previous = value;
			return value;
		}

		var alpha = 2m / (length + 1m);
		var current = previous.HasValue ? previous.Value + alpha * (value - previous.Value) : value;
		previous = current;
		return current;
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private int GetMinimumBars()
	{
		var baseCount = BaseLength + FirstSmoothingLength + SecondSmoothingLength + ThirdSmoothingLength + SignalBar + 3;
		return Math.Max(baseCount, GetRequiredBufferLength());
	}

	private int GetRequiredBufferLength()
	{
		var signalBar = Math.Max(0, SignalBar);

		return Mode switch
		{
			BlauErgodicMdiModes.Twist => signalBar + 3,
			_ => signalBar + 2,
		};
	}

	private void PushValues(decimal hist, decimal signal, DateTimeOffset time, int requiredLength)
	{
		if (requiredLength <= 0)
		{
			requiredLength = 1;
		}

		if (_histBuffer.Length < requiredLength)
		{
			Array.Resize(ref _histBuffer, requiredLength);
		}

		if (_signalBuffer.Length < requiredLength)
		{
			Array.Resize(ref _signalBuffer, requiredLength);
		}

		if (_timeBuffer.Length < requiredLength)
		{
			Array.Resize(ref _timeBuffer, requiredLength);
		}

		var limit = Math.Min(_bufferCount, requiredLength - 1);

		for (var i = limit; i > 0; i--)
		{
			_histBuffer[i] = _histBuffer[i - 1];
			_signalBuffer[i] = _signalBuffer[i - 1];
			_timeBuffer[i] = _timeBuffer[i - 1];
		}

		_histBuffer[0] = hist;
		_signalBuffer[0] = signal;
		_timeBuffer[0] = time;

		_bufferCount = Math.Min(requiredLength, _bufferCount + 1);
	}

	private bool HasSufficientData(int index)
	{
		return _bufferCount > index;
	}

	private bool InTradeWindow(DateTimeOffset time)
	{
		if (!UseTimeFilter)
		{
			return true;
		}

		var hour = time.Hour;
		var minute = time.Minute;

		if (StartHour < EndHour)
		{
			if (hour == StartHour && minute >= StartMinute)
			{
				return true;
			}

			if (hour > StartHour && hour < EndHour)
			{
				return true;
			}

			if (hour > StartHour && hour == EndHour && minute < EndMinute)
			{
				return true;
			}

			return false;
		}

		if (StartHour == EndHour)
		{
			return hour == StartHour && minute >= StartMinute && minute < EndMinute;
		}

		if (hour >= StartHour && minute >= StartMinute)
		{
			return true;
		}

		if (hour < EndHour)
		{
			return true;
		}

		if (hour == EndHour && minute < EndMinute)
		{
			return true;
		}

		return false;
	}

	private void CloseAllPositions()
	{
		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
			ResetRiskLevels();
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskLevels();
		}
	}

	private void SetRiskForLong(decimal entryPrice)
	{
		var step = GetPointValue();

		_longStopPrice = StopLossPoints > 0 ? entryPrice - StopLossPoints * step : null;
		_longTakePrice = TakeProfitPoints > 0 ? entryPrice + TakeProfitPoints * step : null;

		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private void SetRiskForShort(decimal entryPrice)
	{
		var step = GetPointValue();

		_shortStopPrice = StopLossPoints > 0 ? entryPrice + StopLossPoints * step : null;
		_shortTakePrice = TakeProfitPoints > 0 ? entryPrice - TakeProfitPoints * step : null;

		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ResetRiskLevels()
	{
		_entryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private bool CheckRiskManagement(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetRiskLevels();
				return true;
			}

			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetRiskLevels();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetRiskLevels();
				return true;
			}

			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetRiskLevels();
				return true;
			}
		}

		return false;
	}

	private TimeSpan GetCandleSpan()
	{
		return CandleType.Arg switch
		{
			TimeSpan span => span,
			_ => TimeSpan.Zero,
		};
	}
}