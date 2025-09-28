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
/// Cronex RSI crossover strategy converted from the MQL5 expert Exp_CronexRSI.mq5.
/// The strategy smooths the RSI value twice and reacts to fast/slow crossovers with optional trade filters.
/// </summary>
public class CronexRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<CronexSmoothingMethods> _smoothingMethod;
	private readonly StrategyParam<AppliedPriceTypes> _appliedPrice;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;

	private RelativeStrengthIndex _rsi = null!;
	private LengthIndicator<decimal> _fastSmoothing = null!;
	private LengthIndicator<decimal> _slowSmoothing = null!;

	private decimal?[] _fastHistory = Array.Empty<decimal?>();
	private decimal?[] _slowHistory = Array.Empty<decimal?>();

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Fast smoothing period applied to RSI output.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothing period applied on top of the fast series.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Number of closed bars used to confirm signals (0 reacts immediately).
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Smoothing method replicated from the Cronex RSI indicator.
	/// </summary>
	public CronexSmoothingMethods SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Applied price used for RSI calculations.
	/// </summary>
	public AppliedPriceTypes AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
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
	/// Base trade volume used when opening new positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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
	/// Allow closing long positions on sell signals.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on buy signals.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CronexRsiStrategy"/> class.
	/// </summary>
	public CronexRsiStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Lookback period for the RSI calculation", "Indicators")
			.SetCanOptimize(true);

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "First smoothing period", "Indicators")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Second smoothing period", "Indicators")
			.SetCanOptimize(true);

		_signalShift = Param(nameof(SignalShift), 1)
			.SetNotNegative()
			.SetDisplay("Signal Shift", "Number of completed bars used for confirmation", "Trading");

		_smoothingMethod = Param(nameof(SmoothingMethod), CronexSmoothingMethods.Simple)
			.SetDisplay("Smoothing Method", "Moving average type applied to the RSI", "Indicators");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceTypes.Close)
			.SetDisplay("Applied Price", "Price component passed to the RSI", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculations", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetNotNegative()
			.SetDisplay("Trade Volume", "Volume used for new entries", "Risk")
			.SetCanOptimize(true);

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
			.SetDisplay("Enable Long Exit", "Close long positions when a sell signal appears", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
			.SetDisplay("Enable Short Exit", "Close short positions when a buy signal appears", "Trading");
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

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_fastSmoothing = CreateSmoothingIndicator(SmoothingMethod, FastPeriod);
		_slowSmoothing = CreateSmoothingIndicator(SmoothingMethod, SlowPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _fastSmoothing);
			DrawIndicator(area, _slowSmoothing);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = GetAppliedPrice(candle, AppliedPrice);
		var rsiValue = _rsi.Process(new DecimalIndicatorValue(_rsi, price, candle.OpenTime));
		if (rsiValue is not DecimalIndicatorValue { IsFinal: true, Value: var rsi })
			return;

		var fastValue = _fastSmoothing.Process(new DecimalIndicatorValue(_fastSmoothing, rsi, candle.OpenTime));
		if (fastValue is not DecimalIndicatorValue { IsFinal: true, Value: var fast })
			return;

		var slowValue = _slowSmoothing.Process(new DecimalIndicatorValue(_slowSmoothing, fast, candle.OpenTime));
		if (slowValue is not DecimalIndicatorValue { IsFinal: true, Value: var slow })
			return;

		UpdateHistory(fast, slow);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!TryGetShiftedValues(out var fastCurrent, out var fastPrevious, out var slowCurrent, out var slowPrevious))
			return;

		var buySignal = fastPrevious > slowPrevious && fastCurrent <= slowCurrent;
		var sellSignal = fastPrevious < slowPrevious && fastCurrent >= slowCurrent;

		if (buySignal)
		{
			HandleBuySignal();
		}
		else if (sellSignal)
		{
			HandleSellSignal();
		}
	}

	private void HandleBuySignal()
	{
		if (EnableShortExit && Position < 0m)
		{
			var coverVolume = Math.Abs(Position);
			if (coverVolume > 0m)
			{
				// Close existing short exposure before reversing.
				BuyMarket(coverVolume);
			}
		}

		if (!EnableLongEntry || TradeVolume <= 0m)
			return;

		if (Position <= 0m)
		{
			var volume = TradeVolume + Math.Max(0m, -Position);
			if (volume > 0m)
			{
				// Enter or increase the long position after a bullish crossover.
				BuyMarket(volume);
			}
		}
	}

	private void HandleSellSignal()
	{
		if (EnableLongExit && Position > 0m)
		{
			var exitVolume = Math.Abs(Position);
			if (exitVolume > 0m)
			{
				// Close existing long exposure before opening shorts.
				SellMarket(exitVolume);
			}
		}

		if (!EnableShortEntry || TradeVolume <= 0m)
			return;

		if (Position >= 0m)
		{
			var volume = TradeVolume + Math.Max(0m, Position);
			if (volume > 0m)
			{
				// Enter or increase the short position after a bearish crossover.
				SellMarket(volume);
			}
		}
	}

	private void UpdateHistory(decimal fast, decimal slow)
	{
		var required = Math.Max(2, SignalShift + 2);
		if (_fastHistory.Length != required)
		{
			_fastHistory = new decimal?[required];
			_slowHistory = new decimal?[required];
		}

		for (var i = _fastHistory.Length - 1; i > 0; i--)
		{
			_fastHistory[i] = _fastHistory[i - 1];
			_slowHistory[i] = _slowHistory[i - 1];
		}

		_fastHistory[0] = fast;
		_slowHistory[0] = slow;
	}

	private bool TryGetShiftedValues(out decimal fastCurrent, out decimal fastPrevious, out decimal slowCurrent, out decimal slowPrevious)
	{
		fastCurrent = 0m;
		fastPrevious = 0m;
		slowCurrent = 0m;
		slowPrevious = 0m;

		var shift = Math.Max(0, SignalShift);
		var previousIndex = shift + 1;

		if (_fastHistory.Length <= previousIndex)
			return false;

		if (_fastHistory[shift] is not decimal fastCurr ||
			_fastHistory[previousIndex] is not decimal fastPrev ||
			_slowHistory[shift] is not decimal slowCurr ||
			_slowHistory[previousIndex] is not decimal slowPrev)
		{
			return false;
		}

		fastCurrent = fastCurr;
		fastPrevious = fastPrev;
		slowCurrent = slowCurr;
		slowPrevious = slowPrev;
		return true;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceTypes type)
	{
		return type switch
		{
			AppliedPriceTypes.Open => candle.OpenPrice,
			AppliedPriceTypes.High => candle.HighPrice,
			AppliedPriceTypes.Low => candle.LowPrice,
			AppliedPriceTypes.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceTypes.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceTypes.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static LengthIndicator<decimal> CreateSmoothingIndicator(CronexSmoothingMethods method, int length)
	{
		return method switch
		{
			CronexSmoothingMethods.Exponential => new ExponentialMovingAverage { Length = length },
			CronexSmoothingMethods.Smoothed => new SmoothedMovingAverage { Length = length },
			CronexSmoothingMethods.LinearWeighted => new WeightedMovingAverage { Length = length },
			CronexSmoothingMethods.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	public enum CronexSmoothingMethods
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
	/// Linear weighted moving average.
	/// </summary>
	LinearWeighted,

	/// <summary>
	/// Volume weighted moving average as a pragmatic substitute for VIDYA and AMA options.
	/// </summary>
	VolumeWeighted,
	}

	/// <summary>
	/// Applied price selection matching the MQL5 Cronex RSI inputs.
	/// </summary>
	public enum AppliedPriceTypes
	{
	/// <summary>
	/// Close price.
	/// </summary>
	Close,

	/// <summary>
	/// Open price.
	/// </summary>
	Open,

	/// <summary>
	/// High price.
	/// </summary>
	High,

	/// <summary>
	/// Low price.
	/// </summary>
	Low,

	/// <summary>
	/// Median price = (High + Low) / 2.
	/// </summary>
	Median,

	/// <summary>
	/// Typical price = (High + Low + Close) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted close = (High + Low + 2 * Close) / 4.
	/// </summary>
	Weighted,
	}
}
