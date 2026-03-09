using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid hedging strategy based on EMA crossover direction.
/// Opens positions on EMA trend, reverses on direction change.
/// </summary>
public class OverHedgeV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _longEmaPeriod;

	private int _prevSignal;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

	public int LongEmaPeriod
	{
		get => _longEmaPeriod.Value;
		set => _longEmaPeriod.Value = value;
	}

	public OverHedgeV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Fast EMA length", "Indicators");

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Slow EMA length", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSignal = 0;

		var shortEma = new ExponentialMovingAverage { Length = ShortEmaPeriod };
		var longEma = new ExponentialMovingAverage { Length = LongEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortEma, longEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortEma, decimal longEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var signal = shortEma > longEma ? 1 : shortEma < longEma ? -1 : _prevSignal;

		if (signal == _prevSignal)
			return;

		var oldSignal = _prevSignal;
		_prevSignal = signal;

		if (signal == 1 && oldSignal <= 0)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (signal == -1 && oldSignal >= 0)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}
	}
}
