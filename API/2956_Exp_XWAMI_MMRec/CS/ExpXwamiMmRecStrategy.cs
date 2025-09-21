using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy Exp_XWAMI_MMRec.
/// Uses a four-stage smoothed momentum filter and a loss-recorder money management module.
/// The strategy flips between long and short positions on XWAMI signal crossings and scales risk after consecutive losses.
/// </summary>
public class ExpXwamiMmRecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<XwamiSmoothingMethod> _method1;
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _phase1;
	private readonly StrategyParam<XwamiSmoothingMethod> _method2;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<int> _phase2;
	private readonly StrategyParam<XwamiSmoothingMethod> _method3;
	private readonly StrategyParam<int> _length3;
	private readonly StrategyParam<int> _phase3;
	private readonly StrategyParam<XwamiSmoothingMethod> _method4;
	private readonly StrategyParam<int> _length4;
	private readonly StrategyParam<int> _phase4;
	private readonly StrategyParam<XwamiAppliedPrice> _appliedPrice;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<decimal> _normalVolume;
	private readonly StrategyParam<decimal> _reducedVolume;
	private readonly StrategyParam<int> _buyTotalTrigger;
	private readonly StrategyParam<int> _buyLossTrigger;
	private readonly StrategyParam<int> _sellTotalTrigger;
	private readonly StrategyParam<int> _sellLossTrigger;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private DifferenceCalculator _difference = null!;
	private IXwamiFilter _filter1 = null!;
	private IXwamiFilter _filter2 = null!;
	private IXwamiFilter _filter3 = null!;
	private IXwamiFilter _filter4 = null!;

	private readonly List<(decimal up, decimal down)> _history = new();
	private readonly List<decimal> _recentBuyResults = new();
	private readonly List<decimal> _recentSellResults = new();

	private decimal? _longEntryPrice;
	private decimal _longEntryVolume;
	private decimal? _shortEntryPrice;
	private decimal _shortEntryVolume;

	private decimal _priceStep;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpXwamiMmRecStrategy"/> class.
	/// </summary>
	public ExpXwamiMmRecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Source timeframe for the indicator", "General")
		.SetCanOptimize(true);

		_period = Param(nameof(Period), 1)
		.SetDisplay("Momentum Shift", "Distance between the compared prices", "Indicator")
		.SetCanOptimize(true);

		_method1 = Param(nameof(Method1), XwamiSmoothingMethod.T3)
		.SetDisplay("Smoothing 1", "First smoothing method", "Indicator");
		_length1 = Param(nameof(Length1), 4)
		.SetDisplay("Length 1", "Length of the first smoothing", "Indicator")
		.SetGreaterThanZero();
		_phase1 = Param(nameof(Phase1), 15)
		.SetDisplay("Phase 1", "Phase used by Jurik/T3 smoothers", "Indicator");

		_method2 = Param(nameof(Method2), XwamiSmoothingMethod.Jjma)
		.SetDisplay("Smoothing 2", "Second smoothing method", "Indicator");
		_length2 = Param(nameof(Length2), 13)
		.SetDisplay("Length 2", "Length of the second smoothing", "Indicator")
		.SetGreaterThanZero();
		_phase2 = Param(nameof(Phase2), 15)
		.SetDisplay("Phase 2", "Phase used by Jurik/T3 smoothers", "Indicator");

		_method3 = Param(nameof(Method3), XwamiSmoothingMethod.Jjma)
		.SetDisplay("Smoothing 3", "Third smoothing method", "Indicator");
		_length3 = Param(nameof(Length3), 13)
		.SetDisplay("Length 3", "Length of the third smoothing", "Indicator")
		.SetGreaterThanZero();
		_phase3 = Param(nameof(Phase3), 15)
		.SetDisplay("Phase 3", "Phase used by Jurik/T3 smoothers", "Indicator");

		_method4 = Param(nameof(Method4), XwamiSmoothingMethod.Jjma)
		.SetDisplay("Smoothing 4", "Fourth smoothing method", "Indicator");
		_length4 = Param(nameof(Length4), 4)
		.SetDisplay("Length 4", "Length of the fourth smoothing", "Indicator")
		.SetGreaterThanZero();
		_phase4 = Param(nameof(Phase4), 15)
		.SetDisplay("Phase 4", "Phase used by Jurik/T3 smoothers", "Indicator");

		_appliedPrice = Param(nameof(AppliedPrice), XwamiAppliedPrice.Close)
		.SetDisplay("Applied Price", "Price source forwarded into the filter", "Indicator");
		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Index of the bar used for signal detection", "Indicator")
		.SetNotNegative();

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
		.SetDisplay("Allow Long Entries", "Enable bullish position openings", "Trading");
		_allowSellOpen = Param(nameof(AllowSellOpen), true)
		.SetDisplay("Allow Short Entries", "Enable bearish position openings", "Trading");
		_allowBuyClose = Param(nameof(AllowBuyClose), true)
		.SetDisplay("Allow Long Exits", "Permit closing long positions on bearish signals", "Trading");
		_allowSellClose = Param(nameof(AllowSellClose), true)
		.SetDisplay("Allow Short Exits", "Permit closing short positions on bullish signals", "Trading");

		_normalVolume = Param(nameof(NormalVolume), 0.1m)
		.SetDisplay("Normal Volume", "Standard position size", "Money Management")
		.SetGreaterThanZero();
		_reducedVolume = Param(nameof(ReducedVolume), 0.01m)
		.SetDisplay("Reduced Volume", "Size used after consecutive losses", "Money Management")
		.SetNotNegative();

		_buyTotalTrigger = Param(nameof(BuyTotalTrigger), 5)
		.SetDisplay("Buy Total Trigger", "Number of recent buy trades to evaluate", "Money Management")
		.SetNotNegative();
		_buyLossTrigger = Param(nameof(BuyLossTrigger), 3)
		.SetDisplay("Buy Loss Trigger", "Losses inside the evaluated window before volume reduction", "Money Management")
		.SetNotNegative();
		_sellTotalTrigger = Param(nameof(SellTotalTrigger), 5)
		.SetDisplay("Sell Total Trigger", "Number of recent sell trades to evaluate", "Money Management")
		.SetNotNegative();
		_sellLossTrigger = Param(nameof(SellLossTrigger), 3)
		.SetDisplay("Sell Loss Trigger", "Losses inside the evaluated window before volume reduction", "Money Management")
		.SetNotNegative();

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss (points)", "Protective distance measured in points", "Protection")
		.SetNotNegative();
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit (points)", "Profit target distance measured in points", "Protection")
		.SetNotNegative();
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Shift used when computing the raw momentum difference.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// First smoothing method.
	/// </summary>
	public XwamiSmoothingMethod Method1
	{
		get => _method1.Value;
		set => _method1.Value = value;
	}

	/// <summary>
	/// Length of the first smoothing stage.
	/// </summary>
	public int Length1
	{
		get => _length1.Value;
		set => _length1.Value = value;
	}

	/// <summary>
	/// Phase value forwarded to the first smoother when applicable.
	/// </summary>
	public int Phase1
	{
		get => _phase1.Value;
		set => _phase1.Value = value;
	}

	/// <summary>
	/// Second smoothing method.
	/// </summary>
	public XwamiSmoothingMethod Method2
	{
		get => _method2.Value;
		set => _method2.Value = value;
	}

	/// <summary>
	/// Length of the second smoothing stage.
	/// </summary>
	public int Length2
	{
		get => _length2.Value;
		set => _length2.Value = value;
	}

	/// <summary>
	/// Phase value forwarded to the second smoother when applicable.
	/// </summary>
	public int Phase2
	{
		get => _phase2.Value;
		set => _phase2.Value = value;
	}

	/// <summary>
	/// Third smoothing method.
	/// </summary>
	public XwamiSmoothingMethod Method3
	{
		get => _method3.Value;
		set => _method3.Value = value;
	}

	/// <summary>
	/// Length of the third smoothing stage.
	/// </summary>
	public int Length3
	{
		get => _length3.Value;
		set => _length3.Value = value;
	}

	/// <summary>
	/// Phase value forwarded to the third smoother when applicable.
	/// </summary>
	public int Phase3
	{
		get => _phase3.Value;
		set => _phase3.Value = value;
	}

	/// <summary>
	/// Fourth smoothing method.
	/// </summary>
	public XwamiSmoothingMethod Method4
	{
		get => _method4.Value;
		set => _method4.Value = value;
	}

	/// <summary>
	/// Length of the fourth smoothing stage.
	/// </summary>
	public int Length4
	{
		get => _length4.Value;
		set => _length4.Value = value;
	}

	/// <summary>
	/// Phase value forwarded to the fourth smoother when applicable.
	/// </summary>
	public int Phase4
	{
		get => _phase4.Value;
		set => _phase4.Value = value;
	}

	/// <summary>
	/// Price source used as the indicator input.
	/// </summary>
	public XwamiAppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Index of the historical bar used for final signal evaluation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Enables closing long positions on bearish signals.
	/// </summary>
	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	/// <summary>
	/// Enables closing short positions on bullish signals.
	/// </summary>
	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	/// <summary>
	/// Default position size.
	/// </summary>
	public decimal NormalVolume
	{
		get => _normalVolume.Value;
		set => _normalVolume.Value = value;
	}

	/// <summary>
	/// Reduced position size used after repeated losses.
	/// </summary>
	public decimal ReducedVolume
	{
		get => _reducedVolume.Value;
		set => _reducedVolume.Value = value;
	}

	/// <summary>
	/// Number of buy trades considered by the money-management window.
	/// </summary>
	public int BuyTotalTrigger
	{
		get => _buyTotalTrigger.Value;
		set => _buyTotalTrigger.Value = value;
	}

	/// <summary>
	/// Maximum losses tolerated inside the buy window before reducing size.
	/// </summary>
	public int BuyLossTrigger
	{
		get => _buyLossTrigger.Value;
		set => _buyLossTrigger.Value = value;
	}

	/// <summary>
	/// Number of sell trades considered by the money-management window.
	/// </summary>
	public int SellTotalTrigger
	{
		get => _sellTotalTrigger.Value;
		set => _sellTotalTrigger.Value = value;
	}

	/// <summary>
	/// Maximum losses tolerated inside the sell window before reducing size.
	/// </summary>
	public int SellLossTrigger
	{
		get => _sellLossTrigger.Value;
		set => _sellLossTrigger.Value = value;
	}

	/// <summary>
	/// Protective stop distance expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit target distance expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;

		_difference = new DifferenceCalculator(Math.Max(0, Period));
		_filter1 = CreateFilter(Method1, Length1, Phase1);
		_filter2 = CreateFilter(Method2, Length2, Phase2);
		_filter3 = CreateFilter(Method3, Length3, Phase3);
		_filter4 = CreateFilter(Method4, Length4, Phase4);

		_history.Clear();
		_recentBuyResults.Clear();
		_recentSellResults.Clear();
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longEntryVolume = 0m;
		_shortEntryVolume = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = GetAppliedPrice(candle, AppliedPrice);
		var diff = _difference.Process(price);
		if (diff is null)
		return;

		var time = candle.CloseTime;
		var value1 = ProcessFilter(_filter1, diff.Value, time);
		if (value1 is null)
		return;

		var value2 = ProcessFilter(_filter2, value1.Value, time);
		if (value2 is null)
		return;

		var value3 = ProcessFilter(_filter3, value2.Value, time);
		if (value3 is null)
		return;

		var value4 = ProcessFilter(_filter4, value3.Value, time);
		if (value4 is null)
		return;

		_history.Insert(0, (value3.Value, value4.Value));
		var required = SignalBar + 2;
		if (_history.Count > required)
		_history.RemoveAt(_history.Count - 1);

		if (_history.Count < required)
		return;

		if (CheckStops(candle))
		return;

		var current = _history[SignalBar];
		var previous = _history[SignalBar + 1];

		var closeLong = previous.up < previous.down && AllowBuyClose && Position > 0m;
		var closeShort = previous.up > previous.down && AllowSellClose && Position < 0m;
		var openLong = previous.up > previous.down && AllowBuyOpen && current.up <= current.down && Position <= 0m;
		var openShort = previous.up < previous.down && AllowSellOpen && current.up >= current.down && Position >= 0m;

		if (closeLong)
		{
			SellMarket(Position);
			RegisterLongResult(candle.ClosePrice);
		}

		if (closeShort)
		{
			BuyMarket(Math.Abs(Position));
			RegisterShortResult(candle.ClosePrice);
		}

		if (openLong)
		{
			var volume = GetMoneyManagementVolume(true);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_longEntryPrice = candle.ClosePrice;
				_longEntryVolume = volume;
			}

			_shortEntryPrice = null;
			_shortEntryVolume = 0m;
			return;
		}

		if (openShort)
		{
			var volume = GetMoneyManagementVolume(false);
			if (volume > 0m)
			{
				SellMarket(volume);
				_shortEntryPrice = candle.ClosePrice;
				_shortEntryVolume = volume;
			}

			_longEntryPrice = null;
			_longEntryVolume = 0m;
		}
	}

	private bool CheckStops(ICandleMessage candle)
	{
		if (_priceStep <= 0m)
		return false;

		if (Position > 0m && _longEntryPrice is decimal entryLong)
		{
			var stop = StopLossPoints * _priceStep;
			if (stop > 0m && candle.LowPrice <= entryLong - stop)
			{
				SellMarket(Position);
				RegisterLongResult(entryLong - stop);
				return true;
			}

			var take = TakeProfitPoints * _priceStep;
			if (take > 0m && candle.HighPrice >= entryLong + take)
			{
				SellMarket(Position);
				RegisterLongResult(entryLong + take);
				return true;
			}
		}

		if (Position < 0m && _shortEntryPrice is decimal entryShort)
		{
			var stop = StopLossPoints * _priceStep;
			if (stop > 0m && candle.HighPrice >= entryShort + stop)
			{
				BuyMarket(Math.Abs(Position));
				RegisterShortResult(entryShort + stop);
				return true;
			}

			var take = TakeProfitPoints * _priceStep;
			if (take > 0m && candle.LowPrice <= entryShort - take)
			{
				BuyMarket(Math.Abs(Position));
				RegisterShortResult(entryShort - take);
				return true;
			}
		}

		return false;
	}

	private decimal GetMoneyManagementVolume(bool isLong)
	{
		var history = isLong ? _recentBuyResults : _recentSellResults;
		var totalLimit = isLong ? BuyTotalTrigger : SellTotalTrigger;
		var lossLimit = isLong ? BuyLossTrigger : SellLossTrigger;

		var normal = Math.Max(0m, NormalVolume);
		var reduced = Math.Max(0m, ReducedVolume);

		if (normal <= 0m)
		return 0m;

		if (totalLimit <= 0 || lossLimit <= 0 || history.Count == 0)
		return normal;

		var losses = 0;
		var count = 0;

		for (var i = history.Count - 1; i >= 0 && count < totalLimit; i--)
		{
			if (history[i] < 0m)
			{
				losses++;
				if (losses >= lossLimit)
				return reduced;
			}

			count++;
		}

		return normal;
	}

	private void RegisterLongResult(decimal exitPrice)
	{
		if (_longEntryPrice is decimal entry && _longEntryVolume > 0m)
		{
			var result = (exitPrice - entry) * _longEntryVolume;
			_recentBuyResults.Add(result);
			TrimResults(_recentBuyResults, Math.Max(BuyTotalTrigger, 1));
		}

		_longEntryPrice = null;
		_longEntryVolume = 0m;
	}

	private void RegisterShortResult(decimal exitPrice)
	{
		if (_shortEntryPrice is decimal entry && _shortEntryVolume > 0m)
		{
			var result = (entry - exitPrice) * _shortEntryVolume;
			_recentSellResults.Add(result);
			TrimResults(_recentSellResults, Math.Max(SellTotalTrigger, 1));
		}

		_shortEntryPrice = null;
		_shortEntryVolume = 0m;
	}

	private static void TrimResults(List<decimal> results, int maxCount)
	{
		if (maxCount <= 0)
		return;

		var extra = results.Count - maxCount * 2;
		if (extra > 0)
		results.RemoveRange(0, extra);
	}

	private static decimal? ProcessFilter(IXwamiFilter filter, decimal value, DateTimeOffset time)
	{
		var result = filter.Process(value, time);
		return filter.IsFormed ? result : null;
	}

	private IXwamiFilter CreateFilter(XwamiSmoothingMethod method, int length, int phase)
	{
		var normalizedLength = Math.Max(1, length);

		return method switch
		{
			XwamiSmoothingMethod.Sma => new IndicatorWrapper(new SimpleMovingAverage { Length = normalizedLength }),
			XwamiSmoothingMethod.Ema => new IndicatorWrapper(new ExponentialMovingAverage { Length = normalizedLength }),
			XwamiSmoothingMethod.Smma => new IndicatorWrapper(new SmoothedMovingAverage { Length = normalizedLength }),
			XwamiSmoothingMethod.Lwma => new IndicatorWrapper(new WeightedMovingAverage { Length = normalizedLength }),
			XwamiSmoothingMethod.Jjma => new IndicatorWrapper(new JurikMovingAverage { Length = normalizedLength, Phase = phase }),
			XwamiSmoothingMethod.JurX => new IndicatorWrapper(new JurikMovingAverage { Length = normalizedLength, Phase = phase }),
			XwamiSmoothingMethod.ParMa => new IndicatorWrapper(new ExponentialMovingAverage { Length = normalizedLength }),
			XwamiSmoothingMethod.T3 => new TillsonT3Filter(normalizedLength, phase),
			XwamiSmoothingMethod.Vidya => new IndicatorWrapper(new ExponentialMovingAverage { Length = normalizedLength }),
			XwamiSmoothingMethod.Ama => new IndicatorWrapper(new KaufmanAdaptiveMovingAverage { Length = normalizedLength }),
			_ => new IndicatorWrapper(new SimpleMovingAverage { Length = normalizedLength })
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, XwamiAppliedPrice type)
	{
		return type switch
		{
			XwamiAppliedPrice.Close => candle.ClosePrice,
			XwamiAppliedPrice.Open => candle.OpenPrice,
			XwamiAppliedPrice.High => candle.HighPrice,
			XwamiAppliedPrice.Low => candle.LowPrice,
			XwamiAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			XwamiAppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			XwamiAppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			XwamiAppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			XwamiAppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			XwamiAppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			XwamiAppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			XwamiAppliedPrice.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
		res = (res + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
		res = (res + candle.HighPrice) / 2m;
		else
		res = (res + candle.ClosePrice) / 2m;

		return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
	}

	private interface IXwamiFilter
	{
		decimal Process(decimal value, DateTimeOffset time);
		bool IsFormed { get; }
	}

	private sealed class IndicatorWrapper : IXwamiFilter
	{
		private readonly IIndicator _indicator;

		public IndicatorWrapper(IIndicator indicator)
		{
			_indicator = indicator;
		}

		public bool IsFormed => _indicator.IsFormed;

		public decimal Process(decimal value, DateTimeOffset time)
		{
			return _indicator.Process(value, time, true).ToDecimal();
		}
	}

	private sealed class TillsonT3Filter : IXwamiFilter
	{
		private readonly decimal _w1;
		private readonly decimal _w2;
		private readonly decimal _c1;
		private readonly decimal _c2;
		private readonly decimal _c3;
		private readonly decimal _c4;
		private decimal _e1;
		private decimal _e2;
		private decimal _e3;
		private decimal _e4;
		private decimal _e5;
		private decimal _e6;
		private bool _initialized;

		public TillsonT3Filter(int length, int curvature)
		{
			var b = curvature / 100m;
			var b2 = b * b;
			var b3 = b2 * b;
			_c1 = -b3;
			_c2 = 3m * (b2 + b3);
			_c3 = -3m * (2m * b2 + b + b3);
			_c4 = 1m + 3m * b + b3 + 3m * b2;
			var n = 1m + 0.5m * (length - 1m);
			_w1 = 2m / (n + 1m);
			_w2 = 1m - _w1;
		}

		public bool IsFormed => _initialized;

		public decimal Process(decimal value, DateTimeOffset time)
		{
			if (!_initialized)
			{
				_e1 = _e2 = _e3 = _e4 = _e5 = _e6 = value;
				_initialized = true;
			}

			_e1 = _w1 * value + _w2 * _e1;
			_e2 = _w1 * _e1 + _w2 * _e2;
			_e3 = _w1 * _e2 + _w2 * _e3;
			_e4 = _w1 * _e3 + _w2 * _e4;
			_e5 = _w1 * _e4 + _w2 * _e5;
			_e6 = _w1 * _e5 + _w2 * _e6;

			return _c1 * _e6 + _c2 * _e5 + _c3 * _e4 + _c4 * _e3;
		}
	}

	private sealed class DifferenceCalculator
	{
		private readonly int _period;
		private readonly Queue<decimal> _buffer = new();

		public DifferenceCalculator(int period)
		{
			_period = Math.Max(0, period);
		}

		public decimal? Process(decimal value)
		{
			if (_period == 0)
			return 0m;

			_buffer.Enqueue(value);

			if (_buffer.Count <= _period)
			return null;

			if (_buffer.Count > _period + 1)
			_buffer.Dequeue();

			var previous = _buffer.Peek();
			return value - previous;
		}
	}
}

/// <summary>
/// Smoothing modes supported by the XWAMI conversion.
/// </summary>
public enum XwamiSmoothingMethod
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
	/// Smoothed moving average (RMA).
	/// </summary>
	Smma,
	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma,
	/// <summary>
	/// Jurik moving average (JJMA).
	/// </summary>
	Jjma,
	/// <summary>
	/// JurX approximation mapped to Jurik moving average.
	/// </summary>
	JurX,
	/// <summary>
	/// Parabolic moving average approximation (mapped to EMA).
	/// </summary>
	ParMa,
	/// <summary>
	/// Tillson T3 filter.
	/// </summary>
	T3,
	/// <summary>
	/// Variable index dynamic average approximation.
	/// </summary>
	Vidya,
	/// <summary>
	/// Kaufman adaptive moving average.
	/// </summary>
	Ama
}
