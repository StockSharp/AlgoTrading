using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple SMA crossover strategy.
/// Goes long when fast > medium > slow, short when fast less than medium less than slow.
/// Exits when fast crosses medium in opposite direction.
/// </summary>
public class TripleSmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevMed;
	private bool _hasPrev;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MediumPeriod { get => _mediumPeriod.Value; set => _mediumPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TripleSmaCrossoverStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");

		_mediumPeriod = Param(nameof(MediumPeriod), 10)
			.SetDisplay("Medium SMA", "Medium SMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 20)
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_prevFast = 0m;
		_prevMed = 0m;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var fast = new SimpleMovingAverage { Length = FastPeriod };
		var medium = new SimpleMovingAverage { Length = MediumPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, medium, slow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal med, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevMed = med;
			_hasPrev = true;
			return;
		}

		// Bullish alignment: fast > medium > slow
		if (fast > med && med > slow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Bearish alignment: fast < medium < slow
		else if (fast < med && med < slow && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit long when fast crosses below medium
		else if (Position > 0 && _prevFast >= _prevMed && fast < med)
		{
			SellMarket();
		}
		// Exit short when fast crosses above medium
		else if (Position < 0 && _prevFast <= _prevMed && fast > med)
		{
			BuyMarket();
		}

		_prevFast = fast;
		_prevMed = med;
	}
}
