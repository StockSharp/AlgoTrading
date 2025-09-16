using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AFStar strategy converted from MetaTrader 5 expert advisor.
/// It searches for fast and slow EMA crossovers across a configurable range
/// and confirms them with a dynamic Williams %R channel breakout.
/// </summary>
public class AfStarStrategy : Strategy
{
	private const int RangeLength = 10;
	private const int MaxHistory = 512;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _startFast;
	private readonly StrategyParam<decimal> _endFast;
	private readonly StrategyParam<decimal> _startSlow;
	private readonly StrategyParam<decimal> _endSlow;
	private readonly StrategyParam<decimal> _stepPeriod;
	private readonly StrategyParam<decimal> _startRisk;
	private readonly StrategyParam<decimal> _endRisk;
	private readonly StrategyParam<decimal> _stepRisk;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;

	private readonly List<ICandleMessage> _candles = new();
	private readonly List<decimal> _value2History = new();
	private readonly Queue<AfStarSignal> _signalQueue = new();
	private readonly Dictionary<int, decimal> _prevWpr = new();

	private bool _prevBuy1;
	private bool _prevSell1;
	private bool _prevBuy2;
	private bool _prevSell2;

	private decimal? _longStopLevel;
	private decimal? _longTakeLevel;
	private decimal? _shortStopLevel;
	private decimal? _shortTakeLevel;

