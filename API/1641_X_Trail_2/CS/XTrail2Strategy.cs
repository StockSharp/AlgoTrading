using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy based on X_trail_2.
/// Enters long when fast MA crosses above slow MA (confirmed over 2 bars),
/// enters short on the reverse crossover.
/// </summary>
public class XTrail2Strategy : Strategy
{
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _ma1Prev;
	private decimal? _ma2Prev;

	public int Ma1Length { get => _ma1Length.Value; set => _ma1Length.Value = value; }
	public int Ma2Length { get => _ma2Length.Value; set => _ma2Length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XTrail2Strategy()
	{
		_ma1Length = Param(nameof(Ma1Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Length of the fast MA", "Moving Averages");

		_ma2Length = Param(nameof(Ma2Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Length of the slow MA", "Moving Averages");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_ma1Prev = _ma2Prev = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new SimpleMovingAverage { Length = Ma1Length };
		var slow = new SimpleMovingAverage { Length = Ma2Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1, decimal ma2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_ma1Prev != null && _ma2Prev != null)
		{
			// Simple crossover detection
			if (ma1 > ma2 && _ma1Prev <= _ma2Prev)
			{
				if (Position <= 0)
					BuyMarket();
			}
			else if (ma1 < ma2 && _ma1Prev >= _ma2Prev)
			{
				if (Position >= 0)
					SellMarket();
			}
		}

		_ma1Prev = ma1;
		_ma2Prev = ma2;
	}
}
