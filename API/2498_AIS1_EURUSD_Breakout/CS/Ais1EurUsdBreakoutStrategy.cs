using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy converted from the AIS1 expert advisor.
/// Tracks previous day levels, applies a risk based position size and manages trailing exits.
/// </summary>
public class Ais1EurUsdBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _accountReserve;
	private readonly StrategyParam<decimal> _orderReserve;
	private readonly StrategyParam<decimal> _takeFactor;
	private readonly StrategyParam<decimal> _stopFactor;
	private readonly StrategyParam<decimal> _trailFactor;
	private readonly StrategyParam<DataType> _entryCandleType;
	private readonly StrategyParam<DataType> _trailCandleType;

	private decimal _prevDayHigh;
	private decimal _prevDayLow;
	private decimal _prevDayClose;
	private decimal _prevTrailRange;
	private bool _hasPrevDay;
	private bool _hasPrevTrail;
	private decimal _entryPrice;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;
	private decimal _longTrail;
	private decimal _shortTrail;
	private decimal _maxEquity;
	private DateTimeOffset _nextActionTime;

	private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);

	public decimal AccountReserve
	{
		get => _accountReserve.Value;
		set => _accountReserve.Value = value;
	}

	public decimal OrderReserve
	{
		get => _orderReserve.Value;
		set => _orderReserve.Value = value;
	}

	public decimal TakeFactor
	{
		get => _takeFactor.Value;
		set => _takeFactor.Value = value;
	}

	public decimal StopFactor
	{
		get => _stopFactor.Value;
		set => _stopFactor.Value = value;
	}

	public decimal TrailFactor
	{
		get => _trailFactor.Value;
		set => _trailFactor.Value = value;
	}

	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	public DataType TrailCandleType
	{
		get => _trailCandleType.Value;
		set => _trailCandleType.Value = value;
	}

	public Ais1EurUsdBreakoutStrategy()
	{
		_accountReserve = Param(nameof(AccountReserve), 0.2m)
			.SetDisplay("Account Reserve", "Equity share kept outside of trading", "Risk")
			.SetCanOptimize();

		_orderReserve = Param(nameof(OrderReserve), 0.04m)
			.SetDisplay("Order Reserve", "Equity share risked per trade", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_takeFactor = Param(nameof(TakeFactor), 0.8m)
			.SetDisplay("Take Factor", "Daily range multiplier for take profit", "Targets")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_stopFactor = Param(nameof(StopFactor), 1m)
			.SetDisplay("Stop Factor", "Daily range multiplier for stop loss", "Targets")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_trailFactor = Param(nameof(TrailFactor), 5m)
			.SetDisplay("Trail Factor", "Intraday range multiplier for trailing", "Targets")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Entry Candle", "Primary timeframe for breakout levels", "Data");

		_trailCandleType = Param(nameof(TrailCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Trail Candle", "Secondary timeframe for trailing", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, EntryCandleType);

		if (TrailCandleType != EntryCandleType)
			yield return (Security, TrailCandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetPositionState();
		_prevDayHigh = 0m;
		_prevDayLow = 0m;
		_prevDayClose = 0m;
		_prevTrailRange = 0m;
		_hasPrevDay = false;
		_hasPrevTrail = false;
		_maxEquity = 0m;
		_nextActionTime = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetPositionState();
		_maxEquity = GetEquity();
		_nextActionTime = DateTimeOffset.MinValue;

		var dailySubscription = SubscribeCandles(EntryCandleType);
		dailySubscription.Bind(ProcessDailyCandle).Start();

		var intradaySubscription = SubscribeCandles(TrailCandleType);
		intradaySubscription.Bind(ProcessIntradayCandle).Start();
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest completed day to use as breakout reference on the next session.
		_prevDayHigh = candle.HighPrice;
		_prevDayLow = candle.LowPrice;
		_prevDayClose = candle.ClosePrice;
		_hasPrevDay = true;
	}

	private void ProcessIntradayCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Respect the original EA cooldown before issuing another order modification.
		if (candle.CloseTime <= _nextActionTime)
		{
			UpdateTrailRange(candle);
			return;
		}

		var equity = GetEquity();
		UpdateMaxEquity(equity);

		if (IsDrawdownBreached(equity))
		{
			UpdateTrailRange(candle);
			return;
		}

		if (!_hasPrevDay)
		{
			UpdateTrailRange(candle);
			return;
		}

		var dayRange = _prevDayHigh - _prevDayLow;
		if (dayRange <= 0m)
		{
			UpdateTrailRange(candle);
			return;
		}

		var average = (_prevDayHigh + _prevDayLow) / 2m;
		var takeDistance = dayRange * TakeFactor;
		var stopDistance = dayRange * StopFactor;

		var trailRange = _hasPrevTrail ? _prevTrailRange : candle.HighPrice - candle.LowPrice;
		var trailDistance = trailRange * TrailFactor;

		if (Position != 0m)
		{
			HandleExistingPosition(candle, trailDistance);
			UpdateTrailRange(candle);
			return;
		}

		TryEnterPosition(candle, average, stopDistance, takeDistance);
		UpdateTrailRange(candle);
	}

	private void HandleExistingPosition(ICandleMessage candle, decimal trailDistance)
	{
		if (Position > 0m)
		{
			var exitVolume = Math.Abs(Position);

			// Respect take profit first so gains are locked immediately.
			if (_longTake > 0m && candle.HighPrice >= _longTake)
			{
				SellMarket(exitVolume);
				ResetAfterExit(candle.CloseTime);
				return;
			}

			var trailingStop = _longStop;

			// Update trailing stop only after the trade moves into profit.
			if (trailDistance > 0m && candle.ClosePrice > _entryPrice)
			{
				var candidate = candle.ClosePrice - trailDistance;
				if (_longTrail == 0m || candidate > _longTrail)
					_longTrail = candidate;
			}

			if (_longTrail > 0m)
				trailingStop = trailingStop > 0m ? Math.Max(trailingStop, _longTrail) : _longTrail;

			if (trailingStop > 0m && candle.LowPrice <= trailingStop)
			{
				SellMarket(exitVolume);
				ResetAfterExit(candle.CloseTime);
			}
		}
		else if (Position < 0m)
		{
			var exitVolume = Math.Abs(Position);

			if (_shortTake > 0m && candle.LowPrice <= _shortTake)
			{
				BuyMarket(exitVolume);
				ResetAfterExit(candle.CloseTime);
				return;
			}

			var trailingStop = _shortStop;

			if (trailDistance > 0m && candle.ClosePrice < _entryPrice)
			{
				var candidate = candle.ClosePrice + trailDistance;
				if (_shortTrail == 0m || candidate < _shortTrail)
					_shortTrail = candidate;
			}

			if (_shortTrail > 0m)
				trailingStop = trailingStop > 0m ? Math.Min(trailingStop, _shortTrail) : _shortTrail;

			if (trailingStop > 0m && candle.HighPrice >= trailingStop)
			{
				BuyMarket(exitVolume);
				ResetAfterExit(candle.CloseTime);
			}
		}
	}

	private void TryEnterPosition(ICandleMessage candle, decimal average, decimal stopDistance, decimal takeDistance)
	{
		var breakoutUp = _prevDayClose > average && candle.HighPrice > _prevDayHigh;
		var breakoutDown = _prevDayClose < average && candle.LowPrice < _prevDayLow;

		if (breakoutUp)
		{
			var entryPrice = candle.ClosePrice;
			var stopPrice = _prevDayHigh - stopDistance;
			var risk = entryPrice - stopPrice;
			if (risk <= 0m)
				return;

			var volume = CalculatePositionSize(risk);
			if (volume <= 0m)
				return;

			BuyMarket(volume);

			_entryPrice = entryPrice;
			_longStop = stopPrice;
			_longTake = entryPrice + takeDistance;
			_longTrail = 0m;
			_shortStop = 0m;
			_shortTake = 0m;
			_shortTrail = 0m;
			_nextActionTime = candle.CloseTime + Cooldown;
		}
		else if (breakoutDown)
		{
			var entryPrice = candle.ClosePrice;
			var stopPrice = _prevDayLow + stopDistance;
			var risk = stopPrice - entryPrice;
			if (risk <= 0m)
				return;

			var volume = CalculatePositionSize(risk);
			if (volume <= 0m)
				return;

			SellMarket(volume);

			_entryPrice = entryPrice;
			_shortStop = stopPrice;
			_shortTake = entryPrice - takeDistance;
			_shortTrail = 0m;
			_longStop = 0m;
			_longTake = 0m;
			_longTrail = 0m;
			_nextActionTime = candle.CloseTime + Cooldown;
		}
	}

	private decimal CalculatePositionSize(decimal riskPerUnit)
	{
		if (riskPerUnit <= 0m)
			return 0m;

		var equity = GetEquity();
		if (equity <= 0m)
			return 0m;

		var maxRisk = equity * OrderReserve;
		if (maxRisk <= 0m)
			return 0m;

		var rawSize = maxRisk / riskPerUnit;
		if (rawSize <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 1m;
		var minVolume = Security?.MinVolume ?? step;
		var maxVolume = Security?.MaxVolume ?? Math.Max(minVolume, step * 1000m);

		var steps = Math.Floor(rawSize / step);
		var volume = steps * step;

		if (volume < minVolume)
		{
			if (rawSize >= minVolume)
				volume = minVolume;
			else
				return 0m;
		}

		if (volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private void UpdateTrailRange(ICandleMessage candle)
	{
		_prevTrailRange = candle.HighPrice - candle.LowPrice;
		_hasPrevTrail = true;
	}

	private void ResetAfterExit(DateTimeOffset time)
	{
		ResetPositionState();
		_nextActionTime = time + Cooldown;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
		_longTrail = 0m;
		_shortTrail = 0m;
	}

	private void UpdateMaxEquity(decimal equity)
	{
		if (equity > _maxEquity)
			_maxEquity = equity;
	}

	private bool IsDrawdownBreached(decimal equity)
	{
		if (_maxEquity <= 0m)
			return false;

		var drawdownLimit = AccountReserve - OrderReserve;
		if (drawdownLimit <= 0m)
			return false;

		var threshold = _maxEquity * (1m - drawdownLimit);
		return equity < threshold;
	}

	private decimal GetEquity()
	{
		return Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
	}
}
