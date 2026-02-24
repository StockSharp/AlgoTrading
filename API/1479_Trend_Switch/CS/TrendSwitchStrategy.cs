using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified trend switch strategy using EMA slope and adaptive moving average crossover.
/// Goes long when slope is positive and fast MA above slow MA, short on reverse.
/// </summary>
public class TrendSwitchStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();
	private decimal _prevEma;
	private decimal _prevWma;
	private decimal _entryPrice;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendSwitchStrategy()
	{
		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback period", "General");

		_threshold = Param(nameof(Threshold), 0.05m)
			.SetDisplay("Slope Threshold", "Min slope ratio for trend", "General");

		_stopLoss = Param(nameof(StopLoss), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_prevEma = 0;
		_prevWma = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = Length };
		var wma = new WeightedMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, wma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, wma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal wmaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);
		if (_closes.Count > Length + 1)
			_closes.RemoveAt(0);

		if (_prevEma == 0 || _prevWma == 0 || _closes.Count < Length)
		{
			_prevEma = emaVal;
			_prevWma = wmaVal;
			return;
		}

		// Calculate simple slope: change in EMA relative to price
		var slope = _prevEma > 0 ? (emaVal - _prevEma) / _prevEma * 100m : 0m;

		var upTrend = slope > Threshold && emaVal > wmaVal;
		var downTrend = slope < -Threshold && emaVal < wmaVal;

		// Check exits first
		if (Position > 0)
		{
			var stop = _entryPrice * (1m - StopLoss / 100m);
			if (candle.LowPrice <= stop || downTrend)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			var stop = _entryPrice * (1m + StopLoss / 100m);
			if (candle.HighPrice >= stop || upTrend)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entries
		if (Position == 0)
		{
			if (upTrend)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (downTrend)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevEma = emaVal;
		_prevWma = wmaVal;
	}
}
