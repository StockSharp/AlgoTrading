namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class MovingAverageWithFramesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<int> _movingShift;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage? _movingAverage;
	private readonly Queue<decimal> _maBuffer = new(); // Maintains indicator values to reproduce the MetaTrader shift parameter.
	private decimal _signedPosition;
	private Sides? _lastEntrySide;
	private decimal _lastEntryPrice;
	private int _consecutiveLosses;
	private int _finishedCandles;

	public MovingAverageWithFramesStrategy()
	{
		_maximumRisk = Param(nameof(MaximumRisk), 0.02m)
			.SetNotNegative()
			.SetDisplay("Maximum Risk", "Fraction of portfolio equity used for each entry.", "Risk Management");

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
			.SetNotNegative()
			.SetDisplay("Decrease Factor", "Lot reduction divisor applied after consecutive losing trades.", "Risk Management");

		_movingPeriod = Param(nameof(MovingPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Moving Period", "Number of bars for the simple moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_movingShift = Param(nameof(MovingShift), 6)
			.SetNotNegative()
			.SetDisplay("Moving Shift", "Number of completed candles used to offset the moving average.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series processed by the strategy.", "General");
	}

	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
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

		_maBuffer.Clear();
		_signedPosition = 0m;
		_lastEntrySide = null;
		_lastEntryPrice = 0m;
		_consecutiveLosses = 0;
		_finishedCandles = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_movingAverage = new SimpleMovingAverage
		{
			Length = Math.Max(1, MovingPeriod)
		};

		_maBuffer.Clear();
		_signedPosition = 0m;
		_lastEntrySide = null;
		_lastEntryPrice = 0m;
		_consecutiveLosses = 0;
		_finishedCandles = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_movingAverage, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return; // Wait for completed candles, emulating the MQL expert that reacts on the first tick of each bar.

		if (_movingAverage == null || !_movingAverage.IsFormed)
			return;

		var shift = Math.Max(0, MovingShift);

		_maBuffer.Enqueue(maValue);
		var maxSize = shift + 1;
		while (_maBuffer.Count > maxSize)
			_maBuffer.Dequeue();

		if (_maBuffer.Count < maxSize)
			return; // Not enough historical values to satisfy the requested shift.

		var shiftedMa = shift == 0 ? maValue : _maBuffer.Peek();

		_finishedCandles++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_finishedCandles <= 100)
			return; // The MetaTrader version waits for at least 100 bars before trading.

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		var bullishCross = open < shiftedMa && close > shiftedMa;
		var bearishCross = open > shiftedMa && close < shiftedMa;

		if (Position > 0m)
		{
			if (bearishCross)
				SellMarket(Position); // Close long positions when price crosses below the delayed moving average.

			return;
		}

		if (Position < 0m)
		{
			if (bullishCross)
				BuyMarket(Math.Abs(Position)); // Close short positions on bullish crossovers.

			return;
		}

		if (!bullishCross && !bearishCross)
			return;

		if (bullishCross)
		{
			var askPrice = GetCurrentAskPrice(candle);
			var volume = CalculateOrderVolume(askPrice);
			if (volume > 0m)
				BuyMarket(volume);
		}
		else
		{
			var bidPrice = GetCurrentBidPrice(candle);
			var volume = CalculateOrderVolume(bidPrice);
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		var volume = Volume;

		if (price > 0m)
		{
			var equity = Portfolio?.CurrentValue ?? 0m;

			if (MaximumRisk > 0m && equity > 0m)
			{
				var riskAmount = equity * MaximumRisk;
				if (riskAmount > 0m)
					volume = riskAmount / price;
			}
		}

		if (DecreaseFactor > 0m && _consecutiveLosses > 1 && volume > 0m)
		{
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
			volume -= reduction;
		}

		if (volume <= 0m)
			volume = Volume;

		if (volume <= 0m)
			volume = Security?.MinVolume ?? 0m;

		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			if (steps < 1m)
				steps = 1m;
			volume = steps * step;
		}
		else
		{
			volume = Math.Round(volume, 2, MidpointRounding.AwayFromZero);
		}

		var minVolume = Security?.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security?.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private decimal GetCurrentAskPrice(ICandleMessage candle)
	{
		var ask = Security?.BestAsk?.Price ?? 0m;
		if (ask <= 0m)
			ask = Security?.LastPrice ?? 0m;
		if (ask <= 0m)
			ask = candle.ClosePrice;
		return ask;
	}

	private decimal GetCurrentBidPrice(ICandleMessage candle)
	{
		var bid = Security?.BestBid?.Price ?? 0m;
		if (bid <= 0m)
			bid = Security?.LastPrice ?? 0m;
		if (bid <= 0m)
			bid = candle.ClosePrice;
		return bid;
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var volume = trade.Trade.Volume;
		if (volume <= 0)
			return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previousPosition = _signedPosition;
		_signedPosition += delta;

		if (previousPosition == 0m && _signedPosition != 0m)
		{
			_lastEntrySide = delta > 0m ? Sides.Buy : Sides.Sell;
			_lastEntryPrice = trade.Trade.Price;
		}
		else if (previousPosition != 0m && _signedPosition == 0m)
		{
			var exitPrice = trade.Trade.Price;

			if (_lastEntrySide != null && _lastEntryPrice != 0m)
			{
				var profit = _lastEntrySide == Sides.Buy
					? exitPrice - _lastEntryPrice
					: _lastEntryPrice - exitPrice;

				if (profit > 0m)
				{
					_consecutiveLosses = 0;
				}
				else if (profit < 0m)
				{
					_consecutiveLosses++;
				}
			}

			_lastEntrySide = null;
			_lastEntryPrice = 0m;
		}
	}
}
