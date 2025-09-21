using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class Twenty200TimeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitLong;
	private readonly StrategyParam<decimal> _stopLossLong;
	private readonly StrategyParam<decimal> _takeProfitShort;
	private readonly StrategyParam<decimal> _stopLossShort;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _lookbackFar;
	private readonly StrategyParam<int> _lookbackNear;
	private readonly StrategyParam<decimal> _longDelta;
	private readonly StrategyParam<decimal> _shortDelta;
	private readonly StrategyParam<int> _maxOpenHours;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<bool> _autoLotEnabled;
	private readonly StrategyParam<decimal> _autoLotFactor;
	private readonly StrategyParam<decimal> _bigLotMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(DateTimeOffset time, decimal price)> _openHistory = new();

	private DateTimeOffset? _currentCandleTime;
	private DateTime? _lastTradeDate;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private bool _isLongPosition;
	private DateTimeOffset? _positionEntryTime;
	private bool _closeRequested;
	private decimal _lastRecordedBalance;
	private bool _balanceInitialized;

	public Twenty200TimeBreakoutStrategy()
	{
		_takeProfitLong = Param(nameof(TakeProfitLong), 39m)
			.SetNotNegative()
			.SetDisplay("Long Take Profit", "Target distance for long trades expressed in pips", "Risk");

		_stopLossLong = Param(nameof(StopLossLong), 147m)
			.SetNotNegative()
			.SetDisplay("Long Stop Loss", "Stop loss distance for long trades expressed in pips", "Risk");

		_takeProfitShort = Param(nameof(TakeProfitShort), 32m)
			.SetNotNegative()
			.SetDisplay("Short Take Profit", "Target distance for short trades expressed in pips", "Risk");

		_stopLossShort = Param(nameof(StopLossShort), 267m)
			.SetNotNegative()
			.SetDisplay("Short Stop Loss", "Stop loss distance for short trades expressed in pips", "Risk");

		_tradeHour = Param(nameof(TradeHour), 18)
			.SetRange(0, 23)
			.SetDisplay("Trade Hour", "Hour of the day (exchange time) when the strategy can enter a position", "Timing");

		_lookbackFar = Param(nameof(LookbackFar), 6)
			.SetGreaterThan(0)
			.SetDisplay("Far Lookback", "Number of bars for the distant open price (Open[t1])", "Signals");

		_lookbackNear = Param(nameof(LookbackNear), 2)
			.SetGreaterThan(0)
			.SetDisplay("Near Lookback", "Number of bars for the closer open price (Open[t2])", "Signals");

		_longDelta = Param(nameof(LongDelta), 6m)
			.SetNotNegative()
			.SetDisplay("Long Delta", "Required pip difference Open[t2] - Open[t1] to enter a long trade", "Signals");

		_shortDelta = Param(nameof(ShortDelta), 21m)
			.SetNotNegative()
			.SetDisplay("Short Delta", "Required pip difference Open[t1] - Open[t2] to enter a short trade", "Signals");

		_maxOpenHours = Param(nameof(MaxOpenHours), 504)
			.SetGreaterThanOrEquals(0)
			.SetDisplay("Max Open Hours", "Maximum lifetime for an open position in hours (0 disables the guard)", "Risk");

		_fixedVolume = Param(nameof(FixedVolume), 0.01m)
			.SetGreaterThan(0m)
			.SetDisplay("Fixed Volume", "Fallback trade volume used when auto lot sizing is disabled", "Money Management");

		_autoLotEnabled = Param(nameof(AutoLotEnabled), true)
			.SetDisplay("Use Auto Lot", "Enable adaptive lot sizing based on portfolio value", "Money Management");

		_autoLotFactor = Param(nameof(AutoLotFactor), 0.000038m)
			.SetNotNegative()
			.SetDisplay("Auto Lot Factor", "Multiplier applied to portfolio value to emulate the MT4 lot table", "Money Management");

		_bigLotMultiplier = Param(nameof(BigLotMultiplier), 6m)
			.SetGreaterThanOrEquals(1m)
			.SetDisplay("Big Lot Multiplier", "Multiplier applied after an equity drop, mimicking the recovery lot", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used to build candles", "General");
	}

	public decimal TakeProfitLong { get => _takeProfitLong.Value; set => _takeProfitLong.Value = value; }
	public decimal StopLossLong { get => _stopLossLong.Value; set => _stopLossLong.Value = value; }
	public decimal TakeProfitShort { get => _takeProfitShort.Value; set => _takeProfitShort.Value = value; }
	public decimal StopLossShort { get => _stopLossShort.Value; set => _stopLossShort.Value = value; }
	public int TradeHour { get => _tradeHour.Value; set => _tradeHour.Value = value; }
	public int LookbackFar { get => _lookbackFar.Value; set => _lookbackFar.Value = value; }
	public int LookbackNear { get => _lookbackNear.Value; set => _lookbackNear.Value = value; }
	public decimal LongDelta { get => _longDelta.Value; set => _longDelta.Value = value; }
	public decimal ShortDelta { get => _shortDelta.Value; set => _shortDelta.Value = value; }
	public int MaxOpenHours { get => _maxOpenHours.Value; set => _maxOpenHours.Value = value; }
	public decimal FixedVolume { get => _fixedVolume.Value; set => _fixedVolume.Value = value; }
	public bool AutoLotEnabled { get => _autoLotEnabled.Value; set => _autoLotEnabled.Value = value; }
	public decimal AutoLotFactor { get => _autoLotFactor.Value; set => _autoLotFactor.Value = value; }
	public decimal BigLotMultiplier { get => _bigLotMultiplier.Value; set => _bigLotMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_openHistory.Clear();
		_currentCandleTime = null;
		_lastTradeDate = null;
		ResetPositionState();
		_positionEntryTime = null;
		_closeRequested = false;
		_lastRecordedBalance = 0m;
		_balanceInitialized = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		var portfolio = Portfolio;
		if (portfolio != null)
		{
			var balance = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
			if (balance > 0m)
			{
				_lastRecordedBalance = balance;
				_balanceInitialized = true;
			}
		}
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order?.Security != Security)
			return;

		var tradeVolume = trade.Trade.Volume;
		if (tradeVolume <= 0m)
			return;

		var direction = trade.Order.Side;
		var signedVolume = direction == Sides.Buy ? tradeVolume : -tradeVolume;
		var previousPosition = Position - signedVolume;

		if (Position > 0m)
		{
			if (direction == Sides.Buy)
			{
				HandlePositionIncrease(trade, previousPosition, true);
			}
			else if (Position == 0m)
			{
				ResetPositionState();
				_closeRequested = false;
			}
		}
		else if (Position < 0m)
		{
			if (direction == Sides.Sell)
			{
				HandlePositionIncrease(trade, previousPosition, false);
			}
			else if (Position == 0m)
			{
				ResetPositionState();
				_closeRequested = false;
			}
		}
		else
		{
			ResetPositionState();
			_closeRequested = false;
		}

		var portfolio = Portfolio;
		if (portfolio != null)
		{
			var balance = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
			if (balance > 0m)
			{
				_lastRecordedBalance = balance;
				_balanceInitialized = true;
			}
		}
	}

	private void HandlePositionIncrease(MyTrade trade, decimal previousPosition, bool isLong)
	{
		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;
		var currentPosition = Math.Abs(Position);
		var previousAbs = Math.Abs(previousPosition);

		if (previousPosition == 0m)
		{
			_entryPrice = price;
			_isLongPosition = isLong;
			_positionEntryTime = trade.Trade.ServerTime;
			UpdateRiskTargets();
			_closeRequested = false;
			return;
		}

		if (_entryPrice is not decimal existing || currentPosition <= 0m)
			return;

		var newAverage = (existing * previousAbs + price * volume) / currentPosition;
		_entryPrice = newAverage;
		_isLongPosition = isLong;
		_positionEntryTime = trade.Trade.ServerTime;
		UpdateRiskTargets();
		_closeRequested = false;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (_currentCandleTime != candle.OpenTime)
		{
			_currentCandleTime = candle.OpenTime;
			_openHistory.Add((candle.OpenTime, candle.OpenPrice));
			var maxHistory = Math.Max(LookbackFar, LookbackNear) + 5;
			if (_openHistory.Count > maxHistory)
				_openHistory.RemoveRange(0, _openHistory.Count - maxHistory);
		}

		if (Position != 0m)
		{
			ManageActivePosition(candle);
		}

		if (candle.State != CandleStates.Active)
			return;

		TryEnterPosition(candle);
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (!_closeRequested && TryCloseByTargets(candle))
			return;

		if (MaxOpenHours <= 0 || _positionEntryTime is null || _closeRequested)
			return;

		var now = candle.OpenTime;
		var lifetime = now - _positionEntryTime.Value;
		var limit = TimeSpan.FromHours(MaxOpenHours);

		if (lifetime >= limit && Position != 0m)
		{
			ClosePosition(candle.ClosePrice);
			_closeRequested = true;
			LogInfo($"Forced exit after {lifetime.TotalHours:F2} hours exceeding limit {MaxOpenHours}.");
		}
	}

	private bool TryCloseByTargets(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
			return false;

		var stopHit = _stopPrice is decimal stop && (_isLongPosition ? candle.LowPrice <= stop : candle.HighPrice >= stop);
		var takeHit = _takeProfitPrice is decimal target && (_isLongPosition ? candle.HighPrice >= target : candle.LowPrice <= target);

		if (!stopHit && !takeHit)
			return false;

		ClosePosition(candle.ClosePrice);
		_closeRequested = true;
		LogInfo(stopHit ? "Stop-loss triggered." : "Take-profit reached.");
		return true;
	}

	private void TryEnterPosition(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return;

		var currentDate = candle.OpenTime.Date;
		if (_lastTradeDate == currentDate)
			return;

		if (candle.OpenTime.Hour != TradeHour)
			return;

		var required = Math.Max(LookbackFar, LookbackNear);
		if (_openHistory.Count <= required)
			return;

		var point = GetPointSize();
		if (point <= 0m)
			return;

		var farOpen = GetOpenPrice(LookbackFar);
		var nearOpen = GetOpenPrice(LookbackNear);
		var shortThreshold = ShortDelta * point;
		var longThreshold = LongDelta * point;

		var shortSignal = ShortDelta > 0m && farOpen - nearOpen > shortThreshold;
		var longSignal = LongDelta > 0m && nearOpen - farOpen > longThreshold;

		if (!shortSignal && !longSignal)
			return;

		var volume = CalculateTradeVolume();
		if (volume <= 0m)
			return;

		if (shortSignal)
		{
			SellMarket(volume);
			_lastTradeDate = currentDate;
			LogInfo($"Enter short at {candle.OpenTime:yyyy-MM-dd HH:mm}: farOpen={farOpen}, nearOpen={nearOpen}, volume={volume}");
		}
		else if (longSignal)
		{
			BuyMarket(volume);
			_lastTradeDate = currentDate;
			LogInfo($"Enter long at {candle.OpenTime:yyyy-MM-dd HH:mm}: farOpen={farOpen}, nearOpen={nearOpen}, volume={volume}");
		}
	}

	private decimal CalculateTradeVolume()
	{
		var volume = FixedVolume;
		var portfolio = Portfolio;
		var balance = 0m;

		if (portfolio != null)
		{
			balance = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
			if (AutoLotEnabled && balance > 0m)
			{
				var autoVolume = Math.Round(balance * AutoLotFactor, 2, MidpointRounding.AwayFromZero);
				if (autoVolume > 0m)
					volume = autoVolume;
			}

			if (_balanceInitialized && _lastRecordedBalance > balance && BigLotMultiplier > 1m)
			{
				volume *= BigLotMultiplier;
			}
		}

		if (volume <= 0m)
			volume = FixedVolume;

		volume = AdjustVolume(volume);

		if (balance > 0m)
		{
			_lastRecordedBalance = balance;
			_balanceInitialized = true;
		}

		return volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security == null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			if (steps <= 0m)
				volume = step;
			else
				volume = steps * step;
		}

		var minVolume = Security.MinVolume;
		if (minVolume is decimal min && min > 0m && volume < min)
			volume = min;

		var maxVolume = Security.MaxVolume;
		if (maxVolume is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}

	private decimal GetOpenPrice(int lookback)
	{
		var index = _openHistory.Count - 1 - lookback;
		return index >= 0 ? _openHistory[index].price : 0m;
	}

	private decimal GetPointSize()
	{
		var step = Security?.PriceStep;
		if (step is null || step == 0m)
			return 0.0001m;
		return step.Value;
	}

	private void UpdateRiskTargets()
	{
		if (_entryPrice is not decimal entry)
			return;

		var point = GetPointSize();
		if (point <= 0m)
		{
			_stopPrice = null;
			_takeProfitPrice = null;
			return;
		}

		if (_isLongPosition)
		{
			_stopPrice = StopLossLong > 0m ? entry - StopLossLong * point : (decimal?)null;
			_takeProfitPrice = TakeProfitLong > 0m ? entry + TakeProfitLong * point : (decimal?)null;
		}
		else
		{
			_stopPrice = StopLossShort > 0m ? entry + StopLossShort * point : (decimal?)null;
			_takeProfitPrice = TakeProfitShort > 0m ? entry - TakeProfitShort * point : (decimal?)null;
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_isLongPosition = false;
		_positionEntryTime = null;
	}
}
