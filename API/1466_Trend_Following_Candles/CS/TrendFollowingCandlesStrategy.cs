using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following using a simple moving average and basic candlestick patterns.
/// Enters long when price is above MA with bullish candle and breaks pivot resistance.
/// Enters short when price is below MA with bearish candle and breaks pivot support.
/// </summary>
public class TrendFollowingCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrendFollowingCandlesStrategy"/>.
	/// </summary>
	public TrendFollowingCandlesStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving average period", "General")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ma = default;
		_prevHigh = default;
		_prevLow = default;
		_prevClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_prevHigh == null || _prevLow == null || _prevClose == null)
		{
				_prevHigh = candle.HighPrice;
				_prevLow = candle.LowPrice;
				_prevClose = candle.ClosePrice;
				return;
		}

		var pivot = (_prevHigh.Value + _prevLow.Value + _prevClose.Value) / 3m;
		var r1 = pivot * 2m - _prevLow.Value;
		var s1 = pivot * 2m - _prevHigh.Value;

		var bullish = candle.ClosePrice > candle.OpenPrice;
		var bearish = candle.ClosePrice < candle.OpenPrice;
		var uptrend = candle.ClosePrice > maValue;
		var downtrend = candle.ClosePrice < maValue;

		if (uptrend && bullish && candle.ClosePrice > r1 && Position <= 0)
		{
				BuyMarket();
		}
		else if (downtrend && bearish && candle.ClosePrice < s1 && Position >= 0)
		{
				SellMarket();
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;
	}
}
