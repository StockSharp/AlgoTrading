using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class PhaseCrossStrategyWithZoneStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _offset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevLead;
	private decimal? _prevLag;

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

	public PhaseCrossStrategyWithZoneStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Smoothing length", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 1);

		_offset = Param(nameof(Offset), 0.5m)
			.SetDisplay("Offset", "Phase offset", "General")
			.SetCanOptimize(true)
			.SetOptimize(0m, 1m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevLead = null;
		_prevLag = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var sma = new Sma { Length = Length };
		var ema = new Ema { Length = Length };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var lead = smaValue + Offset;
		var lag = emaValue - Offset;

		if (_prevLead is decimal prevLead && _prevLag is decimal prevLag)
		{
			var crossedUp = prevLead <= prevLag && lead > lag;
			var crossedDown = prevLead >= prevLag && lead < lag;

			if (crossedUp && Position <= 0)
				BuyMarket();
			else if (crossedDown && Position > 0)
				SellMarket();
		}

		_prevLead = lead;
		_prevLag = lag;
	}
}

