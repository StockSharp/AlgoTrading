namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Blau Candle Stochastic Index strategy converted from MetaTrader 5.
/// </summary>
public class ExpBlauCsiStrategy : Strategy
{
	private readonly StrategyParam<BlauCsiEntryMode> _entryMode;
	private readonly StrategyParam<BlauCsiSmoothMethod> _smoothMethod;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _firstSmoothLength;
	private readonly StrategyParam<int> _secondSmoothLength;
	private readonly StrategyParam<int> _thirdSmoothLength;
	private readonly StrategyParam<int> _smoothingPhase;
	private readonly StrategyParam<BlauCsiAppliedPrice> _firstPrice;
	private readonly StrategyParam<BlauCsiAppliedPrice> _secondPrice;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private BlauCsiIndicator _blauCsi = null!;
	private readonly List<decimal> _indicatorValues = new();
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpBlauCsiStrategy"/> class.
	/// </summary>
	public ExpBlauCsiStrategy()
	{
		_entryMode = Param(nameof(EntryMode), BlauCsiEntryMode.Twist)
			.SetDisplay("Entry Mode", "Zero cross or direction change logic", "Parameters");

		_smoothMethod = Param(nameof(SmoothingMethod), BlauCsiSmoothMethod.Exponential)
			.SetDisplay("Smoothing Method", "Moving average type used inside Blau CSI", "Indicator");

		_momentumLength = Param(nameof(MomentumLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Number of bars for momentum calculation", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_firstSmoothLength = Param(nameof(FirstSmoothingLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("First Smoothing", "Depth of first smoothing stage", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_secondSmoothLength = Param(nameof(SecondSmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Second Smoothing", "Depth of second smoothing stage", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 30, 1);

		_thirdSmoothLength = Param(nameof(ThirdSmoothingLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Third Smoothing", "Depth of third smoothing stage", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_smoothingPhase = Param(nameof(SmoothingPhase), 15)
			.SetDisplay("Smoothing Phase", "Phase parameter used by Jurik smoothing", "Indicator");

		_firstPrice = Param(nameof(FirstPrice), BlauCsiAppliedPrice.Close)
			.SetDisplay("Momentum Price", "Price constant for the leading value", "Indicator");

		_secondPrice = Param(nameof(SecondPrice), BlauCsiAppliedPrice.Open)
			.SetDisplay("Reference Price", "Price constant for the lagging value", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Bar", "Offset in bars used to confirm a signal", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pts)", "Stop loss distance measured in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pts)", "Take profit distance measured in price steps", "Risk");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
			.SetDisplay("Allow Long Entries", "Enable opening long positions", "Trading");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
			.SetDisplay("Allow Short Entries", "Enable opening short positions", "Trading");

		_allowLongExits = Param(nameof(AllowLongExits), true)
			.SetDisplay("Allow Long Exits", "Enable closing long positions", "Trading");

		_allowShortExits = Param(nameof(AllowShortExits), true)
			.SetDisplay("Allow Short Exits", "Enable closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for indicator calculations", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Backtest start date", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "Backtest end date", "General");

		Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume sent with market orders", "Trading");
	}

	/// <summary>
	/// Entry mode determining how Blau CSI generates signals.
	/// </summary>
	public BlauCsiEntryMode EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Smoothing method used inside Blau CSI calculation.
	/// </summary>
	public BlauCsiSmoothMethod SmoothingMethod
	{
		get => _smoothMethod.Value;
		set => _smoothMethod.Value = value;
	}

	/// <summary>
	/// Momentum length controlling the lookback for price difference and range.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// First smoothing depth.
	/// </summary>
	public int FirstSmoothingLength
	{
		get => _firstSmoothLength.Value;
		set => _firstSmoothLength.Value = value;
	}

	/// <summary>
	/// Second smoothing depth.
	/// </summary>
	public int SecondSmoothingLength
	{
		get => _secondSmoothLength.Value;
		set => _secondSmoothLength.Value = value;
	}

	/// <summary>
	/// Third smoothing depth.
	/// </summary>
	public int ThirdSmoothingLength
	{
		get => _thirdSmoothLength.Value;
		set => _thirdSmoothLength.Value = value;
	}

	/// <summary>
	/// Phase parameter used by Jurik smoothing.
	/// </summary>
	public int SmoothingPhase
	{
		get => _smoothingPhase.Value;
		set => _smoothingPhase.Value = value;
	}

	/// <summary>
	/// Price constant for the leading momentum value.
	/// </summary>
	public BlauCsiAppliedPrice FirstPrice
	{
		get => _firstPrice.Value;
		set => _firstPrice.Value = value;
	}

	/// <summary>
	/// Price constant for the lagging momentum value.
	/// </summary>
	public BlauCsiAppliedPrice SecondPrice
	{
		get => _secondPrice.Value;
		set => _secondPrice.Value = value;
	}

	/// <summary>
	/// Offset in bars used when checking the Blau CSI buffer.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Stop loss size expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit size expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Toggle controlling long entries.
	/// </summary>
	public bool AllowLongEntries
	{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	/// <summary>
	/// Toggle controlling short entries.
	/// </summary>
	public bool AllowShortEntries
	{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	/// <summary>
	/// Toggle controlling long exits.
	/// </summary>
	public bool AllowLongExits
	{
		get => _allowLongExits.Value;
		set => _allowLongExits.Value = value;
	}

	/// <summary>
	/// Toggle controlling short exits.
	/// </summary>
	public bool AllowShortExits
	{
		get => _allowShortExits.Value;
		set => _allowShortExits.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Start date filter for trading.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date filter for trading.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
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

		_indicatorValues.Clear();
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_blauCsi = new BlauCsiIndicator
		{
			SmoothMethod = SmoothingMethod,
			MomentumLength = MomentumLength,
			FirstSmoothingLength = FirstSmoothingLength,
			SecondSmoothingLength = SecondSmoothingLength,
			ThirdSmoothingLength = ThirdSmoothingLength,
			Phase = SmoothingPhase,
			FirstPrice = FirstPrice,
			SecondPrice = SecondPrice
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_blauCsi, ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _blauCsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (HandleStops(candle))
			return;

		var time = candle.OpenTime;
		var inRange = time >= StartDate && time <= EndDate;

		if (!inRange)
		{
			if (Position != 0)
			{
				ClosePosition();
				ResetTargets();
			}

			return;
		}

		if (!_blauCsi.IsFormed)
			return;

		StoreIndicatorValue(indicatorValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var (openLong, openShort, closeLong, closeShort) = EvaluateSignals();

		if (closeLong && AllowLongExits && Position > 0)
		{
			SellMarket(Position);
			ResetTargets();
		}

		if (closeShort && AllowShortExits && Position < 0)
		{
			BuyMarket(-Position);
			ResetTargets();
		}

		if (openLong && AllowLongEntries && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			if (volume > 0)
			{
				BuyMarket(volume);
				SetTargets(candle.ClosePrice, true);
			}
		}
		else if (openShort && AllowShortEntries && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			if (volume > 0)
			{
				SellMarket(volume);
				SetTargets(candle.ClosePrice, false);
			}
		}
	}

	private (bool openLong, bool openShort, bool closeLong, bool closeShort) EvaluateSignals()
	{
		var required = EntryMode == BlauCsiEntryMode.Twist ? 3 : 2;
		var count = _indicatorValues.Count;

		if (SignalBar < 0)
			return (false, false, false, false);

		var signalIndex = count - 1 - SignalBar;
		if (signalIndex < required - 1)
			return (false, false, false, false);

		var openLong = false;
		var openShort = false;
		var closeLong = false;
		var closeShort = false;

		if (EntryMode == BlauCsiEntryMode.Breakdown)
		{
			var current = _indicatorValues[signalIndex];
			var previous = _indicatorValues[signalIndex - 1];

			if (previous > 0m)
			{
				if (current <= 0m)
					openLong = true;

				closeShort = true;
			}

			if (previous < 0m)
			{
				if (current >= 0m)
					openShort = true;

				closeLong = true;
			}
		}
		else
		{
			var current = _indicatorValues[signalIndex];
			var previous = _indicatorValues[signalIndex - 1];
			var older = _indicatorValues[signalIndex - 2];

			if (previous < older)
			{
				if (current >= previous)
					openLong = true;

				closeShort = true;
			}

			if (previous > older)
			{
				if (current <= previous)
					openShort = true;

				closeLong = true;
			}
		}

		return (openLong, openShort, closeLong, closeShort);
	}

	private bool HandleStops(ICandleMessage candle)
	{
		var triggered = false;

		if (Position > 0)
		{
			if (_stopPrice != null && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				triggered = true;
			}
			else if (_takePrice != null && candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				triggered = true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice != null && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(-Position);
				triggered = true;
			}
			else if (_takePrice != null && candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				triggered = true;
			}
		}

		if (triggered)
			ResetTargets();

		return triggered;
	}

	private void SetTargets(decimal entryPrice, bool isLong)
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
		{
			_stopPrice = null;
			_takePrice = null;
			return;
		}

		_stopPrice = StopLossPoints > 0
			? isLong ? entryPrice - StopLossPoints * step : entryPrice + StopLossPoints * step
			: null;

		_takePrice = TakeProfitPoints > 0
			? isLong ? entryPrice + TakeProfitPoints * step : entryPrice - TakeProfitPoints * step
			: null;
	}

	private void ResetTargets()
	{
		_stopPrice = null;
		_takePrice = null;
	}

	private void StoreIndicatorValue(decimal value)
	{
		_indicatorValues.Add(value);

		var keep = SignalBar + (EntryMode == BlauCsiEntryMode.Twist ? 3 : 2) + 5;
		if (keep < 10)
			keep = 10;

		if (_indicatorValues.Count > keep)
			_indicatorValues.RemoveRange(0, _indicatorValues.Count - keep);
	}
}

/// <summary>
/// Available entry modes for the Blau CSI strategy.
/// </summary>
public enum BlauCsiEntryMode
{
	/// <summary>
	/// Use zero level breakdowns as signals.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Use direction changes (twists) as signals.
	/// </summary>
	Twist
}

/// <summary>
/// Applied price constants supported by the Blau CSI indicator.
/// </summary>
public enum BlauCsiAppliedPrice
{
	/// <summary>
	/// Close price.
	/// </summary>
	Close = 1,

	/// <summary>
	/// Open price.
	/// </summary>
	Open = 2,

	/// <summary>
	/// High price.
	/// </summary>
	High = 3,

	/// <summary>
	/// Low price.
	/// </summary>
	Low = 4,

	/// <summary>
	/// Median price = (High + Low) / 2.
	/// </summary>
	Median = 5,

	/// <summary>
	/// Typical price = (High + Low + Close) / 3.
	/// </summary>
	Typical = 6,

	/// <summary>
	/// Weighted close price = (2 * Close + High + Low) / 4.
	/// </summary>
	Weighted = 7,

	/// <summary>
	/// Average of open and close.
	/// </summary>
	Simple = 8,

	/// <summary>
	/// Average of open, high, low, and close.
	/// </summary>
	Quarter = 9,

	/// <summary>
	/// Trend-following price variant 0.
	/// </summary>
	TrendFollow0 = 10,

	/// <summary>
	/// Trend-following price variant 1.
	/// </summary>
	TrendFollow1 = 11,

	/// <summary>
	/// Demark price.
	/// </summary>
	Demark = 12
}

/// <summary>
/// Smoothing methods available for Blau CSI.
/// </summary>
public enum BlauCsiSmoothMethod
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
	/// Smoothed moving average (RMA).
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	LinearWeighted,

	/// <summary>
	/// Jurik moving average.
	/// </summary>
	Jurik
}

/// <summary>
/// Blau Candle Stochastic Index implementation.
/// </summary>
public class BlauCsiIndicator : BaseIndicator<decimal>
{
	private readonly Queue<ICandleMessage> _window = new();
	private IIndicator? _momentumStage1;
	private IIndicator? _momentumStage2;
	private IIndicator? _momentumStage3;
	private IIndicator? _rangeStage1;
	private IIndicator? _rangeStage2;
	private IIndicator? _rangeStage3;

	/// <summary>
	/// Selected smoothing method.
	/// </summary>
	public BlauCsiSmoothMethod SmoothMethod { get; set; } = BlauCsiSmoothMethod.Exponential;

	/// <summary>
	/// Momentum length.
	/// </summary>
	public int MomentumLength { get; set; } = 1;

	/// <summary>
	/// First smoothing length.
	/// </summary>
	public int FirstSmoothingLength { get; set; } = 20;

	/// <summary>
	/// Second smoothing length.
	/// </summary>
	public int SecondSmoothingLength { get; set; } = 5;

	/// <summary>
	/// Third smoothing length.
	/// </summary>
	public int ThirdSmoothingLength { get; set; } = 3;

	/// <summary>
	/// Phase parameter for Jurik average.
	/// </summary>
	public int Phase { get; set; } = 15;

	/// <summary>
	/// Price constant used for the leading price.
	/// </summary>
	public BlauCsiAppliedPrice FirstPrice { get; set; } = BlauCsiAppliedPrice.Close;

	/// <summary>
	/// Price constant used for the lagging price.
	/// </summary>
	public BlauCsiAppliedPrice SecondPrice { get; set; } = BlauCsiAppliedPrice.Open;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DecimalIndicatorValue(this, default, input.Time);

		if (_momentumStage1 == null)
			Initialize();

		_window.Enqueue(candle);
		while (_window.Count > Math.Max(MomentumLength, 1))
			_window.Dequeue();

		if (_window.Count < Math.Max(MomentumLength, 1))
		{
			IsFormed = false;
			return new DecimalIndicatorValue(this, default, input.Time);
		}

		var currentPrice = GetPrice(candle, FirstPrice);
		var pastCandle = _window.Peek();
		var pastPrice = GetPrice(pastCandle, SecondPrice);

		var min = decimal.MaxValue;
		var max = decimal.MinValue;

		foreach (var item in _window)
		{
			if (item.LowPrice < min)
				min = item.LowPrice;

			if (item.HighPrice > max)
				max = item.HighPrice;
		}

		var range = max - min;
		var momentum = currentPrice - pastPrice;

		var time = input.Time;
		var m1 = _momentumStage1!.Process(momentum, time, true).ToDecimal();
		var r1 = _rangeStage1!.Process(range, time, true).ToDecimal();

		var m2 = _momentumStage2!.Process(m1, time, true).ToDecimal();
		var r2 = _rangeStage2!.Process(r1, time, true).ToDecimal();

		var m3 = _momentumStage3!.Process(m2, time, true).ToDecimal();
		var r3 = _rangeStage3!.Process(r2, time, true).ToDecimal();

		decimal value;
		if (r3 != 0m)
			value = 100m * m3 / r3;
		else
			value = 0m;

		IsFormed = _momentumStage3.IsFormed && _rangeStage3.IsFormed;
		return new DecimalIndicatorValue(this, value, input.Time);
	}

	private void Initialize()
	{
		_momentumStage1 = CreateSmoother(FirstSmoothingLength);
		_momentumStage2 = CreateSmoother(SecondSmoothingLength);
		_momentumStage3 = CreateSmoother(ThirdSmoothingLength);

		_rangeStage1 = CreateSmoother(FirstSmoothingLength);
		_rangeStage2 = CreateSmoother(SecondSmoothingLength);
		_rangeStage3 = CreateSmoother(ThirdSmoothingLength);
	}

	private IIndicator CreateSmoother(int length)
	{
		return SmoothMethod switch
		{
			BlauCsiSmoothMethod.Simple => new SimpleMovingAverage { Length = length },
			BlauCsiSmoothMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			BlauCsiSmoothMethod.LinearWeighted => new LinearWeightedMovingAverage { Length = length },
			BlauCsiSmoothMethod.Jurik => new JurikMovingAverage { Length = length, Phase = Phase },
			_ => new ExponentialMovingAverage { Length = length }
		};
	}

	private static decimal GetPrice(ICandleMessage candle, BlauCsiAppliedPrice price)
	{
		return price switch
		{
			BlauCsiAppliedPrice.Close => candle.ClosePrice,
			BlauCsiAppliedPrice.Open => candle.OpenPrice,
			BlauCsiAppliedPrice.High => candle.HighPrice,
			BlauCsiAppliedPrice.Low => candle.LowPrice,
			BlauCsiAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			BlauCsiAppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			BlauCsiAppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			BlauCsiAppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			BlauCsiAppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			BlauCsiAppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
				? candle.HighPrice
				: candle.ClosePrice < candle.OpenPrice
					? candle.LowPrice
					: candle.ClosePrice,
			BlauCsiAppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: candle.ClosePrice < candle.OpenPrice
					? (candle.LowPrice + candle.ClosePrice) / 2m
					: candle.ClosePrice,
			BlauCsiAppliedPrice.Demark => GetDemarkPrice(candle),
			_ => candle.ClosePrice
		};
	}

	private static decimal GetDemarkPrice(ICandleMessage candle)
	{
		var sum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
			sum = (sum + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			sum = (sum + candle.HighPrice) / 2m;
		else
			sum = (sum + candle.ClosePrice) / 2m;

		return ((sum - candle.LowPrice) + (sum - candle.HighPrice)) / 2m;
	}
}
