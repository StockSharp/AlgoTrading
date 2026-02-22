using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MaxProfitMinLossOptionsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<decimal> _trailProfitPerc;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _maFast;
	private ExponentialMovingAverage _maSlow;
	private RelativeStrengthIndex _rsi;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal StopLossPerc { get => _stopLossPerc.Value; set => _stopLossPerc.Value = value; }
	public decimal TrailProfitPerc { get => _trailProfitPerc.Value; set => _trailProfitPerc.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaxProfitMinLossOptionsStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9);
		_slowLength = Param(nameof(SlowLength), 21);
		_rsiLength = Param(nameof(RsiLength), 14);
		_stopLossPerc = Param(nameof(StopLossPerc), 1m);
		_trailProfitPerc = Param(nameof(TrailProfitPerc), 4m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_maFast = new ExponentialMovingAverage { Length = FastLength };
		_maSlow = new ExponentialMovingAverage { Length = SlowLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		_entryPrice = 0;
		_highestPrice = 0;
		_lowestPrice = decimal.MaxValue;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_maFast, _maSlow, _rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maFast, decimal maSlow, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_maFast.IsFormed || !_maSlow.IsFormed || !_rsi.IsFormed)
			return;

		var bullishTrend = maFast > maSlow;
		var bearishTrend = maFast < maSlow;

		if (bullishTrend && rsi > 30 && rsi < 70 && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_highestPrice = candle.ClosePrice;
		}
		else if (bearishTrend && rsi > 30 && rsi < 70 && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_lowestPrice = candle.ClosePrice;
		}

		if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.ClosePrice);
			var stop = _entryPrice * (1m - StopLossPerc / 100m);
			var trail = _highestPrice * (1m - TrailProfitPerc / 100m);
			var exit = Math.Max(stop, trail);
			if (candle.ClosePrice <= exit)
				SellMarket();
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.ClosePrice);
			var stop = _entryPrice * (1m + StopLossPerc / 100m);
			var trail = _lowestPrice * (1m + TrailProfitPerc / 100m);
			var exit = Math.Min(stop, trail);
			if (candle.ClosePrice >= exit)
				BuyMarket();
		}
	}
}
