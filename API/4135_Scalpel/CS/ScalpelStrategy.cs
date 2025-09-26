using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalpel breakout scalping strategy converted from the MetaTrader 4 expert advisor "Scalpel.mq4".
/// </summary>
public class ScalpelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLimit;
	private readonly StrategyParam<int> _maxDirectionalPositions;
	private readonly StrategyParam<int> _reentryIntervalMinutes;
	private readonly StrategyParam<int> _takeProfitReduceMinutes;
	private readonly StrategyParam<int> _liveMinutes;
	private readonly StrategyParam<int> _volatilityWindow;
	private readonly StrategyParam<decimal> _volatilityThresholdPoints;
	private readonly StrategyParam<int> _fridayCloseHour;
	private readonly StrategyParam<decimal> _spreadLimitPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _hour1CandleType;
	private readonly StrategyParam<DataType> _hour4CandleType;
	private readonly StrategyParam<DataType> _minute30CandleType;
	private readonly StrategyParam<DataType> _volatilityCandleType;

	private readonly Queue<(decimal Up, decimal Down)> _volatilityHistory = new();

	private ICandleMessage _previousCandle;
	private ICandleMessage _previousSecondCandle;
	private ICandleMessage _previousThirdCandle;

	private ICandleMessage _hour1Previous;
	private ICandleMessage _hour1Current;
	private ICandleMessage _hour4Previous;
	private ICandleMessage _hour4Current;
	private ICandleMessage _minute30Previous;
	private ICandleMessage _minute30Current;

	private decimal _volatilityCurrentUp;
	private decimal _volatilityCurrentDown;
	private decimal _volatilityPreviousUp;
	private decimal _volatilityPreviousDown;

	private decimal _pipSize = 1m;
	private decimal _volatilityThreshold;
	private decimal _spreadLimit;

	private decimal? _previousCci;
	private decimal? _entryPrice;
	private decimal? _trailingStopPrice;

	private DateTimeOffset? _lastEntryTime;
	private DateTimeOffset? _positionOpenedTime;

	private decimal _bestBid;
	private decimal _bestAsk;

	/// <summary>
	/// Initializes a new instance of <see cref="ScalpelStrategy"/>.
	/// </summary>
	public ScalpelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Base Timeframe", "Primary timeframe used for trade execution", "General")
			.SetCanOptimize(true);

		_hour1CandleType = Param(nameof(Hour1CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("H1 Timeframe", "One hour candles for higher time-frame confirmation", "General")
			.SetCanOptimize(true);

		_hour4CandleType = Param(nameof(Hour4CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("H4 Timeframe", "Four hour candles for the dominant trend", "General")
			.SetCanOptimize(true);

		_minute30CandleType = Param(nameof(Minute30CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("M30 Timeframe", "Thirty minute candles for medium-term confirmation", "General")
			.SetCanOptimize(true);

		_volatilityCandleType = Param(nameof(VolatilityCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Volatility Timeframe", "Short timeframe used to measure directional volume", "Filters")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), -5m)
			.SetDisplay("Trade Volume", "Positive value = lots, negative value = percent of capital", "Trading")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 40m)
			.SetDisplay("Take Profit (points)", "Distance to the profit target expressed in price steps", "Risk")
			.SetGreaterOrEqualTo(0m)
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 340m)
			.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price steps", "Risk")
			.SetGreaterOrEqualTo(0m)
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 25m)
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance expressed in price steps", "Risk")
			.SetGreaterOrEqualTo(0m)
			.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "Commodity Channel Index lookback for signal generation", "Signals")
			.SetGreaterOrEqualTo(1)
			.SetCanOptimize(true);

		_cciLimit = Param(nameof(CciLimit), 75m)
			.SetDisplay("CCI Limit", "Upper bound for long entries and mirrored negative bound for shorts", "Signals")
			.SetCanOptimize(true);

		_maxDirectionalPositions = Param(nameof(MaxDirectionalPositions), 1)
			.SetDisplay("Max Net Positions", "Maximum number of position units allowed in one direction", "Trading")
			.SetGreaterOrEqualTo(1)
			.SetCanOptimize(true);

		_reentryIntervalMinutes = Param(nameof(ReentryIntervalMinutes), 0)
			.SetDisplay("Reentry Interval (min)", "Minimum minutes to wait before entering again", "Behavior")
			.SetGreaterOrEqualTo(0)
			.SetCanOptimize(true);

		_takeProfitReduceMinutes = Param(nameof(TakeProfitReduceMinutes), 600)
			.SetDisplay("TP Reduce Interval (min)", "Minutes before reducing the take profit threshold", "Risk")
			.SetGreaterOrEqualTo(0)
			.SetCanOptimize(true);

		_liveMinutes = Param(nameof(LiveMinutes), 0)
			.SetDisplay("Max Position Lifetime (min)", "Force close trades after the specified lifetime", "Risk")
			.SetGreaterOrEqualTo(0)
			.SetCanOptimize(true);

		_volatilityWindow = Param(nameof(VolatilityWindow), 100)
			.SetDisplay("Volatility Window", "Number of 1-minute candles examined for the volume filter", "Filters")
			.SetCanOptimize(true);

		_volatilityThresholdPoints = Param(nameof(VolatilityThresholdPoints), 1m)
			.SetDisplay("Volatility Threshold (points)", "Minimum candle body/ range to accumulate directional volume", "Filters")
			.SetCanOptimize(true);

		_fridayCloseHour = Param(nameof(FridayCloseHour), 22)
			.SetDisplay("Friday Close Hour", "Hour (0-23) to liquidate positions on Friday", "Behavior")
			.SetGreaterOrEqualTo(0)
			.SetCanOptimize(true);

		_spreadLimitPoints = Param(nameof(SpreadLimitPoints), 5.5m)
			.SetDisplay("Spread Limit (points)", "Maximum spread allowed when opening new positions", "Risk")
			.SetGreaterOrEqualTo(0m)
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Trade volume parameter. Positive values are fixed lots, negative values represent percent of portfolio value.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index lookback length.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index bounds for signal validation.
	/// Positive values use a symmetric window around zero.
	/// </summary>
	public decimal CciLimit
	{
		get => _cciLimit.Value;
		set => _cciLimit.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous position units allowed in one direction.
	/// </summary>
	public int MaxDirectionalPositions
	{
		get => _maxDirectionalPositions.Value;
		set => _maxDirectionalPositions.Value = value;
	}

	/// <summary>
	/// Minimum waiting time between two consecutive entries.
	/// </summary>
	public int ReentryIntervalMinutes
	{
		get => _reentryIntervalMinutes.Value;
		set => _reentryIntervalMinutes.Value = value;
	}

	/// <summary>
	/// Minutes after which the take profit threshold starts shrinking.
	/// </summary>
	public int TakeProfitReduceMinutes
	{
		get => _takeProfitReduceMinutes.Value;
		set => _takeProfitReduceMinutes.Value = value;
	}

	/// <summary>
	/// Maximum lifetime of an open position.
	/// </summary>
	public int LiveMinutes
	{
		get => _liveMinutes.Value;
		set => _liveMinutes.Value = value;
	}

	/// <summary>
	/// Window length used when summing directional volumes.
	/// </summary>
	public int VolatilityWindow
	{
		get => _volatilityWindow.Value;
		set => _volatilityWindow.Value = value;
	}

	/// <summary>
	/// Minimum price move that classifies a candle as directional for the volume filter.
	/// </summary>
	public decimal VolatilityThresholdPoints
	{
		get => _volatilityThresholdPoints.Value;
		set => _volatilityThresholdPoints.Value = value;
	}

	/// <summary>
	/// Hour of day when all trades are closed on Friday.
	/// </summary>
	public int FridayCloseHour
	{
		get => _fridayCloseHour.Value;
		set => _fridayCloseHour.Value = value;
	}

	/// <summary>
	/// Maximum spread allowed when opening a new position.
	/// </summary>
	public decimal SpreadLimitPoints
	{
		get => _spreadLimitPoints.Value;
		set => _spreadLimitPoints.Value = value;
	}

	/// <summary>
	/// Base timeframe used for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// One hour timeframe used for directional confirmation.
	/// </summary>
	public DataType Hour1CandleType
	{
		get => _hour1CandleType.Value;
		set => _hour1CandleType.Value = value;
	}

	/// <summary>
	/// Four hour timeframe used for dominant trend confirmation.
	/// </summary>
	public DataType Hour4CandleType
	{
		get => _hour4CandleType.Value;
		set => _hour4CandleType.Value = value;
	}

	/// <summary>
	/// Thirty minute timeframe used for medium-term direction.
	/// </summary>
	public DataType Minute30CandleType
	{
		get => _minute30CandleType.Value;
		set => _minute30CandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to compute the directional volume filter.
	/// </summary>
	public DataType VolatilityCandleType
	{
		get => _volatilityCandleType.Value;
		set => _volatilityCandleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCandle = null;
		_previousSecondCandle = null;
		_previousThirdCandle = null;

		_hour1Previous = null;
		_hour1Current = null;
		_hour4Previous = null;
		_hour4Current = null;
		_minute30Previous = null;
		_minute30Current = null;

		_volatilityHistory.Clear();
		_volatilityCurrentUp = 0m;
		_volatilityCurrentDown = 0m;
		_volatilityPreviousUp = 0m;
		_volatilityPreviousDown = 0m;

		_previousCci = null;
		_entryPrice = null;
		_trailingStopPrice = null;

		_lastEntryTime = null;
		_positionOpenedTime = null;

		_bestBid = 0m;
		_bestAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePipSize();
		UpdateThresholds();

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription
			.BindEx(cci, ProcessBaseCandle)
			.Start();

		var hour1Subscription = SubscribeCandles(Hour1CandleType);
		hour1Subscription
			.Bind(ProcessHour1Candle)
			.Start();

		var hour4Subscription = SubscribeCandles(Hour4CandleType);
		hour4Subscription
			.Bind(ProcessHour4Candle)
			.Start();

		var minute30Subscription = SubscribeCandles(Minute30CandleType);
		minute30Subscription
			.Bind(ProcessMinute30Candle)
			.Start();

		var volatilitySubscription = SubscribeCandles(VolatilityCandleType);
		volatilitySubscription
			.Bind(ProcessVolatilityCandle)
			.Start();

		SubscribeOrderBook()
			.Bind(depth =>
			{
				var bid = depth.GetBestBid();
				if (bid != null)
					_bestBid = bid.Price;

				var ask = depth.GetBestAsk();
				if (ask != null)
					_bestAsk = ask.Price;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_entryPrice = null;
			_trailingStopPrice = null;
			_positionOpenedTime = null;
			return;
		}

		var previousPosition = Position - delta;
		var positionChangedDirection = Math.Sign(previousPosition) != Math.Sign(Position);
		var openedFromFlat = previousPosition == 0m;

		if (openedFromFlat || positionChangedDirection)
		{
			_entryPrice = PositionPrice != 0m ? PositionPrice : _entryPrice;
			_positionOpenedTime = CurrentTime;
			_lastEntryTime = CurrentTime;
			_trailingStopPrice = null;
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle, IIndicatorValue cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var cciSignal = _previousCci;

		if (cciValue.IsFinal)
			_previousCci = cciValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateBaseHistory(candle);
			return;
		}

		ManageOpenPosition(candle);

		if (candle.CloseTime.DayOfWeek == DayOfWeek.Friday)
		{
			UpdateBaseHistory(candle);
			return;
		}

		if (Position != 0m)
		{
			UpdateBaseHistory(candle);
			return;
		}

		if (cciSignal == null)
		{
			UpdateBaseHistory(candle);
			return;
		}

		var cci = cciSignal.Value;

		var allowBuy = IsCciBullish(cci);
		var allowSell = IsCciBearish(cci);

		if (!allowBuy && !allowSell)
		{
			UpdateBaseHistory(candle);
			return;
		}

		if (!HasMultiTimeframeData())
		{
			UpdateBaseHistory(candle);
			return;
		}

		if (!HasBaseHistory())
		{
			UpdateBaseHistory(candle);
			return;
		}

		if (ReentryIntervalMinutes > 0 && _lastEntryTime != null)
		{
			var minutes = (candle.CloseTime - _lastEntryTime.Value).TotalMinutes;
			if (minutes < ReentryIntervalMinutes)
			{
				UpdateBaseHistory(candle);
				return;
			}
		}

		if (SpreadLimitPoints > 0m && _bestBid > 0m && _bestAsk > 0m)
		{
			var spread = _bestAsk - _bestBid;
			if (spread > _spreadLimit)
			{
				UpdateBaseHistory(candle);
				return;
			}
		}

		var entryVolume = CalculateVolume();
		if (entryVolume <= 0m)
		{
			UpdateBaseHistory(candle);
			return;
		}

		var maxExposure = MaxDirectionalPositions * entryVolume;
		if (maxExposure <= 0m)
		{
			UpdateBaseHistory(candle);
			return;
		}

		var askPrice = GetBestAskOrClose(candle);
		var bidPrice = GetBestBidOrClose(candle);

		var vol0Up = _volatilityCurrentUp;
		var vol0Down = _volatilityCurrentDown;
		var vol1Up = _volatilityPreviousUp;
		var vol1Down = _volatilityPreviousDown;

		var highBreakout = _previousCandle != null && askPrice > _previousCandle.HighPrice;
		var lowBreakout = _previousCandle != null && bidPrice < _previousCandle.LowPrice;

		var risingHighs = _previousCandle != null && _previousSecondCandle != null && _previousThirdCandle != null
			&& _previousSecondCandle.HighPrice > _previousCandle.HighPrice
			&& _previousThirdCandle.HighPrice > _previousSecondCandle.HighPrice;

		var fallingLows = _previousCandle != null && _previousSecondCandle != null && _previousThirdCandle != null
			&& _previousSecondCandle.LowPrice < _previousCandle.LowPrice
			&& _previousThirdCandle.LowPrice < _previousSecondCandle.LowPrice;

		var higherTimeframeBull = _hour4Current!.LowPrice > _hour4Previous!.LowPrice
			&& _hour1Current!.LowPrice > _hour1Previous!.LowPrice
			&& _minute30Current!.LowPrice > _minute30Previous!.LowPrice;

		var higherTimeframeBear = _hour4Current!.HighPrice < _hour4Previous!.HighPrice
			&& _hour1Current!.HighPrice < _hour1Previous!.HighPrice
			&& _minute30Current!.HighPrice < _minute30Previous!.HighPrice;

		var longSignal = allowBuy
			&& higherTimeframeBull
			&& highBreakout
			&& risingHighs
			&& vol0Up > vol1Up
			&& vol1Up > 0m;

		var shortSignal = allowSell
			&& higherTimeframeBear
			&& lowBreakout
			&& fallingLows
			&& vol0Down > vol1Down
			&& vol1Down > 0m
			&& MatchesBearishCciBranch(cci);

		if (longSignal && Math.Abs(Position) < maxExposure)
		{
			BuyMarket(entryVolume);
			_lastEntryTime = candle.CloseTime;
		}
		else if (shortSignal && Math.Abs(Position) < maxExposure)
		{
			SellMarket(entryVolume);
			_lastEntryTime = candle.CloseTime;
		}

		UpdateBaseHistory(candle);
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0m)
			return;

		var isLong = Position > 0m;
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var entryPrice = _entryPrice ?? (PositionPrice != 0m ? PositionPrice : candle.ClosePrice);
		var currentPrice = isLong ? GetBestBidOrClose(candle) : GetBestAskOrClose(candle);

		if (LiveMinutes > 0 && _positionOpenedTime != null)
		{
			var lifetime = (candle.CloseTime - _positionOpenedTime.Value).TotalMinutes;
			if (lifetime >= LiveMinutes)
			{
				ExitPosition(isLong, volume);
				return;
			}
		}

		if (FridayCloseHour > 0 && candle.CloseTime.DayOfWeek == DayOfWeek.Friday && candle.CloseTime.Hour >= FridayCloseHour)
		{
			ExitPosition(isLong, volume);
			return;
		}

		if (StopLossPoints > 0m)
		{
			var stopPrice = isLong
				? entryPrice - StopLossPoints * _pipSize
				: entryPrice + StopLossPoints * _pipSize;

			var hit = isLong ? candle.LowPrice <= stopPrice : candle.HighPrice >= stopPrice;
			if (hit)
			{
				ExitPosition(isLong, volume);
				return;
			}
		}

		if (TakeProfitPoints > 0m)
		{
			var target = isLong
				? entryPrice + TakeProfitPoints * _pipSize
				: entryPrice - TakeProfitPoints * _pipSize;

			var reached = isLong ? currentPrice >= target : currentPrice <= target;
			if (reached)
			{
				ExitPosition(isLong, volume);
				return;
			}
		}

		if (TakeProfitReduceMinutes > 0 && TakeProfitPoints > 0m && _positionOpenedTime != null)
		{
			var minutes = (int)(candle.CloseTime - _positionOpenedTime.Value).TotalMinutes;
			if (minutes > 0)
			{
				var reduceSteps = minutes / TakeProfitReduceMinutes;
				if (reduceSteps > 0)
				{
					var reduceDistance = reduceSteps * _pipSize;
					var adjustedTarget = isLong
						? entryPrice + TakeProfitPoints * _pipSize - reduceDistance
						: entryPrice - TakeProfitPoints * _pipSize + reduceDistance;

					if (isLong)
						adjustedTarget = Math.Max(adjustedTarget, entryPrice);
					else
						adjustedTarget = Math.Min(adjustedTarget, entryPrice);

					var reached = isLong ? currentPrice >= adjustedTarget : currentPrice <= adjustedTarget;
					if (reached)
					{
						ExitPosition(isLong, volume);
						return;
					}
				}
			}
		}

		if (TrailingStopPoints > 0m)
		{
			var trailingDistance = TrailingStopPoints * _pipSize;
			if (trailingDistance > 0m)
			{
				var candidate = isLong ? currentPrice - trailingDistance : currentPrice + trailingDistance;

				if (_trailingStopPrice == null)
				{
					var profitMove = isLong ? currentPrice - entryPrice : entryPrice - currentPrice;
					if (profitMove >= trailingDistance)
						_trailingStopPrice = candidate;
				}
				else
				{
					if (isLong)
					{
						if (candidate > _trailingStopPrice)
							_trailingStopPrice = candidate;

						if (candle.LowPrice <= _trailingStopPrice)
						{
							ExitPosition(true, volume);
							return;
						}
					}
					else
					{
						if (candidate < _trailingStopPrice)
							_trailingStopPrice = candidate;

						if (candle.HighPrice >= _trailingStopPrice)
						{
							ExitPosition(false, volume);
							return;
						}
					}
				}
			}
		}
	}

	private bool HasMultiTimeframeData()
	{
		return _hour1Current != null && _hour1Previous != null
			&& _hour4Current != null && _hour4Previous != null
			&& _minute30Current != null && _minute30Previous != null;
	}

	private bool HasBaseHistory()
	{
		return _previousCandle != null && _previousSecondCandle != null && _previousThirdCandle != null;
	}

	private bool IsCciBullish(decimal cci)
	{
		var limit = CciLimit;

		if (limit > 0m)
			return cci > 0m && cci < limit;

		if (limit < 0m)
			return cci > -limit;

		return cci > 0m;
	}

	private bool IsCciBearish(decimal cci)
	{
		var limit = CciLimit;

		if (limit > 0m)
			return cci < 0m && cci > -limit;

		if (limit < 0m)
			return cci < limit;

		return cci < 0m;
	}

	private bool MatchesBearishCciBranch(decimal cci)
	{
		var limit = CciLimit;

		if (limit > 0m)
			return cci < 0m && cci > -limit;

		if (limit < 0m)
			return cci < limit;

		return cci < 0m;
	}

	private void ProcessHour1Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_hour1Previous = _hour1Current;
		_hour1Current = candle;
	}

	private void ProcessHour4Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_hour4Previous = _hour4Current;
		_hour4Current = candle;
	}

	private void ProcessMinute30Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_minute30Previous = _minute30Current;
		_minute30Current = candle;
	}

	private void ProcessVolatilityCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var threshold = _volatilityThreshold;
		var volume = candle.TotalVolume ?? 0m;
		decimal upVolume = 0m;
		decimal downVolume = 0m;

		if (VolatilityWindow > 0)
		{
			var body = candle.ClosePrice - candle.OpenPrice;

			if (body > threshold)
				upVolume = volume;
			else if (-body > threshold)
				downVolume = volume;
		}
		else if (VolatilityWindow < 0)
		{
			var range = candle.HighPrice - candle.LowPrice;
			if (range >= threshold)
			{
				upVolume = volume;
				downVolume = volume;
			}
		}
		else
		{
			var range = candle.HighPrice - candle.LowPrice;
			if (range >= threshold)
			{
				_volatilityCurrentUp = volume;
				_volatilityCurrentDown = volume;
			}
			else
			{
				_volatilityCurrentUp = 0m;
				_volatilityCurrentDown = 0m;
			}

			_volatilityPreviousUp = _volatilityCurrentUp;
			_volatilityPreviousDown = _volatilityCurrentDown;
			return;
		}

		if (VolatilityThresholdPoints < 0m)
		{
			var temp = upVolume;
			upVolume = downVolume;
			downVolume = temp;
		}

		var window = Math.Abs(VolatilityWindow);
		if (window == 0)
			window = 1;

		_volatilityHistory.Enqueue((upVolume, downVolume));
		while (_volatilityHistory.Count > window * 2)
			_volatilityHistory.Dequeue();

		decimal currentUp = 0m;
		decimal currentDown = 0m;
		decimal previousUp = 0m;
		decimal previousDown = 0m;

		var items = _volatilityHistory.ToArray();
		for (var i = 0; i < items.Length; i++)
		{
			var item = items[items.Length - 1 - i];
			if (i < window)
			{
				currentUp += item.Up;
				currentDown += item.Down;
			}
			else if (i < window * 2)
			{
				previousUp += item.Up;
				previousDown += item.Down;
			}
		}

		_volatilityCurrentUp = currentUp;
		_volatilityCurrentDown = currentDown;
		_volatilityPreviousUp = previousUp;
		_volatilityPreviousDown = previousDown;
	}

	private void UpdateBaseHistory(ICandleMessage candle)
	{
		_previousThirdCandle = _previousSecondCandle;
		_previousSecondCandle = _previousCandle;
		_previousCandle = candle;
	}

	private decimal CalculateVolume()
	{
		var volume = TradeVolume;
		if (volume > 0m)
			return AdjustVolume(volume);

		var portfolio = Portfolio;
		var security = Security;
		if (portfolio == null || security == null)
			return AdjustVolume(Math.Abs(volume));

		var capital = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (capital <= 0m)
			return AdjustVolume(Math.Abs(volume));

		var percent = Math.Abs(volume) / 100m;
		var moneyToUse = capital * percent;

		var price = _bestAsk > 0m ? _bestAsk : security.LastPrice ?? 0m;
		if (price <= 0m)
			price = security.BestAsk?.Price ?? security.BestBid?.Price ?? 0m;

		if (price <= 0m)
			return AdjustVolume(Math.Abs(volume));

		var estimatedVolume = moneyToUse / price;
		if (estimatedVolume <= 0m)
			return AdjustVolume(Math.Abs(volume));

		return AdjustVolume(estimatedVolume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var volumeStep = security.VolumeStep ?? 0m;
		if (volumeStep > 0m)
			volume = Math.Floor(volume / volumeStep) * volumeStep;

		var minVolume = security.VolumeMin ?? (volumeStep > 0m ? volumeStep : 0m);
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = security.VolumeMax;
		if (maxVolume.HasValue && volume > maxVolume.Value)
			volume = maxVolume.Value;

		if (volume <= 0m && volumeStep > 0m)
			volume = volumeStep;

		return volume;
	}

	private decimal GetBestBidOrClose(ICandleMessage candle)
	{
		return _bestBid > 0m ? _bestBid : candle.ClosePrice;
	}

	private decimal GetBestAskOrClose(ICandleMessage candle)
	{
		return _bestAsk > 0m ? _bestAsk : candle.ClosePrice;
	}

	private void ExitPosition(bool isLong, decimal volume)
	{
		if (volume <= 0m)
			return;

		if (isLong)
			SellMarket(volume);
		else
			BuyMarket(volume);
	}

	private void UpdatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			_pipSize = 1m;
			return;
		}

		var decimals = Security?.Decimals ?? GetDecimalsFromStep(step);
		var coefficient = decimals == 3 || decimals == 5 ? 10m : 1m;
		_pipSize = step * coefficient;
	}

	private void UpdateThresholds()
	{
		_volatilityThreshold = Math.Abs(VolatilityThresholdPoints) * _pipSize;
		_spreadLimit = SpreadLimitPoints * _pipSize;
	}

	private static int GetDecimalsFromStep(decimal step)
	{
		if (step <= 0m)
			return 0;

		var decimals = 0;
		var value = step;

		while (value < 1m && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
