namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class MaMirrorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<int> _movingShift;
	private readonly StrategyParam<decimal> _tradeVolume;

	private SimpleMovingAverage _closeMovingAverage;
	private SimpleMovingAverage _openMovingAverage;

	private readonly Queue<decimal> _closeBuffer = new(); // Stores SMA values to emulate the MetaTrader shift parameter for closes
	private readonly Queue<decimal> _openBuffer = new(); // Stores SMA values to emulate the MetaTrader shift parameter for opens

	private Sides _lastSignal = Sides.Sell; // The original EA starts with a virtual sell signal to wait for the first bullish flip
	private bool _signalInitialized;

	public MaMirrorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Timeframe processed by the strategy.", "General");

		_movingPeriod = Param(nameof(MovingPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Moving period", "Length of the SMA calculated on open and close prices.", "Indicator");

		_movingShift = Param(nameof(MovingShift), 0)
			.SetNotNegative()
			.SetDisplay("Moving shift", "Number of completed candles used to shift the SMA backwards.", "Indicator");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Default order volume for market entries.", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MovingPeriod
	{
		get => _movingPeriod.Value;
		set => _movingPeriod.Value = value;
	}

	public int MovingShift
	{
		get => _movingShift.Value;
		set => _movingShift.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Align helper methods with the configured trade size

		_closeMovingAverage = new SimpleMovingAverage { Length = MovingPeriod };
		_openMovingAverage = new SimpleMovingAverage { Length = MovingPeriod };

		_closeBuffer.Clear();
		_openBuffer.Clear();

		_lastSignal = Sides.Sell;
		_signalInitialized = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_closeMovingAverage != null)
				DrawIndicator(area, _closeMovingAverage);
			if (_openMovingAverage != null)
				DrawIndicator(area, _openMovingAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return; // Wait for fully closed candles just like the MetaTrader expert

		if (_closeMovingAverage == null || _openMovingAverage == null)
			return;

		var shift = Math.Max(MovingShift, 0);

		var closeValue = ProcessMovingAverage(_closeMovingAverage, _closeBuffer, shift, candle.ClosePrice, candle);
		var openValue = ProcessMovingAverage(_openMovingAverage, _openBuffer, shift, candle.OpenPrice, candle);

		if (closeValue is null || openValue is null)
			return; // Not enough data for both moving averages yet

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		Sides? desiredSignal = null;
		if (closeValue > openValue)
		{
			desiredSignal = Sides.Buy;
		}
		else if (closeValue < openValue)
		{
			desiredSignal = Sides.Sell;
		}
		else
		{
			return; // Keep the previous signal when both averages match
		}

		if (!_signalInitialized)
		{
			if (desiredSignal != Sides.Buy)
				return; // The legacy EA stays flat until the first bullish crossover appears
		}
		else if (desiredSignal == _lastSignal)
		{
			return; // Ignore duplicate signals while position direction remains unchanged
		}

		ExecuteSignal(desiredSignal.Value);
	}

	private void ExecuteSignal(Sides signal)
	{
		var tradeVolume = TradeVolume;
		if (tradeVolume <= 0m)
			return;

		if (signal == Sides.Buy)
		{
			if (Position < 0m)
				BuyMarket(Math.Abs(Position)); // Close any existing short exposure before opening a long

			BuyMarket(tradeVolume); // Enter or increase the long side
		}
		else
		{
			if (Position > 0m)
				SellMarket(Position); // Close any existing long exposure before opening a short

			SellMarket(tradeVolume); // Enter or increase the short side
		}

		_lastSignal = signal;
		_signalInitialized = true;
	}

	private static decimal? ProcessMovingAverage(LengthIndicator<decimal> indicator, Queue<decimal> buffer, int shift, decimal price, ICandleMessage candle)
	{
		if (indicator == null)
			return null;

		var value = indicator.Process(price, candle.OpenTime, true);

		if (!indicator.IsFormed)
			return null;

		var maValue = value.ToDecimal();

		buffer.Enqueue(maValue);
		var maxSize = shift + 1;
		while (buffer.Count > maxSize)
			buffer.Dequeue();

		if (buffer.Count < maxSize)
			return null; // Need additional historical values to respect the requested shift

		return shift == 0 ? maValue : buffer.Peek();
	}
}
