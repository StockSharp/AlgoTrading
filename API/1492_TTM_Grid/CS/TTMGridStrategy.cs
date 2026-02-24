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
/// TTM-based grid trading strategy.
/// Uses fast and slow EMA to define a channel, places grid trades within the channel.
/// </summary>
public class TTMGridStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _gridLevels;
	private readonly StrategyParam<decimal> _gridSpacing;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _gridBasePrice;
	private int _gridDirection = -1;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int GridLevels { get => _gridLevels.Value; set => _gridLevels.Value = value; }
	public decimal GridSpacing { get => _gridSpacing.Value; set => _gridSpacing.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TTMGridStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period", "Indicators");

		_gridLevels = Param(nameof(GridLevels), 5)
			.SetDisplay("Grid Levels", "Number of price levels in the grid", "Strategy");

		_gridSpacing = Param(nameof(GridSpacing), 0.005m)
			.SetDisplay("Grid Spacing", "Distance between grid levels (fraction)", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_gridBasePrice = 0m;
		_gridDirection = -1;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, OnProcess).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Use fast/slow EMA as channel boundaries
		var lowMa = Math.Min(fastMa, slowMa);
		var highMa = Math.Max(fastMa, slowMa);

		var range = highMa - lowMa;
		if (range <= 0) return;

		var lowThird = lowMa + range / 3m;
		var highThird = lowMa + 2m * range / 3m;

		var currentState = candle.ClosePrice > highThird
			? 1
			: candle.ClosePrice < lowThird ? 0 : -1;

		if (currentState != -1 && currentState != _gridDirection)
		{
			_gridBasePrice = candle.ClosePrice;
			_gridDirection = currentState;
		}

		if (_gridDirection == -1 || _gridBasePrice == 0)
			return;

		for (var i = 1; i <= GridLevels; i++)
		{
			var multiplier = i * GridSpacing;

			if (_gridDirection == 1)
			{
				var buyLevel = _gridBasePrice * (1 - multiplier);
				var sellLevel = _gridBasePrice * (1 + multiplier);

				if (candle.LowPrice <= buyLevel)
					BuyMarket();

				if (candle.HighPrice >= sellLevel && Position > 0)
					SellMarket();
			}
			else
			{
				var sellLevel = _gridBasePrice * (1 - multiplier);
				var buyLevel = _gridBasePrice * (1 + multiplier);

				if (candle.LowPrice <= sellLevel)
					SellMarket();

				if (candle.HighPrice >= buyLevel && Position < 0)
					BuyMarket();
			}
		}
	}
}