	/// <summary>
	/// Initializes a new instance of <see cref="AfStarStrategy"/>.
	/// </summary>
	public AfStarStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume used for market orders", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for candles", "General");

		_startFast = Param(nameof(StartFast), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Start Fast", "Lower bound for fast EMA period", "Indicator");

		_endFast = Param(nameof(EndFast), 3.5m)
		.SetGreaterThanZero()
		.SetDisplay("End Fast", "Upper bound for fast EMA period", "Indicator");

		_startSlow = Param(nameof(StartSlow), 8m)
		.SetGreaterThanZero()
		.SetDisplay("Start Slow", "Lower bound for slow EMA period", "Indicator");

		_endSlow = Param(nameof(EndSlow), 9m)
		.SetGreaterThanZero()
		.SetDisplay("End Slow", "Upper bound for slow EMA period", "Indicator");

		_stepPeriod = Param(nameof(StepPeriod), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("Period Step", "Increment for scanning EMA periods", "Indicator");

		_startRisk = Param(nameof(StartRisk), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Start Risk", "Lower bound for risk scan", "Williams %R");

		_endRisk = Param(nameof(EndRisk), 2.8m)
		.SetGreaterThanZero()
		.SetDisplay("End Risk", "Upper bound for risk scan", "Williams %R");

		_stepRisk = Param(nameof(StepRisk), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Step", "Increment for risk parameter", "Williams %R");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetRange(0, 10, 1)
		.SetDisplay("Signal Bar", "Delay in bars before executing a signal", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 1000)
		.SetRange(0, 100000, 1)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance in price steps", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 2000)
		.SetRange(0, 100000, 1)
		.SetDisplay("Take Profit (pips)", "Take profit distance in price steps", "Risk");

		_enableBuyEntries = Param(nameof(BuyEntriesEnabled), true)
		.SetDisplay("Enable Buy Entries", "Allow long entries on buy signals", "Trading");

		_enableSellEntries = Param(nameof(SellEntriesEnabled), true)
		.SetDisplay("Enable Sell Entries", "Allow short entries on sell signals", "Trading");

		_enableBuyExits = Param(nameof(BuyExitsEnabled), true)
		.SetDisplay("Enable Buy Exits", "Allow closing longs on sell signals", "Trading");

		_enableSellExits = Param(nameof(SellExitsEnabled), true)
		.SetDisplay("Enable Sell Exits", "Allow closing shorts on buy signals", "Trading");
	}

	/// <summary>
	/// Trade volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lower bound for the fast EMA search.
	/// </summary>
	public decimal StartFast
	{
		get => _startFast.Value;
		set => _startFast.Value = value;
	}

	/// <summary>
	/// Upper bound for the fast EMA search.
	/// </summary>
	public decimal EndFast
	{
		get => _endFast.Value;
		set => _endFast.Value = value;
	}

	/// <summary>
	/// Lower bound for the slow EMA search.
	/// </summary>
	public decimal StartSlow
	{
		get => _startSlow.Value;
		set => _startSlow.Value = value;
	}

	/// <summary>
	/// Upper bound for the slow EMA search.
	/// </summary>
	public decimal EndSlow
	{
		get => _endSlow.Value;
		set => _endSlow.Value = value;
	}

	/// <summary>
	/// Step used when scanning EMA periods.
	/// </summary>
	public decimal StepPeriod
	{
		get => _stepPeriod.Value;
		set => _stepPeriod.Value = value;
	}

	/// <summary>
	/// Lower bound for the risk parameter scan.
	/// </summary>
	public decimal StartRisk
	{
		get => _startRisk.Value;
		set => _startRisk.Value = value;
	}

	/// <summary>
	/// Upper bound for the risk parameter scan.
	/// </summary>
	public decimal EndRisk
	{
		get => _endRisk.Value;
		set => _endRisk.Value = value;
	}

	/// <summary>
	/// Step for the risk parameter scan.
	/// </summary>
	public decimal StepRisk
	{
		get => _stepRisk.Value;
		set => _stepRisk.Value = value;
	}

	/// <summary>
	/// Bars to wait before executing a signal.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool BuyEntriesEnabled
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool SellEntriesEnabled
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on sell signals.
	/// </summary>
	public bool BuyExitsEnabled
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on buy signals.
	/// </summary>
	public bool SellExitsEnabled
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
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

		_candles.Clear();
		_value2History.Clear();
		_signalQueue.Clear();
		_prevWpr.Clear();
		_prevBuy1 = false;
		_prevSell1 = false;
		_prevBuy2 = false;
		_prevSell2 = false;
		_longStopLevel = null;
		_longTakeLevel = null;
		_shortStopLevel = null;
		_shortTakeLevel = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

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
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		_candles.Add(candle);
		_value2History.Add(0m);

		if (_candles.Count > MaxHistory)
		{
			_candles.RemoveAt(0);
			if (_value2History.Count > 0)
			_value2History.RemoveAt(0);
		}

		var signal = ComputeSignal();

		ApplyStops(candle);

		if (signal.HasValue)
		{
			_signalQueue.Enqueue(signal.Value);

			if (_signalQueue.Count > SignalBar)
			{
				var activeSignal = _signalQueue.Dequeue();
				ExecuteSignal(activeSignal, candle);
			}
		}
	}

	private AfStarSignal? ComputeSignal()
	{
		if (_candles.Count < GetMinHistory())
		return null;

		var buy1 = false;
		var sell1 = false;

		foreach (var slow in EnumerateRange(StartSlow, EndSlow, StepPeriod))
		{
			foreach (var fast in EnumerateRange(StartFast, EndFast, StepPeriod))
			{
				var slowPer = 2m / (slow + 1m);
				var fastPer = 2m / (fast + 1m);

				var slowCurrent = GetClose(0) * slowPer + GetClose(1) * (1m - slowPer);
				var slowPrevious = GetClose(1) * slowPer + GetClose(2) * (1m - slowPer);
				var fastCurrent = GetClose(0) * fastPer + GetClose(1) * (1m - fastPer);
				var fastPrevious = GetClose(1) * fastPer + GetClose(2) * (1m - fastPer);

				if (!buy1 && fastPrevious < slowPrevious && fastCurrent > slowCurrent)
				{
					buy1 = true;
					break;
				}

				if (!sell1 && fastPrevious > slowPrevious && fastCurrent < slowCurrent)
				{
					sell1 = true;
					break;
				}
			}

			if (buy1 || sell1)
			break;
		}

		var range = ComputeAverageRange();
		var mro1 = FindMro1(range);
		var mro2 = FindMro2(range);
		var value2 = 0m;
		var hasBuy2 = false;
		var hasSell2 = false;

		foreach (var risk in EnumerateRange(StartRisk, EndRisk, StepRisk))
		{
			var value10 = 3m + risk * 2m;
			var x1 = 67m + risk;
			var x2 = 33m - risk;

			var value11 = value10;
			value11 = mro1 > -1 ? 3m : value10;
			value11 = mro2 > -1 ? 4m : value10;

			var period = Math.Max(1, (int)value11);
			var wpr = GetWilliamsR(period);
			value2 = 100m - Math.Abs(wpr);

			if (!hasSell2 && value2 < x2)
			{
				var offset = 1;
				while (TryGetPrevValue2(offset, out var prev) && prev >= x2 && prev <= x1)
				offset++;

				if (TryGetPrevValue2(offset, out var prevOutside) && prevOutside > x1)
				hasSell2 = true;
			}

			if (!hasBuy2 && value2 > x1)
			{
				var offset = 1;
				while (TryGetPrevValue2(offset, out var prev) && prev >= x2 && prev <= x1)
				offset++;

				if (TryGetPrevValue2(offset, out var prevOutside) && prevOutside < x2)
				hasBuy2 = true;
			}

			if (hasBuy2 || hasSell2)
			break;
		}

		var buySignal = (buy1 && hasBuy2) || (buy1 && _prevBuy2) || (_prevBuy1 && hasBuy2);
		var sellSignal = (sell1 && hasSell2) || (sell1 && _prevSell2) || (_prevSell1 && hasSell2);

		if (buySignal && sellSignal)
		{
			buySignal = false;
			sellSignal = false;
		}

		_prevBuy1 = buy1;
		_prevSell1 = sell1;
		_prevBuy2 = hasBuy2;
		_prevSell2 = hasSell2;

		_value2History[^1] = value2;

		return new AfStarSignal(buySignal, sellSignal);
	}

	private void ExecuteSignal(AfStarSignal signal, ICandleMessage candle)
	{
		if (signal.BuyArrow)
		{
			if (SellExitsEnabled)
			ExitShort();

			if (BuyEntriesEnabled && Position == 0)
			{
				BuyMarket(Volume);
				InitializeLongTargets(candle.ClosePrice);
			}
		}

		if (signal.SellArrow)
		{
			if (BuyExitsEnabled)
			ExitLong();

			if (SellEntriesEnabled && Position == 0)
			{
				SellMarket(Volume);
				InitializeShortTargets(candle.ClosePrice);
			}
		}
	}

	private void ApplyStops(ICandleMessage candle)
	{
		var position = Position;

		if (position > 0)
		{
			if (_longStopLevel.HasValue && candle.LowPrice <= _longStopLevel.Value)
			{
				ExitLong();
				position = Position;
			}
			else if (_longTakeLevel.HasValue && candle.HighPrice >= _longTakeLevel.Value)
			{
				ExitLong();
				position = Position;
			}
		}

		if (position < 0)
		{
			if (_shortStopLevel.HasValue && candle.HighPrice >= _shortStopLevel.Value)
			{
				ExitShort();
			}
			else if (_shortTakeLevel.HasValue && candle.LowPrice <= _shortTakeLevel.Value)
			{
				ExitShort();
			}
		}
	}

	private void ExitLong()
	{
		var position = Position;
		if (position > 0)
		{
			SellMarket(Math.Abs(position));
			ResetLongTargets();
		}
	}

	private void ExitShort()
	{
		var position = Position;
		if (position < 0)
		{
			BuyMarket(Math.Abs(position));
			ResetShortTargets();
		}
	}

	private void InitializeLongTargets(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		_longStopLevel = StopLossPips > 0 ? entryPrice - step * StopLossPips : null;
		_longTakeLevel = TakeProfitPips > 0 ? entryPrice + step * TakeProfitPips : null;
	}

	private void InitializeShortTargets(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		_shortStopLevel = StopLossPips > 0 ? entryPrice + step * StopLossPips : null;
		_shortTakeLevel = TakeProfitPips > 0 ? entryPrice - step * TakeProfitPips : null;
	}

	private void ResetLongTargets()
	{
		_longStopLevel = null;
		_longTakeLevel = null;
	}

	private void ResetShortTargets()
	{
		_shortStopLevel = null;
		_shortTakeLevel = null;
	}

	private int GetMinHistory()
	{
		return 12 + 3 + SignalBar;
	}

	private decimal ComputeAverageRange()
	{
		var sum = 0m;
		for (var i = 0; i < RangeLength; i++)
		{
			sum += Math.Abs(GetHigh(i) - GetLow(i));
		}

		return sum / RangeLength;
	}

	private int FindMro1(decimal range)
	{
		for (var offset = 0; offset < 9; offset++)
		{
			if (Math.Abs(GetOpen(offset) - GetClose(offset + 1)) >= range * 2m)
			return offset;
		}

		return -1;
	}

	private int FindMro2(decimal range)
	{
		for (var offset = 0; offset < 6; offset++)
		{
			if (Math.Abs(GetClose(offset + 3) - GetClose(offset)) >= range * 4.6m)
			return offset;
		}

		return -1;
	}

	private decimal GetClose(int offset)
	{
		return _candles[^(offset + 1)].ClosePrice;
	}

	private decimal GetOpen(int offset)
	{
		return _candles[^(offset + 1)].OpenPrice;
	}

	private decimal GetHigh(int offset)
	{
		return _candles[^(offset + 1)].HighPrice;
	}

	private decimal GetLow(int offset)
	{
		return _candles[^(offset + 1)].LowPrice;
	}

	private decimal GetWilliamsR(int period)
	{
		var maxHigh = GetHigh(0);
		var minLow = GetLow(0);

		for (var i = 1; i < period && i < _candles.Count; i++)
		{
			var high = GetHigh(i);
			var low = GetLow(i);

			if (high > maxHigh)
			maxHigh = high;

			if (low < minLow)
			minLow = low;
		}

		var close = GetClose(0);
		var range = maxHigh - minLow;

		if (range == 0m)
		{
			if (_prevWpr.TryGetValue(period, out var prev))
			return prev;

			return -50m;
		}

		var wpr = -(maxHigh - close) * 100m / range;
		_prevWpr[period] = wpr;
		return wpr;
	}

	private bool TryGetPrevValue2(int offset, out decimal value)
	{
		var index = _value2History.Count - 1 - offset;
		if (index >= 0)
		{
			value = _value2History[index];
			return true;
		}

		value = 0m;
		return false;
	}

	private IEnumerable<decimal> EnumerateRange(decimal start, decimal end, decimal step)
	{
		if (step <= 0m)
		yield break;

		if (start <= end)
		{
			for (var value = start; value <= end + 0.0000001m; value += step)
			yield return value;
		}
		else
		{
			for (var value = start; value >= end - 0.0000001m; value -= step)
			yield return value;
		}
	}

	private readonly struct AfStarSignal
	{
		public AfStarSignal(bool buyArrow, bool sellArrow)
		{
			BuyArrow = buyArrow;
			SellArrow = sellArrow;
		}

		public bool BuyArrow { get; }
		public bool SellArrow { get; }
	}
}
