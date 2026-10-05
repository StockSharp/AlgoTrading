using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Average high-low range IBS reversal strategy.
/// The buy threshold is the highest high of the last Length candles minus 2.5 times the SMA of the candle range over Length
/// candles. When the close has stayed below that threshold for BarsBelowThreshold consecutive candles and the internal bar
/// strength (close - low) / (high - low) is below IbsBuyThreshold, a long opens, provided the candle lies between StartTime and
/// EndTime. The long closes when a close exceeds the previous candle's high. Long only.
/// </summary>
public class AverageHighLowRangeIbsReversalStrategy : Strategy
{
	private const decimal _rangeMultiplier = 2.5m;

	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _barsBelowThreshold;
	private readonly StrategyParam<decimal> _ibsBuyThreshold;
	private readonly StrategyParam<DateTime> _startTime;
	private readonly StrategyParam<DateTime> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _rangeAverage;
	private int _barsBelow;
	private decimal? _prevHigh;

	/// <summary>
	/// Lookback of the range average and the highest high.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Consecutive closes below the threshold required for an entry.
	/// </summary>
	public int BarsBelowThreshold
	{
		get => _barsBelowThreshold.Value;
		set => _barsBelowThreshold.Value = value;
	}

	/// <summary>
	/// Maximum internal bar strength for an entry.
	/// </summary>
	public decimal IbsBuyThreshold
	{
		get => _ibsBuyThreshold.Value;
		set => _ibsBuyThreshold.Value = value;
	}

	/// <summary>
	/// Start of the trading window.
	/// </summary>
	public DateTime StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End of the trading window.
	/// </summary>
	public DateTime EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
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
	public AverageHighLowRangeIbsReversalStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback of the range average and the highest high", "Indicators");

		_barsBelowThreshold = Param(nameof(BarsBelowThreshold), 2)
			.SetGreaterThanZero()
			.SetDisplay("Bars Below Threshold", "Consecutive closes below the threshold required for an entry", "Signals");

		_ibsBuyThreshold = Param(nameof(IbsBuyThreshold), 0.2m)
			.SetDisplay("IBS Buy Threshold", "Maximum internal bar strength for an entry", "Signals");

		_startTime = Param(nameof(StartTime), new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("Start Time", "Start of the trading window", "Time");

		_endTime = Param(nameof(EndTime), new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("End Time", "End of the trading window", "Time");

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
		_rangeAverage = null;
		_barsBelow = 0;
		_prevHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barsBelow = 0;
		_prevHigh = null;

		var highest = new Highest { Length = Length };
		_rangeAverage = new SimpleMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		var averageRange = _rangeAverage.Process(range, candle.ServerTime, true).ToDecimal();

		var prevHigh = _prevHigh;
		_prevHigh = candle.HighPrice;

		if (!_rangeAverage.IsFormed)
			return;

		var close = candle.ClosePrice;
		var threshold = highest - _rangeMultiplier * averageRange;
		_barsBelow = close < threshold ? _barsBelow + 1 : 0;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (prevHigh is decimal ph && close > ph)
				SellMarket(Position);

			return;
		}

		var ibs = range > 0m ? (close - candle.LowPrice) / range : 0.5m;
		var inWindow = candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

		if (Position == 0 && inWindow && _barsBelow >= BarsBelowThreshold && ibs < IbsBuyThreshold)
			BuyMarket(Volume);
	}
}
