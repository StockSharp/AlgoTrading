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
/// Breakout strategy converted from the "ACB1" MetaTrader expert advisor.
/// Combines a daily breakout filter with H4-based trailing stops and risk-driven position sizing.
/// </summary>
public class Acb1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<DataType> _trailCandleType;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _trailFactor;
	private readonly StrategyParam<decimal> _riskFraction;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _minStopDistancePoints;
	private readonly StrategyParam<decimal> _cooldownSeconds;

	private decimal? _signalHigh;
	private decimal? _signalLow;
	private decimal? _signalClose;
	private decimal? _takeDistance;

	private decimal? _trailDistance;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private decimal? _bestBid;
	private decimal? _bestAsk;

	private decimal _maxEquity;
	private DateTimeOffset? _lastActionTime;

	/// <summary>
	/// Initializes a new instance of the strategy with default parameters that mirror the original EA.
	/// </summary>
	public Acb1Strategy()
	{
		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Signal candles", "Time frame used to detect daily breakouts", "General");

		_trailCandleType = Param(nameof(TrailCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Trailing candles", "Time frame that defines the trailing distance", "General");

		_takeFactor = Param(nameof(TakeFactor), 0.8m)
		.SetDisplay("Take factor", "Multiplier applied to the signal candle range for take-profit", "Execution");

		_trailFactor = Param(nameof(TrailFactor), 10m)
		.SetDisplay("Trail factor", "Multiplier applied to the trailing candle range for the stop", "Execution");

		_riskFraction = Param(nameof(RiskFraction), 0.05m)
		.SetDisplay("Risk fraction", "Share of equity risked per trade (0.05 = 5%)", "Risk");

		_maxVolume = Param(nameof(MaxVolume), 5m)
		.SetDisplay("Max volume", "Upper volume cap that mirrors the 5-lot limit", "Risk");

		_minStopDistancePoints = Param(nameof(MinStopDistancePoints), 0m)
		.SetDisplay("Min stop distance", "Broker stop-level constraint expressed in price points", "Risk");

		_cooldownSeconds = Param(nameof(CooldownSeconds), 5m)
		.SetDisplay("Cooldown (sec)", "Minimum delay between trade actions", "Risk");
	}

	/// <summary>
	/// Daily candle type used for breakout detection.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// H4 candle type used to estimate the trailing distance.
	/// </summary>
	public DataType TrailCandleType
	{
		get => _trailCandleType.Value;
		set => _trailCandleType.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the signal candle range to place the take-profit.
	/// </summary>
	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the trailing candle range to move the stop.
	/// </summary>
	public decimal TrailFactor
	{
		get => _trailFactor.Value;
		set => _trailFactor.Value = value;
	}

	/// <summary>
	/// Share of the current equity allocated to risk each time a position is opened.
	/// </summary>
	public decimal RiskFraction
	{
		get => _riskFraction.Value;
		set => _riskFraction.Value = value;
	}

	/// <summary>
	/// Maximum allowed order volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Minimal stop distance expressed in instrument points to emulate MODE_STOPLEVEL.
	/// </summary>
	public decimal MinStopDistancePoints
	{
		get => _minStopDistancePoints.Value;
		set => _minStopDistancePoints.Value = value;
	}

	/// <summary>
	/// Cooldown duration that replicates the 5 second throttle from the EA.
	/// </summary>
	public decimal CooldownSeconds
	{
		get => _cooldownSeconds.Value;
		set => _cooldownSeconds.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security == null)
		yield break;

		yield return (security, SignalCandleType);
		yield return (security, TrailCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_signalHigh = null;
		_signalLow = null;
		_signalClose = null;
		_takeDistance = null;
		_trailDistance = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_bestBid = null;
		_bestAsk = null;
		_maxEquity = 0m;
		_lastActionTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription
		.Bind(ProcessSignalCandle)
		.Start();

		var trailSubscription = SubscribeCandles(TrailCandleType);
		trailSubscription
		.Bind(ProcessTrailCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, signalSubscription);
			if (SignalCandleType != TrailCandleType)
			DrawCandles(area, trailSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessSignalCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_signalHigh = candle.HighPrice;
		_signalLow = candle.LowPrice;
		_signalClose = candle.ClosePrice;

		var range = _signalHigh - _signalLow;
		_takeDistance = range > 0m ? range * TakeFactor : 0m;

		TryEvaluateEntries(GetActionTime(candle.CloseTime));
	}

	private void ProcessTrailCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var range = high - low;
		_trailDistance = range > 0m ? range * TrailFactor : 0m;

		ManagePosition(GetActionTime(candle.CloseTime));
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var changes = level1.Changes;

		if (changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj is decimal bid)
		_bestBid = bid;

		if (changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj is decimal ask)
		_bestAsk = ask;

		UpdateEquitySnapshot();

		var time = GetActionTime(level1.ServerTime);

		ManagePosition(time);
		TryEvaluateEntries(time);
	}

	private void ManagePosition(DateTimeOffset time)
	{
		time = GetActionTime(time);
		var bid = _bestBid;
		var ask = _bestAsk;

		if (Position > 0m && bid is decimal currentBid)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
			return;

			if (_longStop is decimal stop && currentBid <= stop)
			{
				SellMarket(volume);
				ResetPositionTargets();
				_lastActionTime = time;
				return;
			}

			if (_longTake is decimal take && currentBid >= take)
			{
				SellMarket(volume);
				ResetPositionTargets();
				_lastActionTime = time;
				return;
			}

			if (_trailDistance is decimal trail && trail > 0m && _longStop is decimal currentStop)
			{
				if (PositionPrice > 0m && currentBid > PositionPrice && currentBid - currentStop > trail)
				{
					var minOffset = GetMinStopOffset();
					if (_longTake is decimal takeLimit && currentBid >= takeLimit - minOffset)
					return;

					if (!IsCooldownElapsed(time))
					return;

					var desired = NormalizePrice(currentBid - trail);
					if (desired > currentStop)
					{
						_longStop = desired;
						_lastActionTime = time;
					}
				}
			}
		}
		else if (Position < 0m && ask is decimal currentAsk)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
			return;

			if (_shortStop is decimal stop && currentAsk >= stop)
			{
				BuyMarket(volume);
				ResetPositionTargets();
				_lastActionTime = time;
				return;
			}

			if (_shortTake is decimal take && currentAsk <= take)
			{
				BuyMarket(volume);
				ResetPositionTargets();
				_lastActionTime = time;
				return;
			}

			if (_trailDistance is decimal trail && trail > 0m && _shortStop is decimal currentStop)
			{
				if (PositionPrice > 0m && currentAsk < PositionPrice && currentStop - currentAsk > trail)
				{
					var minOffset = GetMinStopOffset();
					if (_shortTake is decimal takeLimit && currentAsk <= takeLimit + minOffset)
					return;

					if (!IsCooldownElapsed(time))
					return;

					var desired = NormalizePrice(currentAsk + trail);
					if (desired < currentStop)
					{
						_shortStop = desired;
						_lastActionTime = time;
					}
				}
			}
		}
		else if (Position == 0m)
		{
			ResetPositionTargets();
		}
	}

	private void TryEvaluateEntries(DateTimeOffset time)
	{
		time = GetActionTime(time);
		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
		return;

		if (Position != 0m)
		return;

		if (!IsCooldownElapsed(time))
		return;

		if (!CanTrade())
		return;

		if (_signalHigh is not decimal high || _signalLow is not decimal low || _signalClose is not decimal close)
		return;

		var mid = (high + low) / 2m;
		var takeDistance = _takeDistance ?? 0m;
		var minOffset = GetMinStopOffset();

		if (close > mid && ask > high)
		{
			var entryPrice = ask;
			var stopPrice = NormalizePrice(low);
			var distance = entryPrice - stopPrice;
			if (distance <= minOffset)
			return;

			var takePrice = takeDistance > 0m ? NormalizePrice(entryPrice + takeDistance) : (decimal?)null;

			var volume = CalculateVolume(distance);
			if (volume <= 0m)
			return;

			BuyMarket(volume);
			_longStop = stopPrice;
			_longTake = takePrice;
			_shortStop = null;
			_shortTake = null;
			_lastActionTime = time;
		}
		else if (close < mid && bid < low)
		{
			var entryPrice = bid;
			var stopPrice = NormalizePrice(high);
			var distance = stopPrice - entryPrice;
			if (distance <= minOffset)
			return;

			var takePrice = takeDistance > 0m ? NormalizePrice(entryPrice - takeDistance) : (decimal?)null;

			var volume = CalculateVolume(distance);
			if (volume <= 0m)
			return;

			SellMarket(volume);
			_shortStop = stopPrice;
			_shortTake = takePrice;
			_longStop = null;
			_longTake = null;
			_lastActionTime = time;
		}
	}


	private DateTimeOffset GetActionTime(DateTimeOffset time)
	{
		return time != default ? time : CurrentTime;
	}
	private decimal CalculateVolume(decimal stopDistance)
	{
		if (stopDistance <= 0m)
		return 0m;

		var portfolio = Portfolio;
		if (portfolio?.CurrentValue is not decimal equity || equity <= 0m)
		return 0m;

		var risk = equity * RiskFraction;
		if (risk <= 0m)
		return 0m;

		var moneyPerUnit = GetMoneyPerUnit(stopDistance);
		if (moneyPerUnit <= 0m)
		return 0m;

		var rawVolume = risk / moneyPerUnit;
		var volume = NormalizeVolume(rawVolume);

		if (MaxVolume > 0m && volume > MaxVolume)
		volume = MaxVolume;

		return volume;
	}

	private decimal GetMoneyPerUnit(decimal stopDistance)
	{
		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m && security.StepPrice is decimal stepPrice && stepPrice > 0m)
		{
			var steps = stopDistance / step;
			return steps * stepPrice;
		}

		return stopDistance;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security?.VolumeStep is decimal step && step > 0m)
		{
			volume = Math.Floor(volume / step) * step;
		}

		if (security?.MinVolume is decimal min && min > 0m && volume < min)
		volume = min;

		if (security?.MaxVolume is decimal max && max > 0m && volume > max)
		volume = max;

		return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
		{
			price = Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
		}

		if (security?.Decimals is int decimals && decimals >= 0)
		price = Math.Round(price, decimals, MidpointRounding.AwayFromZero);

		return price;
	}

	private decimal GetMinStopOffset()
	{
		var minPoints = Math.Max(MinStopDistancePoints, 0m);
		if (minPoints <= 0m)
		return 0m;

		var security = Security;
		if (security?.PriceStep is decimal step && step > 0m)
		return minPoints * step;

		return minPoints;
	}

	private bool IsCooldownElapsed(DateTimeOffset time)
	{
		var cooldown = Math.Max(CooldownSeconds, 0m);
		if (_lastActionTime is DateTimeOffset last && cooldown > 0m)
		{
			var elapsed = (time - last).TotalSeconds;
			return elapsed >= (double)cooldown;
		}

		return true;
	}

	private bool CanTrade()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue is not decimal equity || equity <= 0m)
		return false;

		if (equity > _maxEquity)
		_maxEquity = equity;

		if (_maxEquity > 0m && equity < _maxEquity / 2m)
		return false;

		return true;
	}

	private void UpdateEquitySnapshot()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue is not decimal equity || equity <= 0m)
		return;

		if (equity > _maxEquity)
		_maxEquity = equity;
	}

	private void ResetPositionTargets()
	{
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}
}
