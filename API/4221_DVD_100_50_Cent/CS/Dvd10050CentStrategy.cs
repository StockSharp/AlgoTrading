using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion limit strategy converted from the MT4 expert advisor "DVD 100-50 cent".
/// </summary>
public class Dvd10050CentStrategy : Strategy
{
	private const int M1HistoryLength = 64;
	private const int M30HistoryLength = 16;
	private const int H1HistoryLength = 16;

	private readonly StrategyParam<bool> _accountIsMini;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _tradeSizePercent;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _pointFromLevelGoPips;
	private readonly StrategyParam<decimal> _riseFilterPips;
	private readonly StrategyParam<decimal> _highLevelPips;
	private readonly StrategyParam<decimal> _lowLevelPips;
	private readonly StrategyParam<decimal> _lowLevel2Pips;
	private readonly StrategyParam<decimal> _marginCutoff;
	private readonly StrategyParam<int> _orderExpiryMinutes;

	private SimpleMovingAverage _h1Fast = null!;
	private SimpleMovingAverage _h1Slow = null!;
	private SimpleMovingAverage _d1Fast = null!;
	private SimpleMovingAverage _d1Slow = null!;

	private readonly List<ICandleMessage> _m1History = new();
	private readonly List<ICandleMessage> _m30History = new();
	private readonly List<ICandleMessage> _h1Finished = new();
	private ICandleMessage _h1Current;

	private decimal? _raviH1;
	private decimal? _raviD1Current;
	private decimal? _raviD1Prev1;
	private decimal? _raviD1Prev2;
	private decimal? _raviD1Prev3;

	private decimal _pipSize;
	private decimal _pointValue;

	private DateTimeOffset? _buyOrderExpiry;
	private DateTimeOffset? _sellOrderExpiry;

	private decimal? _pendingBuyStop;
	private decimal? _pendingBuyTake;
	private decimal? _pendingSellStop;
	private decimal? _pendingSellTake;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private decimal _previousPosition;

