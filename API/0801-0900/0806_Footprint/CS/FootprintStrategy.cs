using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Footprint strategy based on aggressive buy/sell volume imbalance.
/// </summary>
public class FootprintStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _imbalancePercent;
	private readonly StrategyParam<bool> _useDailyTrendFilter;
	private readonly StrategyParam<int> _dailyTrendPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private bool _dailyTrendBullish;
	private bool _dailyTrendReady;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal ImbalancePercent { get => _imbalancePercent.Value; set => _imbalancePercent.Value = value; }
	public bool UseDailyTrendFilter { get => _useDailyTrendFilter.Value; set => _useDailyTrendFilter.Value = value; }
	public int DailyTrendPeriod { get => _dailyTrendPeriod.Value; set => _dailyTrendPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	public FootprintStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Bars carrying footprint buy/sell volume.", "General");
		_imbalancePercent = Param(nameof(ImbalancePercent), 25m)
			.SetNotNegative()
			.SetDisplay("Imbalance %", "Minimum excess of buy volume over sell volume.", "Signal");
		_useDailyTrendFilter = Param(nameof(UseDailyTrendFilter), false)
			.SetDisplay("Use Daily Trend", "Require daily close to be above its SMA.", "Filter");
		_dailyTrendPeriod = Param(nameof(DailyTrendPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Daily Trend Period", "Daily SMA period.", "Filter");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Built-in protective stop distance.", "Protection");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Built-in protective target distance.", "Protection");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		if (UseDailyTrendFilter)
			yield return (Security, TimeSpan.FromDays(1).TimeFrame());
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_dailyTrendBullish = false;
		_dailyTrendReady = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (StopLossPercent > 0m || TakeProfitPercent > 0m)
		{
			StartProtection(
				TakeProfitPercent > 0m ? new Unit(TakeProfitPercent, UnitTypes.Percent) : null,
				StopLossPercent > 0m ? new Unit(StopLossPercent, UnitTypes.Percent) : null);
		}

		if (UseDailyTrendFilter)
		{
			var dailySma = new SimpleMovingAverage { Length = DailyTrendPeriod };
			SubscribeCandles(TimeSpan.FromDays(1).TimeFrame())
				.Bind(dailySma, (candle, sma) =>
				{
					if (candle.State != CandleStates.Finished || !dailySma.IsFormed)
						return;

					_dailyTrendBullish = candle.ClosePrice > sma;
					_dailyTrendReady = true;
				})
				.Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || Position != 0)
			return;

		if (UseDailyTrendFilter && (!_dailyTrendReady || !_dailyTrendBullish))
			return;

		var buy = candle.BuyVolume ?? 0m;
		var sell = candle.SellVolume ?? 0m;

		if (buy <= 0m && sell <= 0m)
			return;

		var requiredBuy = sell * (1m + ImbalancePercent / 100m);

		// The documented footprint setup is contrarian: aggressive buyers dominate
		// while the candle itself still closes red.
		if (buy > requiredBuy && candle.ClosePrice < candle.OpenPrice)
			BuyMarket();
	}
}
