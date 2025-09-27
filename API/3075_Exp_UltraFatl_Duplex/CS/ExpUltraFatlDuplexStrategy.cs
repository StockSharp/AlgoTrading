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

using StockSharp.Algo;

/// <summary>
/// Conversion of the MetaTrader strategy "Exp_UltraFatl_Duplex".
/// The logic runs the UltraFATL histogram twice with separate parameter blocks for long and short trades.
/// Signals are generated from the balance between smoothed bullish and bearish counters.
/// </summary>
public class ExpUltraFatlDuplexStrategy : Strategy
{
	private readonly StrategyParam<decimal> _longVolume;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<AppliedPrice> _longAppliedPrice;
	private readonly StrategyParam<UltraSmoothMethod> _longTrendMethod;
	private readonly StrategyParam<int> _longStartLength;
	private readonly StrategyParam<int> _longPhase;
	private readonly StrategyParam<int> _longStep;
	private readonly StrategyParam<int> _longStepsTotal;
	private readonly StrategyParam<UltraSmoothMethod> _longSmoothMethod;
	private readonly StrategyParam<int> _longSmoothLength;
	private readonly StrategyParam<int> _longSmoothPhase;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<int> _longStopLossPoints;
	private readonly StrategyParam<int> _longTakeProfitPoints;

	private readonly StrategyParam<decimal> _shortVolume;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<AppliedPrice> _shortAppliedPrice;
	private readonly StrategyParam<UltraSmoothMethod> _shortTrendMethod;
	private readonly StrategyParam<int> _shortStartLength;
	private readonly StrategyParam<int> _shortPhase;
	private readonly StrategyParam<int> _shortStep;
	private readonly StrategyParam<int> _shortStepsTotal;
	private readonly StrategyParam<UltraSmoothMethod> _shortSmoothMethod;
	private readonly StrategyParam<int> _shortSmoothLength;
	private readonly StrategyParam<int> _shortSmoothPhase;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<int> _shortStopLossPoints;
	private readonly StrategyParam<int> _shortTakeProfitPoints;

	private UltraFatlContext _longContext;
	private UltraFatlContext _shortContext;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _priceStep;
	private bool _priceChartInitialized;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpUltraFatlDuplexStrategy"/> class.
	/// </summary>
	public ExpUltraFatlDuplexStrategy()
	{
		_longVolume = Param(nameof(LongVolume), 0.1m)
			.SetNotNegative()
			.SetDisplay("Long Volume", "Order volume for long entries.", "Long");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
			.SetDisplay("Allow Long Entries", "Enable opening long positions.", "Long");

		_allowLongExits = Param(nameof(AllowLongExits), true)
			.SetDisplay("Allow Long Exits", "Enable closing long positions on opposite signals.", "Long");

		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(12).TimeFrame())
			.SetDisplay("Long Candle Type", "Timeframe used by the long UltraFATL block.", "Long");

		_longAppliedPrice = Param(nameof(LongAppliedPrice), AppliedPrice.Close)
			.SetDisplay("Long Applied Price", "Price source fed into the long UltraFATL filter.", "Long");

		_longTrendMethod = Param(nameof(LongTrendMethod), UltraSmoothMethod.Jurik)
			.SetDisplay("Long Trend Method", "Smoothing method for the long FATL ladder.", "Long");

