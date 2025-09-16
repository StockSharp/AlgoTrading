using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple crossover strategy inspired by ColorZerolagJCCX indicator.
/// Uses two moving averages and trades on crossovers.
/// </summary>
public class ColorZerolagJccxStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;

	private bool _initialized;
	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorZerolagJccxStrategy"/>.
	/// </summary>
	public ColorZerolagJccxStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Moving Average")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Moving Average")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = FastPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var wasFastAbove = _prevFast > _prevSlow;
		var isFastAbove = fast > slow;

		if (wasFastAbove && !isFastAbove && Position <= 0)
		{
			// Fast line crossed below slow line -> go long
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!wasFastAbove && isFastAbove && Position >= 0)
		{
			// Fast line crossed above slow line -> go short
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
