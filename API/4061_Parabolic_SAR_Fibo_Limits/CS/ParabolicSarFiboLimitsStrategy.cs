using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy with Fibonacci retracement-based targets.
/// Enters on SAR flip, uses Highest/Lowest range for Fibonacci levels.
/// </summary>
public class ParabolicSarFiboLimitsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;

	private decimal _prevSar;
	private bool _hasPrevSar;
	private decimal _entryPrice;

	public ParabolicSarFiboLimitsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(3).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_lookback = Param(nameof(Lookback), 20)
			.SetDisplay("Lookback", "Period for Highest/Lowest range.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevSar = 0;
		_hasPrevSar = false;
		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSar = 0;
		_hasPrevSar = false;
		_entryPrice = 0;

		var sar = new ParabolicSar();
		var highest = new Highest { Length = Lookback };
		var lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var range = highestValue - lowestValue;

		// SAR flip detection
		var sarBelow = sarValue < close;
		var prevSarBelow = _hasPrevSar && _prevSar < close;

		var sarAbove = sarValue > close;
		var prevSarAbove = _hasPrevSar && _prevSar > close;

		// Fibonacci levels from the range
		var fib382 = lowestValue + range * 0.382m;
		var fib618 = lowestValue + range * 0.618m;

		// Position management
		if (Position > 0)
		{
			// Exit at 61.8% Fibonacci or SAR flip above
			if (close >= fib618 || sarAbove)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			// Exit at 38.2% Fibonacci or SAR flip below
			if (close <= fib382 || sarBelow)
			{
				BuyMarket();
			}
		}

		// Entry on SAR flip with range confirmation
		if (Position == 0 && _hasPrevSar && range > 0)
		{
			if (sarBelow && !prevSarBelow && close > fib382)
			{
				// SAR flipped below price - bullish
				_entryPrice = close;
				BuyMarket();
			}
			else if (sarAbove && !prevSarAbove && close < fib618)
			{
				// SAR flipped above price - bearish
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevSar = sarValue;
		_hasPrevSar = true;
	}
}
