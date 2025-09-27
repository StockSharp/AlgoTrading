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
/// Money Rain strategy converted from the MetaTrader expert advisor.
/// </summary>
public class MoneyRainRecoveryStrategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _lossesLimit;
	private readonly StrategyParam<bool> _fastOptimization;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _entryPrice;
	private decimal _entryVolume;
	private Sides? _entryDirection;
	private bool _openRequestPending;
	private Sides? _pendingEntryDirection;
	private decimal _pendingEntryVolume;

	private bool _closePending;
	private decimal _pendingExitVolume;
	private decimal _filledExitVolume;
	private decimal _weightedExitPrice;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _currentSpreadPoints;

	private decimal _lossVolumeUnits;
	private int _consecutiveLosses;
	private int _consecutiveProfits;
	private decimal _nextTradeVolume;
	private bool _lossLimitReached;

	/// <summary>
	/// Initializes a new instance of <see cref="MoneyRainRecoveryStrategy"/>.
	/// </summary>
	public MoneyRainRecoveryStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "Oscillator length", "General")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Distance to the take-profit expressed in price steps", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Distance to the stop-loss expressed in price steps", "Risk")
			.SetCanOptimize(true);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Baseline position size before recovery adjustment", "Money Management")
			.SetCanOptimize(true);

		_lossesLimit = Param(nameof(LossesLimit), 1_000_000)
			.SetDisplay("Loss Limit", "Maximum allowed consecutive losing trades", "Risk")
			.SetCanOptimize(false);

		_fastOptimization = Param(nameof(FastOptimization), true)
			.SetDisplay("Fast Optimization", "Disable recovery sizing while optimizing", "Money Management")
			.SetCanOptimize(false);

		_threshold = Param(nameof(Threshold), 0.5m)
			.SetDisplay("Threshold", "DeMarker threshold separating buys and sells", "Signals")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// DeMarker period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Base position size.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Maximum consecutive losing trades allowed before pausing entries.
	/// </summary>
	public int LossesLimit
	{
		get => _lossesLimit.Value;
		set => _lossesLimit.Value = value;
	}

	/// <summary>
	/// Skip recovery sizing during optimization runs.
	/// </summary>
	public bool FastOptimization
	{
		get => _fastOptimization.Value;
		set => _fastOptimization.Value = value;
	}

	/// <summary>
	/// Threshold separating bullish and bearish signals.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Candle type to process.
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

		_entryPrice = null;
		_entryVolume = 0m;
		_entryDirection = null;
		_openRequestPending = false;
		_pendingEntryDirection = null;
		_pendingEntryVolume = 0m;

		_closePending = false;
		_pendingExitVolume = 0m;
		_filledExitVolume = 0m;
		_weightedExitPrice = 0m;

		_bestBid = null;
		_bestAsk = null;
		_currentSpreadPoints = 0m;

		_lossVolumeUnits = 0m;
		_consecutiveLosses = 0;
		_consecutiveProfits = 0;
		_nextTradeVolume = NormalizeVolume(BaseVolume);
		_lossLimitReached = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var deMarker = new DeMarker { Length = DeMarkerPeriod };

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.Bind(deMarker, ProcessCandle)
			.Start();

		SubscribeLevel1().Bind(ProcessLevel1).Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
				_bestBid = bid;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		{
			var ask = (decimal)askValue;
			if (ask > 0m)
				_bestAsk = ask;
		}

		UpdateSpread();
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_lossLimitReached)
			return;

		if (_entryDirection is null && !_openRequestPending)
		{
			TryOpenPosition(candle, deMarkerValue);
		}
		else if (_entryDirection is not null)
		{
			TryClosePosition(candle);
		}
	}

	private void TryOpenPosition(ICandleMessage candle, decimal deMarkerValue)
	{
		var volume = FastOptimization ? NormalizeVolume(BaseVolume) : _nextTradeVolume;
		if (volume <= 0m)
			return;

		var direction = deMarkerValue > Threshold ? Sides.Buy : Sides.Sell;

		_openRequestPending = true;
		_pendingEntryDirection = direction;
		_pendingEntryVolume = volume;

		if (direction == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}
	}

	private void TryClosePosition(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry || _entryDirection is not Sides direction)
			return;

		if (_closePending || Position == 0m)
			return;

		var stopDistance = PointsToPrice(StopLossPoints);
		var takeDistance = PointsToPrice(TakeProfitPoints);

		if (direction == Sides.Buy)
		{
			if (StopLossPoints > 0m && candle.LowPrice <= entry - stopDistance)
			{
				RequestClosePosition();
				return;
			}

			if (TakeProfitPoints > 0m && candle.HighPrice >= entry + takeDistance)
			{
				RequestClosePosition();
			}
		}
		else
		{
			if (StopLossPoints > 0m && candle.HighPrice >= entry + stopDistance)
			{
				RequestClosePosition();
				return;
			}

			if (TakeProfitPoints > 0m && candle.LowPrice <= entry - takeDistance)
			{
				RequestClosePosition();
			}
		}
	}

	private void RequestClosePosition()
	{
		if (_entryDirection is not Sides direction)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		_pendingExitVolume = volume;
		_filledExitVolume = 0m;
		_weightedExitPrice = 0m;

		_closePending = true;

		if (direction == Sides.Buy)
		{
			SellMarket(volume);
		}
		else
		{
			BuyMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var direction = trade.Order.Side;
		var tradeVolume = trade.Trade.Volume ?? trade.Order.Volume;
		if (tradeVolume is null || tradeVolume <= 0m)
			return;

		if (_openRequestPending && _pendingEntryDirection is Sides expected && direction == expected)
		{
			RegisterEntryFill(expected, trade.Trade.Price, tradeVolume.Value);

			_openRequestPending = false;
			_pendingEntryDirection = null;
			_pendingEntryVolume = 0m;
			_closePending = false;
			return;
		}

		if (_entryDirection is not Sides currentDirection)
			return;

		if (direction == currentDirection)
		{
			RegisterEntryFill(currentDirection, trade.Trade.Price, tradeVolume.Value);
			return;
		}

		var isClosingTrade = (currentDirection == Sides.Buy && direction == Sides.Sell) ||
			(currentDirection == Sides.Sell && direction == Sides.Buy);

		if (!isClosingTrade)
			return;

		_filledExitVolume += tradeVolume.Value;
		_weightedExitPrice += trade.Trade.Price * tradeVolume.Value;

		if (_filledExitVolume < _pendingExitVolume)
			return;

		var exitPrice = _filledExitVolume > 0m ? _weightedExitPrice / _filledExitVolume : trade.Trade.Price;
		ProcessCompletedTrade(exitPrice);
	}

	private void RegisterEntryFill(Sides direction, decimal price, decimal volume)
	{
		if (volume <= 0m)
			return;

		var previousVolume = _entryVolume;
		var totalVolume = previousVolume + volume;

		decimal newPrice;

		if (previousVolume > 0m)
		{
			var prevPrice = _entryPrice ?? price;
			newPrice = ((prevPrice * previousVolume) + price * volume) / totalVolume;
		}
		else
		{
			newPrice = price;
		}

		_entryPrice = newPrice;
		_entryVolume = totalVolume;
		_entryDirection = direction;
	}

	private void ProcessCompletedTrade(decimal exitPrice)
	{
		if (_entryPrice is not decimal entry || _entryDirection is not Sides direction)
			return;

		var priceDiff = direction == Sides.Buy ? exitPrice - entry : entry - exitPrice;
		var isProfit = priceDiff > 0m;

		UpdateMoneyManagement(isProfit, _entryVolume);

		if (!isProfit && LossesLimit > 0 && _consecutiveLosses >= LossesLimit)
		{
			_lossLimitReached = true;
			LogWarning("Loss limit reached. Entries are paused after {0} consecutive losses.", _consecutiveLosses);
		}

		if (isProfit)
		{
			_lossLimitReached = false;
		}

		_entryPrice = null;
		_entryVolume = 0m;
		_entryDirection = null;
		_closePending = false;
		_pendingExitVolume = 0m;
		_filledExitVolume = 0m;
		_weightedExitPrice = 0m;
	}

	private void UpdateMoneyManagement(bool isProfit, decimal closedVolume)
	{
		var baseVolume = NormalizeVolume(BaseVolume);

		if (!isProfit)
		{
			_consecutiveLosses++;
			_consecutiveProfits = 0;

			if (BaseVolume > 0m)
				_lossVolumeUnits += closedVolume / BaseVolume;

			_nextTradeVolume = baseVolume;
			return;
		}

		var nextVolume = baseVolume;

		if (!FastOptimization && _lossVolumeUnits > 0.5m && _consecutiveProfits < 1)
		{
			var adjustedVolume = CalculateRecoveryVolume();
			nextVolume = NormalizeVolume(adjustedVolume);
		}

		_consecutiveLosses = 0;

		if (_consecutiveProfits > 1)
			_lossVolumeUnits = 0m;

		_consecutiveProfits++;

		_nextTradeVolume = FastOptimization ? baseVolume : nextVolume;
	}

	private decimal CalculateRecoveryVolume()
	{
		if (BaseVolume <= 0m)
			return 0m;

		var tp = TakeProfitPoints;
		var sl = StopLossPoints;
		if (tp <= 0m || sl <= 0m)
			return BaseVolume;

		var spread = Math.Max(_currentSpreadPoints, 0m);
		var denominator = tp - spread;
		if (denominator <= 0m)
			denominator = tp;

		if (denominator <= 0m)
			return BaseVolume;

		return BaseVolume * _lossVolumeUnits * (sl + spread) / denominator;
	}

	private void UpdateSpread()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			_currentSpreadPoints = 0m;
			return;
		}

		if (_bestBid is decimal bid && _bestAsk is decimal ask && ask > bid)
		{
			_currentSpreadPoints = (ask - bid) / step;
		}
	}

	private decimal PointsToPrice(decimal points)
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? points * step : points;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		var min = Security?.MinVolume;
		var max = Security?.MaxVolume;

		if (min is decimal minVolume && volume < minVolume)
			return 0m;

		decimal normalized;

		if (step > 0m)
		{
			var stepsCount = (decimal)Math.Floor((double)(volume / step));
			if (stepsCount <= 0m)
				stepsCount = 1m;

			normalized = stepsCount * step;
		}
		else
		{
			normalized = Math.Round(volume, 2, MidpointRounding.ToZero);
		}

		if (min is decimal minVol && normalized < minVol)
			normalized = minVol;

		if (max is decimal maxVol && normalized > maxVol)
			normalized = maxVol;

		return normalized;
	}

	/// <inheritdoc />
	protected override void OnOrderFailed(Order order, OrderFail fail)
	{
		base.OnOrderFailed(order, fail);

		if (_openRequestPending && _pendingEntryDirection is Sides expected && order.Direction == expected)
		{
			_openRequestPending = false;
			_pendingEntryDirection = null;
			_pendingEntryVolume = 0m;
		}

		if (_closePending && _entryDirection is Sides direction)
		{
			var isExitOrder = (direction == Sides.Buy && order.Direction == Sides.Sell) ||
			(direction == Sides.Sell && order.Direction == Sides.Buy);

			if (isExitOrder)
			{
				_closePending = false;
				_pendingExitVolume = 0m;
				_filledExitVolume = 0m;
				_weightedExitPrice = 0m;
			}
		}
	}
}

