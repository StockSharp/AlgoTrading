using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Z-Score indicator for mean reversion trading.
/// Z-Score measures the distance from the price to its moving average in standard deviations.
/// </summary>
public class ZScoreStrategy : Strategy
{
	private readonly StrategyParam<decimal> _zScoreEntryThreshold;
	private readonly StrategyParam<decimal> _zScoreExitThreshold;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevZScore;
	private int _cooldown;

	/// <summary>
	/// Z-Score threshold for entry (default: 2.0)
	/// </summary>
	public decimal ZScoreEntryThreshold
	{
		get => _zScoreEntryThreshold.Value;
		set => _zScoreEntryThreshold.Value = value;
	}

	/// <summary>
	/// Z-Score threshold for exit (default: 0.5)
	/// </summary>
	public decimal ZScoreExitThreshold
	{
		get => _zScoreExitThreshold.Value;
		set => _zScoreExitThreshold.Value = value;
	}

	/// <summary>
	/// Period for Moving Average calculation (default: 20)
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period for Standard Deviation calculation (default: 20)
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize the Z-Score strategy.
	/// </summary>
	public ZScoreStrategy()
	{
		_zScoreEntryThreshold = Param(nameof(ZScoreEntryThreshold), 1.5m)
			.SetDisplay("Z-Score Entry", "Distance from mean in std devs for entry", "Z-Score")
			.SetOptimize(1.5m, 3.0m, 0.5m);

		_zScoreExitThreshold = Param(nameof(ZScoreExitThreshold), 0.5m)
			.SetDisplay("Z-Score Exit", "Distance from mean in std devs for exit", "Z-Score")
			.SetOptimize(0.0m, 1.0m, 0.2m);

		_maPeriod = Param(nameof(MAPeriod), 10)
			.SetDisplay("MA Period", "Period for Moving Average", "Indicators")
			.SetOptimize(10, 50, 5);

		_stdDevPeriod = Param(nameof(StdDevPeriod), 10)
			.SetDisplay("StdDev Period", "Period for Standard Deviation", "Indicators")
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevZScore = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevZScore = 0;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var stdDev = new StandardDeviation { Length = StdDevPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stdDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stdDevValue == 0)
			return;

		var zScore = (candle.ClosePrice - maValue) / stdDevValue;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevZScore = zScore;
			return;
		}

		if (Position == 0)
		{
			// Entry: price far below mean -> buy, far above -> sell
			if (zScore < -ZScoreEntryThreshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (zScore > ZScoreEntryThreshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			// Exit long: z-score crossed above exit threshold
			if (zScore > ZScoreExitThreshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			// Exit short: z-score crossed below negative exit threshold
			if (zScore < -ZScoreExitThreshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		_prevZScore = zScore;
	}
}
