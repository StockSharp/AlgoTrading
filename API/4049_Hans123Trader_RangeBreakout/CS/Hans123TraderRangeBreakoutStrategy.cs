using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hans123 breakout strategy. Builds a range from recent highest/lowest prices
/// and enters on breakout above range high or below range low.
/// </summary>
public class Hans123TraderRangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;

	public Hans123TraderRangeBreakoutStrategy()
	{
		_rangeLength = Param(nameof(RangeLength), 20)
			.SetDisplay("Range Length", "Number of candles used to compute the breakout range.", "Breakout");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for range detection.", "General");
	}

	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0;
		_stopPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_stopPrice = 0;

		var highest = new Highest { Length = RangeLength };
		var lowest = new Lowest { Length = RangeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (highestValue <= 0 || lowestValue <= 0)
			return;

		// Manage open position with simple stop
		if (Position > 0)
		{
			if (_stopPrice > 0 && candle.LowPrice <= _stopPrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
			else
			{
				// Trail stop to lowest
				var newStop = lowestValue;
				if (newStop > _stopPrice)
					_stopPrice = newStop;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice > 0 && candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopPrice = 0;
			}
			else
			{
				// Trail stop to highest
				var newStop = highestValue;
				if (_stopPrice == 0 || newStop < _stopPrice)
					_stopPrice = newStop;
			}
		}

		// Entry on breakout
		if (Position == 0)
		{
			if (candle.ClosePrice > highestValue)
			{
				// Bullish breakout
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = lowestValue;
			}
			else if (candle.ClosePrice < lowestValue)
			{
				// Bearish breakout
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = highestValue;
			}
		}
	}
}
