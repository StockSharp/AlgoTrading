using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DLMv FX Fish Grid strategy. Uses Highest/Lowest range with Fisher transform crossover.
/// </summary>
public class DlmvFxFishGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal? _prevFish;
	private decimal _prevValue;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public DlmvFxFishGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Lookback period for high/low range", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFish = null;
		_prevValue = 0m;

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = high - low;
		var midPrice = (candle.HighPrice + candle.LowPrice) / 2m;

		var normalized = range != 0m ? (midPrice - low) / range : 0.5m;
		var value = 0.66m * (normalized - 0.5m) + 0.67m * _prevValue;
		value = Math.Min(Math.Max(value, -0.999m), 0.999m);

		var ratio = (double)((1m + value) / (1m - value));
		var fish = 0.5m * (decimal)Math.Log(ratio);

		_prevValue = value;

		if (_prevFish == null)
		{
			_prevFish = fish;
			return;
		}

		// Fisher crosses zero from below → buy
		if (_prevFish.Value < 0m && fish >= 0m && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Fisher crosses zero from above → sell
		else if (_prevFish.Value > 0m && fish <= 0m && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevFish = fish;
	}
}
