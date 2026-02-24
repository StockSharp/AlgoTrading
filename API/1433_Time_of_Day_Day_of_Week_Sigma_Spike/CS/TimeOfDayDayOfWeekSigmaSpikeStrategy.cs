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
/// Time of day / day of week sigma spike strategy.
/// Calculates return z-score and buys on spikes, sells when spike subsides.
/// </summary>
public class TimeOfDayDayOfWeekSigmaSpikeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<int> _stdevLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private readonly List<decimal> _returns = new();

	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public int StdevLength { get => _stdevLength.Value; set => _stdevLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimeOfDayDayOfWeekSigmaSpikeStrategy()
	{
		_threshold = Param(nameof(Threshold), 2.0m)
			.SetGreaterThanZero();
		_stdevLength = Param(nameof(StdevLength), 20)
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0m;
		_returns.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var ret = candle.ClosePrice / _prevClose - 1m;
		_prevClose = candle.ClosePrice;

		_returns.Add(ret);
		if (_returns.Count > StdevLength)
			_returns.RemoveAt(0);

		if (_returns.Count < StdevLength)
			return;

		var mean = _returns.Average();
		var variance = _returns.Sum(r => (r - mean) * (r - mean)) / _returns.Count;
		var sd = (decimal)Math.Sqrt((double)variance);

		if (sd <= 0)
			return;

		var sigma = Math.Abs(ret / sd);

		// Check exits first
		if (Position > 0 && sigma < Threshold * 0.5m)
		{
			SellMarket();
			return;
		}
		else if (Position < 0 && sigma < Threshold * 0.5m)
		{
			BuyMarket();
			return;
		}

		// Entry on spike
		if (Position == 0 && sigma >= Threshold)
		{
			if (ret > 0)
				BuyMarket();
			else
				SellMarket();
		}
	}
}
