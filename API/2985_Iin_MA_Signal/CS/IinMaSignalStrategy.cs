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
/// Trend-following strategy based on the Iin MA Signal moving average cross indicator.
/// </summary>
public class IinMaSignalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<MaTypes> _fastMaType;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<MaTypes> _slowMaType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _closeLongOnSignal;
	private readonly StrategyParam<bool> _closeShortOnSignal;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private LengthIndicator<decimal> _fastMa = null!;
	private LengthIndicator<decimal> _slowMa = null!;
	private readonly List<MaSample> _maHistory = new();
	private int _trend;

	public IinMaSignalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Timeframe used to request candles for the strategy.", "General");

		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetDisplay("Fast MA Period", "Length of the fast moving average.", "Moving averages")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_fastMaType = Param(nameof(FastMaType), MaTypes.Ema)
			.SetDisplay("Fast MA Type", "Moving average type for the fast line (SMA, EMA, SMMA, LWMA).", "Moving averages");

		_slowPeriod = Param(nameof(SlowPeriod), 22)
			.SetDisplay("Slow MA Period", "Length of the slow moving average.", "Moving averages")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_slowMaType = Param(nameof(SlowMaType), MaTypes.Sma)
			.SetDisplay("Slow MA Type", "Moving average type for the slow line (SMA, EMA, SMMA, LWMA).", "Moving averages");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Number of bars back where the cross must occur.", "Signals");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions on bullish crosses.", "Trading");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions on bearish crosses.", "Trading");

		_closeLongOnSignal = Param(nameof(CloseLongOnSignal), true)
			.SetDisplay("Close Long On Short Signal", "Close existing long positions when a bearish cross appears.", "Trading");

		_closeShortOnSignal = Param(nameof(CloseShortOnSignal), true)
			.SetDisplay("Close Short On Long Signal", "Close existing short positions when a bullish cross appears.", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss Points", "Absolute distance for the protective stop loss (0 disables).", "Risk management")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit Points", "Absolute distance for the protective take profit (0 disables).", "Risk management")
			.SetCanOptimize(true);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public MaTypes FastMaType
	{
		get => _fastMaType.Value;
		set => _fastMaType.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public MaTypes SlowMaType
	{
		get => _slowMaType.Value;
		set => _slowMaType.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public bool AllowLongEntries
	{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	public bool AllowShortEntries
	{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	public bool CloseLongOnSignal
	{
		get => _closeLongOnSignal.Value;
		set => _closeLongOnSignal.Value = value;
	}

	public bool CloseShortOnSignal
	{
		get => _closeShortOnSignal.Value;
		set => _closeShortOnSignal.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create the moving averages that replicate the MQL indicator configuration.
		_fastMa = CreateMa(FastMaType, FastPeriod);
		_slowMa = CreateMa(SlowMaType, SlowPeriod);

		// Subscribe to candle series and bind both moving averages.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		// Configure optional protective orders only when distances are specified.
		var takeProfit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
		var stopLoss = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;

		if (takeProfit != null || stopLoss != null)
		{
			StartProtection(
				takeProfit: takeProfit,
				stopLoss: stopLoss,
				useMarketOrders: true);
		}

		// Draw indicators and trades when a chart area is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_maHistory.Clear();
		_trend = 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue)
	{
		// Only process completed candles to mirror the original indicator behaviour.
		if (candle.State != CandleStates.Finished)
			return;

		// Wait until both moving averages are formed before generating signals.
		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		// Keep a compact history so the signal can be evaluated at the configured bar index.
		_maHistory.Add(new MaSample(fastMaValue, slowMaValue));

		var maxHistory = Math.Max(2, SignalBar + 2);
		if (_maHistory.Count > maxHistory)
			_maHistory.RemoveRange(0, _maHistory.Count - maxHistory);

		if (_maHistory.Count < maxHistory)
			return;

		var signalIndex = _maHistory.Count - 1 - SignalBar;
		if (signalIndex <= 0)
			return;

		var signalSample = _maHistory[signalIndex];
		var previousSample = _maHistory[signalIndex - 1];

		var crossUp = _trend <= 0
			&& previousSample.Fast < previousSample.Slow
			&& signalSample.Fast > signalSample.Slow;

		var crossDown = _trend >= 0
			&& previousSample.Fast > previousSample.Slow
			&& signalSample.Fast < signalSample.Slow;

		if (crossUp)
			_trend = 1;
		else if (crossDown)
			_trend = -1;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (crossUp)
		{
			// Close short positions first to match the MQL template logic.
			if (CloseShortOnSignal && Position < 0)
				ClosePosition();

			// Open a new long position when the bullish cross is allowed.
			if (AllowLongEntries && Position <= 0)
				BuyMarket();
		}
		else if (crossDown)
		{
			// Close long positions first to mirror the automated template.
			if (CloseLongOnSignal && Position > 0)
				ClosePosition();

			// Open a short position when bearish entries are permitted.
			if (AllowShortEntries && Position >= 0)
				SellMarket();
		}
	}

	private static LengthIndicator<decimal> CreateMa(MaTypes type, int length)
	{
		return type switch
		{
			MaTypes.Sma => new SimpleMovingAverage { Length = length },
			MaTypes.Ema => new ExponentialMovingAverage { Length = length },
			MaTypes.Smma => new SmoothedMovingAverage { Length = length },
			MaTypes.Lwma => new WeightedMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported moving average type."),
		};
	}

	private readonly struct MaSample
	{
		public MaSample(decimal fast, decimal slow)
		{
			Fast = fast;
			Slow = slow;
		}

		public decimal Fast { get; }
		public decimal Slow { get; }
	}

	public enum MaTypes
	{
		Sma,
		Ema,
		Smma,
		Lwma
	}
}