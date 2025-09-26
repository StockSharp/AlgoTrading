namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// RSI crossing its own moving average with optional trade direction filters and time window.
/// Converted from the MetaTrader expert advisor "RSI_MAonRSI_Filling Step EA.mq5".
/// </summary>
public class RsiMaOnRsiFillingStepStrategy : Strategy
{
	/// <summary>
	/// Trade direction selection.
	/// </summary>
	public enum TradeMode
	{
		/// <summary>
		/// Only long trades are allowed.
		/// </summary>
		BuyOnly,

		/// <summary>
		/// Only short trades are allowed.
		/// </summary>
		SellOnly,

		/// <summary>
		/// Both directions are allowed.
		/// </summary>
		Both,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<MovingAverageTypes> _maType;
	private readonly StrategyParam<decimal> _middleLevel;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<TradeMode> _tradeMode;
	private readonly StrategyParam<bool> _closeOppositePositions;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _useTimeWindow;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;

	private RelativeStrengthIndex _rsi;
	private MovingAverage _signalMa;
	private decimal? _previousRsi;
	private decimal? _previousSignal;
	private DateTimeOffset? _lastSignalBarTime;

	/// <summary>
	/// Initializes a new instance of <see cref="RsiMaOnRsiFillingStepStrategy"/>.
	/// </summary>
	public RsiMaOnRsiFillingStepStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for RSI calculations.", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Number of bars for the RSI smoothing window.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_maPeriod = Param(nameof(MaPeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the moving average applied to the RSI.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_maType = Param(nameof(MaType), MovingAverageTypes.Simple)
		.SetDisplay("MA Type", "Moving average type for the RSI smoothing.", "Indicators");

		_middleLevel = Param(nameof(MiddleLevel), 50m)
		.SetDisplay("Middle Level", "Central RSI level used by the strategy.", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(30m, 70m, 1m);

		_reverseSignals = Param(nameof(ReverseSignals), false)
		.SetDisplay("Reverse Signals", "Swap buy and sell conditions.", "Signals");

		_tradeMode = Param(nameof(Mode), TradeMode.Both)
		.SetDisplay("Trade Mode", "Restrict trading direction.", "Signals");

		_closeOppositePositions = Param(nameof(CloseOppositePositions), false)
		.SetDisplay("Close Opposite", "Flatten opposite positions before entering.", "Risk");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
		.SetDisplay("Single Position", "Allow only one open position at a time.", "Risk");

		_useTimeWindow = Param(nameof(UseTimeWindow), false)
		.SetDisplay("Use Time Window", "Enable daily trading session filter.", "Time");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(10, 0, 0))
		.SetDisplay("Session Start", "Local time when trading becomes allowed.", "Time");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(15, 0, 0))
		.SetDisplay("Session End", "Local time when trading stops.", "Time");
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
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Moving average period applied to the RSI output.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type applied to the RSI output.
	/// </summary>
	public MovingAverageTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Central level used by both RSI and its moving average.
	/// </summary>
	public decimal MiddleLevel
	{
		get => _middleLevel.Value;
		set => _middleLevel.Value = value;
	}

	/// <summary>
	/// Invert buy and sell signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Trade direction restriction.
	/// </summary>
	public TradeMode Mode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Close opposite exposure before submitting a new order.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOppositePositions.Value;
		set => _closeOppositePositions.Value = value;
	}

	/// <summary>
	/// Allow only one open position across both directions.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Enable the daily trading window filter.
	/// </summary>
	public bool UseTimeWindow
	{
		get => _useTimeWindow.Value;
		set => _useTimeWindow.Value = value;
	}

	/// <summary>
	/// Start time of the trading session.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// End time of the trading session.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null;
		_signalMa = null;
		_previousRsi = null;
		_previousSignal = null;
		_lastSignalBarTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_signalMa = new MovingAverage { Length = MaPeriod, Type = MaType };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, _signalMa, ProcessCandle)
		.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);
		}

		var oscillatorArea = CreateChartArea("RSI");
		if (oscillatorArea != null)
		{
			if (_rsi != null)
			{
				DrawIndicator(oscillatorArea, _rsi);
			}

			if (_signalMa != null)
			{
				DrawIndicator(oscillatorArea, _signalMa);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (UseTimeWindow && !IsWithinTradingWindow(candle.OpenTime))
		{
			_previousRsi = rsiValue;
			_previousSignal = signalValue;
			return;
		}

		if (_lastSignalBarTime == candle.OpenTime)
		{
			_previousRsi = rsiValue;
			_previousSignal = signalValue;
			return;
		}

		if (_previousRsi is null || _previousSignal is null)
		{
			_previousRsi = rsiValue;
			_previousSignal = signalValue;
			return;
		}

		var crossUp = _previousRsi < _previousSignal && rsiValue > signalValue;
		var crossDown = _previousRsi > _previousSignal && rsiValue < signalValue;
		var belowMiddle = rsiValue < MiddleLevel && signalValue < MiddleLevel;
		var aboveMiddle = rsiValue > MiddleLevel && signalValue > MiddleLevel;

		var signalTriggered = false;

		if (crossUp && belowMiddle)
		{
			signalTriggered = TryExecuteSignal(TradeDirection.Long, candle, rsiValue, signalValue);
		}
		else if (crossDown && aboveMiddle)
		{
			signalTriggered = TryExecuteSignal(TradeDirection.Short, candle, rsiValue, signalValue);
		}

		if (signalTriggered)
		{
			_lastSignalBarTime = candle.OpenTime;
		}

		_previousRsi = rsiValue;
		_previousSignal = signalValue;
	}

	private bool TryExecuteSignal(TradeDirection direction, ICandleMessage candle, decimal rsiValue, decimal signalValue)
	{
		if (ReverseSignals)
		{
			direction = direction == TradeDirection.Long ? TradeDirection.Short : TradeDirection.Long;
		}

		switch (direction)
		{
		case TradeDirection.Long:
			if (Mode == TradeMode.SellOnly)
			return false;

			if (!EnsureCapacityForNewPosition(isLong: true))
			return false;

			BuyMarket();
			LogInfo($"Buy signal: RSI {rsiValue:F2} crossed above MA {signalValue:F2} below middle level {MiddleLevel:F2}.");
			return true;

		case TradeDirection.Short:
			if (Mode == TradeMode.BuyOnly)
			return false;

			if (!EnsureCapacityForNewPosition(isLong: false))
			return false;

			SellMarket();
			LogInfo($"Sell signal: RSI {rsiValue:F2} crossed below MA {signalValue:F2} above middle level {MiddleLevel:F2}.");
			return true;

		default:
			return false;
		}
	}

	private bool EnsureCapacityForNewPosition(bool isLong)
	{
		if (OnlyOnePosition && Position != 0)
		return false;

		if (CloseOppositePositions)
		{
			if (isLong && Position < 0)
			{
				ClosePosition();
			}
			else if (!isLong && Position > 0)
			{
				ClosePosition();
			}
		}

		if (isLong && Position > 0)
		return false;

		if (!isLong && Position < 0)
		return false;

		return true;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = SessionStart;
		var end = SessionEnd;
		var current = time.TimeOfDay;

		if (start == end)
		return false;

		if (start < end)
		return current >= start && current < end;

		return current >= start || current < end;
	}

	private enum TradeDirection
	{
		Long,
		Short,
	}
}
