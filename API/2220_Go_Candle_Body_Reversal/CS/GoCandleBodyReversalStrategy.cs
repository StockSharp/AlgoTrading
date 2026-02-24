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
/// Strategy based on smoothed candle body direction.
/// Smooths (close-open) with SMA, trades on sign changes.
/// </summary>
public class GoCandleBodyReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _bodySma;
	private int _prevSign;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GoCandleBodyReversalStrategy()
	{
		_period = Param(nameof(Period), 30)
			.SetGreaterThanZero()
			.SetDisplay("Period", "SMA period for candle body", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_bodySma = null;
		_prevSign = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bodySma = new SimpleMovingAverage { Length = Period };

		// Use a warmup SMA bound to close price
		var warmup = new SimpleMovingAverage { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(warmup, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _warmupVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = candle.ClosePrice - candle.OpenPrice;
		var maResult = _bodySma.Process(new DecimalIndicatorValue(_bodySma, body, candle.OpenTime) { IsFinal = true });

		if (!maResult.IsFormed)
			return;

		var value = maResult.GetValue<decimal>();
		var sign = value > 0 ? 1 : value < 0 ? -1 : 0;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevSign = sign;
			return;
		}

		if (_prevSign == 0)
		{
			_prevSign = sign;
			return;
		}

		// Body direction turns negative (bearish reversal) -> sell
		if (sign < 0 && _prevSign > 0 && Position >= 0)
			SellMarket();
		// Body direction turns positive (bullish reversal) -> buy
		else if (sign > 0 && _prevSign < 0 && Position <= 0)
			BuyMarket();

		_prevSign = sign;
	}
}
