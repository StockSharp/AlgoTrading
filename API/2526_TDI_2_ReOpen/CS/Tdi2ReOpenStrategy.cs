using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Direction Index re-entry strategy converted from the MQL5 expert "Exp_TDI-2_ReOpen".
/// Trades based on crossings between the TDI momentum line and the TDI index line, with optional scale-in logic.
/// </summary>
public class Tdi2ReOpenStrategy : Strategy
{
	private readonly StrategyParam<decimal> _moneyManagement;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<decimal> _priceStepPoints;
	private readonly StrategyParam<bool> _buyOpenAllowed;
	private readonly StrategyParam<bool> _sellOpenAllowed;
	private readonly StrategyParam<bool> _buyCloseAllowed;
	private readonly StrategyParam<bool> _sellCloseAllowed;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SmoothMethod> _tdiMethod;
	private readonly StrategyParam<int> _tdiPeriod;
	private readonly StrategyParam<int> _tdiPhase;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<int> _signalBar;

	private decimal?[] _directionalHistory = Array.Empty<decimal?>();
	private decimal?[] _indexHistory = Array.Empty<decimal?>();
	private int _historyCount;

	private decimal _lastPosition;
	private int _longEntries;
	private int _shortEntries;
	private decimal _lastLongEntryPrice;
	private decimal _lastShortEntryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="Tdi2ReOpenStrategy"/> class.
	/// </summary>
	public Tdi2ReOpenStrategy()
	{
		_moneyManagement = Param(nameof(MoneyManagement), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Money Management", "Volume used for each market order", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 1m, 0.05m);

		_maxEntries = Param(nameof(MaxEntries), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max Entries", "Maximum number of scale-in trades per direction", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetGreaterOrEqual(0)
		.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in instrument points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(100, 2000, 100);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetGreaterOrEqual(0)
		.SetDisplay("Take Profit (points)", "Take profit distance expressed in instrument points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(200, 3000, 100);

		_slippagePoints = Param(nameof(SlippagePoints), 10)
		.SetGreaterOrEqual(0)
		.SetDisplay("Slippage (points)", "Maximum acceptable slippage when submitting market orders", "Trading");

		_priceStepPoints = Param(nameof(PriceStepPoints), 300m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Re-entry Step (points)", "Minimum favorable price movement (in points) before adding to an open position", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(50m, 500m, 50m);

		_buyOpenAllowed = Param(nameof(BuyOpenAllowed), true)
		.SetDisplay("Allow Long Entries", "Enable opening of long positions", "Permissions");

		_sellOpenAllowed = Param(nameof(SellOpenAllowed), true)
		.SetDisplay("Allow Short Entries", "Enable opening of short positions", "Permissions");

		_buyCloseAllowed = Param(nameof(BuyCloseAllowed), true)
		.SetDisplay("Allow Long Exits", "Enable closing of existing long positions", "Permissions");

		_sellCloseAllowed = Param(nameof(SellCloseAllowed), true)
		.SetDisplay("Allow Short Exits", "Enable closing of existing short positions", "Permissions");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Data series used for indicator calculations", "Data")
		.SetCanOptimize(false);

		_tdiMethod = Param(nameof(TdiMethod), SmoothMethod.Sma)
		.SetDisplay("TDI Smoothing", "Smoothing method used inside the TDI-2 indicator", "Indicator")
		.SetCanOptimize(true);

		_tdiPeriod = Param(nameof(TdiPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("TDI Period", "Momentum lookback period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);

		_tdiPhase = Param(nameof(TdiPhase), 15)
		.SetDisplay("TDI Phase", "Phase parameter used by advanced smoothing modes", "Indicator")
		.SetCanOptimize(false);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrice.Close)
		.SetDisplay("Applied Price", "Price source used by the TDI-2 indicator", "Indicator")
		.SetCanOptimize(false);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal Bar", "Number of closed candles to look back when evaluating crosses", "Indicator")
		.SetCanOptimize(false);
	}

	/// <summary>
	/// Gets or sets the money management value (per order volume).
	/// </summary>
	public decimal MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	/// <summary>
	/// Gets or sets the maximum number of entries per direction (including the initial trade).
	/// </summary>
	public int MaxEntries
	{
		get => _maxEntries.Value;
		set => _maxEntries.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop loss distance in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the take profit distance in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the slippage tolerance in points.
	/// </summary>
	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the favorable movement (in points) required before scaling into an existing position.
	/// </summary>
	public decimal PriceStepPoints
	{
		get => _priceStepPoints.Value;
		set => _priceStepPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether long entries are allowed.
	/// </summary>
	public bool BuyOpenAllowed
	{
		get => _buyOpenAllowed.Value;
		set => _buyOpenAllowed.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether short entries are allowed.
	/// </summary>
	public bool SellOpenAllowed
	{
		get => _sellOpenAllowed.Value;
		set => _sellOpenAllowed.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether long exits are allowed.
	/// </summary>
	public bool BuyCloseAllowed
	{
		get => _buyCloseAllowed.Value;
		set => _buyCloseAllowed.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether short exits are allowed.
	/// </summary>
	public bool SellCloseAllowed
	{
		get => _sellCloseAllowed.Value;
		set => _sellCloseAllowed.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Gets or sets the smoothing method used by the TDI-2 indicator.
	/// </summary>
	public SmoothMethod TdiMethod
	{
		get => _tdiMethod.Value;
		set => _tdiMethod.Value = value;
	}

	/// <summary>
	/// Gets or sets the momentum lookback period.
	/// </summary>
	public int TdiPeriod
	{
		get => _tdiPeriod.Value;
		set => _tdiPeriod.Value = value;
	}

	/// <summary>
	/// Gets or sets the smoothing phase parameter.
	/// </summary>
	public int TdiPhase
	{
		get => _tdiPhase.Value;
		set => _tdiPhase.Value = value;
	}

	/// <summary>
	/// Gets or sets the applied price used by the indicator.
	/// </summary>
	public AppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Gets or sets the signal bar offset (number of closed candles used for cross evaluation).
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_directionalHistory = Array.Empty<decimal?>();
		_indexHistory = Array.Empty<decimal?>();
		_historyCount = 0;
		_lastPosition = 0m;
		_longEntries = 0;
		_shortEntries = 0;
		_lastLongEntryPrice = 0m;
		_lastShortEntryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = MoneyManagement;

		AllocateHistoryBuffers();

		var tdiIndicator = new Tdi2Indicator
		{
			Method = TdiMethod,
			Period = TdiPeriod,
			Phase = TdiPhase,
			AppliedPrice = AppliedPrice
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(tdiIndicator, ProcessCandle)
		.Start();

		var priceStep = Security?.PriceStep ?? 1m;
		var stopLoss = StopLossPoints > 0 ? StopLossPoints * priceStep : (decimal?)null;
		var takeProfit = TakeProfitPoints > 0 ? TakeProfitPoints * priceStep : (decimal?)null;

		if (stopLoss.HasValue || takeProfit.HasValue)
		{
			StartProtection(
			takeProfit: takeProfit.HasValue ? new Unit(takeProfit.Value) : null,
			stopLoss: stopLoss.HasValue ? new Unit(stopLoss.Value) : null);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!value.IsFinal || value is not Tdi2IndicatorValue tdiValue)
		return;

		var directional = tdiValue.Directional;
		var index = tdiValue.Index;

		StoreIndicatorValues(directional, index);

		if (!HasSufficientHistory())
		return;

		var previousDirectional = GetHistoryValue(_directionalHistory, SignalBar + 1);
		var previousIndex = GetHistoryValue(_indexHistory, SignalBar + 1);
		var currentDirectional = GetHistoryValue(_directionalHistory, SignalBar);
		var currentIndex = GetHistoryValue(_indexHistory, SignalBar);

		if (previousDirectional is null || previousIndex is null || currentDirectional is null || currentIndex is null)
		return;

		var exitShort = false;
		var exitLong = false;
		var openLong = false;
		var openShort = false;

		if (previousDirectional > previousIndex)
		{
			if (SellCloseAllowed && Position < 0)
			exitShort = true;

			if (BuyOpenAllowed && Position <= 0 && currentDirectional <= currentIndex)
			openLong = true;
		}

		if (previousDirectional < previousIndex)
		{
			if (BuyCloseAllowed && Position > 0)
			exitLong = true;

			if (SellOpenAllowed && Position >= 0 && currentDirectional >= currentIndex)
			openShort = true;
		}

		if (exitLong)
		{
			SellMarket(Position);
		}

		if (exitShort)
		{
			BuyMarket(Math.Abs(Position));
		}

		var priceStep = (Security?.PriceStep ?? 1m) * PriceStepPoints;
		var closePrice = candle.ClosePrice;

		var scaleInLong = false;
		var scaleInShort = false;

		if (priceStep > 0m)
		{
			if (Position > 0m && _longEntries > 0 && _longEntries < MaxEntries && _lastLongEntryPrice > 0m)
			{
				var move = closePrice - _lastLongEntryPrice;
				if (move >= priceStep)
				scaleInLong = true;
			}

			if (Position < 0m && _shortEntries > 0 && _shortEntries < MaxEntries && _lastShortEntryPrice > 0m)
			{
				var move = _lastShortEntryPrice - closePrice;
				if (move >= priceStep)
				scaleInShort = true;
			}
		}

		if (scaleInLong)
		{
			BuyMarket(Volume);
		}

		if (scaleInShort)
		{
			SellMarket(Volume);
		}

		if (openLong)
		{
			var volume = Volume;
			if (Position < 0m)
			{
				volume += Math.Abs(Position);
			}

			if (volume > 0m)
			{
				BuyMarket(volume);
			}
		}

		if (openShort)
		{
			var volume = Volume;
			if (Position > 0m)
			{
				volume += Position;
			}

			if (volume > 0m)
			{
				SellMarket(volume);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(StockSharp.BusinessEntities.MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var currentPosition = Position;

		if (currentPosition > 0m)
		{
			if (_lastPosition <= 0m)
			{
				_longEntries = 1;
				_lastLongEntryPrice = trade.Trade.Price;
			}
			else if (currentPosition > _lastPosition)
			{
				_longEntries++;
				_lastLongEntryPrice = trade.Trade.Price;
			}
		}
		else if (currentPosition < 0m)
		{
			if (_lastPosition >= 0m)
			{
				_shortEntries = 1;
				_lastShortEntryPrice = trade.Trade.Price;
			}
			else if (currentPosition < _lastPosition)
			{
				_shortEntries++;
				_lastShortEntryPrice = trade.Trade.Price;
			}
		}
		else
		{
			_longEntries = 0;
			_shortEntries = 0;
			_lastLongEntryPrice = 0m;
			_lastShortEntryPrice = 0m;
		}

		_lastPosition = currentPosition;
	}

	private void AllocateHistoryBuffers()
	{
		var size = Math.Max(2, SignalBar + 2);
		_directionalHistory = new decimal?[size];
		_indexHistory = new decimal?[size];
		_historyCount = 0;
	}

	private void StoreIndicatorValues(decimal directional, decimal index)
	{
		var size = _directionalHistory.Length;
		var position = _historyCount % size;
		_directionalHistory[position] = directional;
		_indexHistory[position] = index;
		_historyCount++;
	}

	private bool HasSufficientHistory()
	{
		return _historyCount >= SignalBar + 2;
	}

	private decimal? GetHistoryValue(decimal?[] buffer, int shift)
	{
		if (buffer.Length == 0 || _historyCount <= shift)
		{
			return null;
		}

		var size = buffer.Length;
		var position = (_historyCount - 1 - shift) % size;
		if (position < 0)
		{
			position += size;
		}

		return buffer[position];
	}
}

/// <summary>
/// Available smoothing methods supported by the TDI-2 indicator implementation.
/// </summary>
public enum SmoothMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Sma,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Ema,

	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	Smma,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma
}

/// <summary>
/// Applied price selection for the TDI-2 indicator.
/// </summary>
public enum AppliedPrice
{
	/// <summary>
	/// Close price.
	/// </summary>
	Close = 1,

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
	/// Median price (high + low) / 2.
	/// </summary>
	Median,

	/// <summary>
	/// Typical price (high + low + close) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted close price (2 * close + high + low) / 4.
	/// </summary>
	Weighted,

	/// <summary>
	/// Average of open and close.
	/// </summary>
	Simple,

	/// <summary>
	/// Quarted price (open + close + high + low) / 4.
	/// </summary>
	Quarter,

	/// <summary>
	/// Trend-following price #1 (close price).
	/// </summary>
	TrendFollow0,

	/// <summary>
	/// Trend-following price #2 (average of close and trend-following #1).
	/// </summary>
	TrendFollow1,

	/// <summary>
	/// Demark price (close + low + high + high) / 4.
	/// </summary>
	Demark
}

/// <summary>
/// Custom implementation of the TDI-2 indicator.
/// Produces the directional momentum line and the TDI index line.
/// </summary>
public class Tdi2Indicator : BaseIndicator<decimal>
{
	private Momentum _momentum;
	private IIndicator _momentumSmoother;
	private IIndicator _absMomentumSmoother;
	private IIndicator _absMomentumDoubleSmoother;

	/// <summary>
	/// Gets or sets the smoothing method.
	/// </summary>
	public SmoothMethod Method { get; set; } = SmoothMethod.Sma;

	/// <summary>
	/// Gets or sets the momentum period.
	/// </summary>
	public int Period { get; set; } = 20;

	/// <summary>
	/// Gets or sets the smoothing phase parameter (reserved for compatibility).
	/// </summary>
	public int Phase { get; set; } = 15;

	/// <summary>
	/// Gets or sets the price source used by the indicator.
	/// </summary>
	public AppliedPrice AppliedPrice { get; set; } = AppliedPrice.Close;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new DecimalIndicatorValue(this, default, input.Time);

		EnsureIndicators();

		var price = GetAppliedPrice(candle);
		var momentumValue = _momentum.Process(new DecimalIndicatorValue(_momentum, price, input.Time));

		if (!momentumValue.IsFinal)
		return new DecimalIndicatorValue(this, default, input.Time);

		var momentum = momentumValue.ToDecimal();
		var absMomentum = Math.Abs(momentum);

		var momSmooth = _momentumSmoother.Process(new DecimalIndicatorValue(_momentumSmoother, momentum, input.Time));
		var absSmooth = _absMomentumSmoother.Process(new DecimalIndicatorValue(_absMomentumSmoother, absMomentum, input.Time));
		var absDoubleSmooth = _absMomentumDoubleSmoother.Process(new DecimalIndicatorValue(_absMomentumDoubleSmoother, absMomentum, input.Time));

		if (!momSmooth.IsFinal || !absSmooth.IsFinal || !absDoubleSmooth.IsFinal)
		return new DecimalIndicatorValue(this, default, input.Time);

		var momSum = Period * momSmooth.ToDecimal();
		var momAbsSum = Period * absSmooth.ToDecimal();
		var momAbsSum2 = 2 * Period * absDoubleSmooth.ToDecimal();

		var directional = momSum;
		var index = Math.Abs(momSum) - (momAbsSum2 - absMomentum);

		return new Tdi2IndicatorValue(this, input, directional, index);
	}

	private void EnsureIndicators()
	{
		if (_momentum != null && _momentum.Length == Period && _momentumSmoother != null && _absMomentumSmoother != null && _absMomentumDoubleSmoother != null)
		return;

		_momentum = new Momentum { Length = Period };
		_momentumSmoother = CreateMovingAverage(Period);
		_absMomentumSmoother = CreateMovingAverage(Period);
		_absMomentumDoubleSmoother = CreateMovingAverage(Period * 2);
	}

	private IIndicator CreateMovingAverage(int length)
	{
		return Method switch
		{
			SmoothMethod.Sma => new SimpleMovingAverage { Length = length },
			SmoothMethod.Ema => new ExponentialMovingAverage { Length = length },
			SmoothMethod.Smma => new SmoothedMovingAverage { Length = length },
			SmoothMethod.Lwma => new WeightedMovingAverage { Length = length },
			_ => throw new NotSupportedException($"Smoothing method {Method} is not supported."),
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
		{
			res = (res + candle.LowPrice) / 2m;
		}
		else if (candle.ClosePrice > candle.OpenPrice)
		{
			res = (res + candle.HighPrice) / 2m;
		}
		else
		{
			res = (res + candle.ClosePrice) / 2m;
		}

		return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			AppliedPrice.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}
}

/// <summary>
/// Complex indicator value holding the TDI-2 components.
/// </summary>
public class Tdi2IndicatorValue : ComplexIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Tdi2IndicatorValue"/> class.
	/// </summary>
	public Tdi2IndicatorValue(IIndicator indicator, IIndicatorValue input, decimal directional, decimal index)
	: base(indicator, input, (nameof(Directional), directional), (nameof(Index), index))
	{
	}

	/// <summary>
	/// Gets the directional momentum line value.
	/// </summary>
	public decimal Directional => (decimal)GetValue(nameof(Directional));

	/// <summary>
	/// Gets the TDI index line value.
	/// </summary>
	public decimal Index => (decimal)GetValue(nameof(Index));
}
