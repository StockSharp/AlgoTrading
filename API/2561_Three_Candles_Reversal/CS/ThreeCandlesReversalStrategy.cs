using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum ThreeCandlesVolumeType
{
	Tick,
	Real,
	None,
}

/// <summary>
/// Translates the classic Three Candles reversal expert advisor from MQL5.
/// The strategy searches for two candles in one direction followed by a strong opposite candle and trades the expected reversal.
/// </summary>
public class ThreeCandlesReversalStrategy : Strategy
{
	private readonly Queue<ICandleMessage> _candles = new();

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _maxBarSize;
	private readonly StrategyParam<ThreeCandlesVolumeType> _volumeFilter;
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowBuyExit;
	private readonly StrategyParam<bool> _allowSellExit;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private DateTimeOffset? _lastBullishSignalTime;
	private DateTimeOffset? _lastBearishSignalTime;
	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public int MaxBarSize { get => _maxBarSize.Value; set => _maxBarSize.Value = value; }
	public ThreeCandlesVolumeType VolumeFilter { get => _volumeFilter.Value; set => _volumeFilter.Value = value; }
	public bool AllowBuyEntry { get => _allowBuyEntry.Value; set => _allowBuyEntry.Value = value; }
	public bool AllowSellEntry { get => _allowSellEntry.Value; set => _allowSellEntry.Value = value; }
	public bool AllowBuyExit { get => _allowBuyExit.Value; set => _allowBuyExit.Value = value; }
	public bool AllowSellExit { get => _allowSellExit.Value; set => _allowSellExit.Value = value; }
	public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	public ThreeCandlesReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for the candle subscription", "General");
		_signalBar = Param(nameof(SignalBar), 1)
			.SetMinMax(0, 20)
			.SetDisplay("Signal Bar", "Historical offset where the signal is evaluated", "Pattern");
		_maxBarSize = Param(nameof(MaxBarSize), 300)
			.SetMinMax(0, 100000)
			.SetDisplay("Max Bar Size", "Disable the volume filter when the oldest candle range exceeds this value (in price steps)", "Pattern");
		_volumeFilter = Param(nameof(VolumeFilter), ThreeCandlesVolumeType.Tick)
			.SetDisplay("Volume Filter", "Volume filter used to confirm the reversal", "Pattern");
		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
			.SetDisplay("Allow Buy Entry", "Enable long entries on bullish signals", "Trading");
		_allowSellEntry = Param(nameof(AllowSellEntry), true)
			.SetDisplay("Allow Sell Entry", "Enable short entries on bearish signals", "Trading");
		_allowBuyExit = Param(nameof(AllowBuyExit), true)
			.SetDisplay("Allow Buy Exit", "Close long positions when a bearish pattern appears", "Trading");
		_allowSellExit = Param(nameof(AllowSellExit), true)
			.SetDisplay("Allow Sell Exit", "Close short positions when a bullish pattern appears", "Trading");
		_stopLossPips = Param(nameof(StopLossPips), 1000m)
			.SetMinMax(0m, 100000m)
			.SetDisplay("Stop Loss", "Distance to the protective stop in price steps", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 2000m)
			.SetMinMax(0m, 100000m)
			.SetDisplay("Take Profit", "Distance to the profit target in price steps", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_candles.Clear();
		_lastBullishSignalTime = null;
		_lastBearishSignalTime = null;
		_entryPrice = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_candles.Enqueue(candle);

		var required = SignalBar + 5;
		while (_candles.Count > required)
		_candles.Dequeue();

		if (_candles.Count < required)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
		priceStep = 1m;

		if (CheckRiskManagement(candle, priceStep))
		return;

		var buffer = _candles.ToArray();
		var bullishSignal = IsBullishSignal(buffer, priceStep);
		var bearishSignal = IsBearishSignal(buffer, priceStep);

		if (bullishSignal)
		{
		var signalCandle = GetSeries(buffer, SignalBar);
		HandleBullish(signalCandle);
		}

		if (bearishSignal)
		{
		var signalCandle = GetSeries(buffer, SignalBar);
		HandleBearish(signalCandle);
		}
	}

