using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class Mfs3BarsPatternStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private decimal _stopPrice;
	private decimal _takePrice;

	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Mfs3BarsPatternStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 20);
		_riskReward = Param(nameof(RiskReward), 2m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = SmaLength };
		_prev1 = null;
		_prev2 = null;
		_stopPrice = 0;
		_takePrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed || _prev1 == null || _prev2 == null)
		{
			_prev2 = _prev1;
			_prev1 = candle;
			return;
		}

		// 3-bar pattern: big bullish bar, small pullback, bullish confirmation
		var bar1Bullish = _prev2.ClosePrice > _prev2.OpenPrice;
		var bar1Body = Math.Abs(_prev2.ClosePrice - _prev2.OpenPrice);
		var bar2Bearish = _prev1.ClosePrice < _prev1.OpenPrice;
		var bar2Body = Math.Abs(_prev1.ClosePrice - _prev1.OpenPrice);
		var bar3Bullish = candle.ClosePrice > candle.OpenPrice;

		var pattern = bar1Bullish && bar1Body > 0 && bar2Bearish && bar2Body < bar1Body * 0.5m && bar3Bullish;

		if (pattern && Position <= 0 && candle.ClosePrice < smaValue)
		{
			BuyMarket();
			_stopPrice = _prev2.LowPrice;
			_takePrice = candle.ClosePrice + (candle.ClosePrice - _stopPrice) * RiskReward;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket();
		}

		_prev2 = _prev1;
		_prev1 = candle;
	}
}
