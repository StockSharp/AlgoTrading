using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "earlyBird3" MetaTrader strategy that trades breakouts of the
/// early morning range with an RSI filter and multi-stage exits.
/// </summary>
public class EarlyBirdRangeBreakStrategy : Strategy
{
	private const int MaxParts = 3;

	private readonly StrategyParam<bool> _autoTrading;
	private readonly StrategyParam<bool> _hedgeTrading;
	private readonly StrategyParam<int> _orderType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfit1Points;
	private readonly StrategyParam<decimal> _takeProfit2Points;
	private readonly StrategyParam<decimal> _takeProfit3Points;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingRiskMultiplier;
	private readonly StrategyParam<decimal> _entryBufferPoints;
	private readonly StrategyParam<int> _rangeStartHour;
	private readonly StrategyParam<int> _rangeEndHour;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingStartMinute;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<int> _closingHour;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex? _rsi;
	private AverageTrueRange? _atr;

	private readonly DirectionState _longState = new(true);
	private readonly DirectionState _shortState = new(false);

	private DateTime _currentDay = DateTime.MinValue;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _rangeComplete;
	private int _longTradesToday;
	private int _shortTradesToday;

	/// <summary>
	/// Enables automatic order generation.
	/// </summary>
	public bool AutoTrading
	{
		get => _autoTrading.Value;
		set => _autoTrading.Value = value;
	}

	/// <summary>
	/// Allows reversing the position when an opposite breakout appears.
	/// </summary>
	public bool HedgeTrading
	{
		get => _hedgeTrading.Value;
		set => _hedgeTrading.Value = value;
	}

	/// <summary>
	/// Restricts the strategy to long only, short only or both directions.
	/// 0 = both, 1 = long only, 2 = short only.
	/// </summary>
	public int OrderType
	{
		get => _orderType.Value;
		set => _orderType.Value = value;
	}

	/// <summary>
	/// Volume of each individual market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for the first partial exit in points.
	/// </summary>
	public decimal TakeProfit1Points
	{
		get => _takeProfit1Points.Value;
		set => _takeProfit1Points.Value = value;
	}

	/// <summary>
	/// Take-profit distance for the second partial exit in points.
	/// </summary>
	public decimal TakeProfit2Points
	{
		get => _takeProfit2Points.Value;
		set => _takeProfit2Points.Value = value;
	}

	/// <summary>
	/// Take-profit distance for the third partial exit in points.
	/// </summary>
	public decimal TakeProfit3Points
	{
		get => _takeProfit3Points.Value;
		set => _takeProfit3Points.Value = value;
	}

	/// <summary>
	/// Trailing stop activation distance in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Volatility multiplier that must be exceeded before trailing is applied.
	/// </summary>
	public decimal TrailingRiskMultiplier
	{
		get => _trailingRiskMultiplier.Value;
		set => _trailingRiskMultiplier.Value = value;
	}

	/// <summary>
	/// Buffer added above the range high / below the range low before entering.
	/// </summary>
	public decimal EntryBufferPoints
	{
		get => _entryBufferPoints.Value;
		set => _entryBufferPoints.Value = value;
	}

	/// <summary>
	/// Hour of the day when the reference range starts.
	/// </summary>
	public int RangeStartHour
	{
		get => _rangeStartHour.Value;
		set => _rangeStartHour.Value = value;
	}

	/// <summary>
	/// Hour of the day when the reference range ends.
	/// </summary>
	public int RangeEndHour
	{
		get => _rangeEndHour.Value;
		set => _rangeEndHour.Value = value;
	}

