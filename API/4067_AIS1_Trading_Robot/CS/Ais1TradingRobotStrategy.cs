using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AIS1 breakout strategy converted from MetaTrader code in MQL/8700.
/// Trades EURUSD daily breakouts with risk based sizing and trailing stops.
/// </summary>
public class Ais1TradingRobotStrategy : Strategy
{
	private static readonly TimeSpan PauseDuration = TimeSpan.FromSeconds(5);

	private readonly StrategyParam<decimal> _accountReserve;
	private readonly StrategyParam<decimal> _orderReserve;
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _secondaryCandleType;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _stopFactor;
	private readonly StrategyParam<decimal> _trailFactor;
	private readonly StrategyParam<decimal> _trailStepMultiplier;
	private readonly StrategyParam<decimal> _stopBufferTicks;

	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _quoteSpread;
	private decimal _quoteStopsBuffer;
	private decimal _quoteTakeDistance;
	private decimal _quoteStopDistance;
	private decimal _quoteTrailDistance;
	private decimal _trailStepDistance;
	private decimal _primaryAverage;
	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTargetPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTargetPrice;
	private decimal _maxEquity;
	private bool _isEquitySafe = true;
	private DateTimeOffset _lastActionTime = DateTimeOffset.MinValue;

	/// <summary>
	/// Fraction of account equity that should remain untouched.
	/// </summary>
	public decimal AccountReserve
	{
		get => _accountReserve.Value;
		set => _accountReserve.Value = value;
	}

	/// <summary>
	/// Fraction of equity allocated to a single trade.
	/// </summary>
	public decimal OrderReserve
	{
		get => _orderReserve.Value;
		set => _orderReserve.Value = value;
	}

	/// <summary>
	/// Primary timeframe used for breakout detection.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Secondary timeframe providing the trailing stop range.
	/// </summary>
	public DataType SecondaryCandleType
	{
		get => _secondaryCandleType.Value;
		set => _secondaryCandleType.Value = value;
	}

	/// <summary>
	/// Take profit multiplier relative to the primary candle range.
	/// </summary>
	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	/// <summary>
	/// Stop loss multiplier relative to the primary candle range.
	/// </summary>
	public decimal StopFactor
	{
		get => _stopFactor.Value;
		set => _stopFactor.Value = value;
	}

