
namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// True Strength Index cross with delayed line.
/// Opens long when TSI crosses above its shifted value and short on opposite cross.
/// </summary>
public class TsiCloudCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _triggerShift;
	private readonly StrategyParam<bool> _invert;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _closeLongOnSignal;
	private readonly StrategyParam<bool> _closeShortOnSignal;

	private TrueStrengthIndex _tsi;
	private readonly Queue<decimal> _tsiValues = new();
	private decimal _prevTsi;
	private decimal _prevTrigger;
	private bool _isInitialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	public int TriggerShift
	{
		get => _triggerShift.Value;
		set => _triggerShift.Value = value;
	}

	public bool Invert
	{
		get => _invert.Value;
		set => _invert.Value = value;
	}

	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	public bool CloseLongOnSignal
	{
		get => _closeLongOnSignal.Value;
		set => _closeLongOnSignal.Value = value;
	}

	public bool CloseShortOnSignal
	{
		get => _closeShortOnSignal.Value;
		set => _closeShortOnSignal.Value = value;
	}

	public TsiCloudCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_longLength = Param(nameof(LongLength), 25)
			.SetDisplay("Long Length", "Long EMA length for TSI", "TSI")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 1);
		_shortLength = Param(nameof(ShortLength), 13)
			.SetDisplay("Short Length", "Short EMA length for TSI", "TSI")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);
		_triggerShift = Param(nameof(TriggerShift), 1)
			.SetDisplay("Trigger Shift", "Bars to shift TSI", "TSI")
			.SetGreaterThanZero();
		_invert = Param(nameof(Invert), false)
			.SetDisplay("Invert", "Reverse signal direction", "General");
		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "Trading");
		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "Trading");
		_closeLongOnSignal = Param(nameof(CloseLongOnSignal), true)
			.SetDisplay("Close Long On Signal", "Close long when opposite signal", "Trading");
		_closeShortOnSignal = Param(nameof(CloseShortOnSignal), true)
			.SetDisplay("Close Short On Signal", "Close short when opposite signal", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_tsi = new TrueStrengthIndex
		{
			LongLength = LongLength,
			ShortLength = ShortLength,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_tsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal tsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_tsiValues.Enqueue(tsiValue);
		if (_tsiValues.Count > TriggerShift + 1)
		_tsiValues.Dequeue();

		if (_tsiValues.Count < TriggerShift + 1)
		{
		_prevTsi = tsiValue;
		_prevTrigger = tsiValue;
		return;
		}

		var trigger = _tsiValues.Peek();

		if (!_isInitialized)
		{
		_prevTsi = tsiValue;
		_prevTrigger = trigger;
		_isInitialized = true;
		return;
		}

		var crossUp = _prevTsi <= _prevTrigger && tsiValue > trigger;
		var crossDown = _prevTsi >= _prevTrigger && tsiValue < trigger;

		_prevTsi = tsiValue;
		_prevTrigger = trigger;

		if (Invert)
		(crossUp, crossDown) = (crossDown, crossUp);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (EnableLong && crossUp && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (EnableShort && crossDown && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && CloseLongOnSignal && crossDown)
		SellMarket(Position);
		else if (Position < 0 && CloseShortOnSignal && crossUp)
		BuyMarket(-Position);
	}
}
