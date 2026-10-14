using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enhanced time segmented volume strategy.
/// TSV is the sum over TsvLength candles of the close change multiplied by the candle volume, and its SMA over MaLength is the
/// signal line. TSV above the signal line and above zero goes long, TSV below the signal line and below zero goes short; an
/// opposite signal reverses the position.
/// </summary>
public class EnhancedTimeSegmentedVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _tsvLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _tsvSum;
	private SimpleMovingAverage _tsvAverage;
	private decimal? _prevClose;

	/// <summary>
	/// Number of candles summed into TSV.
	/// </summary>
	public int TsvLength
	{
		get => _tsvLength.Value;
		set => _tsvLength.Value = value;
	}

	/// <summary>
	/// Length of the TSV moving average.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
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
	public EnhancedTimeSegmentedVolumeStrategy()
	{
		_tsvLength = Param(nameof(TsvLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("TSV Length", "Number of candles summed into TSV", "Indicators");

		_maLength = Param(nameof(MaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length of the TSV moving average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;

		// The SMA times its length gives the rolling sum.
		_tsvSum = new SimpleMovingAverage { Length = TsvLength };
		_tsvAverage = new SimpleMovingAverage { Length = MaLength };

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

		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (prevClose is not decimal pc)
			return;

		var flow = (candle.ClosePrice - pc) * candle.TotalVolume;
		var sumValue = _tsvSum.Process(new DecimalIndicatorValue(_tsvSum, flow, candle.OpenTime) { IsFinal = true });

		if (!_tsvSum.IsFormed)
			return;

		var tsv = sumValue.GetValue<decimal>() * TsvLength;
		var averageValue = _tsvAverage.Process(new DecimalIndicatorValue(_tsvAverage, tsv, candle.OpenTime) { IsFinal = true });

		if (!_tsvAverage.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var average = averageValue.GetValue<decimal>();

		if (tsv > average && tsv > 0m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (tsv < average && tsv < 0m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
