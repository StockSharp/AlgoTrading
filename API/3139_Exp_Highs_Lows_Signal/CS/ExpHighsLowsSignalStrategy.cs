using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor Exp_HighsLowsSignal.
/// Generates trades from a Highs/Lows directional sequence detector with configurable delays and risk controls.
/// </summary>
public class ExpHighsLowsSignalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _sequenceLength;
	private readonly StrategyParam<int> _signalBarDelay;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleSnapshot> _history = new();
	private readonly Queue<SignalInfo> _signalQueue = new();

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ExpHighsLowsSignalStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetDisplay("Volume", "Base order volume", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
		.SetDisplay("Allow Long Entries", "Open long trades on bullish Highs/Lows sequences", "Signals");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
		.SetDisplay("Allow Short Entries", "Open short trades on bearish Highs/Lows sequences", "Signals");

		_allowLongExit = Param(nameof(AllowLongExit), true)
		.SetDisplay("Allow Long Exits", "Close longs when a bearish sequence appears", "Signals");

		_allowShortExit = Param(nameof(AllowShortExit), true)
		.SetDisplay("Allow Short Exits", "Close shorts when a bullish sequence appears", "Signals");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
		.SetDisplay("Stop Loss (ticks)", "Protective stop distance expressed in price steps", "Risk")
		.SetGreaterOrEqualZero()
		.SetCanOptimize(true)
		.SetOptimize(0, 3000, 250);

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
		.SetDisplay("Take Profit (ticks)", "Profit target distance expressed in price steps", "Risk")
		.SetGreaterOrEqualZero()
		.SetCanOptimize(true)
		.SetOptimize(0, 4000, 250);

		_sequenceLength = Param(nameof(SequenceLength), 3)
		.SetDisplay("Sequence Length", "Consecutive bars required for a signal", "Indicator")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(1, 6, 1);

		_signalBarDelay = Param(nameof(SignalBarDelay), 1)
		.SetDisplay("Signal Delay", "Number of completed bars to wait before acting", "Indicator")
		.SetGreaterOrEqualZero()
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for Highs/Lows analysis", "Indicator");
	}

	/// <summary>
	/// Order volume in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enables opening long trades.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Enables opening short trades.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Enables closing long trades on bearish signals.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Enables closing short trades on bullish signals.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Number of consecutive higher highs/lows or lower highs/lows required.
	/// </summary>
	public int SequenceLength
	{
		get => _sequenceLength.Value;
		set => _sequenceLength.Value = value;
	}

	/// <summary>
	/// Number of closed candles that the signal is delayed.
	/// </summary>
	public int SignalBarDelay
	{
		get => _signalBarDelay.Value;
		set => _signalBarDelay.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the Highs/Lows detector.
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

		_history.Clear();
		_signalQueue.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Synchronize the base strategy volume with the configured parameter.
		base.Volume = OrderVolume;

		// Subscribe to the working timeframe candles that drive the pattern detection.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		// Configure default protective orders using pip distances converted to absolute points.
		var step = Security.PriceStep ?? 1m;
		StartProtection(
		new Unit(TakeProfitTicks * step, UnitTypes.Point),
		new Unit(StopLossTicks * step, UnitTypes.Point));

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Store the finished candle at the front to mirror MT5 time-series indexing.
		_history.Insert(0, new CandleSnapshot(candle.HighPrice, candle.LowPrice));

		var maxHistory = SequenceLength + SignalBarDelay + 2;
		if (_history.Count > maxHistory)
		_history.RemoveRange(maxHistory, _history.Count - maxHistory);

		// Evaluate the most recent closed candle and queue the signal for delayed execution.
		var signal = EvaluateSignal();
		_signalQueue.Enqueue(signal);

		if (_signalQueue.Count <= SignalBarDelay)
		return;

		var readySignal = _signalQueue.Dequeue();
		if (!readySignal.HasAction)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		HandleSignal(readySignal);
	}

	private SignalInfo EvaluateSignal()
	{
		// Require enough candles to perform the sequence comparison.
		if (_history.Count <= SequenceLength)
		return SignalInfo.Empty;

		var higherHighs = true;
		var higherLows = true;
		var lowerHighs = true;
		var lowerLows = true;

		for (var i = 0; i < SequenceLength; i++)
		{
		var current = _history[i];
		var next = _history[i + 1];

		if (current.High <= next.High)
		higherHighs = false;

		if (current.Low <= next.Low)
		higherLows = false;

		if (current.High >= next.High)
		lowerHighs = false;

		if (current.Low >= next.Low)
		lowerLows = false;

		if (!higherHighs && !higherLows && !lowerHighs && !lowerLows)
		break;
		}

		var longSignal = higherHighs && higherLows;
		var shortSignal = lowerHighs && lowerLows;

		return new SignalInfo(
		longSignal && AllowLongEntry,
		shortSignal && AllowShortEntry,
		shortSignal && AllowLongExit,
		longSignal && AllowShortExit);
	}

	private void HandleSignal(SignalInfo signal)
	{
		if (signal.CloseLong && Position > 0)
		{
		// Close any open long position before responding to bearish pressure.
		ClosePosition();
		}

		if (signal.CloseShort && Position < 0)
		{
		// Close any open short position before responding to bullish pressure.
		ClosePosition();
		}

		if (signal.OpenLong && Position <= 0)
		{
		// Flip to long by covering shorts and adding the configured volume.
		var volume = OrderVolume + Math.Max(0m, -Position);
		if (volume > 0m)
		BuyMarket(volume);
		}

		if (signal.OpenShort && Position >= 0)
		{
		// Flip to short by covering longs and adding the configured volume.
		var volume = OrderVolume + Math.Max(0m, Position);
		if (volume > 0m)
		SellMarket(volume);
		}
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal high, decimal low)
		{
		High = high;
		Low = low;
		}

		public decimal High { get; }
		public decimal Low { get; }
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

		public bool HasAction => OpenLong || OpenShort || CloseLong || CloseShort;

		public static SignalInfo Empty => default;
	}
}
