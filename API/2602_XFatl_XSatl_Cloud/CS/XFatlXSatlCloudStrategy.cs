using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian strategy based on the XFatl and XSatl cloud crossovers.
/// </summary>
public class XFatlXSatlCloudStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SmoothMethod> _fastMethod;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _fastPhase;
	private readonly StrategyParam<SmoothMethod> _slowMethod;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _slowPhase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;

	private readonly Queue<decimal> _fastHistory = new();
	private readonly Queue<decimal> _slowHistory = new();

	public XFatlXSatlCloudStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for indicator calculations", "General");

		_fastMethod = Param(nameof(FastMethod), SmoothMethod.Jurik)
			.SetDisplay("Fast Method", "Smoothing algorithm for the fast line", "Indicators");

		_fastLength = Param(nameof(FastLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Length of the fast filter", "Indicators");

		_fastPhase = Param(nameof(FastPhase), 15)
			.SetDisplay("Fast Phase", "Phase parameter for Jurik smoothing", "Indicators");

		_slowMethod = Param(nameof(SlowMethod), SmoothMethod.Jurik)
			.SetDisplay("Slow Method", "Smoothing algorithm for the slow line", "Indicators");

		_slowLength = Param(nameof(SlowLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Length of the slow filter", "Indicators");

		_slowPhase = Param(nameof(SlowPhase), 15)
			.SetDisplay("Slow Phase", "Phase parameter for Jurik smoothing", "Indicators");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Bar", "Index of the bar used for signals", "Logic");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size in lots", "Risk");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Enable contrarian long trades", "Logic");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Enable contrarian short trades", "Logic");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Allow indicator to close long trades", "Logic");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Allow indicator to close short trades", "Logic");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Ticks", "Distance to take profit in price steps", "Risk");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Ticks", "Distance to stop loss in price steps", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SmoothMethod FastMethod
	{
		get => _fastMethod.Value;
		set => _fastMethod.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int FastPhase
	{
		get => _fastPhase.Value;
		set => _fastPhase.Value = value;
	}

	public SmoothMethod SlowMethod
	{
		get => _slowMethod.Value;
		set => _slowMethod.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int SlowPhase
	{
		get => _slowPhase.Value;
		set => _slowPhase.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastIndicator = CreateIndicator(FastMethod, FastLength, FastPhase);
		var slowIndicator = CreateIndicator(SlowMethod, SlowLength, SlowPhase);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastIndicator, slowIndicator, ProcessCandle).Start();

		var step = Security?.PriceStep ?? 1m;
		Unit? takeProfit = null;
		if (TakeProfitTicks > 0)
			takeProfit = new Unit(TakeProfitTicks * step, UnitTypes.Point);

		Unit? stopLoss = null;
		if (StopLossTicks > 0)
			stopLoss = new Unit(StopLossTicks * step, UnitTypes.Point);

		if (takeProfit != null || stopLoss != null)
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateHistory(_fastHistory, fastValue);
		UpdateHistory(_slowHistory, slowValue);

		var required = SignalBar + 2;
		if (_fastHistory.Count < required || _slowHistory.Count < required)
			return;

		var fastCurrent = GetShiftedValue(_fastHistory, SignalBar);
		var fastPrevious = GetShiftedValue(_fastHistory, SignalBar + 1);
		var slowCurrent = GetShiftedValue(_slowHistory, SignalBar);
		var slowPrevious = GetShiftedValue(_slowHistory, SignalBar + 1);

		// The cloud is considered bullish when the fast line was above the slow line on the prior bar.
		var fastWasAbove = fastPrevious > slowPrevious;
		var fastWasBelow = fastPrevious < slowPrevious;

		var closeShort = AllowShortExit && fastWasAbove && Position < 0;
		if (closeShort)
		{
			var volume = Math.Abs(Position);
			if (volume > 0)
				BuyMarket(volume);
		}

		var closeLong = AllowLongExit && fastWasBelow && Position > 0;
		if (closeLong)
		{
			var volume = Position;
			if (volume > 0)
				SellMarket(volume);
		}

		var enterLong = AllowLongEntry && fastWasAbove && fastCurrent <= slowCurrent;
		var enterShort = AllowShortEntry && fastWasBelow && fastCurrent >= slowCurrent;

		// Wait for the portfolio to flatten before issuing a new entry order.
		if (Position != 0)
			return;

		if (enterLong)
		{
			var volume = TradeVolume;
			if (volume > 0)
				BuyMarket(volume);
		}
		else if (enterShort)
		{
			var volume = TradeVolume;
			if (volume > 0)
				SellMarket(volume);
		}
	}

	private void UpdateHistory(Queue<decimal> history, decimal value)
	{
		history.Enqueue(value);
		var maxSize = SignalBar + 2;
		while (history.Count > maxSize)
			history.Dequeue();
	}

	private static decimal GetShiftedValue(Queue<decimal> history, int shift)
	{
		var index = history.Count - shift - 1;
		var i = 0;
		foreach (var value in history)
		{
			if (i == index)
				return value;

			i++;
		}

		return 0m;
	}

	private static IIndicator CreateIndicator(SmoothMethod method, int length, int phase)
	{
		return method switch
		{
			SmoothMethod.Sma => new SimpleMovingAverage { Length = length },
			SmoothMethod.Ema => new ExponentialMovingAverage { Length = length },
			SmoothMethod.Smma => new SmoothedMovingAverage { Length = length },
			SmoothMethod.Wma => new WeightedMovingAverage { Length = length },
			_ => CreateJurikIndicator(length, phase),
		};
	}

	private static IIndicator CreateJurikIndicator(int length, int phase)
	{
		var jurik = new JurikMovingAverage { Length = length };

		// Configure the Jurik phase through reflection because the property is optional across versions.
		var phaseProperty = jurik.GetType().GetProperty("Phase");
		if (phaseProperty != null && phaseProperty.CanWrite)
		{
			var converted = Convert.ChangeType(phase, phaseProperty.PropertyType);
			phaseProperty.SetValue(jurik, converted);
		}

		return jurik;
	}

	public enum SmoothMethod
	{
		Sma = 1,
		Ema,
		Smma,
		Wma,
		Jurik
	}
}
