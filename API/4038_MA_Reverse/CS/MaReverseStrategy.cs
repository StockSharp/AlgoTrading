using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average reversal strategy converted from the MetaTrader MA_Reverse expert advisor.
/// Counts how many consecutive closes remain on one side of the SMA and opens a trade once the streak is long enough.
/// </summary>
public class MaReverseStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _streakThreshold;
	private readonly StrategyParam<decimal> _minimumDeviation;
	private readonly StrategyParam<DataType> _candleType;

	private int _streak;

	public MaReverseStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 14)
			.SetDisplay("SMA Period", "Number of candles used by the moving average.", "Indicator");

		_streakThreshold = Param(nameof(StreakThreshold), 3)
			.SetDisplay("Streak Threshold", "Number of consecutive closes required before reversing.", "Logic");

		_minimumDeviation = Param(nameof(MinimumDeviation), 0.0001m)
			.SetDisplay("Minimum Deviation", "Minimum distance between price and SMA to confirm the reversal.", "Logic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for SMA calculation.", "General");
	}

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public int StreakThreshold
	{
		get => _streakThreshold.Value;
		set => _streakThreshold.Value = value;
	}

	public decimal MinimumDeviation
	{
		get => _minimumDeviation.Value;
		set => _minimumDeviation.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_streak = 0;

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;
		var deviation = closePrice - smaValue;

		if (deviation == 0m)
		{
			_streak = 0;
			return;
		}

		if (deviation > 0m)
		{
			// Price above SMA
			if (_streak < 0)
				_streak = 0;
			_streak++;

			if (_streak >= StreakThreshold && deviation > MinimumDeviation)
			{
				// Long streak above SMA => sell (reversal)
				if (Position >= 0)
				{
					SellMarket();
					_streak = 0;
				}
			}
		}
		else
		{
			// Price below SMA
			if (_streak > 0)
				_streak = 0;
			_streak--;

			if (-_streak >= StreakThreshold && -deviation > MinimumDeviation)
			{
				// Long streak below SMA => buy (reversal)
				if (Position <= 0)
				{
					BuyMarket();
					_streak = 0;
				}
			}
		}
	}
}
