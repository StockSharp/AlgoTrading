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

public class PhaseCrossWithZoneStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _offset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevLead;
	private decimal _prevLag;
	private bool _prevInit;

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public decimal Offset
	{
		get => _offset.Value;
		set => _offset.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PhaseCrossWithZoneStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Smoothing length", "General")
			
			.SetOptimize(5, 50, 1);

		_offset = Param(nameof(Offset), 0.5m)
			.SetDisplay("Offset", "Phase offset", "General")
			
			.SetOptimize(0m, 1m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevLead = 0;
		_prevLag = 0;
		_prevInit = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var sma = new SimpleMovingAverage { Length = Length };
		var ema = new ExponentialMovingAverage { Length = Length * 2 };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var lead = smaValue + Offset;
		var lag = emaValue - Offset;

		if (_prevInit)
		{
			var crossedUp = _prevLead <= _prevLag && lead > lag;
			var crossedDown = _prevLead >= _prevLag && lead < lag;

			if (crossedUp && Position <= 0)
				BuyMarket();
			else if (crossedDown && Position >= 0)
				SellMarket();
		}

		_prevLead = lead;
		_prevLag = lag;
		_prevInit = true;
	}
}
