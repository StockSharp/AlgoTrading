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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Loco indicator (price direction detector).
/// </summary>
public class LocoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	private readonly Queue<decimal> _prices = new();
	private decimal? _prev;
	private int _prevColor = -1;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }

	public LocoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 1)
			.SetDisplay("Length", "Lookback length", "Indicator")
			.SetOptimize(1, 10, 1);
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
		_prices.Clear();
		_prev = null;
		_prevColor = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		_prices.Enqueue(price);

		if (_prices.Count <= Length)
		{
			_prev = price;
			return;
		}

		var series1 = _prices.Dequeue();
		var prev = _prev ?? price;
		decimal result;
		int color;

		if (price == prev)
		{
			result = prev;
			color = 0;
		}
		else if (series1 > prev && price > prev)
		{
			result = Math.Max(prev, price * 0.999m);
			color = 0;
		}
		else if (series1 < prev && price < prev)
		{
			result = Math.Min(prev, price * 1.001m);
			color = 1;
		}
		else
		{
			if (price > prev)
			{
				result = price * 0.999m;
				color = 0;
			}
			else
			{
				result = price * 1.001m;
				color = 1;
			}
		}

		_prev = result;

		if (_prevColor == -1)
		{
			_prevColor = color;
			return;
		}

		if (color != _prevColor)
		{
			if (color == 1)
			{
				if (Position < 0)
					BuyMarket();
				if (Position <= 0)
					BuyMarket();
			}
			else
			{
				if (Position > 0)
					SellMarket();
				if (Position >= 0)
					SellMarket();
			}
		}

		_prevColor = color;
	}
}
