using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bar Range strategy.
/// Long only: buys a bearish candle (close below open) whose high-low range has a percent rank of at least PercentRankThreshold
/// among the previous LookbackPeriod ranges, and closes the long after ExitBars bars.
/// </summary>
public class BarRangeStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _percentRankThreshold;
	private readonly StrategyParam<int> _exitBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _ranges = new();
	private int _barsInPosition;

	/// <summary>
	/// Previous bars the range is ranked against.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Minimum percent rank of the range that allows an entry.
	/// </summary>
	public decimal PercentRankThreshold
	{
		get => _percentRankThreshold.Value;
		set => _percentRankThreshold.Value = value;
	}

	/// <summary>
	/// Bars after which the long is closed.
	/// </summary>
	public int ExitBars
	{
		get => _exitBars.Value;
		set => _exitBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BarRangeStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Previous bars the range is ranked against", "Indicators");

		_percentRankThreshold = Param(nameof(PercentRankThreshold), 95m)
			.SetRange(0m, 100m)
			.SetDisplay("Percent Rank Threshold", "Minimum percent rank of the range that allows an entry", "Indicators");

		_exitBars = Param(nameof(ExitBars), 1)
			.SetGreaterThanZero()
			.SetDisplay("Exit Bars", "Bars after which the long is closed", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ranges.Clear();
		_barsInPosition = 0;

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

		var range = candle.HighPrice - candle.LowPrice;

		// Percent rank: share of the previous LookbackPeriod ranges that do not exceed the current one.
		decimal? rank = null;
		if (_ranges.Count == LookbackPeriod)
			rank = 100m * _ranges.Count(r => r <= range) / LookbackPeriod;

		_ranges.Enqueue(range);
		while (_ranges.Count > LookbackPeriod)
			_ranges.Dequeue();

		if (Position > 0)
			_barsInPosition++;
		else
			_barsInPosition = 0;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (_barsInPosition >= ExitBars)
				SellMarket(Position);

			return;
		}

		if (Position == 0 && rank is decimal r && r >= PercentRankThreshold && candle.ClosePrice < candle.OpenPrice)
			BuyMarket(Volume);
	}
}
