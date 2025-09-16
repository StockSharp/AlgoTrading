using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Available price sources for the Stopreversal trailing stop.
/// </summary>
public enum StopreversalAppliedPrice
{
	Close = 1,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted,
	Simple,
	Quarter,
	TrendFollow0,
	TrendFollow1,
	Demark
}

/// <summary>
/// Stopreversal trailing stop strategy with a configurable trading session filter.
/// </summary>
public class StopreversalTmStrategy : Strategy
{
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowBuyExit;
	private readonly StrategyParam<bool> _allowSellExit;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<decimal> _nPips;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<StopreversalAppliedPrice> _appliedPrice;

	private readonly Queue<SignalInfo> _signalQueue = new();

	private decimal? _previousAppliedPrice;
	private decimal? _previousStopLevel;

	public StopreversalTmStrategy()
	{
		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
			.SetDisplay("Allow Buy Entries", "Enable opening long positions on bullish signals", "Signals")
			.SetCanOptimize(true);

		_allowSellEntry = Param(nameof(AllowSellEntry), true)
			.SetDisplay("Allow Sell Entries", "Enable opening short positions on bearish signals", "Signals")
			.SetCanOptimize(true);

		_allowBuyExit = Param(nameof(AllowBuyExit), true)
			.SetDisplay("Allow Long Exits", "Close existing long positions when a sell signal arrives", "Signals")
			.SetCanOptimize(true);

		_allowSellExit = Param(nameof(AllowSellExit), true)
			.SetDisplay("Allow Short Exits", "Close existing short positions when a buy signal arrives", "Signals")
			.SetCanOptimize(true);

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Restrict trading to the configured session", "Session");

		_startHour = Param(nameof(StartHour), 0)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Session start hour (0-23)", "Session")
			.SetCanOptimize(true);

		_startMinute = Param(nameof(StartMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Session start minute (0-59)", "Session")
			.SetCanOptimize(true);

		_endHour = Param(nameof(EndHour), 23)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Session end hour (0-23)", "Session")
			.SetCanOptimize(true);

		_endMinute = Param(nameof(EndMinute), 59)
			.SetRange(0, 59)
			.SetDisplay("End Minute", "Session end minute (0-59)", "Session")
			.SetCanOptimize(true);

		_nPips = Param(nameof(Npips), 0.004m)
			.SetGreaterThanZero()
			.SetDisplay("Sensitivity", "Relative offset used to build the trailing stop", "Indicator")
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Bar Delay", "Number of completed bars to wait before acting", "Indicator")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");

		_appliedPrice = Param(nameof(AppliedPrice), StopreversalAppliedPrice.Close)
			.SetDisplay("Applied Price", "Price source for the trailing stop", "Indicator")
			.SetCanOptimize(true);
	}

	public bool AllowBuyEntry { get => _allowBuyEntry.Value; set => _allowBuyEntry.Value = value; }
	public bool AllowSellEntry { get => _allowSellEntry.Value; set => _allowSellEntry.Value = value; }
	public bool AllowBuyExit { get => _allowBuyExit.Value; set => _allowBuyExit.Value = value; }
	public bool AllowSellExit { get => _allowSellExit.Value; set => _allowSellExit.Value = value; }
	public bool UseTimeFilter { get => _useTimeFilter.Value; set => _useTimeFilter.Value = value; }
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	public int EndMinute { get => _endMinute.Value; set => _endMinute.Value = value; }
	public decimal Npips { get => _nPips.Value; set => _nPips.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public StopreversalAppliedPrice AppliedPrice { get => _appliedPrice.Value; set => _appliedPrice.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_previousAppliedPrice = null;
		_previousStopLevel = null;
		_signalQueue.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = GetAppliedPrice(candle);

		if (_previousAppliedPrice is null || _previousStopLevel is null)
		{
		_previousAppliedPrice = price;
		_previousStopLevel = price;
		EnqueueSignal(new SignalInfo(false, false, false, false), candle.CloseTime);
		return;
		}

		var prevPrice = _previousAppliedPrice.Value;
		var prevStop = _previousStopLevel.Value;

		var trailingStop = CalculateTrailingStop(price, prevPrice, prevStop);

		var buySignal = price > trailingStop && prevPrice < prevStop;
		var sellSignal = price < trailingStop && prevPrice > prevStop;

		_previousStopLevel = trailingStop;
		_previousAppliedPrice = price;

		var action = new SignalInfo(
			buySignal && AllowBuyEntry,
			sellSignal && AllowSellEntry,
			sellSignal && AllowBuyExit,
			buySignal && AllowSellExit
		);

		EnqueueSignal(action, candle.CloseTime);
	}

	private void EnqueueSignal(SignalInfo signal, DateTimeOffset currentTime)
	{
		_signalQueue.Enqueue(signal);

		while (_signalQueue.Count > SignalBar)
		{
		var action = _signalQueue.Dequeue();
		HandleSignal(action, currentTime);
		}
	}

	private void HandleSignal(SignalInfo signal, DateTimeOffset currentTime)
	{
		var inWindow = !UseTimeFilter || IsWithinTradingWindow(currentTime);

		if (UseTimeFilter && !inWindow && Position != 0)
		ClosePosition();

		if (signal.CloseLong && Position > 0)
		SellMarket();

		if (signal.CloseShort && Position < 0)
		BuyMarket();

		if (!UseTimeFilter || inWindow)
		{
		if (signal.OpenLong && Position <= 0)
		BuyMarket();

		if (signal.OpenShort && Position >= 0)
		SellMarket();
		}
	}

	private decimal CalculateTrailingStop(decimal price, decimal prevPrice, decimal prevStop)
	{
		var shift = Npips;

		if (price == prevStop)
		return prevStop;

		if (prevPrice < prevStop && price < prevStop)
		return Math.Min(prevStop, price * (1 + shift));

		if (prevPrice > prevStop && price > prevStop)
		return Math.Max(prevStop, price * (1 - shift));

		return price > prevStop
		? price * (1 - shift)
		: price * (1 + shift);
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
		StopreversalAppliedPrice.Close => candle.ClosePrice,
		StopreversalAppliedPrice.Open => candle.OpenPrice,
		StopreversalAppliedPrice.High => candle.HighPrice,
		StopreversalAppliedPrice.Low => candle.LowPrice,
		StopreversalAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
		StopreversalAppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
		StopreversalAppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
		StopreversalAppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
		StopreversalAppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
		StopreversalAppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
		? candle.HighPrice
		: candle.ClosePrice < candle.OpenPrice
		? candle.LowPrice
		: candle.ClosePrice,
		StopreversalAppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
		? (candle.HighPrice + candle.ClosePrice) / 2m
		: candle.ClosePrice < candle.OpenPrice
		? (candle.LowPrice + candle.ClosePrice) / 2m
		: candle.ClosePrice,
		StopreversalAppliedPrice.Demark =>
		{
		var result = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
		result = (result + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
		result = (result + candle.HighPrice) / 2m;
		else
		result = (result + candle.ClosePrice) / 2m;

		return ((result - candle.LowPrice) + (result - candle.HighPrice)) / 2m;
		},
		_ => candle.ClosePrice
		};
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(EndHour, EndMinute, 0);
		var current = time.TimeOfDay;

		if (start == end)
		return false;

		if (start < end)
		return current >= start && current < end;

		return current >= start || current < end;
	}

	private readonly struct SignalInfo
	{
		public SignalInfo(bool openLong, bool openShort, bool closeLong, bool closeShort)
		{
		OpenLong = openLong;
		OpenShort = openShort;
		CloseLong = closeLong;
		CloseShort = closeShort;
		}

		public bool OpenLong { get; }
		public bool OpenShort { get; }
		public bool CloseLong { get; }
		public bool CloseShort { get; }
	}
}
