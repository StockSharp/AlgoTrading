using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bar Range Strategy - enters long when bar range percentile is high and closes after a fixed number of bars.
/// </summary>
public class BarRangeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _percentRankThreshold;
	private readonly StrategyParam<int> _exitBars;

	private readonly Queue<decimal> _ranges = [];
	private int _barsSinceEntry;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period for percent rank calculation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Percent rank threshold for entry.
	/// </summary>
	public decimal PercentRankThreshold
	{
		get => _percentRankThreshold.Value;
		set => _percentRankThreshold.Value = value;
	}

	/// <summary>
	/// Number of bars to hold the position before exit.
	/// </summary>
	public int ExitBars
	{
		get => _exitBars.Value;
		set => _exitBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BarRangeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Percent rank lookback", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_percentRankThreshold = Param(nameof(PercentRankThreshold), 95m)
			.SetDisplay("Percent Rank Threshold", "Minimum percentile for entry", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(80m, 99m, 1m);

		_exitBars = Param(nameof(ExitBars), 1)
			.SetGreaterThanZero()
			.SetDisplay("Exit Bars", "Bars to hold the position", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);
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

		_ranges.Clear();
		_barsSinceEntry = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position > 0)
		{
		_barsSinceEntry++;
		if (_barsSinceEntry >= ExitBars)
		{
		ClosePosition();
		return;
		}
		}

		var range = candle.HighPrice - candle.LowPrice;

		_ranges.Enqueue(range);
		if (_ranges.Count > LookbackPeriod)
		_ranges.Dequeue();

		if (_ranges.Count < LookbackPeriod)
		return;

		var count = 0;
		foreach (var r in _ranges)
		{
		if (r <= range)
		count++;
		}

		var percentRank = (decimal)count / LookbackPeriod * 100m;

		if (percentRank >= PercentRankThreshold && candle.ClosePrice < candle.OpenPrice && Position <= 0)
		{
		RegisterBuy();
		_barsSinceEntry = 0;
		}
	}
}

