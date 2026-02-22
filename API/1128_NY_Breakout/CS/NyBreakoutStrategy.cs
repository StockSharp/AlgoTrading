using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class NyBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _hi;
	private decimal? _lo;
	private bool _wasSession;
	private decimal _stopPrice;
	private decimal _takePrice;

	public decimal RewardRisk { get => _rewardRisk.Value; set => _rewardRisk.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NyBreakoutStrategy()
	{
		_rewardRisk = Param(nameof(RewardRisk), 2m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hi = null;
		_lo = null;
		_wasSession = false;
		_stopPrice = 0;
		_takePrice = 0;

		var sma = new SimpleMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var t = candle.OpenTime;
		// Use first hour of each day as session range
		var inSession = t.TimeOfDay.TotalHours >= 0 && t.TimeOfDay.TotalHours < 1;

		if (inSession)
		{
			_hi = _hi.HasValue ? Math.Max(_hi.Value, candle.HighPrice) : candle.HighPrice;
			_lo = _lo.HasValue ? Math.Min(_lo.Value, candle.LowPrice) : candle.LowPrice;
		}

		// Manage position
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Math.Abs(Position));
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		if (!inSession && _wasSession && _hi.HasValue && _lo.HasValue)
		{
			// Range just finished, ready for breakout
		}

		if (!inSession && _hi.HasValue && _lo.HasValue && Position == 0)
		{
			var hi = _hi.Value;
			var lo = _lo.Value;
			var range = hi - lo;

			if (range > 0)
			{
				if (candle.ClosePrice > hi)
				{
					BuyMarket(Volume);
					_stopPrice = lo;
					_takePrice = candle.ClosePrice + range * RewardRisk;
					_hi = null;
					_lo = null;
				}
				else if (candle.ClosePrice < lo)
				{
					SellMarket(Volume);
					_stopPrice = hi;
					_takePrice = candle.ClosePrice - range * RewardRisk;
					_hi = null;
					_lo = null;
				}
			}
		}

		_wasSession = inSession;

		// Reset range for new day
		if (inSession && !_wasSession)
		{
			_hi = candle.HighPrice;
			_lo = candle.LowPrice;
		}
	}
}
