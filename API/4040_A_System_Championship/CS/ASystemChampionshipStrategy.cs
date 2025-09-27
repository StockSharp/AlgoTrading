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
/// Multi-timeframe breakout strategy converted from the original MetaTrader "A System" expert advisor.
/// The strategy looks for momentum breakouts on the main timeframe, manages risk using a trailing stop,
/// and applies an equity stop together with a configurable cool-down period between trades.
/// </summary>
public class ASystemChampionshipStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _primaryTimeFrame;
	private readonly StrategyParam<TimeSpan> _secondaryTimeFrame;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _trailFactor;
	private readonly StrategyParam<decimal> _fallLimit;
	private readonly StrategyParam<decimal> _fallFactor;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _minStopDistance;
	private readonly StrategyParam<TimeSpan> _tradePause;
	private readonly StrategyParam<decimal> _systemStop;
	private readonly StrategyParam<int> _lossesExpected;
	private readonly StrategyParam<int> _tradesExpected;

	private ICandleMessage _primaryCandle;
	private ICandleMessage _secondaryCandle;
	private decimal _quoteTake;
	private decimal _quoteTrail;

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBid;
	private bool _hasAsk;

	private decimal _maxEquity;
	private bool _equityStopLogged;

	private DateTimeOffset? _nextTradeTime;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private Sides? _entrySide;
	private decimal _maxPriceMove;
	private decimal _entryTakeDistance;
	private decimal? _pendingStopPrice;
	private decimal? _pendingTakePrice;

	private decimal _lastTradePrice;
	private Sides? _lastTradeSide;

	private int _completedTrades;
	private int _completedLosingTrades;

	/// <summary>
	/// Initializes the parameters exposed by the strategy.
	/// </summary>
	public ASystemChampionshipStrategy()
	{
		_primaryTimeFrame = Param(nameof(PrimaryTimeFrame), TimeSpan.FromDays(1))
			.SetDisplay("Primary Timeframe", "Higher timeframe used to evaluate breakout candles", "General");

		_secondaryTimeFrame = Param(nameof(SecondaryTimeFrame), TimeSpan.FromHours(4))
			.SetDisplay("Secondary Timeframe", "Lower timeframe used to derive trailing distance", "General");

		_takeFactor = Param(nameof(TakeFactor), 0.8m)
			.SetGreaterThanZero()
			.SetDisplay("Take Factor", "Multiplier applied to the primary candle range for the take-profit", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_trailFactor = Param(nameof(TrailFactor), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Factor", "Multiplier applied to the secondary candle range for trailing", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 20m, 0.5m);

		_fallLimit = Param(nameof(FallLimit), 0.5m)
			.SetNotNegative()
			.SetDisplay("Fall Limit", "Fraction of the maximum profit that enables the retracement exit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.9m, 0.1m);

		_fallFactor = Param(nameof(FallFactor), 0.4m)
			.SetNotNegative()
			.SetDisplay("Fall Factor", "Minimum progress toward the target before retracement exit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.9m, 0.1m);

		_riskPerTrade = Param(nameof(RiskPerTrade), 0.02m)
			.SetNotNegative()
			.SetDisplay("Risk Per Trade", "Share of equity allocated to each position", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.05m, 0.005m);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Fallback trade size used when risk sizing is not available", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_minStopDistance = Param(nameof(MinStopDistance), 0m)
			.SetNotNegative()
			.SetDisplay("Minimum Stop Distance", "Minimal gap required between price and protective orders", "Risk");

		_tradePause = Param(nameof(TradePause), TimeSpan.FromMinutes(5))
			.SetDisplay("Trade Pause", "Cool-down period enforced after each order", "Money Management");

		_systemStop = Param(nameof(SystemStop), 0.8m)
			.SetNotNegative()
			.SetDisplay("Equity Stop", "Fraction of the equity peak that keeps trading enabled", "Risk");

		_lossesExpected = Param(nameof(LossesExpected), 20)
			.SetGreaterThanZero()
			.SetDisplay("Expected Losses", "Loss count used to project risk adjustment", "Money Management");

		_tradesExpected = Param(nameof(TradesExpected), 85)
			.SetGreaterThanZero()
			.SetDisplay("Expected Trades", "Trade count used to project risk adjustment", "Money Management");
	}

	/// <summary>
	/// Primary candle timeframe used for breakout detection.
	/// </summary>
	public TimeSpan PrimaryTimeFrame
	{
		get => _primaryTimeFrame.Value;
		set => _primaryTimeFrame.Value = value;
	}

	/// <summary>
	/// Secondary candle timeframe that controls the trailing distance.
	/// </summary>
	public TimeSpan SecondaryTimeFrame
	{
		get => _secondaryTimeFrame.Value;
		set => _secondaryTimeFrame.Value = value;
	}

	/// <summary>
	/// Range multiplier that defines the take-profit distance.
	/// </summary>
	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	/// <summary>
	/// Range multiplier that defines the trailing stop distance.
	/// </summary>
	public decimal TrailFactor
	{
		get => _trailFactor.Value;
		set => _trailFactor.Value = value;
	}

	/// <summary>
	/// Portion of the maximum profit that activates the retracement exit.
	/// </summary>
	public decimal FallLimit
	{
		get => _fallLimit.Value;
		set => _fallLimit.Value = value;
	}

	/// <summary>
	/// Portion of the full target that must be reached before a retracement exit is allowed.
	/// </summary>
	public decimal FallFactor
	{
		get => _fallFactor.Value;
		set => _fallFactor.Value = value;
	}

	/// <summary>
	/// Fraction of the account equity risked on each trade.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	/// <summary>
	/// Minimal trade volume used when risk sizing falls below the exchange requirements.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Smallest stop distance allowed by the trading venue.
	/// </summary>
	public decimal MinStopDistance
	{
		get => _minStopDistance.Value;
		set => _minStopDistance.Value = value;
	}

	/// <summary>
	/// Pause enforced after each completed order.
	/// </summary>
	public TimeSpan TradePause
	{
		get => _tradePause.Value;
		set => _tradePause.Value = value;
	}

	/// <summary>
	/// Drawdown factor used for the global equity stop.
	/// </summary>
	public decimal SystemStop
	{
		get => _systemStop.Value;
		set => _systemStop.Value = value;
	}

	/// <summary>
	/// Loss count projected by the original expert advisor.
	/// </summary>
	public int LossesExpected
	{
		get => _lossesExpected.Value;
		set => _lossesExpected.Value = value;
	}

	/// <summary>
	/// Trade count projected by the original expert advisor.
	/// </summary>
	public int TradesExpected
	{
		get => _tradesExpected.Value;
		set => _tradesExpected.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, PrimaryTimeFrame.TimeFrame());
		yield return (Security, SecondaryTimeFrame.TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_primaryCandle = null;
		_secondaryCandle = null;
		_quoteTake = 0m;
		_quoteTrail = 0m;
		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBid = false;
		_hasAsk = false;
		_nextTradeTime = null;
		_maxEquity = 0m;
		_equityStopLogged = false;
		_completedTrades = 0;
		_completedLosingTrades = 0;
		_lastTradePrice = 0m;
		_lastTradeSide = null;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_maxEquity = Portfolio?.CurrentValue ?? 0m;
		_equityStopLogged = false;

		var primarySubscription = SubscribeCandles(PrimaryTimeFrame.TimeFrame());
		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		var secondarySubscription = SubscribeCandles(SecondaryTimeFrame.TimeFrame());
		secondarySubscription
			.Bind(ProcessSecondaryCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_primaryCandle = candle;

		var range = candle.HighPrice - candle.LowPrice;
		_quoteTake = range > 0m ? range * TakeFactor : 0m;

		if (_secondaryCandle == null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.CloseTime ?? candle.OpenTime;

		if (!CanEnterAt(time))
			return;

		TryEnterPosition(candle, time);
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_secondaryCandle = candle;

		var range = candle.HighPrice - candle.LowPrice;
		_quoteTrail = range > 0m ? range * TrailFactor : 0m;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid && bid > 0m)
		{
			_bestBid = bid;
			_hasBid = true;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask && ask > 0m)
		{
			_bestAsk = ask;
			_hasAsk = true;
		}

		UpdateEquitySnapshot();

		if (!_hasBid || !_hasAsk)
			return;

		ProcessOpenPosition(message.ServerTime);
	}

	private bool CanEnterAt(DateTimeOffset time)
	{
		if (!_hasBid || !_hasAsk)
			return false;

		if (_nextTradeTime.HasValue && time < _nextTradeTime.Value)
			return false;

		return CheckEquityStop();
	}

	private void TryEnterPosition(ICandleMessage candle, DateTimeOffset time)
	{
		if (Position != 0m)
			return;

		if (_quoteTake <= MinStopDistance)
			return;

		var midpoint = (candle.HighPrice + candle.LowPrice) / 2m;

		var longSignal = candle.ClosePrice > midpoint
			&& _bestAsk > candle.HighPrice
			&& MinStopDistance < _bestBid - candle.LowPrice;

		var shortSignal = candle.ClosePrice < midpoint
			&& _bestBid < candle.LowPrice
			&& MinStopDistance < candle.HighPrice - _bestAsk;

		if (!longSignal && !shortSignal)
			return;

		decimal entryPrice;
		decimal stopPrice;
		decimal takePrice;
		decimal risk;
		Sides side;

		if (longSignal)
		{
			entryPrice = _bestAsk;
			stopPrice = candle.LowPrice;
			takePrice = entryPrice + _quoteTake;
			risk = entryPrice - stopPrice;
			side = Sides.Buy;
		}
		else
		{
			entryPrice = _bestBid;
			stopPrice = candle.HighPrice;
			takePrice = entryPrice - _quoteTake;
			risk = stopPrice - entryPrice;
			side = Sides.Sell;
		}

		if (risk <= 0m)
			return;

		var volume = CalculateTradeVolume(risk);

		if (volume <= 0m)
			return;

		_pendingStopPrice = stopPrice;
		_pendingTakePrice = takePrice;
		_entryTakeDistance = Math.Abs(takePrice - entryPrice);

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_nextTradeTime = time + TradePause;
	}

	private void ProcessOpenPosition(DateTimeOffset time)
	{
		if (_entryPrice is null || _entrySide is null)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (_entrySide == Sides.Buy)
		{
			var bid = _bestBid;
			if (bid <= 0m)
				return;

			var move = bid - _entryPrice.Value;
			if (move > _maxPriceMove)
				_maxPriceMove = move;

			if (_stopPrice is decimal stop && bid <= stop)
			{
				SellMarket(volume);
				_nextTradeTime = time + TradePause;
				return;
			}

			if (_takePrice is decimal take && bid >= take)
			{
				SellMarket(volume);
				_nextTradeTime = time + TradePause;
				return;
			}

			if (TryCloseLongByRetrace(bid, volume, time))
				return;

			TryTrailLong(bid);
		}
		else
		{
			var ask = _bestAsk;
			if (ask <= 0m)
				return;

			var move = _entryPrice.Value - ask;
			if (move > _maxPriceMove)
				_maxPriceMove = move;

			if (_stopPrice is decimal stop && ask >= stop)
			{
				BuyMarket(volume);
				_nextTradeTime = time + TradePause;
				return;
			}

			if (_takePrice is decimal take && ask <= take)
			{
				BuyMarket(volume);
				_nextTradeTime = time + TradePause;
				return;
			}

			if (TryCloseShortByRetrace(ask, volume, time))
				return;

			TryTrailShort(ask);
		}
	}

	private bool TryCloseLongByRetrace(decimal bid, decimal volume, DateTimeOffset time)
	{
		if (_entryPrice is null || _entryTakeDistance <= 0m)
			return false;

		var move = bid - _entryPrice.Value;
		if (move <= 0m)
			return false;

		if (_maxPriceMove <= 0m)
			return false;

		var threshold = _maxPriceMove * FallLimit;
		if (move >= threshold)
			return false;

		var requiredAdvance = _entryTakeDistance > 0m ? _entryTakeDistance : _quoteTake;
		if (requiredAdvance <= 0m)
			return false;

		if (move > requiredAdvance * FallFactor)
		{
			SellMarket(volume);
			_nextTradeTime = time + TradePause;
			return true;
		}

		return false;
	}

	private bool TryCloseShortByRetrace(decimal ask, decimal volume, DateTimeOffset time)
	{
		if (_entryPrice is null || _entryTakeDistance <= 0m)
			return false;

		var move = _entryPrice.Value - ask;
		if (move <= 0m)
			return false;

		if (_maxPriceMove <= 0m)
			return false;

		var threshold = _maxPriceMove * FallLimit;
		if (move >= threshold)
			return false;

		var requiredAdvance = _entryTakeDistance > 0m ? _entryTakeDistance : _quoteTake;
		if (requiredAdvance <= 0m)
			return false;

		if (move > requiredAdvance * FallFactor)
		{
			BuyMarket(volume);
			_nextTradeTime = time + TradePause;
			return true;
		}

		return false;
	}

	private void TryTrailLong(decimal bid)
	{
		if (_entryPrice is null || _stopPrice is null || _takePrice is null)
			return;

		if (_quoteTrail <= MinStopDistance)
			return;

		var move = bid - _entryPrice.Value;
		if (move <= 0m)
			return;

		if (bid >= _takePrice.Value - MinStopDistance)
			return;

		var newStop = bid - _quoteTrail;
		if (newStop <= _stopPrice.Value)
			return;

		if (newStop >= _takePrice.Value)
			return;

		var step = Security?.PriceStep ?? 0m;
		if (step > 0m && newStop - _stopPrice.Value < step)
			return;

		_stopPrice = newStop;
	}

	private void TryTrailShort(decimal ask)
	{
		if (_entryPrice is null || _stopPrice is null || _takePrice is null)
			return;

		if (_quoteTrail <= MinStopDistance)
			return;

		var move = _entryPrice.Value - ask;
		if (move <= 0m)
			return;

		if (ask <= _takePrice.Value + MinStopDistance)
			return;

		var newStop = ask + _quoteTrail;
		if (newStop >= _stopPrice.Value)
			return;

		if (newStop <= _takePrice.Value)
			return;

		var step = Security?.PriceStep ?? 0m;
		if (step > 0m && _stopPrice.Value - newStop < step)
			return;

		_stopPrice = newStop;
	}

	private decimal CalculateTradeVolume(decimal risk)
	{
		var baseVolume = BaseVolume;
		var security = Security;
		var portfolio = Portfolio;

		if (risk <= 0m)
			return 0m;

		if (security == null || portfolio?.CurrentValue is not decimal equity || equity <= 0m)
			return baseVolume;

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume <= 0m)
			minVolume = 1m;

		var volumeStep = security.VolumeStep ?? minVolume;
		if (volumeStep <= 0m)
			volumeStep = minVolume;

		var maxVolume = security.MaxVolume;

		if (baseVolume < minVolume)
			baseVolume = minVolume;

		var stepSize = security.StepSize ?? security.PriceStep ?? 0m;
		if (stepSize <= 0m)
			stepSize = 1m;

		var stepValue = security.StepPrice ?? stepSize;
		if (stepValue <= 0m)
			stepValue = 1m;

		var riskAdjustment = CalculateRiskAdjustment();
		var riskAmount = equity * RiskPerTrade * riskAdjustment;

		var riskPerUnit = risk / stepSize * stepValue;
		if (riskPerUnit <= 0m)
			return baseVolume;

		var rawVolume = riskAmount / riskPerUnit;
		if (rawVolume < minVolume)
			return baseVolume;

		var steps = Math.Floor((rawVolume - minVolume) / volumeStep);
		var volume = minVolume + steps * volumeStep;

		if (volume < minVolume)
			volume = minVolume;

		if (maxVolume.HasValue && volume > maxVolume.Value)
			volume = maxVolume.Value;

		if (volume < baseVolume)
			volume = baseVolume;

		return volume;
	}

	private decimal CalculateRiskAdjustment()
	{
		if (TradesExpected <= 0)
			return 1m;

		var lossProbability = (decimal)LossesExpected / TradesExpected;
		var actualLossRatio = _completedTrades > 0 ? (decimal)_completedLosingTrades / _completedTrades : 0m;
		var adjustment = 1m + lossProbability - actualLossRatio;
		return adjustment < 0.1m ? 0.1m : adjustment;
	}

	private void UpdateEquitySnapshot()
	{
		var equity = Portfolio?.CurrentValue;
		if (equity is decimal value && value > _maxEquity)
			_maxEquity = value;
	}

	private bool CheckEquityStop()
	{
		var equity = Portfolio?.CurrentValue;
		if (equity is not decimal value || value <= 0m)
			return true;

		if (value > _maxEquity)
		{
			_maxEquity = value;
			if (_equityStopLogged)
			{
				LogInfo($"Equity recovered above stop level at {value:0.##}. Trading resumed.");
				_equityStopLogged = false;
			}
		}

		var threshold = _maxEquity * SystemStop;
		if (value <= threshold)
		{
			if (!_equityStopLogged && Position == 0m)
			{
				LogInfo($"Equity stop triggered at {value:0.##}. Waiting for recovery above {threshold:0.##}.");
				_equityStopLogged = true;
			}

			return false;
		}

		if (_equityStopLogged)
		{
			LogInfo($"Equity recovered above stop level at {value:0.##}. Trading resumed.");
			_equityStopLogged = false;
		}

		return true;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
			return;

		_lastTradePrice = trade.Trade.Price;
		_lastTradeSide = trade.Order.Side;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			if (_entryPrice.HasValue && _entrySide.HasValue && delta != 0m && _lastTradePrice > 0m)
			{
				var closedVolume = Math.Abs(delta);
				var profitPerUnit = _entrySide == Sides.Buy
					? _lastTradePrice - _entryPrice.Value
					: _entryPrice.Value - _lastTradePrice;

				if (closedVolume > 0m)
				{
					_completedTrades++;
					if (profitPerUnit < 0m)
						_completedLosingTrades++;
				}
			}

			ResetPositionState();
			return;
		}

		_entryPrice = Position.AveragePrice;
		_entrySide = Position > 0m ? Sides.Buy : Sides.Sell;
		_maxPriceMove = 0m;

		if (_pendingStopPrice.HasValue)
		{
			_stopPrice = _pendingStopPrice;
			_pendingStopPrice = null;
		}

		if (_pendingTakePrice.HasValue)
		{
			_takePrice = _pendingTakePrice;
			_pendingTakePrice = null;
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_entrySide = null;
		_maxPriceMove = 0m;
		_entryTakeDistance = 0m;
		_pendingStopPrice = null;
		_pendingTakePrice = null;
	}
}
