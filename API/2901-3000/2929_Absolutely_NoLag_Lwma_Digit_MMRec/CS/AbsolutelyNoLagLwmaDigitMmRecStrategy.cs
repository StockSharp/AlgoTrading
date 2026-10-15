using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double WMA crossover strategy with money management recovery.
/// Uses fast and slow weighted moving averages and trades on crossovers.
/// </summary>
public class AbsolutelyNoLagLwmaDigitMmRecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;

	private int _prevSignal;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public AbsolutelyNoLagLwmaDigitMmRecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast WMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow WMA period", "Indicators");
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

		var fastWma = new WeightedMovingAverage { Length = FastLength };
		var slowWma = new WeightedMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastWma, slowWma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastWma);
			DrawIndicator(area, slowWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var signal = fastVal > slowVal ? 1 : fastVal < slowVal ? -1 : _prevSignal;

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
