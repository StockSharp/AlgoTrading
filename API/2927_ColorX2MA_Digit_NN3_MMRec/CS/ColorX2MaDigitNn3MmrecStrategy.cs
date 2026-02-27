using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double-smoothed moving average slope strategy.
/// Uses two SMAs (fast and slow). Trades on slope direction changes.
/// </summary>
public class ColorX2MaDigitNn3MmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;

	private decimal? _prevFast;
	private decimal? _prevSlow;
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

	public ColorX2MaDigitNn3MmrecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastLength = Param(nameof(FastLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;
		_prevSignal = 0;

		var fastSma = new SimpleMovingAverage { Length = FastLength };
		var slowSma = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == null || _prevSlow == null)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		// Determine signal based on fast vs slow crossover
		var signal = fastVal > slowVal ? 1 : fastVal < slowVal ? -1 : _prevSignal;

		_prevFast = fastVal;
		_prevSlow = slowVal;

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
