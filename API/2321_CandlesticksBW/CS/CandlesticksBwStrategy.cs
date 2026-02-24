using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CandlesticksBW strategy based on Bill Williams' color classification of candles.
/// Uses Awesome and Accelerator oscillators to detect momentum shifts.
/// </summary>
public class CandlesticksBwStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private readonly SimpleMovingAverage _aoFast = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlow = new() { Length = 34 };
	private readonly SimpleMovingAverage _acMa = new() { Length = 5 };

	private decimal _prevAo;
	private decimal _prevAc;
	private bool _hasPrev;
	private int _prevColor = -1;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CandlesticksBwStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAo = 0;
		_prevAc = 0;
		_hasPrev = false;
		_prevColor = -1;

		var sma = new SimpleMovingAverage { Length = 1 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _unused)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;
		var t = candle.CloseTime;

		var aoFastResult = _aoFast.Process(hl2, t, true);
		var aoSlowResult = _aoSlow.Process(hl2, t, true);

		if (!_aoFast.IsFormed || !_aoSlow.IsFormed)
			return;

		var ao = aoFastResult.GetValue<decimal>() - aoSlowResult.GetValue<decimal>();
		var acMaResult = _acMa.Process(ao, t, true);

		if (!_acMa.IsFormed)
			return;

		var ac = ao - acMaResult.GetValue<decimal>();

		// Bill Williams candle color classification:
		// 0 = green (bullish candle + AO up + AC up)
		// 1 = fade (bearish candle + AO up + AC up)
		// 2 = squat green (bullish, mixed)
		// 3 = squat red (bearish, mixed)
		// 4 = fake (bullish candle + AO down + AC down)
		// 5 = red (bearish candle + AO down + AC down)
		int color;
		if (_hasPrev && ao >= _prevAo && ac >= _prevAc)
			color = candle.OpenPrice <= candle.ClosePrice ? 0 : 1;
		else if (_hasPrev && ao <= _prevAo && ac <= _prevAc)
			color = candle.OpenPrice >= candle.ClosePrice ? 5 : 4;
		else
			color = candle.OpenPrice <= candle.ClosePrice ? 2 : 3;

		_prevAo = ao;
		_prevAc = ac;
		_hasPrev = true;

		if (!IsFormedAndOnline())
		{
			_prevColor = color;
			return;
		}

		if (_prevColor < 0)
		{
			_prevColor = color;
			return;
		}

		// Bullish signal: prev was up momentum (0 or 1), current transitions
		if (_prevColor < 2 && color > 1 && Position <= 0)
			BuyMarket();
		// Bearish signal: prev was down momentum (4 or 5), current transitions
		else if (_prevColor > 3 && color < 4 && Position >= 0)
			SellMarket();

		_prevColor = color;
	}
}