	/// <summary>
	/// Trailing distance multiplier based on the secondary range.
	/// </summary>
	public decimal TrailFactor
	{
		get => _trailFactor.Value;
		set => _trailFactor.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the spread when computing the minimal trailing step.
	/// </summary>
	public decimal TrailStepMultiplier
	{
		get => _trailStepMultiplier.Value;
		set => _trailStepMultiplier.Value = value;
	}

	/// <summary>
	/// Extra price steps added as a safety buffer for stops and targets.
	/// </summary>
	public decimal StopBufferTicks
	{
		get => _stopBufferTicks.Value;
		set => _stopBufferTicks.Value = value;
	}

	public Ais1TradingRobotStrategy()
	{
		_accountReserve = Param(nameof(AccountReserve), 0.20m)
			.SetDisplay("Account Reserve", "Fraction of equity kept untouched", "Risk")
			.SetGreaterOrEqual(0m)
			.SetLessOrEquals(0.95m);

		_orderReserve = Param(nameof(OrderReserve), 0.04m)
			.SetDisplay("Order Reserve", "Fraction of equity per trade", "Risk")
			.SetGreaterOrEqual(0m)
			.SetLessOrEquals(0.50m);

		_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Primary Candle", "Timeframe used for breakouts", "General");

		_secondaryCandleType = Param(nameof(SecondaryCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Secondary Candle", "Timeframe feeding trailing calculations", "General");

		_takeFactor = Param(nameof(TakeFactor), 0.8m)
			.SetDisplay("Take Factor", "Take profit multiplier of the daily range", "Targets")
			.SetGreaterThanZero();

		_stopFactor = Param(nameof(StopFactor), 1.0m)
			.SetDisplay("Stop Factor", "Stop loss multiplier of the daily range", "Targets")
			.SetGreaterThanZero();

		_trailFactor = Param(nameof(TrailFactor), 5.0m)
			.SetDisplay("Trail Factor", "Trailing multiplier of the secondary range", "Targets")
			.SetGreaterThanZero();

		_trailStepMultiplier = Param(nameof(TrailStepMultiplier), 1.0m)
			.SetDisplay("Trail Step Mult", "Spread multiplier for trailing activation", "Execution")
			.SetGreaterThanZero();

		_stopBufferTicks = Param(nameof(StopBufferTicks), 0m)
			.SetDisplay("Stop Buffer Ticks", "Additional ticks separating levels", "Execution")
			.SetNotNegative();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		// Subscribe to both candle series so the strategy always receives required data.
		return new[]
		{
			(Security, PrimaryCandleType),
			(Security, SecondaryCandleType)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Clear cached market data and position state.
		_bestBid = 0m;
		_bestAsk = 0m;
		_quoteSpread = 0m;
		_quoteStopsBuffer = 0m;
		_quoteTakeDistance = 0m;
		_quoteStopDistance = 0m;
		_quoteTrailDistance = 0m;
		_trailStepDistance = 0m;
		_primaryAverage = 0m;
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopPrice = null;
		_longTargetPrice = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
		_maxEquity = 0m;
		_isEquitySafe = true;
		_lastActionTime = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_maxEquity = Portfolio?.CurrentValue ?? 0m;
		_isEquitySafe = true;
		_lastActionTime = DateTimeOffset.MinValue;

		// Subscribe to daily candles used for breakout decisions.
		var primarySubscription = SubscribeCandles(PrimaryCandleType);
		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		// Subscribe to four-hour candles providing trailing ranges.
		var secondarySubscription = SubscribeCandles(SecondaryCandleType);
		secondarySubscription
			.Bind(ProcessSecondaryCandle)
			.Start();

		// Track best bid/ask to align with the original EA behaviour.
		SubscribeOrderBook()
			.Bind(depth =>
			{
				var bestBid = depth.GetBestBid();
				if (bestBid != null)
					_bestBid = bestBid.Price;

				var bestAsk = depth.GetBestAsk();
				if (bestAsk != null)
					_bestAsk = bestAsk.Price;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			// Plot candles and trades for easier visual debugging.
			DrawCandles(area, primarySubscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateBestPricesFromCandle(candle);
		UpdateEquityState(candle.CloseTime);
		UpdatePrimaryMetrics(candle);
		ManagePosition(candle);
		TryEnterTrade(candle);
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateBestPricesFromCandle(candle);
		UpdateEquityState(candle.CloseTime);
		UpdateSecondaryMetrics(candle);
		ManagePosition(candle);
	}

	private void UpdateEquityState(DateTimeOffset time)
	{
		if (Portfolio == null)
		{
			_isEquitySafe = true;
			return;
		}

		var equity = Portfolio.CurrentValue;
		if (equity <= 0m)
		{
			_isEquitySafe = false;
			return;
		}

		if (equity > _maxEquity)
			_maxEquity = equity;

		// The original EA blocks trading once equity drops below this threshold.
		var limit = AccountReserve - OrderReserve;
		if (limit < 0m)
			limit = 0m;
		else if (limit > 0.95m)
			limit = 0.95m;

		var threshold = _maxEquity * (1m - limit);
		_isEquitySafe = equity >= threshold;
	}

	private void UpdatePrimaryMetrics(ICandleMessage candle)
	{
		// Mid-point and range from the completed daily candle.
		_primaryAverage = (candle.HighPrice + candle.LowPrice) / 2m;

		var range = Math.Max(0m, candle.HighPrice - candle.LowPrice);
		_quoteStopDistance = range * StopFactor;
		_quoteTakeDistance = range * TakeFactor;

		var priceStep = Security?.PriceStep ?? 0m;

		if (_bestAsk > 0m && _bestBid > 0m)
			_quoteSpread = Math.Max(0m, _bestAsk - _bestBid);
		else if (priceStep > 0m)
			_quoteSpread = priceStep;
		else
			_quoteSpread = 0m;

		var bufferTicks = StopBufferTicks;
		if (bufferTicks < 0m)
			bufferTicks = 0m;
		_quoteStopsBuffer = priceStep > 0m ? bufferTicks * priceStep : 0m;

		// Minimum distance required before tightening the trailing stop.
		_trailStepDistance = _quoteSpread * TrailStepMultiplier;
	}

	private void UpdateSecondaryMetrics(ICandleMessage candle)
	{
		var range = Math.Max(0m, candle.HighPrice - candle.LowPrice);
		_quoteTrailDistance = range * TrailFactor;
	}

	private void TryEnterTrade(ICandleMessage candle)
	{
		if (Position != 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isEquitySafe)
			return;

		if (!IsPauseElapsed(candle.CloseTime))
			return;

		if (_quoteStopDistance <= 0m || _quoteTakeDistance <= 0m)
			return;

		var ask = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;
		var bid = _bestBid > 0m ? _bestBid : candle.ClosePrice;

		if (ask <= 0m || bid <= 0m)
			return;

		var longCondition = candle.ClosePrice > _primaryAverage && ask > candle.HighPrice;
		if (longCondition)
		{
			var stopPrice = candle.HighPrice - _quoteStopDistance;
			var takePrice = ask + _quoteTakeDistance;

			if (!ValidateLongLevels(ask, stopPrice, takePrice))
				return;

			var volume = CalculatePositionVolume(ask, stopPrice);
			if (volume <= 0m)
				return;

			// Enter long position and store protective levels.
			BuyMarket(volume);
			_longEntryPrice = ask;
			_longStopPrice = stopPrice;
			_longTargetPrice = takePrice;
			_shortStopPrice = null;
			_shortTargetPrice = null;
			_lastActionTime = candle.CloseTime;
			return;
		}

		var shortCondition = candle.ClosePrice < _primaryAverage && bid < candle.LowPrice;
		if (shortCondition)
		{
			var stopPrice = candle.LowPrice + _quoteStopDistance;
			var takePrice = bid - _quoteTakeDistance;

			if (!ValidateShortLevels(bid, stopPrice, takePrice))
				return;

			var volume = CalculatePositionVolume(bid, stopPrice);
			if (volume <= 0m)
				return;

			// Enter short position mirroring the MQL logic.
			SellMarket(volume);
			_shortEntryPrice = bid;
			_shortStopPrice = stopPrice;
			_shortTargetPrice = takePrice;
			_longStopPrice = null;
			_longTargetPrice = null;
			_lastActionTime = candle.CloseTime;
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position == 0)
			return;

		var bid = _bestBid > 0m ? _bestBid : candle.ClosePrice;
		var ask = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;

		if (Position > 0)
		{
			// Close long if price hits stored stop or target.
			if (_longStopPrice is decimal stop && bid <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				_lastActionTime = candle.CloseTime;
				return;
			}

			if (_longTargetPrice is decimal target && bid >= target)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				_lastActionTime = candle.CloseTime;
				return;
			}

			if (IsPauseElapsed(candle.CloseTime))
				TryUpdateLongTrailing(bid, candle.CloseTime);
		}
		else
		{
			// Close short if price hits stop or target.
			if (_shortStopPrice is decimal stop && ask >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				_lastActionTime = candle.CloseTime;
				return;
			}

			if (_shortTargetPrice is decimal target && ask <= target)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				_lastActionTime = candle.CloseTime;
				return;
			}

			if (IsPauseElapsed(candle.CloseTime))
				TryUpdateShortTrailing(ask, candle.CloseTime);
		}
	}

	private void TryUpdateLongTrailing(decimal bid, DateTimeOffset time)
	{
		if (!_isEquitySafe)
			return;

		if (_quoteTrailDistance <= 0m || _quoteTrailDistance <= _quoteStopsBuffer)
			return;

		if (_trailStepDistance <= 0m)
			return;

		if (_longStopPrice is not decimal currentStop)
			return;

		if (bid <= currentStop + _trailStepDistance + _quoteTrailDistance)
			return;

		if (_longTargetPrice is decimal target && bid >= target - _quoteStopsBuffer)
			return;

		var newStop = bid - _quoteTrailDistance;
		if (newStop <= currentStop)
			return;

		// Move the long stop closer to the market as profit accrues.
		_longStopPrice = newStop;
		_lastActionTime = time;
	}

	private void TryUpdateShortTrailing(decimal ask, DateTimeOffset time)
	{
		if (!_isEquitySafe)
			return;

		if (_quoteTrailDistance <= 0m || _quoteTrailDistance <= _quoteStopsBuffer)
			return;

		if (_trailStepDistance <= 0m)
			return;

		if (_shortStopPrice is not decimal currentStop)
			return;

		if (ask >= currentStop - _trailStepDistance - _quoteTrailDistance)
			return;

		if (_shortTargetPrice is decimal target && ask <= target + _quoteStopsBuffer)
			return;

		var newStop = ask + _quoteTrailDistance;
		if (newStop >= currentStop)
			return;

		// Tighten the short stop once price moves further in favour.
		_shortStopPrice = newStop;
		_lastActionTime = time;
	}

	private decimal CalculatePositionVolume(decimal entryPrice, decimal stopPrice)
	{
		var riskPerUnit = Math.Abs(entryPrice - stopPrice);
		if (riskPerUnit <= 0m)
			return 0m;

		if (Portfolio == null)
			return 0m;

		var equity = Portfolio.CurrentValue;
		if (equity <= 0m)
			return 0m;

		var allocation = OrderReserve;
		if (allocation <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		var valuePerPoint = priceStep > 0m && stepPrice > 0m ? stepPrice / priceStep : 1m;

		var riskPerVolume = riskPerUnit * valuePerPoint;
		if (riskPerVolume <= 0m)
			return 0m;

		// Equivalent to VARLimit in the original MQL code.
		var riskBudget = equity * allocation;
		if (riskBudget <= 0m)
			return 0m;

		var volume = riskBudget / riskPerVolume;
		volume = AdjustVolume(volume);

		return volume > 0m ? volume : 0m;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security == null)
			return Math.Max(volume, 0m);

		var minVolume = Security.MinVolume ?? 0m;
		var maxVolume = Security.MaxVolume ?? decimal.MaxValue;
		var step = Security.VolumeStep ?? 0m;

		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		if (step > 0m)
		{
			var offset = minVolume > 0m ? minVolume : 0m;
			var steps = Math.Floor((volume - offset) / step);
			volume = offset + step * steps;
		}

		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		return Math.Max(volume, 0m);
	}

	private void ResetPositionState()
	{
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopPrice = null;
		_longTargetPrice = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
	}

	private bool IsPauseElapsed(DateTimeOffset time)
	{
		return time - _lastActionTime >= PauseDuration;
	}

	private void UpdateBestPricesFromCandle(ICandleMessage candle)
	{
		var close = candle.ClosePrice;
		if (close <= 0m)
			return;

		if (_bestBid <= 0m)
			_bestBid = close;

		if (_bestAsk <= 0m)
			_bestAsk = close;
	}

	private bool ValidateLongLevels(decimal entry, decimal stop, decimal take)
	{
		if (stop <= 0m || take <= 0m)
			return false;

		if (stop >= entry)
			return false;

		if (take <= entry)
			return false;

		if (entry - stop <= _quoteStopsBuffer)
			return false;

		if (take - entry <= _quoteStopsBuffer)
			return false;

		return true;
	}

	private bool ValidateShortLevels(decimal entry, decimal stop, decimal take)
	{
		if (stop <= 0m || take <= 0m)
			return false;

		if (stop <= entry)
			return false;

		if (take >= entry)
			return false;

		if (stop - entry <= _quoteStopsBuffer)
			return false;

		if (entry - take <= _quoteStopsBuffer)
			return false;

		return true;
	}
}
