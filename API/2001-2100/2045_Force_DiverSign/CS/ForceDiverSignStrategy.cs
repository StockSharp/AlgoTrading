using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Force DiverSign strategy.
/// Detects divergence between fast and slow Force Index values
/// combined with a specific candle pattern.
/// </summary>
public class ForceDiverSignStrategy : Strategy
{
	private readonly StrategyParam<int> _period1;
	private readonly StrategyParam<int> _period2;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _opens = new decimal[5];
	private readonly decimal[] _closes = new decimal[5];
	private readonly decimal[] _f1 = new decimal[5];
	private readonly decimal[] _f2 = new decimal[5];
	private decimal _prevClose;
	private int _count;

	public int Period1 { get => _period1.Value; set => _period1.Value = value; }
	public int Period2 { get => _period2.Value; set => _period2.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ForceDiverSignStrategy()
	{
		_period1 = Param(nameof(Period1), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Period for fast Force index", "Indicators");

		_period2 = Param(nameof(Period2), 7)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Period for slow Force index", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevClose = default;
		_count = default;
		Array.Clear(_opens);
		Array.Clear(_closes);
		Array.Clear(_f1);
		Array.Clear(_f2);
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma1 = new ExponentialMovingAverage { Length = Period1 };
		var ma2 = new ExponentialMovingAverage { Length = Period2 };
		Indicators.Add(ma1);
		Indicators.Add(ma2);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(candle =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (_count == 0)
			{
				_prevClose = candle.ClosePrice;
				Shift(_opens, candle.OpenPrice);
				Shift(_closes, candle.ClosePrice);
				_count++;
				return;
			}

			var force = (candle.ClosePrice - _prevClose) * candle.TotalVolume;
			_prevClose = candle.ClosePrice;

			var f1v = ma1.Process(force, candle.OpenTime, true);
			var f2v = ma2.Process(force, candle.OpenTime, true);

			Shift(_opens, candle.OpenPrice);
			Shift(_closes, candle.ClosePrice);

			if (f1v.IsEmpty || f2v.IsEmpty)
			{
				_count++;
				return;
			}

			var f1 = f1v.ToDecimal();
			var f2 = f2v.ToDecimal();
			Shift(_f1, f1);
			Shift(_f2, f2);

			if (_count < 5)
			{
				_count++;
				return;
			}

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var sellSignal = _opens[3] < _closes[3] && _opens[2] > _closes[2] && _opens[1] < _closes[1]
				&& _f1[4] < _f1[3] && _f1[3] > _f1[2] && _f1[2] < _f1[1]
				&& _f2[4] < _f2[3] && _f2[3] > _f2[2] && _f2[2] < _f2[1]
				&& ((_f1[3] > _f1[1] && _f2[3] < _f2[1]) || (_f1[3] < _f1[1] && _f2[3] > _f2[1]));

			var buySignal = _opens[3] > _closes[3] && _opens[2] < _closes[2] && _opens[1] > _closes[1]
				&& _f1[4] > _f1[3] && _f1[3] < _f1[2] && _f1[2] > _f1[1]
				&& _f2[4] > _f2[3] && _f2[3] < _f2[2] && _f2[2] > _f2[1]
				&& ((_f1[3] > _f1[1] && _f2[3] < _f2[1]) || (_f1[3] < _f1[1] && _f2[3] > _f2[1]));

			if (buySignal && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			else if (sellSignal && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}).Start();
	}

	private static void Shift(decimal[] array, decimal value)
	{
		for (var i = array.Length - 1; i > 0; i--)
			array[i] = array[i - 1];
		array[0] = value;
	}
}