	/// <summary>
	/// Hour of the day when breakout trading becomes active.
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}

	/// <summary>
	/// Minute of the day when breakout trading becomes active.
	/// </summary>
	public int TradingStartMinute
	{
		get => _tradingStartMinute.Value;
		set => _tradingStartMinute.Value = value;
	}

	/// <summary>
	/// Hour of the day when the strategy stops opening new trades.
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
	}

	/// <summary>
	/// Hour of the day used for forced position liquidation.
	/// </summary>
	public int ClosingHour
	{
		get => _closingHour.Value;
		set => _closingHour.Value = value;
	}

	/// <summary>
	/// RSI lookback period used for directional filtering.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars used to compute the average volatility reference.
	/// </summary>
	public int VolatilityPeriod
	{
		get => _volatilityPeriod.Value;
		set => _volatilityPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for the strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EarlyBirdRangeBreakStrategy"/> class.
	/// </summary>
	public EarlyBirdRangeBreakStrategy()
	{
		_autoTrading = Param(nameof(AutoTrading), true)
		.SetDisplay("Enable Trading", "Turns automatic order placement on or off", "General");

		_hedgeTrading = Param(nameof(HedgeTrading), true)
		.SetDisplay("Allow Reversal", "Allows reversing the position when the opposite signal appears", "General");

		_orderType = Param(nameof(OrderType), 0)
		.SetRange(0, 2)
		.SetDisplay("Order Type", "0 = both, 1 = long only, 2 = short only", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Volume used for each market order", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 60m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pts)", "Stop-loss distance in price points", "Risk");

		_takeProfit1Points = Param(nameof(TakeProfit1Points), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit 1 (pts)", "First take-profit distance", "Exits");

		_takeProfit2Points = Param(nameof(TakeProfit2Points), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit 2 (pts)", "Second take-profit distance", "Exits");

		_takeProfit3Points = Param(nameof(TakeProfit3Points), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit 3 (pts)", "Third take-profit distance", "Exits");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 15m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Trigger (pts)", "Price advance required before trailing activates", "Exits");

		_trailingRiskMultiplier = Param(nameof(TrailingRiskMultiplier), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Risk Multiplier", "Current range must exceed ATR * multiplier to trail", "Exits");

		_entryBufferPoints = Param(nameof(EntryBufferPoints), 2m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Entry Buffer (pts)", "Buffer added around the range breakout levels", "Entries");

		_rangeStartHour = Param(nameof(RangeStartHour), 3)
		.SetRange(0, 23)
		.SetDisplay("Range Start Hour", "Hour when the reference range begins", "Schedule");

		_rangeEndHour = Param(nameof(RangeEndHour), 7)
		.SetRange(0, 23)
		.SetDisplay("Range End Hour", "Hour when the reference range ends", "Schedule");

		_tradingStartHour = Param(nameof(TradingStartHour), 7)
		.SetRange(0, 23)
		.SetDisplay("Trading Start Hour", "Hour when breakout entries are allowed", "Schedule");

		_tradingStartMinute = Param(nameof(TradingStartMinute), 15)
		.SetRange(0, 59)
		.SetDisplay("Trading Start Minute", "Minute when breakout entries are allowed", "Schedule");

		_tradingEndHour = Param(nameof(TradingEndHour), 15)
		.SetRange(0, 23)
		.SetDisplay("Trading End Hour", "Hour after which no new trades are opened", "Schedule");

		_closingHour = Param(nameof(ClosingHour), 17)
		.SetRange(0, 23)
		.SetDisplay("Closing Hour", "Hour for forced liquidation of open positions", "Schedule");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "RSI lookback used for direction filtering", "Indicators");

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 16)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Number of bars for the average true range filter", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used for calculations", "General");
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

		StartProtection();

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_atr = new AverageTrueRange
		{
			Length = VolatilityPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(_rsi, _atr, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var localTime = candle.OpenTime.DateTime;
		var candleDate = localTime.Date;

		if (_currentDay != candleDate)
		ResetDailyState(candleDate);

		UpdateRange(candle, localTime);

		ManageExistingPositions(candle, atrValue, localTime);

		if (!AutoTrading)
		return;

		if (!_rangeComplete || _rsi is null || !_rsi.IsFormed)
		return;

		if (!IsWeekday(localTime))
		return;

		var timeOfDay = localTime.TimeOfDay;

		if (!IsWithinTradingWindow(timeOfDay))
		return;

		var step = GetPriceStep();

		if (step == 0m)
		return;

		var buffer = EntryBufferPoints * step;

		var canGoLong = OrderType != 2 && !_longState.IsActive && _longTradesToday == 0 && rsiValue > 50m;
		var canGoShort = OrderType != 1 && !_shortState.IsActive && _shortTradesToday == 0 && rsiValue <= 50m;

		if (canGoLong && _rangeHigh.HasValue)
		{
			if (_shortState.IsActive)
			{
				if (!HedgeTrading)
				return;

				CloseDirection(_shortState);
			}

			var entryPrice = _rangeHigh.Value + buffer;

			if (candle.HighPrice >= entryPrice)
			EnterDirection(_longState, candle.ClosePrice, true);
		}

		if (canGoShort && _rangeLow.HasValue)
		{
			if (_longState.IsActive)
			{
				if (!HedgeTrading)
				return;

				CloseDirection(_longState);
			}

			var entryPrice = _rangeLow.Value - buffer;

			if (candle.LowPrice <= entryPrice)
			EnterDirection(_shortState, candle.ClosePrice, false);
		}
	}

	private void EnterDirection(DirectionState state, decimal executionPrice, bool isLong)
	{
		var stopDistance = ConvertPoints(StopLossPoints);

		state.EntryPrice = executionPrice;
		state.StopPrice = isLong
		? executionPrice - stopDistance
		: executionPrice + stopDistance;
		state.Targets.Clear();
		state.PartsRemaining = MaxParts;

		foreach (var target in GetTargets(executionPrice, isLong))
		state.Targets.Enqueue(target);

		for (var i = 0; i < MaxParts; i++)
		{
			if (isLong)
			BuyMarket(TradeVolume);
			else
			SellMarket(TradeVolume);
		}

		if (isLong)
		_longTradesToday++;
		else
		_shortTradesToday++;
	}

	private IEnumerable<decimal> GetTargets(decimal entryPrice, bool isLong)
	{
		var targets = new[]
		{
			TakeProfit1Points,
			TakeProfit2Points,
			TakeProfit3Points
		};

		foreach (var tp in targets)
		{
			if (tp <= 0m)
			continue;

			var distance = ConvertPoints(tp);
			yield return isLong ? entryPrice + distance : entryPrice - distance;
		}
	}

	private void ManageExistingPositions(ICandleMessage candle, decimal atrValue, DateTime localTime)
	{
		if (!_longState.IsActive && !_shortState.IsActive)
		return;

		var timeOfDay = localTime.TimeOfDay;

		if (timeOfDay >= ClosingTime)
		{
			CloseDirection(_longState);
			CloseDirection(_shortState);
			return;
		}

		if (_longState.IsActive)
		{
			if (StopLossPoints > 0m && candle.LowPrice <= _longState.StopPrice)
			{
				CloseDirection(_longState);
			}
			else
			{
				ProcessTargets(_longState, candle.HighPrice);
				ApplyTrailing(_longState, candle, atrValue);
			}
		}

		if (_shortState.IsActive)
		{
			if (StopLossPoints > 0m && candle.HighPrice >= _shortState.StopPrice)
			{
				CloseDirection(_shortState);
			}
			else
			{
				ProcessTargets(_shortState, candle.LowPrice);
				ApplyTrailing(_shortState, candle, atrValue);
			}
		}
	}

	private void ProcessTargets(DirectionState state, decimal price)
	{
		while (state.Targets.Count > 0)
		{
			var target = state.Targets.Peek();

			if (state.IsLong && price < target)
			break;

			if (!state.IsLong && price > target)
			break;

			state.Targets.Dequeue();
			state.PartsRemaining--;

			if (state.IsLong)
			SellMarket(TradeVolume);
			else
			BuyMarket(TradeVolume);

			if (state.PartsRemaining <= 0)
			{
				state.Reset();
				return;
			}
		}
	}

	private void ApplyTrailing(DirectionState state, ICandleMessage candle, decimal atrValue)
	{
		if (TrailingStopPoints <= 0m || StopLossPoints <= 0m)
		return;

		if (_atr is null || !_atr.IsFormed)
		return;

		if (state.PartsRemaining > 1)
		return;

		var currentRange = candle.HighPrice - candle.LowPrice;

		if (currentRange <= atrValue * TrailingRiskMultiplier)
		return;

		var stopDistance = ConvertPoints(StopLossPoints);
		var triggerDistance = ConvertPoints(TrailingStopPoints);

		if (state.IsLong)
		{
			var triggerPrice = state.EntryPrice + triggerDistance;

			if (candle.ClosePrice >= triggerPrice)
			{
				var newStop = candle.ClosePrice - stopDistance;

				if (newStop > state.StopPrice)
				state.StopPrice = newStop;
			}
		}
		else
		{
			var triggerPrice = state.EntryPrice - triggerDistance;

			if (candle.ClosePrice <= triggerPrice)
			{
				var newStop = candle.ClosePrice + stopDistance;

				if (newStop < state.StopPrice)
				state.StopPrice = newStop;
			}
		}
	}

	private void CloseDirection(DirectionState state)
	{
		if (!state.IsActive)
		return;

		var volume = TradeVolume * state.PartsRemaining;

		if (state.IsLong)
		SellMarket(volume);
		else
		BuyMarket(volume);

		state.Reset();
	}

	private void UpdateRange(ICandleMessage candle, DateTime localTime)
	{
		var timeOfDay = localTime.TimeOfDay;
		var start = new TimeSpan(RangeStartHour, 0, 0);
		var end = new TimeSpan(RangeEndHour, 0, 0);

		if (timeOfDay >= start && timeOfDay < end)
		{
			_rangeHigh = _rangeHigh.HasValue ? Math.Max(_rangeHigh.Value, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow.HasValue ? Math.Min(_rangeLow.Value, candle.LowPrice) : candle.LowPrice;
		}

		if (!_rangeComplete && timeOfDay >= end && _rangeHigh.HasValue && _rangeLow.HasValue)
		_rangeComplete = true;
	}

	private void ResetDailyState(DateTime date)
	{
		_currentDay = date;
		_rangeHigh = null;
		_rangeLow = null;
		_rangeComplete = false;
		_longTradesToday = 0;
		_shortTradesToday = 0;
	}

	private bool IsWeekday(DateTime time)
	{
		return time.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
	}

	private bool IsWithinTradingWindow(TimeSpan timeOfDay)
	{
		var start = new TimeSpan(TradingStartHour, TradingStartMinute, 0);
		var end = new TimeSpan(TradingEndHour, 0, 0);
		return timeOfDay >= start && timeOfDay < end;
	}

	private TimeSpan ClosingTime => new(ClosingHour, 0, 0);

	private decimal ConvertPoints(decimal points)
	{
		return points * GetPriceStep();
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is null || step == 0m ? 0.0001m : step.Value;
	}

	private sealed class DirectionState
	{
		public DirectionState(bool isLong)
		{
			IsLong = isLong;
		}

		public bool IsLong { get; }
		public decimal EntryPrice { get; set; }
		public decimal StopPrice { get; set; }
		public Queue<decimal> Targets { get; } = new();
		public int PartsRemaining { get; set; }
		public bool IsActive => PartsRemaining > 0;

		public void Reset()
		{
			Targets.Clear();
			PartsRemaining = 0;
			EntryPrice = 0m;
			StopPrice = 0m;
		}
	}
}
