namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Range Follower strategy: ATR-based range breakout.
/// Tracks high/low range and enters when price breaks out by ATR threshold.
/// </summary>
public class RangeFollowerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _rangePeriod;

	private decimal _rangeHigh;
	private decimal _rangeLow;
	private int _barCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public int RangePeriod { get => _rangePeriod.Value; set => _rangePeriod.Value = value; }

	public RangeFollowerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");
		_rangePeriod = Param(nameof(RangePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Range Period", "Bars for range calculation", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_rangeHigh = 0m;
		_rangeLow = decimal.MaxValue;
		_barCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_rangeHigh = 0;
		_rangeLow = decimal.MaxValue;
		_barCount = 0;
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		_barCount++;

		if (candle.HighPrice > _rangeHigh) _rangeHigh = candle.HighPrice;
		if (candle.LowPrice < _rangeLow) _rangeLow = candle.LowPrice;

		if (_barCount < RangePeriod) return;

		var threshold = atrValue * 0.5m;

		if (close > _rangeHigh - threshold && Position <= 0)
		{
			BuyMarket();
			ResetRange();
		}
		else if (close < _rangeLow + threshold && Position >= 0)
		{
			SellMarket();
			ResetRange();
		}

		// Slide the range window
		if (_barCount > RangePeriod * 2)
			ResetRange();
	}

	private void ResetRange()
	{
		_rangeHigh = 0;
		_rangeLow = decimal.MaxValue;
		_barCount = 0;
	}
}
