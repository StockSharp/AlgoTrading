using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class McGinleyDynamicImprovedStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _mdPrev;
	private ExponentialMovingAverage _ema;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public McGinleyDynamicImprovedStrategy()
	{
		_period = Param(nameof(Period), 14);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = Period };
		_mdPrev = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
			return;

		var close = candle.ClosePrice;

		// Calculate McGinley Dynamic
		decimal md;
		if (_mdPrev == null)
		{
			md = close;
		}
		else
		{
			var prev = _mdPrev.Value;
			if (prev == 0m) prev = close;
			var k = 0.6m;
			var period = (decimal)Period;
			var ratio = close / prev;
			var pow = (decimal)Math.Pow((double)ratio, 4.0);
			var denom = k * period * pow;
			if (denom == 0m) denom = 1m;
			md = prev + (close - prev) / denom;
		}
		_mdPrev = md;

		if (close > md && Position <= 0)
			BuyMarket();
		else if (close < md && Position >= 0)
			SellMarket();
	}
}
