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
/// Moving average crossover strategy converted from MetaTrader 5 script.
/// Implements dual SMA crossover signals.
/// </summary>
public class MaCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrevValues;

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
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public MaCrossStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Period for the fast moving average", "Moving Averages")
			.SetOptimize(2, 20, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Period for the slow moving average", "Moving Averages")
			.SetOptimize(5, 60, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrevValues = false;

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_hasPrevValues)
		{
			var crossUp = _prevFast <= _prevSlow && fast > slow;
			var crossDown = _prevFast >= _prevSlow && fast < slow;

			if (crossUp && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (crossDown && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrevValues = true;
	}
}
