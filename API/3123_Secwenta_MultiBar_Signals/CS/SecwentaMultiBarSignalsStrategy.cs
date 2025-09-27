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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

public class SecwentaMultiBarSignalsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useBuySignals;
	private readonly StrategyParam<int> _bullishBarCount;
	private readonly StrategyParam<bool> _useSellSignals;
	private readonly StrategyParam<int> _bearishBarCount;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<CandleDirections> _directions = new();

	private int _bullCounter;
	private int _bearCounter;
	private int _maxBufferSize;

	private enum CandleDirections
	{
		Neutral,
		Bullish,
		Bearish
	}

	public SecwentaMultiBarSignalsStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base market order volume", "Trading");

		_useBuySignals = Param(nameof(UseBuySignals), true)
			.SetDisplay("Enable Buy", "Enable bullish signal processing", "Signals");

		_bullishBarCount = Param(nameof(BullishBarCount), 2)
			.SetGreaterThanZero()
			.SetDisplay("Bull Bars", "Number of bullish bars required", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_useSellSignals = Param(nameof(UseSellSignals), false)
			.SetDisplay("Enable Sell", "Enable bearish signal processing", "Signals");

		_bearishBarCount = Param(nameof(BearishBarCount), 1)
			.SetGreaterThanZero()
			.SetDisplay("Bear Bars", "Number of bearish bars required", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for bar counting", "General");
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public bool UseBuySignals
	{
		get => _useBuySignals.Value;
		set => _useBuySignals.Value = value;
	}

	public int BullishBarCount
	{
		get => _bullishBarCount.Value;
		set => _bullishBarCount.Value = value;
	}

	public bool UseSellSignals
	{
		get => _useSellSignals.Value;
		set => _useSellSignals.Value = value;
	}

	public int BearishBarCount
	{
		get => _bearishBarCount.Value;
		set => _bearishBarCount.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_directions.Clear();
		_bullCounter = 0;
		_bearCounter = 0;
		_maxBufferSize = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_directions.Clear();
		_bullCounter = 0;
		_bearCounter = 0;
		_maxBufferSize = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateBufferCapacity();

		if (_maxBufferSize <= 0)
			return;

		AddDirection(GetDirection(candle));

		if (_directions.Count < _maxBufferSize)
			return;

		var bullThresholdReached = BullishBarCount > 0 && _bullCounter >= BullishBarCount;
		var bearThresholdReached = BearishBarCount > 0 && _bearCounter >= BearishBarCount;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseBuySignals && !UseSellSignals)
		{
			if (bearThresholdReached && Position > 0)
			{
				CloseLongPositions();
				return;
			}

			if (bullThresholdReached && Position == 0)
			{
				OpenLongPosition();
				return;
			}

			return;
		}

		if (!UseBuySignals && UseSellSignals)
		{
			if (bullThresholdReached && Position < 0)
			{
				CloseShortPositions();
				return;
			}

			if (bearThresholdReached && Position == 0)
			{
				OpenShortPosition();
				return;
			}

			return;
		}

		if (!UseBuySignals || !UseSellSignals)
			return;

		if (bullThresholdReached)
		{
			ExecuteBullSignal();
			return;
		}

		if (bearThresholdReached)
			ExecuteBearSignal();
	}

	private void ExecuteBullSignal()
	{
		var closeVolume = Position < 0 ? Math.Abs(Position) : 0m;
		var entryVolume = OrderVolume;

		var totalVolume = closeVolume;

		if (entryVolume > 0m && Position <= 0)
			totalVolume += entryVolume;

		if (totalVolume <= 0m)
			return;

		// A single market order both closes an existing short and opens a fresh long exposure.
		BuyMarket(totalVolume);
	}

	private void ExecuteBearSignal()
	{
		var closeVolume = Position > 0 ? Math.Abs(Position) : 0m;
		var entryVolume = OrderVolume;

		var totalVolume = closeVolume;

		if (entryVolume > 0m && Position >= 0)
			totalVolume += entryVolume;

		if (totalVolume <= 0m)
			return;

		// Selling the combined volume closes any long and establishes a short position.
		SellMarket(totalVolume);
	}

	private void OpenLongPosition()
	{
		var volume = OrderVolume;

		if (volume <= 0m)
			return;

		// Only open a long when the strategy is flat in buy-only mode.
		BuyMarket(volume);
	}

	private void OpenShortPosition()
	{
		var volume = OrderVolume;

		if (volume <= 0m)
			return;

		// Only open a short when no exposure exists in sell-only mode.
		SellMarket(volume);
	}

	private void CloseLongPositions()
	{
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		// Close the active long position after the configured number of bearish bars.
		SellMarket(volume);
	}

	private void CloseShortPositions()
	{
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		// Close the active short position after the configured number of bullish bars.
		BuyMarket(volume);
	}

	private void UpdateBufferCapacity()
	{
		var newSize = 0;

		if (UseBuySignals && BullishBarCount > newSize)
			newSize = BullishBarCount;

		if (UseSellSignals && BearishBarCount > newSize)
			newSize = BearishBarCount;

		if (newSize <= 0)
		{
			_maxBufferSize = 0;
			_directions.Clear();
			_bullCounter = 0;
			_bearCounter = 0;
			return;
		}

		if (_maxBufferSize == newSize)
			return;

		_maxBufferSize = newSize;

		while (_directions.Count > _maxBufferSize)
			RemoveOldest();
	}

	private void AddDirection(CandleDirections direction)
	{
		_directions.Enqueue(direction);

		switch (direction)
		{
			case CandleDirections.Bullish:
				_bullCounter++;
				break;
			case CandleDirections.Bearish:
				_bearCounter++;
				break;
		}

		while (_directions.Count > _maxBufferSize)
			RemoveOldest();
	}

	private void RemoveOldest()
	{
		if (_directions.Count == 0)
			return;

		var removed = _directions.Dequeue();

		switch (removed)
		{
			case CandleDirections.Bullish:
				_bullCounter = Math.Max(0, _bullCounter - 1);
				break;
			case CandleDirections.Bearish:
				_bearCounter = Math.Max(0, _bearCounter - 1);
				break;
		}
	}

	private static CandleDirections GetDirection(ICandleMessage candle)
	{
		if (candle.ClosePrice > candle.OpenPrice)
			return CandleDirections.Bullish;

		if (candle.ClosePrice < candle.OpenPrice)
			return CandleDirections.Bearish;

		return CandleDirections.Neutral;
	}
}

