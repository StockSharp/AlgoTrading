using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Counter-trend reversal on the sign of the simple moving average of candle bodies.
/// A positive-to-negative sign change buys; a negative-to-positive change sells.
/// </summary>
public class GoCandleBodyReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<decimal> _bodies = new();
	private decimal _bodySum;
	private int _prevNonzeroSign;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GoCandleBodyReversalStrategy()
	{
		_period = Param(nameof(Period), 174)
			.SetGreaterThanZero()
			.SetDisplay("Period", "SMA period for candle body", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_bodies.Clear();
		_bodySum = 0m;
		_prevNonzeroSign = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_bodies.Clear();
		_bodySum = 0m;
		_prevNonzeroSign = 0;
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = candle.ClosePrice - candle.OpenPrice;
		_bodies.Enqueue(body);
		_bodySum += body;
		if (_bodies.Count > Period)
			_bodySum -= _bodies.Dequeue();
		if (_bodies.Count < Period)
			return;

		var sign = Math.Sign(_bodySum); // Dividing by positive Period does not change the SMA sign.
		if (sign == 0)
			return; // Retain the last nonzero sign across exact-zero SMA readings.

		var previous = _prevNonzeroSign;
		_prevNonzeroSign = sign;
		if (previous == 0 || previous == sign || !IsFormedAndOnlineAndAllowTrading())
			return;

		var position = Position;
		if (previous > 0 && sign < 0 && position <= 0m)
		{
			if (position < 0m)
				BuyMarket(-position);
			BuyMarket(Volume);
		}
		else if (previous < 0 && sign > 0 && position >= 0m)
		{
			if (position > 0m)
				SellMarket(position);
			SellMarket(Volume);
		}
	}
}