		_longStartLength = Param(nameof(LongStartLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Long Start Length", "Initial smoothing length for the ladder.", "Long");

		_longPhase = Param(nameof(LongPhase), 100)
			.SetDisplay("Long Phase", "Phase parameter applied to Jurik-based smoothers.", "Long");

		_longStep = Param(nameof(LongStep), 5)
			.SetGreaterThanZero()
			.SetDisplay("Long Step", "Increment between ladder lengths.", "Long");

		_longStepsTotal = Param(nameof(LongStepsTotal), 10)
			.SetGreaterThanZero()
			.SetDisplay("Long Steps", "Number of smoothing steps for the ladder.", "Long");

		_longSmoothMethod = Param(nameof(LongSmoothMethod), UltraSmoothMethod.Jurik)
			.SetDisplay("Long Counter Method", "Method applied to the bullish/bearish counters.", "Long");

		_longSmoothLength = Param(nameof(LongSmoothLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Long Counter Length", "Length used when smoothing the counters.", "Long");

		_longSmoothPhase = Param(nameof(LongSmoothPhase), 100)
			.SetDisplay("Long Counter Phase", "Phase parameter for the counter smoother.", "Long");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Long Signal Bar", "Closed-bar offset used when evaluating long signals.", "Long");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Long Stop (pts)", "Protective stop distance in price steps for long trades.", "Long");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 2000)
			.SetNotNegative()
			.SetDisplay("Long Target (pts)", "Take-profit distance in price steps for long trades.", "Long");

		_shortVolume = Param(nameof(ShortVolume), 0.1m)
			.SetNotNegative()
			.SetDisplay("Short Volume", "Order volume for short entries.", "Short");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
			.SetDisplay("Allow Short Entries", "Enable opening short positions.", "Short");

		_allowShortExits = Param(nameof(AllowShortExits), true)
			.SetDisplay("Allow Short Exits", "Enable closing short positions on opposite signals.", "Short");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(12).TimeFrame())
			.SetDisplay("Short Candle Type", "Timeframe used by the short UltraFATL block.", "Short");

		_shortAppliedPrice = Param(nameof(ShortAppliedPrice), AppliedPrice.Close)
			.SetDisplay("Short Applied Price", "Price source fed into the short UltraFATL filter.", "Short");

		_shortTrendMethod = Param(nameof(ShortTrendMethod), UltraSmoothMethod.Jurik)
			.SetDisplay("Short Trend Method", "Smoothing method for the short FATL ladder.", "Short");

		_shortStartLength = Param(nameof(ShortStartLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short Start Length", "Initial smoothing length for the short ladder.", "Short");

		_shortPhase = Param(nameof(ShortPhase), 100)
			.SetDisplay("Short Phase", "Phase parameter applied to the short Jurik-based smoothers.", "Short");

		_shortStep = Param(nameof(ShortStep), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short Step", "Increment between smoothing lengths for the short ladder.", "Short");

		_shortStepsTotal = Param(nameof(ShortStepsTotal), 10)
			.SetGreaterThanZero()
			.SetDisplay("Short Steps", "Number of smoothing steps for the short ladder.", "Short");

		_shortSmoothMethod = Param(nameof(ShortSmoothMethod), UltraSmoothMethod.Jurik)
			.SetDisplay("Short Counter Method", "Method applied to the bearish counters.", "Short");

		_shortSmoothLength = Param(nameof(ShortSmoothLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Short Counter Length", "Length used when smoothing the short counters.", "Short");

		_shortSmoothPhase = Param(nameof(ShortSmoothPhase), 100)
			.SetDisplay("Short Counter Phase", "Phase parameter for the short counter smoother.", "Short");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Short Signal Bar", "Closed-bar offset used when evaluating short signals.", "Short");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Short Stop (pts)", "Protective stop distance in price steps for short trades.", "Short");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 2000)
			.SetNotNegative()
			.SetDisplay("Short Target (pts)", "Take-profit distance in price steps for short trades.", "Short");
	}

	/// <summary>Volume used for long entries.</summary>
	public decimal LongVolume { get => _longVolume.Value; set => _longVolume.Value = value; }

	/// <summary>Enable long-side entries.</summary>
	public bool AllowLongEntries { get => _allowLongEntries.Value; set => _allowLongEntries.Value = value; }

	/// <summary>Enable long-side exits.</summary>
	public bool AllowLongExits { get => _allowLongExits.Value; set => _allowLongExits.Value = value; }

	/// <summary>Candle type for the long indicator.</summary>
	public DataType LongCandleType { get => _longCandleType.Value; set => _longCandleType.Value = value; }

	/// <summary>Applied price for the long ladder.</summary>
	public AppliedPrice LongAppliedPrice { get => _longAppliedPrice.Value; set => _longAppliedPrice.Value = value; }

	/// <summary>Smoothing method for the long ladder.</summary>
	public UltraSmoothMethod LongTrendMethod { get => _longTrendMethod.Value; set => _longTrendMethod.Value = value; }

	/// <summary>Initial length for the long ladder.</summary>
	public int LongStartLength { get => _longStartLength.Value; set => _longStartLength.Value = value; }

	/// <summary>Phase parameter for the long ladder.</summary>
	public int LongPhase { get => _longPhase.Value; set => _longPhase.Value = value; }

	/// <summary>Increment between smoothing lengths for the long ladder.</summary>
	public int LongStep { get => _longStep.Value; set => _longStep.Value = value; }

	/// <summary>Total number of smoothing steps for the long ladder.</summary>
	public int LongStepsTotal { get => _longStepsTotal.Value; set => _longStepsTotal.Value = value; }

	/// <summary>Smoothing method for the long counters.</summary>
	public UltraSmoothMethod LongSmoothMethod { get => _longSmoothMethod.Value; set => _longSmoothMethod.Value = value; }

	/// <summary>Length applied to the long counters.</summary>
	public int LongSmoothLength { get => _longSmoothLength.Value; set => _longSmoothLength.Value = value; }

	/// <summary>Phase parameter for the long counter smoother.</summary>
	public int LongSmoothPhase { get => _longSmoothPhase.Value; set => _longSmoothPhase.Value = value; }

	/// <summary>Closed-bar offset when checking long signals.</summary>
	public int LongSignalBar { get => _longSignalBar.Value; set => _longSignalBar.Value = value; }

	/// <summary>Stop-loss distance for long trades measured in price steps.</summary>
	public int LongStopLossPoints { get => _longStopLossPoints.Value; set => _longStopLossPoints.Value = value; }

	/// <summary>Take-profit distance for long trades measured in price steps.</summary>
	public int LongTakeProfitPoints { get => _longTakeProfitPoints.Value; set => _longTakeProfitPoints.Value = value; }

	/// <summary>Volume used for short entries.</summary>
	public decimal ShortVolume { get => _shortVolume.Value; set => _shortVolume.Value = value; }

	/// <summary>Enable short-side entries.</summary>
	public bool AllowShortEntries { get => _allowShortEntries.Value; set => _allowShortEntries.Value = value; }

	/// <summary>Enable short-side exits.</summary>
	public bool AllowShortExits { get => _allowShortExits.Value; set => _allowShortExits.Value = value; }

	/// <summary>Candle type for the short indicator.</summary>
	public DataType ShortCandleType { get => _shortCandleType.Value; set => _shortCandleType.Value = value; }

	/// <summary>Applied price for the short ladder.</summary>
	public AppliedPrice ShortAppliedPrice { get => _shortAppliedPrice.Value; set => _shortAppliedPrice.Value = value; }

	/// <summary>Smoothing method for the short ladder.</summary>
	public UltraSmoothMethod ShortTrendMethod { get => _shortTrendMethod.Value; set => _shortTrendMethod.Value = value; }

	/// <summary>Initial length for the short ladder.</summary>
	public int ShortStartLength { get => _shortStartLength.Value; set => _shortStartLength.Value = value; }

	/// <summary>Phase parameter for the short ladder.</summary>
	public int ShortPhase { get => _shortPhase.Value; set => _shortPhase.Value = value; }

	/// <summary>Increment between smoothing lengths for the short ladder.</summary>
	public int ShortStep { get => _shortStep.Value; set => _shortStep.Value = value; }

	/// <summary>Total number of smoothing steps for the short ladder.</summary>
	public int ShortStepsTotal { get => _shortStepsTotal.Value; set => _shortStepsTotal.Value = value; }

	/// <summary>Smoothing method for the short counters.</summary>
	public UltraSmoothMethod ShortSmoothMethod { get => _shortSmoothMethod.Value; set => _shortSmoothMethod.Value = value; }

	/// <summary>Length applied to the short counters.</summary>
	public int ShortSmoothLength { get => _shortSmoothLength.Value; set => _shortSmoothLength.Value = value; }

	/// <summary>Phase parameter for the short counter smoother.</summary>
	public int ShortSmoothPhase { get => _shortSmoothPhase.Value; set => _shortSmoothPhase.Value = value; }

	/// <summary>Closed-bar offset when checking short signals.</summary>
	public int ShortSignalBar { get => _shortSignalBar.Value; set => _shortSignalBar.Value = value; }

	/// <summary>Stop-loss distance for short trades measured in price steps.</summary>
	public int ShortStopLossPoints { get => _shortStopLossPoints.Value; set => _shortStopLossPoints.Value = value; }

	/// <summary>Take-profit distance for short trades measured in price steps.</summary>
	public int ShortTakeProfitPoints { get => _shortTakeProfitPoints.Value; set => _shortTakeProfitPoints.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, LongCandleType);

		if (!Equals(LongCandleType, ShortCandleType))
			yield return (Security, ShortCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longContext?.Dispose();
		_shortContext?.Dispose();
		_longContext = null;
		_shortContext = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_priceStep = 0m;
		_priceChartInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.Step ?? 0m;
		Volume = Math.Max(LongVolume, ShortVolume);

		_longContext = new UltraFatlContext(this, true, LongCandleType, LongAppliedPrice, LongTrendMethod,
			LongStartLength, LongPhase, LongStep, LongStepsTotal, LongSmoothMethod, LongSmoothLength,
			LongSmoothPhase, LongSignalBar, LongVolume, AllowLongEntries, AllowLongExits,
			LongStopLossPoints, LongTakeProfitPoints, _priceStep);

		_shortContext = new UltraFatlContext(this, false, ShortCandleType, ShortAppliedPrice, ShortTrendMethod,
			ShortStartLength, ShortPhase, ShortStep, ShortStepsTotal, ShortSmoothMethod, ShortSmoothLength,
			ShortSmoothPhase, ShortSignalBar, ShortVolume, AllowShortEntries, AllowShortExits,
			ShortStopLossPoints, ShortTakeProfitPoints, _priceStep);

		_longContext.Start();
		_shortContext.Start();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var price = trade.Trade?.Price ?? trade.Order.Price ?? 0m;

		if (trade.Order.Direction == Sides.Buy)
		{
			if (Position > 0m)
				_longEntryPrice = price;

			if (Position >= 0m)
				_shortEntryPrice = Position == 0m ? null : _shortEntryPrice;
		}
		else if (trade.Order.Direction == Sides.Sell)
		{
			if (Position < 0m)
				_shortEntryPrice = price;

			if (Position <= 0m)
				_longEntryPrice = Position == 0m ? null : _longEntryPrice;
		}
	}

	private void ProcessDirectionalSignal(bool isLong, bool openSignal, bool closeSignal, UltraFatlSnapshot snapshot)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (isLong)
		{
			if (closeSignal && AllowLongExits && Position > 0m)
			{
				SellMarket(Position);
				_longEntryPrice = null;
			}

			if (openSignal && AllowLongEntries && Position <= 0m && LongVolume > 0m)
			{
				BuyMarket(LongVolume);
				_longEntryPrice = snapshot.ClosePrice;
			}
		}
		else
		{
			if (closeSignal && AllowShortExits && Position < 0m)
			{
				BuyMarket(-Position);
				_shortEntryPrice = null;
			}

			if (openSignal && AllowShortEntries && Position >= 0m && ShortVolume > 0m)
			{
				SellMarket(ShortVolume);
				_shortEntryPrice = snapshot.ClosePrice;
			}
		}
	}

	private void CheckStops(bool isLong, ICandleMessage candle, int stopLossPoints, int takeProfitPoints, decimal priceStep)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (priceStep <= 0m)
			return;

		if (isLong)
		{
			if (Position <= 0m || _longEntryPrice is null)
				return;

			var stopLossPrice = stopLossPoints > 0 ? _longEntryPrice.Value - stopLossPoints * priceStep : (decimal?)null;
			var takeProfitPrice = takeProfitPoints > 0 ? _longEntryPrice.Value + takeProfitPoints * priceStep : (decimal?)null;

			if (stopLossPrice.HasValue && candle.LowPrice <= stopLossPrice.Value)
			{
				SellMarket(Position);
				_longEntryPrice = null;
				return;
			}

			if (takeProfitPrice.HasValue && candle.HighPrice >= takeProfitPrice.Value)
			{
				SellMarket(Position);
				_longEntryPrice = null;
			}
		}
		else
		{
			if (Position >= 0m || _shortEntryPrice is null)
				return;

			var stopLossPrice = stopLossPoints > 0 ? _shortEntryPrice.Value + stopLossPoints * priceStep : (decimal?)null;
			var takeProfitPrice = takeProfitPoints > 0 ? _shortEntryPrice.Value - takeProfitPoints * priceStep : (decimal?)null;

			if (stopLossPrice.HasValue && candle.HighPrice >= stopLossPrice.Value)
			{
				BuyMarket(-Position);
				_shortEntryPrice = null;
				return;
			}

			if (takeProfitPrice.HasValue && candle.LowPrice <= takeProfitPrice.Value)
			{
				BuyMarket(-Position);
				_shortEntryPrice = null;
			}
		}
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice priceMode)
	{
		return priceMode switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simplified => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice >= candle.OpenPrice ? candle.HighPrice : candle.LowPrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice >= candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: (candle.LowPrice + candle.ClosePrice) / 2m,
			AppliedPrice.DeMark => CalculateDeMarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDeMarkPrice(ICandleMessage candle)
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

	private static IndicatorBase<decimal> CreateMovingAverage(UltraSmoothMethod method, int length, int phase)
	{
		var normalizedLength = Math.Max(1, length);

		return method switch
		{
			UltraSmoothMethod.Sma => new SimpleMovingAverage { Length = normalizedLength },
			UltraSmoothMethod.Ema => new ExponentialMovingAverage { Length = normalizedLength },
			UltraSmoothMethod.Smma => new SmoothedMovingAverage { Length = normalizedLength },
			UltraSmoothMethod.Lwma => new WeightedMovingAverage { Length = normalizedLength },
			UltraSmoothMethod.Jurik => new JurikMovingAverage { Length = normalizedLength, Phase = phase },
			UltraSmoothMethod.JurX => new JurikMovingAverage { Length = normalizedLength, Phase = phase },
			UltraSmoothMethod.Parabolic => new ExponentialMovingAverage { Length = normalizedLength },
			UltraSmoothMethod.T3 => new JurikMovingAverage { Length = normalizedLength, Phase = phase },
			UltraSmoothMethod.Vidya => new ExponentialMovingAverage { Length = normalizedLength },
			UltraSmoothMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = normalizedLength },
			_ => new ExponentialMovingAverage { Length = normalizedLength },
		};
	}

	private void RegisterPriceChartOnce(MarketDataSubscription subscription)
	{
		if (_priceChartInitialized)
			return;

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);
			_priceChartInitialized = true;
		}
	}

	private readonly record struct UltraFatlSnapshot(DateTimeOffset Time, decimal Bulls, decimal Bears, decimal ClosePrice, decimal HighPrice, decimal LowPrice);

	private sealed class UltraFatlContext : IDisposable
	{
		private readonly ExpUltraFatlDuplexStrategy _strategy;
		private readonly bool _isLong;
		private readonly DataType _candleType;
		private readonly AppliedPrice _appliedPrice;
		private readonly UltraSmoothMethod _trendMethod;
		private readonly int _startLength;
		private readonly int _phase;
		private readonly int _step;
		private readonly int _stepsTotal;
		private readonly UltraSmoothMethod _smoothMethod;
		private readonly int _smoothLength;
		private readonly int _smoothPhase;
		private readonly int _signalBar;
		private readonly decimal _volume;
		private readonly bool _allowEntries;
		private readonly bool _allowExits;
		private readonly int _stopLossPoints;
		private readonly int _takeProfitPoints;
		private readonly decimal _priceStep;

		private readonly List<IndicatorBase<decimal>> _ladder = new();
		private readonly List<decimal?> _previousValues = new();
		private IndicatorBase<decimal>? _bullsSmoother;
		private IndicatorBase<decimal>? _bearsSmoother;
		private readonly List<UltraFatlSnapshot> _history = new();
		private readonly FatlFilter _fatl = new();
		private MarketDataSubscription _subscription;

		public UltraFatlContext(
			ExpUltraFatlDuplexStrategy strategy,
			bool isLong,
			DataType candleType,
			AppliedPrice appliedPrice,
			UltraSmoothMethod trendMethod,
			int startLength,
			int phase,
			int step,
			int stepsTotal,
			UltraSmoothMethod smoothMethod,
			int smoothLength,
			int smoothPhase,
			int signalBar,
			decimal volume,
			bool allowEntries,
			bool allowExits,
			int stopLossPoints,
			int takeProfitPoints,
			decimal priceStep)
		{
			_strategy = strategy;
			_isLong = isLong;
			_candleType = candleType;
			_appliedPrice = appliedPrice;
			_trendMethod = trendMethod;
			_startLength = startLength;
			_phase = phase;
			_step = step;
			_stepsTotal = stepsTotal;
			_smoothMethod = smoothMethod;
			_smoothLength = smoothLength;
			_smoothPhase = smoothPhase;
			_signalBar = signalBar;
			_volume = volume;
			_allowEntries = allowEntries;
			_allowExits = allowExits;
			_stopLossPoints = stopLossPoints;
			_takeProfitPoints = takeProfitPoints;
			_priceStep = priceStep;
		}

		public void Start()
		{
			_ladder.Clear();
			_previousValues.Clear();
			_history.Clear();
			_fatl.Reset();

			for (var i = 0; i <= _stepsTotal; i++)
			{
				var length = Math.Max(1, _startLength + i * _step);
				var indicator = CreateMovingAverage(_trendMethod, length, _phase);
				_ladder.Add(indicator);
				_previousValues.Add(null);
			}

			var counterLength = Math.Max(1, _smoothLength);
			_bullsSmoother = CreateMovingAverage(_smoothMethod, counterLength, _smoothPhase);
			_bearsSmoother = CreateMovingAverage(_smoothMethod, counterLength, _smoothPhase);

			_subscription = _strategy.SubscribeCandles(_candleType);
			_subscription.Bind(ProcessCandle).Start();

			_strategy.RegisterPriceChartOnce(_subscription);

			var indicatorArea = _strategy.CreateChartArea();
			if (indicatorArea != null)
			{
				if (_bullsSmoother != null)
					_strategy.DrawIndicator(indicatorArea, _bullsSmoother);
				if (_bearsSmoother != null)
					_strategy.DrawIndicator(indicatorArea, _bearsSmoother);
			}
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!_allowEntries && !_allowExits && _stopLossPoints <= 0 && _takeProfitPoints <= 0)
				return;

			_strategy.CheckStops(_isLong, candle, _stopLossPoints, _takeProfitPoints, _priceStep);

			if (_volume <= 0m && !_allowExits)
				return;

			var price = GetAppliedPrice(candle, _appliedPrice);
			var fatlValue = _fatl.Process(price);
			if (fatlValue is null)
				return;

			decimal upCount = 0m;
			decimal downCount = 0m;

			for (var i = 0; i < _ladder.Count; i++)
			{
				var indicatorValue = _ladder[i].Process(fatlValue.Value);
				if (!indicatorValue.IsFinal)
					return;

				var current = indicatorValue.GetValue<decimal>();

				if (_previousValues[i] is not decimal previous)
				{
					_previousValues[i] = current;
					return;
				}

				if (current > previous)
					upCount += 1m;
				else
					downCount += 1m;

				_previousValues[i] = current;
			}

			if (_bullsSmoother is null || _bearsSmoother is null)
				return;

			var bullsValue = _bullsSmoother.Process(upCount);
			var bearsValue = _bearsSmoother.Process(downCount);

			if (!bullsValue.IsFinal || !bearsValue.IsFinal)
				return;

			var bulls = bullsValue.GetValue<decimal>();
			var bears = bearsValue.GetValue<decimal>();

			_history.Add(new UltraFatlSnapshot(candle.CloseTime, bulls, bears, candle.ClosePrice, candle.HighPrice, candle.LowPrice));

			var maxHistory = Math.Max(10, Math.Max(_signalBar, 1) + 5);
			if (_history.Count > maxHistory)
				_history.RemoveRange(0, _history.Count - maxHistory);

			var effectiveShift = Math.Max(1, _signalBar);
			if (_history.Count <= effectiveShift)
				return;

			var currentIndex = _history.Count - effectiveShift;
			var previousIndex = currentIndex - 1;
			if (previousIndex < 0 || currentIndex >= _history.Count)
				return;

			var current = _history[currentIndex];
			var previous = _history[previousIndex];

			bool closeSignal;
			bool openSignal;

			if (_isLong)
			{
				closeSignal = previous.Bears > previous.Bulls;
				openSignal = previous.Bulls > previous.Bears && current.Bulls <= current.Bears;
			}
			else
			{
				closeSignal = previous.Bulls > previous.Bears;
				openSignal = previous.Bulls < previous.Bears && current.Bulls >= current.Bears;
			}

			if (!openSignal && !closeSignal)
				return;

			if (!_allowEntries)
				openSignal = false;

			if (!_allowExits)
				closeSignal = false;

			_strategy.ProcessDirectionalSignal(_isLong, openSignal, closeSignal, current);
		}

		public void Dispose()
		{
			_subscription?.Dispose();
		}
	}

	private sealed class FatlFilter
	{
		private static readonly decimal[] _coefficients =
		{
			0.4360409450m, 0.3658689069m, 0.2460452079m, 0.1104506886m,
			-0.0054034585m, -0.0760367731m, -0.0933058722m, -0.0670110374m,
			-0.0190795053m, 0.0259609206m, 0.0502044896m, 0.0477818607m,
			0.0249252327m, -0.0047706151m, -0.0272432537m, -0.0338917071m,
			-0.0244141482m, -0.0055774838m, 0.0128149838m, 0.0226522218m,
			0.0208778257m, 0.0100299086m, -0.0036771622m, -0.0136744850m,
			-0.0160483392m, -0.0108597376m, -0.0016060704m, 0.0069480557m,
			0.0110573605m, 0.0095711419m, 0.0040444064m, -0.0023824623m,
			-0.0067093714m, -0.0072003400m, -0.0047717710m, 0.0005541115m,
			0.0007860160m, 0.0130129076m, 0.0040364019m
		};

		private readonly decimal[] _buffer = new decimal[_coefficients.Length];
		private int _filled;

		public void Reset()
		{
			Array.Clear(_buffer, 0, _buffer.Length);
			_filled = 0;
		}

		public decimal? Process(decimal value)
		{
			for (var i = _buffer.Length - 1; i > 0; i--)
				_buffer[i] = _buffer[i - 1];

			_buffer[0] = value;

			if (_filled < _buffer.Length)
				_filled++;

			if (_filled < _buffer.Length)
				return null;

			decimal sum = 0m;
			for (var i = 0; i < _coefficients.Length; i++)
				sum += _coefficients[i] * _buffer[i];

			return sum;
		}
	}
}

