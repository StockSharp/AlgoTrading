using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend arrows strategy based on breakouts of recent extremes.
/// Detects when price moves above the highest close or below the lowest close
/// over the specified period and trades in the direction of the breakout.
/// </summary>
public class TrendArrowsStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevTrendUp;
	private bool _prevTrendDown;
	private decimal? _prevHighest;
	private decimal? _prevLowest;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendArrowsStrategy()
	{
		_period = Param(nameof(Period), 15)
			.SetDisplay("Period", "Number of bars for extreme calculation", "Parameters")
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of candles", "Parameters");
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

		_prevTrendUp = false;
		_prevTrendDown = false;
		_prevHighest = null;
		_prevLowest = null;

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestVal, decimal lowestVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Use previous bar's highest/lowest to detect breakout
		if (_prevHighest is null || _prevLowest is null)
		{
			_prevHighest = highestVal;
			_prevLowest = lowestVal;
			return;
		}

		var trendUp = false;
		var trendDown = false;

		if (candle.ClosePrice > _prevHighest.Value)
			trendUp = true;
		else if (candle.ClosePrice < _prevLowest.Value)
			trendDown = true;
		else
		{
			trendUp = _prevTrendUp;
			trendDown = _prevTrendDown;
		}

		// Buy when up trend appears
		if (!_prevTrendUp && trendUp && Position <= 0)
			BuyMarket();

		// Sell when down trend appears
		if (!_prevTrendDown && trendDown && Position >= 0)
			SellMarket();

		_prevTrendUp = trendUp;
		_prevTrendDown = trendDown;
		_prevHighest = highestVal;
		_prevLowest = lowestVal;
	}
}