	private bool CheckRiskManagement(ICandleMessage candle, decimal priceStep)
	{
		if (Position == 0m || _entryPrice == 0m)
		return false;

		var stopDistance = StopLossPips > 0m ? StopLossPips * priceStep : 0m;
		var takeDistance = TakeProfitPips > 0m ? TakeProfitPips * priceStep : 0m;

		if (Position > 0m)
		{
		var stopTriggered = stopDistance > 0m && candle.LowPrice <= _entryPrice - stopDistance;
		var takeTriggered = takeDistance > 0m && candle.HighPrice >= _entryPrice + takeDistance;

		if (stopTriggered || takeTriggered)
		{
		SellMarket(Position);
		ResetTradeState();
		return true;
		}
		}
		else if (Position < 0m)
		{
		var stopTriggered = stopDistance > 0m && candle.HighPrice >= _entryPrice + stopDistance;
		var takeTriggered = takeDistance > 0m && candle.LowPrice <= _entryPrice - takeDistance;

		if (stopTriggered || takeTriggered)
		{
		BuyMarket(-Position);
		ResetTradeState();
		return true;
		}
		}

		return false;
	}

	private void HandleBullish(ICandleMessage signalCandle)
	{
		var signalTime = signalCandle.CloseTime;
		if (_lastBullishSignalTime == signalTime)
		return;

		if (AllowSellExit && Position < 0m)
		{
		BuyMarket(-Position);
		ResetTradeState();
		}

		if (AllowBuyEntry && Position == 0m)
		{
		BuyMarket();
		_entryPrice = signalCandle.ClosePrice;
		}

		_lastBullishSignalTime = signalTime;
	}

	private void HandleBearish(ICandleMessage signalCandle)
	{
		var signalTime = signalCandle.CloseTime;
		if (_lastBearishSignalTime == signalTime)
		return;

		if (AllowBuyExit && Position > 0m)
		{
		SellMarket(Position);
		ResetTradeState();
		}

		if (AllowSellEntry && Position == 0m)
		{
		SellMarket();
		_entryPrice = signalCandle.ClosePrice;
		}

		_lastBearishSignalTime = signalTime;
	}

	private bool IsBullishSignal(IReadOnlyList<ICandleMessage> candles, decimal priceStep)
	{
	var last = GetSeries(candles, SignalBar + 1);
	var middle = GetSeries(candles, SignalBar + 2);
	var oldest = GetSeries(candles, SignalBar + 3);

	if (!(oldest.OpenPrice > oldest.ClosePrice &&
	middle.OpenPrice > middle.ClosePrice &&
	middle.ClosePrice > oldest.LowPrice &&
	last.OpenPrice < last.ClosePrice &&
	last.ClosePrice > middle.OpenPrice))
	{
	return false;
	}

	if (!ShouldApplyVolumeFilter(oldest, priceStep))
	return true;

	var volOldest = GetVolume(oldest);
	var volMiddle = GetVolume(middle);
	var volLast = GetVolume(last);

	return volOldest < volMiddle || volLast > volMiddle || volLast > volOldest;
	}

	private bool IsBearishSignal(IReadOnlyList<ICandleMessage> candles, decimal priceStep)
	{
	var last = GetSeries(candles, SignalBar + 1);
	var middle = GetSeries(candles, SignalBar + 2);
	var oldest = GetSeries(candles, SignalBar + 3);

	if (!(oldest.OpenPrice < oldest.ClosePrice &&
	middle.OpenPrice < middle.ClosePrice &&
	middle.ClosePrice < oldest.HighPrice &&
	last.OpenPrice > last.ClosePrice &&
	last.ClosePrice < middle.OpenPrice))
	{
	return false;
	}

	if (!ShouldApplyVolumeFilter(oldest, priceStep))
	return true;

	var volOldest = GetVolume(oldest);
	var volMiddle = GetVolume(middle);
	var volLast = GetVolume(last);

	return volOldest < volMiddle || volLast > volMiddle || volLast > volOldest;
	}

	private bool ShouldApplyVolumeFilter(ICandleMessage oldest, decimal priceStep)
	{
	if (VolumeFilter == ThreeCandlesVolumeType.None)
	return false;

	if (MaxBarSize <= 0)
	return false;

	var range = oldest.HighPrice - oldest.LowPrice;
	var threshold = MaxBarSize * priceStep;

	if (range > threshold)
	return false;

	return true;
	}

	private static ICandleMessage GetSeries(IReadOnlyList<ICandleMessage> candles, int index)
	{
	var idx = candles.Count - 1 - index;
	return candles[idx];
	}

	private static decimal GetVolume(ICandleMessage candle)
	=> candle.TotalVolume ?? 0m;

	private void ResetTradeState()
	{
	_entryPrice = 0m;
	}
}
