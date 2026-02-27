using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades when price breaks through recent high/low levels.
/// Tracks N-period high and low as support/resistance, enters on breakout.
/// Uses EMA as trend filter.
/// </summary>
public class SimpleLevelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _highestHigh;
	private decimal _lowestLow;
	private int _barCount;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public SimpleLevelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candle timeframe", "General");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetDisplay("Lookback", "Period for high/low levels", "Parameters");

		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetDisplay("EMA Period", "EMA period for trend filter", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highestHigh = decimal.MinValue;
		_lowestLow = decimal.MaxValue;
		_barCount = 0;

		var highest = new Highest { Length = LookbackPeriod };
		var lowest = new Lowest { Length = LookbackPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highLevel, decimal lowLevel, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;
		if (_barCount < 2)
		{
			_highestHigh = highLevel;
			_lowestLow = lowLevel;
			return;
		}

		var close = candle.ClosePrice;
		var prevHigh = _highestHigh;
		var prevLow = _lowestLow;

		// Breakout above previous resistance in uptrend
		if (close > prevHigh && close > emaValue && Position <= 0)
		{
			BuyMarket();
		}
		// Breakout below previous support in downtrend
		else if (close < prevLow && close < emaValue && Position >= 0)
		{
			SellMarket();
		}

		_highestHigh = highLevel;
		_lowestLow = lowLevel;
	}
}
