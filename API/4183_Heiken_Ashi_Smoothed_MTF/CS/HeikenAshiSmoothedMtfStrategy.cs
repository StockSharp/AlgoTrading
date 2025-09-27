using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe strategy based on smoothed Heiken Ashi candles.
/// Uses six synchronized timeframes to detect alignment of bullish or bearish trends
/// and opens trades when the lower timeframe pulls back early while higher timeframes remain strong.
/// Implements manual stop-loss and take-profit management that mirrors the original MetaTrader logic.
/// </summary>
public class HeikenAshiSmoothedMtfStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _extraStopLossPoints;
	private readonly StrategyParam<int> _firstMaPeriod;
	private readonly StrategyParam<HaMaMethod> _firstMaMethod;
	private readonly StrategyParam<int> _secondMaPeriod;
	private readonly StrategyParam<HaMaMethod> _secondMaMethod;
	private readonly StrategyParam<int> _maxM5TrendLength;
	private readonly StrategyParam<int> _minM15TrendLength;
	private readonly StrategyParam<DataType> _m1CandleType;
	private readonly StrategyParam<DataType> _m5CandleType;
	private readonly StrategyParam<DataType> _m15CandleType;
	private readonly StrategyParam<DataType> _m30CandleType;
	private readonly StrategyParam<DataType> _h1CandleType;
	private readonly StrategyParam<DataType> _h4CandleType;

	private readonly Dictionary<TimeframeKey, TimeframeContext> _timeframes = new();

	private decimal _previousPosition;
	private decimal _lastRealizedPnL;
	private bool _lastTradeWasLoss;
	private decimal _pendingEntryPrice;
	private int _pendingStopSteps;
	private decimal _currentEntryPrice;
	private int _currentStopSteps;

	/// <summary>
	/// Trade volume used for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Base stop-loss distance measured in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Additional stop-loss padding applied after a losing trade.
	/// </summary>
	public int ExtraStopLossPoints
	{
		get => _extraStopLossPoints.Value;
		set => _extraStopLossPoints.Value = value;
	}

	/// <summary>
	/// Period of the first smoothing moving average applied to OHLC values.
	/// </summary>
	public int FirstMaPeriod
	{
		get => _firstMaPeriod.Value;
		set => _firstMaPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type used for the first smoothing pass.
	/// </summary>
	public HaMaMethod FirstMaMethod
	{
		get => _firstMaMethod.Value;
		set => _firstMaMethod.Value = value;
	}

	/// <summary>
	/// Period of the second smoothing moving average applied to Heiken Ashi values.
	/// </summary>
	public int SecondMaPeriod
	{
		get => _secondMaPeriod.Value;
		set => _secondMaPeriod.Value = value;
	}

	/// <summary>
	/// Moving average type used for the second smoothing pass.
	/// </summary>
	public HaMaMethod SecondMaMethod
	{
		get => _secondMaMethod.Value;
		set => _secondMaMethod.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive M5 candles allowed before cancelling the pullback entry.
	/// </summary>
	public int MaxM5TrendLength
	{
		get => _maxM5TrendLength.Value;
		set => _maxM5TrendLength.Value = value;
	}

	/// <summary>
	/// Minimum amount of time the M15 trend must persist before trades are permitted.
	/// </summary>
	public int MinM15TrendLength
	{
		get => _minM15TrendLength.Value;
		set => _minM15TrendLength.Value = value;
	}

	/// <summary>
	/// Candle type for the base one-minute stream.
	/// </summary>
	public DataType M1CandleType
	{
		get => _m1CandleType.Value;
		set => _m1CandleType.Value = value;
	}

	/// <summary>
	/// Candle type for the five-minute confirmation stream.
	/// </summary>
	public DataType M5CandleType
	{
		get => _m5CandleType.Value;
		set => _m5CandleType.Value = value;
	}

	/// <summary>
	/// Candle type for the fifteen-minute confirmation stream.
	/// </summary>
	public DataType M15CandleType
	{
		get => _m15CandleType.Value;
		set => _m15CandleType.Value = value;
	}

	/// <summary>
	/// Candle type for the thirty-minute confirmation stream.
	/// </summary>
	public DataType M30CandleType
	{
		get => _m30CandleType.Value;
		set => _m30CandleType.Value = value;
	}

	/// <summary>
	/// Candle type for the hourly confirmation stream.
	/// </summary>
	public DataType H1CandleType
	{
		get => _h1CandleType.Value;
		set => _h1CandleType.Value = value;
	}

	/// <summary>
	/// Candle type for the four-hour confirmation stream.
	/// </summary>
	public DataType H4CandleType
	{
		get => _h4CandleType.Value;
		set => _h4CandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HeikenAshiSmoothedMtfStrategy"/> class.
	/// </summary>
	public HeikenAshiSmoothedMtfStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetNotNegative()
		.SetDisplay("Trade Volume", "Base order volume used for entries", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20)
		.SetNotNegative()
		.SetDisplay("Take Profit (steps)", "Take-profit distance expressed in price steps", "Risk")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 500)
		.SetNotNegative()
		.SetDisplay("Stop Loss (steps)", "Stop-loss distance expressed in price steps", "Risk")
		.SetCanOptimize(true);

		_extraStopLossPoints = Param(nameof(ExtraStopLossPoints), 5)
		.SetNotNegative()
		.SetDisplay("Extra Stop Loss", "Additional stop-loss steps applied after a losing trade", "Risk");

		_firstMaPeriod = Param(nameof(FirstMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("First MA Period", "Length of the first smoothing moving average", "Indicators")
		.SetCanOptimize(true);

		_firstMaMethod = Param(nameof(FirstMaMethod), HaMaMethod.Smoothed)
		.SetDisplay("First MA Method", "Method of the first smoothing moving average", "Indicators");

		_secondMaPeriod = Param(nameof(SecondMaPeriod), 2)
		.SetGreaterThanZero()
		.SetDisplay("Second MA Period", "Length of the second smoothing moving average", "Indicators")
		.SetCanOptimize(true);

		_secondMaMethod = Param(nameof(SecondMaMethod), HaMaMethod.LinearWeighted)
		.SetDisplay("Second MA Method", "Method of the second smoothing moving average", "Indicators");

		_maxM5TrendLength = Param(nameof(MaxM5TrendLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max M5 Trend Length", "Maximum consecutive bullish or bearish M5 readings", "Filters")
		.SetCanOptimize(true);

		_minM15TrendLength = Param(nameof(MinM15TrendLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("Min M15 Trend Length", "Minimum number of updates the M15 trend must persist", "Filters")
		.SetCanOptimize(true);

		_m1CandleType = Param(nameof(M1CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("M1 Candles", "Primary candle series driving updates", "Data");

		_m5CandleType = Param(nameof(M5CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("M5 Candles", "Five-minute confirmation candles", "Data");

		_m15CandleType = Param(nameof(M15CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("M15 Candles", "Fifteen-minute confirmation candles", "Data");

		_m30CandleType = Param(nameof(M30CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("M30 Candles", "Thirty-minute confirmation candles", "Data");

		_h1CandleType = Param(nameof(H1CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("H1 Candles", "Hourly confirmation candles", "Data");

		_h4CandleType = Param(nameof(H4CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("H4 Candles", "Four-hour confirmation candles", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, M1CandleType),
		(Security, M5CandleType),
		(Security, M15CandleType),
		(Security, M30CandleType),
		(Security, H1CandleType),
		(Security, H4CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_timeframes.Clear();
		_previousPosition = 0m;
		_lastRealizedPnL = 0m;
		_lastTradeWasLoss = false;
		_pendingEntryPrice = 0m;
		_pendingStopSteps = StopLossPoints;
		_currentEntryPrice = 0m;
		_currentStopSteps = StopLossPoints;
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeTimeframes();

		Volume = TradeVolume;

		var baseSubscription = SubscribeCandles(M1CandleType);
		baseSubscription
		.WhenCandlesFinished(candle => ProcessTimeframeCandle(TimeframeKey.Minute1, candle))
		.Start();

		SubscribeCandles(M5CandleType)
		.WhenCandlesFinished(candle => ProcessTimeframeCandle(TimeframeKey.Minute5, candle))
		.Start();

		SubscribeCandles(M15CandleType)
		.WhenCandlesFinished(candle => ProcessTimeframeCandle(TimeframeKey.Minute15, candle))
		.Start();

		SubscribeCandles(M30CandleType)
		.WhenCandlesFinished(candle => ProcessTimeframeCandle(TimeframeKey.Minute30, candle))
		.Start();

		SubscribeCandles(H1CandleType)
		.WhenCandlesFinished(candle => ProcessTimeframeCandle(TimeframeKey.Hour1, candle))
		.Start();

		SubscribeCandles(H4CandleType)
		.WhenCandlesFinished(candle => ProcessTimeframeCandle(TimeframeKey.Hour4, candle))
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawOwnTrades(area);
		}
	}

	private void InitializeTimeframes()
	{
		_timeframes[TimeframeKey.Minute1] = CreateContext("M1");
		_timeframes[TimeframeKey.Minute5] = CreateContext("M5");
		_timeframes[TimeframeKey.Minute15] = CreateContext("M15");
		_timeframes[TimeframeKey.Minute30] = CreateContext("M30");
		_timeframes[TimeframeKey.Hour1] = CreateContext("H1");
		_timeframes[TimeframeKey.Hour4] = CreateContext("H4");
	}

	private TimeframeContext CreateContext(string name)
	{
		return new TimeframeContext
		{
			Name = name,
			State = new TimeframeState
			{
				OpenMa = CreateMovingAverage(FirstMaMethod, FirstMaPeriod),
				CloseMa = CreateMovingAverage(FirstMaMethod, FirstMaPeriod),
				HighMa = CreateMovingAverage(FirstMaMethod, FirstMaPeriod),
				LowMa = CreateMovingAverage(FirstMaMethod, FirstMaPeriod),
				OpenSmooth = CreateMovingAverage(SecondMaMethod, SecondMaPeriod),
				CloseSmooth = CreateMovingAverage(SecondMaMethod, SecondMaPeriod)
			}
		};
	}

	private static IIndicator CreateMovingAverage(HaMaMethod method, int length)
	{
		return method switch
		{
			HaMaMethod.Simple => new SimpleMovingAverage { Length = length },
			HaMaMethod.Exponential => new ExponentialMovingAverage { Length = length },
			HaMaMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			HaMaMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private void ProcessTimeframeCandle(TimeframeKey key, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_timeframes.TryGetValue(key, out var context))
		return;

		UpdateHeikenAshi(context.State, candle);

		if (key == TimeframeKey.Minute1)
		{
			HandleBaseCandle(candle);
		}
	}

	private void UpdateHeikenAshi(TimeframeState state, ICandleMessage candle)
	{
		var openValue = state.OpenMa.Process(candle.OpenPrice, candle.OpenTime, true);
		var closeValue = state.CloseMa.Process(candle.ClosePrice, candle.OpenTime, true);
		var highValue = state.HighMa.Process(candle.HighPrice, candle.OpenTime, true);
		var lowValue = state.LowMa.Process(candle.LowPrice, candle.OpenTime, true);

		if (!state.OpenMa.IsFormed || !state.CloseMa.IsFormed || !state.HighMa.IsFormed || !state.LowMa.IsFormed)
		return;

		var maOpen = openValue.ToDecimal();
		var maClose = closeValue.ToDecimal();
		var maHigh = highValue.ToDecimal();
		var maLow = lowValue.ToDecimal();

		var haClose = (maOpen + maClose + maHigh + maLow) / 4m;
		var haOpen = state.PreviousHaOpen.HasValue && state.PreviousHaClose.HasValue
		? (state.PreviousHaOpen.Value + state.PreviousHaClose.Value) / 2m
		: (maOpen + maClose) / 2m;

		var smoothedOpenValue = state.OpenSmooth.Process(haOpen, candle.OpenTime, true);
		var smoothedCloseValue = state.CloseSmooth.Process(haClose, candle.OpenTime, true);

		state.PreviousHaOpen = haOpen;
		state.PreviousHaClose = haClose;

		if (!state.OpenSmooth.IsFormed || !state.CloseSmooth.IsFormed)
		return;

		var finalOpen = smoothedOpenValue.ToDecimal();
		var finalClose = smoothedCloseValue.ToDecimal();

		state.LastOpen = finalOpen;
		state.LastClose = finalClose;
		state.IsBullish = finalClose > finalOpen;
	}

	private void HandleBaseCandle(ICandleMessage candle)
	{
		UpdateDirectionalCounts();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (EvaluateRiskManagement(candle))
		return;

		if (!AreHigherTimeframesReady())
		return;

		TryEnterPosition(candle);
	}

	private void UpdateDirectionalCounts()
	{
		foreach (var context in _timeframes.Values)
		{
			var state = context.State;
			if (state.IsBullish is null)
			continue;

			if (state.CountDirection is null || state.CountDirection != state.IsBullish)
			{
				state.CountDirection = state.IsBullish;
				if (state.IsBullish.Value)
				{
					state.UpCount = 1;
					state.DownCount = 0;
				}
				else
				{
					state.DownCount = 1;
					state.UpCount = 0;
				}
			}
			else
			{
				if (state.IsBullish.Value)
				{
					state.UpCount++;
				}
				else
				{
					state.DownCount++;
				}
			}
		}
	}

	private bool EvaluateRiskManagement(ICandleMessage candle)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		priceStep = 0.0001m;

		var closePrice = candle.ClosePrice;

		if (Position > 0m)
		{
			if (TakeProfitPoints > 0)
			{
				var target = _currentEntryPrice + TakeProfitPoints * priceStep;
				if (closePrice >= target && Position > 0m)
				{
					SellMarket(Position);
					LogInfo($"Long take profit triggered at {closePrice} (target {target}).");
					return true;
				}
			}

			if (_currentStopSteps > 0)
			{
				var stop = _currentEntryPrice - _currentStopSteps * priceStep;
				if (closePrice <= stop && Position > 0m)
				{
					SellMarket(Position);
					LogInfo($"Long stop-loss triggered at {closePrice} (stop {stop}).");
					return true;
				}
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);
			if (TakeProfitPoints > 0)
			{
				var target = _currentEntryPrice - TakeProfitPoints * priceStep;
				if (closePrice <= target && absPosition > 0m)
				{
					BuyMarket(absPosition);
					LogInfo($"Short take profit triggered at {closePrice} (target {target}).");
					return true;
				}
			}

			if (_currentStopSteps > 0)
			{
				var stop = _currentEntryPrice + _currentStopSteps * priceStep;
				if (closePrice >= stop && absPosition > 0m)
				{
					BuyMarket(absPosition);
					LogInfo($"Short stop-loss triggered at {closePrice} (stop {stop}).");
					return true;
				}
			}
		}

		return false;
	}

	private bool AreHigherTimeframesReady()
	{
		return _timeframes.TryGetValue(TimeframeKey.Minute5, out var m5)
		&& _timeframes.TryGetValue(TimeframeKey.Minute15, out var m15)
		&& _timeframes.TryGetValue(TimeframeKey.Minute30, out var m30)
		&& _timeframes.TryGetValue(TimeframeKey.Hour1, out var h1)
		&& _timeframes.TryGetValue(TimeframeKey.Hour4, out var h4)
		&& m5.State.IsBullish.HasValue
		&& m15.State.IsBullish.HasValue
		&& m30.State.IsBullish.HasValue
		&& h1.State.IsBullish.HasValue
		&& h4.State.IsBullish.HasValue;
	}

	private void TryEnterPosition(ICandleMessage candle)
	{
		var m5 = _timeframes[TimeframeKey.Minute5].State;
		var m15 = _timeframes[TimeframeKey.Minute15].State;
		var m30 = _timeframes[TimeframeKey.Minute30].State;
		var h1 = _timeframes[TimeframeKey.Hour1].State;
		var h4 = _timeframes[TimeframeKey.Hour4].State;

		var buySignal = m5.IsBullish == true
		&& m5.UpCount < MaxM5TrendLength
		&& m15.IsBullish == true
		&& m15.UpCount > MinM15TrendLength
		&& m30.IsBullish == true
		&& h1.IsBullish == true
		&& h4.IsBullish == true;

		if (buySignal && Position <= 0m)
		{
			var volume = TradeVolume + (Position < 0m ? Math.Abs(Position) : 0m);
			if (volume > 0m)
			{
				var stopSteps = StopLossPoints + (_lastTradeWasLoss ? ExtraStopLossPoints : 0);
				PlaceEntry(Sides.Buy, volume, candle.ClosePrice, stopSteps);
				LogInfo($"Buy signal: M5 up count {m5.UpCount}, M15 up count {m15.UpCount}.");
			}
		}

		var sellSignal = m5.IsBullish == false
		&& m5.DownCount < MaxM5TrendLength
		&& m15.IsBullish == false
		&& m15.DownCount > MinM15TrendLength
		&& m30.IsBullish == false
		&& h1.IsBullish == false
		&& h4.IsBullish == false;

		if (sellSignal && Position >= 0m)
		{
			var volume = TradeVolume + (Position > 0m ? Math.Abs(Position) : 0m);
			if (volume > 0m)
			{
				var stopSteps = StopLossPoints + (_lastTradeWasLoss ? ExtraStopLossPoints : 0);
				PlaceEntry(Sides.Sell, volume, candle.ClosePrice, stopSteps);
				LogInfo($"Sell signal: M5 down count {m5.DownCount}, M15 down count {m15.DownCount}.");
			}
		}
	}

	private void PlaceEntry(Sides direction, decimal volume, decimal referencePrice, int stopSteps)
	{
		_pendingEntryPrice = referencePrice;
		_pendingStopSteps = stopSteps;

		if (direction == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var current = Position;

		if (_previousPosition == 0m && current != 0m)
		{
			_currentEntryPrice = _pendingEntryPrice;
			_currentStopSteps = _pendingStopSteps;
			_lastRealizedPnL = PnL;
		}
		else if (_previousPosition != 0m && current == 0m)
		{
			var tradePnL = PnL - _lastRealizedPnL;
			_lastTradeWasLoss = tradePnL < 0m;
			_lastRealizedPnL = PnL;
			_currentEntryPrice = 0m;
			_currentStopSteps = StopLossPoints;
		}

		_previousPosition = current;
	}

	private enum TimeframeKey
	{
		Minute1,
		Minute5,
		Minute15,
		Minute30,
		Hour1,
		Hour4
	}

	private sealed class TimeframeContext
	{
		public string Name { get; set; } = string.Empty;
		public TimeframeState State { get; set; } = new();
	}

	private sealed class TimeframeState
	{
		public IIndicator OpenMa { get; set; } = new SimpleMovingAverage();
		public IIndicator CloseMa { get; set; } = new SimpleMovingAverage();
		public IIndicator HighMa { get; set; } = new SimpleMovingAverage();
		public IIndicator LowMa { get; set; } = new SimpleMovingAverage();
		public IIndicator OpenSmooth { get; set; } = new SimpleMovingAverage();
		public IIndicator CloseSmooth { get; set; } = new SimpleMovingAverage();
		public decimal? PreviousHaOpen { get; set; }
		public decimal? PreviousHaClose { get; set; }
		public decimal? LastOpen { get; set; }
		public decimal? LastClose { get; set; }
		public bool? IsBullish { get; set; }
		public bool? CountDirection { get; set; }
		public int UpCount { get; set; }
		public int DownCount { get; set; }
	}
}

/// <summary>
/// Moving average methods supported by the smoothed Heiken Ashi calculation.
/// </summary>
public enum HaMaMethod
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
	LinearWeighted
}
