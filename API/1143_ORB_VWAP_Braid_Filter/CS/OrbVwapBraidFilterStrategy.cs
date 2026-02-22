using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OrbVwapBraidFilterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _orHigh;
	private decimal? _orLow;
	private bool _rangeSet;
	private decimal _stopPrice;
	private decimal _takePrice;
	private DateTime _currentDay;

	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrbVwapBraidFilterStrategy()
	{
		_riskReward = Param(nameof(RiskReward), 1.5m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_orHigh = null; _orLow = null; _rangeSet = false; _currentDay = default;

		var ema1 = new ExponentialMovingAverage { Length = 3 };
		var ema2 = new ExponentialMovingAverage { Length = 7 };
		var ema3 = new ExponentialMovingAverage { Length = 14 };
		var vwap = new VolumeWeightedMovingAverage();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema1, ema2, ema3, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema1);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema1, decimal ema2, decimal ema3)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		if (_currentDay != day)
		{
			_currentDay = day; _orHigh = null; _orLow = null; _rangeSet = false;
		}

		var hour = candle.OpenTime.TimeOfDay.TotalHours;

		if (hour < 1)
		{
			_orHigh = _orHigh.HasValue ? Math.Max(_orHigh.Value, candle.HighPrice) : candle.HighPrice;
			_orLow = _orLow.HasValue ? Math.Min(_orLow.Value, candle.LowPrice) : candle.LowPrice;
			return;
		}

		if (!_rangeSet && _orHigh.HasValue && _orLow.HasValue)
			_rangeSet = true;

		// Braid filter: ema1 > ema2 > ema3 = bullish, opposite = bearish
		var bullBraid = ema1 > ema2 && ema2 > ema3;
		var bearBraid = ema1 < ema2 && ema2 < ema3;

		if (Position > 0 && (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice))
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice))
			BuyMarket(Math.Abs(Position));

		if (_rangeSet && Position == 0 && _orHigh.HasValue && _orLow.HasValue)
		{
			var range = _orHigh.Value - _orLow.Value;
			if (range > 0)
			{
				if (candle.ClosePrice > _orHigh.Value && bullBraid)
				{
					BuyMarket(Volume);
					_stopPrice = _orLow.Value;
					_takePrice = candle.ClosePrice + range * RiskReward;
					_rangeSet = false;
				}
				else if (candle.ClosePrice < _orLow.Value && bearBraid)
				{
					SellMarket(Volume);
					_stopPrice = _orHigh.Value;
					_takePrice = candle.ClosePrice - range * RiskReward;
					_rangeSet = false;
				}
			}
		}
	}
}
