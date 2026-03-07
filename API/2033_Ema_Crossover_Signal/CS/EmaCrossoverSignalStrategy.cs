using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades EMA crossovers with optional separate entry and exit permissions for long and short positions.
/// </summary>
public class EmaCrossoverSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isInitialized;
	private bool _wasFastAboveSlow;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmaCrossoverSignalStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Length of the fast EMA", "EMA");

		_slowPeriod = Param(nameof(SlowPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Length of the slow EMA", "EMA");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_isInitialized = default;
		_wasFastAboveSlow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, Process)
			.Start();
	}

	private void Process(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_wasFastAboveSlow = fastValue > slowValue;
			_isInitialized = true;
			return;
		}

		var isFastAboveSlow = fastValue > slowValue;

		if (_wasFastAboveSlow != isFastAboveSlow)
		{
			if (isFastAboveSlow)
			{
				// Upward crossover - buy signal
				if (Position < 0)
					BuyMarket();
				if (Position <= 0)
					BuyMarket();
			}
			else
			{
				// Downward crossover - sell signal
				if (Position > 0)
					SellMarket();
				if (Position >= 0)
					SellMarket();
			}

			_wasFastAboveSlow = isFastAboveSlow;
		}
	}
}
