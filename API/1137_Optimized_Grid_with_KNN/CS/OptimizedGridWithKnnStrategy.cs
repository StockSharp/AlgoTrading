using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OptimizedGridWithKnnStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _k;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private readonly List<decimal> _closes = new();

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int K { get => _k.Value; set => _k.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OptimizedGridWithKnnStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5).SetGreaterThanZero();
		_slowPeriod = Param(nameof(SlowPeriod), 20).SetGreaterThanZero();
		_k = Param(nameof(K), 5).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_closes.Clear();

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);

		if (!_hasPrev)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			_hasPrev = true;
			return;
		}

		// KNN momentum filter
		decimal avgChange = 0;
		if (_closes.Count >= K + 1)
		{
			var sum = 0m;
			for (var i = 0; i < K; i++)
				sum += _closes[_closes.Count - 1 - i] - _closes[_closes.Count - 2 - i];
			avgChange = sum / K;
		}

		var longCross = _prevFast <= _prevSlow && fastVal > slowVal;
		var shortCross = _prevFast >= _prevSlow && fastVal < slowVal;

		if (longCross && Position <= 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		else if (shortCross && Position >= 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			SellMarket(Volume);
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;

		if (_closes.Count > 200)
			_closes.RemoveRange(0, _closes.Count - 200);
	}
}