	/// <summary>
	/// Initializes a new instance of the <see cref="Dvd10050CentStrategy"/> class.
	/// </summary>
	public Dvd10050CentStrategy()
	{
		_accountIsMini = Param(nameof(AccountIsMini), true)
		.SetDisplay("Mini Account", "Use mini account position sizing", "Risk");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), true)
		.SetDisplay("Use Money Management", "Enable adaptive lot sizing", "Risk");

		_tradeSizePercent = Param(nameof(TradeSizePercent), 10m)
		.SetDisplay("Risk Percent", "Percent of equity allocated per trade", "Risk")
		.SetRange(0m, 100m)
		.SetCanOptimize(true);

		_fixedVolume = Param(nameof(FixedVolume), 0.01m)
		.SetDisplay("Fixed Volume", "Volume used when money management is disabled", "Risk")
		.SetRange(0.01m, 100m)
		.SetCanOptimize(true);

		_maxVolume = Param(nameof(MaxVolume), 4m)
		.SetDisplay("Max Volume", "Ceiling for calculated trade volume", "Risk")
		.SetRange(0.01m, 100m)
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 210m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Orders")
		.SetRange(0m, 1000m)
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 18m)
		.SetDisplay("Take Profit (pips)", "Initial profit target distance", "Orders")
		.SetRange(0m, 500m)
		.SetCanOptimize(true);

		_pointFromLevelGoPips = Param(nameof(PointFromLevelGoPips), 50m)
		.SetDisplay("Base Offset (0.1 pips)", "Offset used to build the 100 level grid", "Filters")
		.SetRange(0m, 1000m)
		.SetCanOptimize(true);

		_riseFilterPips = Param(nameof(RiseFilterPips), 700m)
		.SetDisplay("Rise Filter (0.1 pips)", "Distance for hourly spike confirmation", "Filters")
		.SetRange(0m, 5000m)
		.SetCanOptimize(true);

		_highLevelPips = Param(nameof(HighLevelPips), 600m)
		.SetDisplay("High Level (0.1 pips)", "One-minute spike rejection threshold", "Filters")
		.SetRange(0m, 5000m)
		.SetCanOptimize(true);

		_lowLevelPips = Param(nameof(LowLevelPips), 250m)
		.SetDisplay("Low Level (0.1 pips)", "Half-hour consolidation ceiling", "Filters")
		.SetRange(0m, 5000m)
		.SetCanOptimize(true);

		_lowLevel2Pips = Param(nameof(LowLevel2Pips), 450m)
		.SetDisplay("Low Level 2 (0.1 pips)", "Hourly breakout confirmation threshold", "Filters")
		.SetRange(0m, 5000m)
		.SetCanOptimize(true);

		_marginCutoff = Param(nameof(MarginCutoff), 300m)
		.SetDisplay("Margin Cutoff", "Stop trading when equity falls below this level", "Risk")
		.SetRange(0m, 1_000_000m)
		.SetCanOptimize(true);

		_orderExpiryMinutes = Param(nameof(OrderExpiryMinutes), 20)
		.SetDisplay("Order Expiry (minutes)", "Lifetime of pending limit orders", "Orders")
		.SetRange(1, 240)
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Gets or sets whether the account uses mini lot sizing.
	/// </summary>
	public bool AccountIsMini
	{
		get => _accountIsMini.Value;
		set => _accountIsMini.Value = value;
	}

	/// <summary>
	/// Gets or sets whether money management is enabled.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Gets or sets the risk allocation per trade when money management is enabled.
	/// </summary>
	public decimal TradeSizePercent
	{
		get => _tradeSizePercent.Value;
		set => _tradeSizePercent.Value = value;
	}

	/// <summary>
	/// Gets or sets the fixed trade volume used when money management is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the maximum volume allowed per trade.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the base offset that defines the 100 level grid in 0.1 pip units.
	/// </summary>
	public decimal PointFromLevelGoPips
	{
		get => _pointFromLevelGoPips.Value;
		set => _pointFromLevelGoPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the spike confirmation distance for hourly candles in 0.1 pip units.
	/// </summary>
	public decimal RiseFilterPips
	{
		get => _riseFilterPips.Value;
		set => _riseFilterPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the rejection distance for one-minute highs in 0.1 pip units.
	/// </summary>
	public decimal HighLevelPips
	{
		get => _highLevelPips.Value;
		set => _highLevelPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the consolidation ceiling distance for 30-minute highs in 0.1 pip units.
	/// </summary>
	public decimal LowLevelPips
	{
		get => _lowLevelPips.Value;
		set => _lowLevelPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the hourly breakout confirmation distance in 0.1 pip units.
	/// </summary>
	public decimal LowLevel2Pips
	{
		get => _lowLevel2Pips.Value;
		set => _lowLevel2Pips.Value = value;
	}

	/// <summary>
	/// Gets or sets the equity level that disables new trades when reached.
	/// </summary>
	public decimal MarginCutoff
	{
		get => _marginCutoff.Value;
		set => _marginCutoff.Value = value;
	}

	/// <summary>
	/// Gets or sets the pending order lifetime in minutes.
	/// </summary>
	public int OrderExpiryMinutes
	{
		get => _orderExpiryMinutes.Value;
		set => _orderExpiryMinutes.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

	_h1Fast = new SimpleMovingAverage { Length = 2 };
	_h1Slow = new SimpleMovingAverage { Length = 24 };
	_d1Fast = new SimpleMovingAverage { Length = 2 };
	_d1Slow = new SimpleMovingAverage { Length = 24 };

		_pipSize = CalculatePipSize();
		_pointValue = _pipSize / 10m;

		var m1Subscription = SubscribeCandles(TimeSpan.FromMinutes(1).TimeFrame());
		m1Subscription.Bind(ProcessM1).Start();

		var m30Subscription = SubscribeCandles(TimeSpan.FromMinutes(30).TimeFrame());
		m30Subscription.Bind(ProcessM30).Start();

		var h1Subscription = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
		h1Subscription.Bind(ProcessH1).Start();

		var d1Subscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		d1Subscription.Bind(ProcessD1).Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_m1History.Clear();
		_m30History.Clear();
		_h1Finished.Clear();
		_h1Current = null;

		_raviH1 = null;
		_raviD1Current = null;
		_raviD1Prev1 = null;
		_raviD1Prev2 = null;
		_raviD1Prev3 = null;

		_buyOrderExpiry = null;
		_sellOrderExpiry = null;
		_pendingBuyStop = null;
		_pendingBuyTake = null;
		_pendingSellStop = null;
		_pendingSellTake = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_previousPosition = 0m;
	}

	private void ProcessM1(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_m1History.Add(candle);
		TrimHistory(_m1History, M1HistoryLength);

		HandlePositionState(candle);
		ManageOrderExpirations(candle.CloseTime);
		ManageActivePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (HasExposure())
		return;

		if (!HasSufficientMargin())
		return;

		var orderPlaced = false;

		if (TryCalculateBuyScore(candle, out var buyLevel, out var buyScore) && buyScore >= 50m)
		{
			orderPlaced = PlaceBuyLimit(candle);
		}

		if (!orderPlaced && TryCalculateSellScore(candle, out var sellLevel, out var sellScore) && sellScore >= 50m)
		{
			PlaceSellLimit(candle);
		}
	}

	private void ProcessM30(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_m30History.Add(candle);
		TrimHistory(_m30History, M30HistoryLength);
	}

	private void ProcessH1(ICandleMessage candle)
	{
		_h1Current = candle;

		if (candle.State != CandleStates.Finished)
		return;

		_h1Finished.Add(candle);
		TrimHistory(_h1Finished, H1HistoryLength);

		var fastValue = _h1Fast.Process(candle.OpenPrice);
		var slowValue = _h1Slow.Process(candle.OpenPrice);

		if (!fastValue.IsFinal || !slowValue.IsFinal)
		return;

		var slow = slowValue.GetValue<decimal>();
		if (slow == 0m)
		return;

		var fast = fastValue.GetValue<decimal>();
		_raviH1 = 100m * (fast - slow) / slow;
	}

	private void ProcessD1(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var fastValue = _d1Fast.Process(candle.OpenPrice);
		var slowValue = _d1Slow.Process(candle.OpenPrice);

		if (!fastValue.IsFinal || !slowValue.IsFinal)
		return;

		var slow = slowValue.GetValue<decimal>();
		if (slow == 0m)
		return;

		var fast = fastValue.GetValue<decimal>();
		var ravi = 100m * (fast - slow) / slow;

		_raviD1Prev3 = _raviD1Prev2;
		_raviD1Prev2 = _raviD1Prev1;
		_raviD1Prev1 = _raviD1Current;
		_raviD1Current = ravi;
	}

	private void HandlePositionState(ICandleMessage candle)
	{
		var currentPosition = Position;

		if (currentPosition > 0m && _previousPosition <= 0m)
		{
			_longStop = _pendingBuyStop;
			_longTake = _pendingBuyTake;
			_pendingBuyStop = null;
			_pendingBuyTake = null;
			_buyOrderExpiry = null;
		}
		else if (currentPosition < 0m && _previousPosition >= 0m)
		{
			_shortStop = _pendingSellStop;
			_shortTake = _pendingSellTake;
			_pendingSellStop = null;
			_pendingSellTake = null;
			_sellOrderExpiry = null;
		}
		else if (currentPosition == 0m && _previousPosition != 0m)
		{
			ResetTradeLevels();
		}

		_previousPosition = currentPosition;
	}

	private void ManageOrderExpirations(DateTimeOffset currentTime)
	{
		if (_buyOrderExpiry is DateTimeOffset buyExpiry)
		{
			if (!HasActiveLimitOrder(Sides.Buy))
			{
				_buyOrderExpiry = null;
			}
			else if (currentTime >= buyExpiry)
			{
				CancelSideOrders(Sides.Buy);
				_buyOrderExpiry = null;
				_pendingBuyStop = null;
				_pendingBuyTake = null;
			}
		}

		if (_sellOrderExpiry is DateTimeOffset sellExpiry)
		{
			if (!HasActiveLimitOrder(Sides.Sell))
			{
				_sellOrderExpiry = null;
			}
			else if (currentTime >= sellExpiry)
			{
				CancelSideOrders(Sides.Sell);
				_sellOrderExpiry = null;
				_pendingSellStop = null;
				_pendingSellTake = null;
			}
		}
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeLevels();
				return;
			}

			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeLevels();
			}
		}
		else if (Position < 0m)
		{
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeLevels();
				return;
			}

			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeLevels();
			}
		}
	}

	private bool TryCalculateBuyScore(ICandleMessage candle, out decimal level100, out decimal score)
	{
		score = 0m;
		level100 = 0m;

		if (_raviH1 is not decimal raviH1 || _raviD1Current is not decimal raviD1)
		return false;

		var previousM1 = GetM1Candle(1);
		var h1Low0 = GetH1Low(0);
		var h1Low1 = GetH1Low(1);
		var h1Low2 = GetH1Low(2);
		var h1High1 = GetH1High(1);
		var h1High2 = GetH1High(2);

		if (previousM1 is null || h1Low0 is null || h1Low1 is null || h1Low2 is null || h1High1 is null || h1High2 is null)
		return false;

		level100 = Math.Round(candle.ClosePrice, 2, MidpointRounding.AwayFromZero) + PointFromLevelGoPips * _pointValue;
		var riseThreshold = level100 + RiseFilterPips * _pointValue;
		var baseLow = level100 - PointFromLevelGoPips * _pointValue;
		var tolerance = 30m * _pointValue;

		if (raviH1 < 0m)
		score += 10m;

		if (h1High1 > riseThreshold || h1High2 > riseThreshold)
		score += 7m;

		if (candle.ClosePrice < level100 && previousM1.ClosePrice > level100 &&
		h1Low0.Value > baseLow + tolerance && h1Low1.Value > baseLow + tolerance && h1Low2.Value > baseLow)
		{
			score += 45m;
		}

		if (CheckM1HighAbove(level100 + HighLevelPips * _pointValue, 12))
		score -= 50m;

		if (raviD1 < -2m && CheckM1ImpulseForBuy())
		score -= 50m;

		if (!CheckH1BreakAbove(level100 + LowLevel2Pips * _pointValue))
		score -= 50m;

		if (CheckM30CompressionAbove(level100 + LowLevelPips * _pointValue))
		score -= 50m;

		return true;
	}

	private bool TryCalculateSellScore(ICandleMessage candle, out decimal level100, out decimal score)
	{
		score = 0m;
		level100 = 0m;

		if (_raviH1 is not decimal raviH1 || _raviD1Current is not decimal raviD1)
		return false;

		var previousM1 = GetM1Candle(1);
		var h1High0 = GetH1High(0);
		var h1High1 = GetH1High(1);
		var h1High2 = GetH1High(2);
		var h1Low1 = GetH1Low(1);
		var h1Low2 = GetH1Low(2);

		if (previousM1 is null || h1High0 is null || h1High1 is null || h1High2 is null || h1Low1 is null || h1Low2 is null)
		return false;

		level100 = Math.Round(candle.ClosePrice, 2, MidpointRounding.AwayFromZero) - PointFromLevelGoPips * _pointValue;
		var fallThreshold = level100 - RiseFilterPips * _pointValue;
		var baseHigh = level100 + PointFromLevelGoPips * _pointValue;
		var tolerance = 30m * _pointValue;

		if (raviH1 > 0m)
		score += 10m;

		if (h1Low1 < fallThreshold || h1Low2 < fallThreshold)
		score += 7m;

		if (candle.ClosePrice > level100 && previousM1.ClosePrice < level100 &&
		h1High0.Value < baseHigh - tolerance && h1High1.Value < baseHigh - tolerance && h1High2.Value < baseHigh)
		{
			score += 45m;
		}

		if (CheckM1LowBelow(level100 - HighLevelPips * _pointValue, 12))
		score -= 50m;

		if (raviD1 > 2m && CheckM1ImpulseForSell())
		score -= 50m;

		if (!CheckH1BreakBelow(level100 - LowLevel2Pips * _pointValue))
		score -= 50m;

		if (CheckM30CompressionBelow(level100 - LowLevelPips * _pointValue))
		score -= 50m;

		return true;
	}

	private bool PlaceBuyLimit(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		return false;

		var entryPrice = Math.Max(candle.ClosePrice - 10m * _pipSize, 0m);
		var stopPrice = entryPrice - StopLossPips * _pipSize;
		var takePrice = entryPrice + TakeProfitPips * _pipSize;

		if (_raviD1Current is decimal ravi && _raviD1Prev1 is decimal prev1 && _raviD1Prev2 is decimal prev2 && _raviD1Prev3 is decimal prev3)
		{
			if (ravi > 1m && ravi < 5m && prev1 < ravi && prev2 < prev1 && prev3 < prev2)
			{
				takePrice += 25m * _pipSize;
			}
		}

		BuyLimit(price: entryPrice, volume: volume);
		_buyOrderExpiry = candle.CloseTime + TimeSpan.FromMinutes(OrderExpiryMinutes);
		_pendingBuyStop = stopPrice;
		_pendingBuyTake = takePrice;
		return true;
	}

	private bool PlaceSellLimit(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		return false;

		var entryPrice = candle.ClosePrice + 7m * _pipSize;
		var stopPrice = entryPrice + StopLossPips * _pipSize;
		var takePrice = entryPrice - TakeProfitPips * _pipSize;

		if (_raviD1Current is decimal ravi && _raviD1Prev1 is decimal prev1 && _raviD1Prev2 is decimal prev2 && _raviD1Prev3 is decimal prev3)
		{
			if (ravi < -1m && ravi > -5m && prev1 > ravi && prev2 > prev1 && prev3 > prev2)
			{
				takePrice -= 25m * _pipSize;
			}
		}

		SellLimit(price: entryPrice, volume: volume);
		_sellOrderExpiry = candle.CloseTime + TimeSpan.FromMinutes(OrderExpiryMinutes);
		_pendingSellStop = stopPrice;
		_pendingSellTake = takePrice;
		return true;
	}

	private bool CheckM1HighAbove(decimal threshold, int candles)
	{
		for (var i = 0; i < candles; i++)
		{
			var candle = GetM1Candle(i);
			if (candle is null)
			break;

			if (candle.HighPrice > threshold)
			return true;
		}

		return false;
	}

	private bool CheckM1LowBelow(decimal threshold, int candles)
	{
		for (var i = 0; i < candles; i++)
		{
			var candle = GetM1Candle(i);
			if (candle is null)
			break;

			if (candle.LowPrice < threshold)
			return true;
		}

		return false;
	}

	private bool CheckM1ImpulseForBuy()
	{
		for (var shift = 0; shift <= 30; shift++)
		{
			var current = GetM1Candle(shift);
			var future = GetM1Candle(shift + 3);

			if (current is null || future is null)
			break;

			if (future.HighPrice - current.LowPrice > 300m * _pointValue && future.OpenPrice > current.ClosePrice)
			return true;
		}

		return false;
	}

	private bool CheckM1ImpulseForSell()
	{
		for (var shift = 0; shift <= 30; shift++)
		{
			var current = GetM1Candle(shift);
			var future = GetM1Candle(shift + 3);

			if (current is null || future is null)
			break;

			if (current.HighPrice - future.LowPrice > 300m * _pointValue && current.ClosePrice > future.OpenPrice)
			return true;
		}

		return false;
	}

	private bool CheckH1BreakAbove(decimal threshold)
	{
		for (var shift = 0; shift <= 14; shift++)
		{
			var high = GetH1High(shift);
			if (high is null)
			break;

			if (high.Value > threshold)
			return true;
		}

		return false;
	}

	private bool CheckH1BreakBelow(decimal threshold)
	{
		for (var shift = 0; shift <= 14; shift++)
		{
			var low = GetH1Low(shift);
			if (low is null)
			break;

			if (low.Value < threshold)
			return true;
		}

		return false;
	}

	private bool CheckM30CompressionAbove(decimal threshold)
	{
		for (var shift = 0; shift <= 7; shift++)
		{
			var candle = GetM30Candle(shift);
			if (candle is null)
			return false;

			if (candle.HighPrice >= threshold)
			return false;
		}

		return true;
	}

	private bool CheckM30CompressionBelow(decimal threshold)
	{
		for (var shift = 0; shift <= 7; shift++)
		{
			var candle = GetM30Candle(shift);
			if (candle is null)
			return false;

			if (candle.LowPrice <= threshold)
			return false;
		}

		return true;
	}

	private ICandleMessage GetM1Candle(int shift)
	{
		var index = _m1History.Count - 1 - shift;
		return index >= 0 && index < _m1History.Count ? _m1History[index] : null;
	}

	private ICandleMessage GetM30Candle(int shift)
	{
		var index = _m30History.Count - 1 - shift;
		return index >= 0 && index < _m30History.Count ? _m30History[index] : null;
	}

	private decimal? GetH1High(int shift)
	{
		if (shift == 0)
		return _h1Current?.HighPrice;

		var index = _h1Finished.Count - shift;
		return index >= 0 && index < _h1Finished.Count ? _h1Finished[index].HighPrice : null;
	}

	private decimal? GetH1Low(int shift)
	{
		if (shift == 0)
		return _h1Current?.LowPrice;

		var index = _h1Finished.Count - shift;
		return index >= 0 && index < _h1Finished.Count ? _h1Finished[index].LowPrice : null;
	}

	private void CancelSideOrders(Sides side)
	{
		foreach (var order in ActiveOrders)
		{
			if (order.Type != OrderTypes.Limit || order.Direction != side)
			continue;

			CancelOrder(order);
		}
	}

	private bool HasActiveLimitOrder(Sides side)
	{
		foreach (var order in ActiveOrders)
		{
			if (order.Type == OrderTypes.Limit && order.Direction == side)
			return true;
		}

		return false;
	}

	private bool HasExposure()
	{
		if (Position != 0m)
		return true;

		foreach (var order in ActiveOrders)
		{
			if (order.Type == OrderTypes.Limit)
			return true;
		}

		return false;
	}

	private bool HasSufficientMargin()
	{
		if (MarginCutoff <= 0m)
		return true;

		var portfolio = Portfolio;
		if (portfolio is null)
		return true;

		var equity = portfolio.CurrentValue;
		if (equity <= 0m)
		equity = portfolio.BeginValue;

		return equity >= MarginCutoff;
	}

	private decimal CalculateOrderVolume()
	{
		if (!UseMoneyManagement)
		return FixedVolume;

		var portfolio = Portfolio;
		if (portfolio is null)
		return FixedVolume;

		var equity = portfolio.CurrentValue;
		if (equity <= 0m)
		equity = portfolio.BeginValue;

		if (equity <= 0m)
		return FixedVolume;

		var lot = Math.Floor(equity * TradeSizePercent / 1000m) / 100m;

		if (AccountIsMini)
		{
			lot = Math.Floor(lot * 100m) / 100m;
			if (lot < 0.1m)
			lot = 0.1m;
		}
		else
		{
			if (lot < 1m)
			lot = 1m;
		}

		if (lot > MaxVolume)
		lot = MaxVolume;

		return lot;
	}

	private void ResetTradeLevels()
	{
		_pendingBuyStop = null;
		_pendingBuyTake = null;
		_pendingSellStop = null;
		_pendingSellTake = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	private static void TrimHistory(ICollection<ICandleMessage> history, int maxCount)
	{
		while (history.Count > maxCount)
		{
			if (history is List<ICandleMessage> list)
			list.RemoveAt(0);
			else
			break;
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 0;
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;
		var pip = step * adjust;
		return pip == 0m ? 0.0001m : pip;
	}
}
