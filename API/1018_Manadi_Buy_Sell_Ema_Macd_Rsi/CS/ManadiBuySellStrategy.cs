using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ManadiBuySellStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private RelativeStrengthIndex _rsi;

	private decimal _prevEmaFast;
	private decimal _prevEmaSlow;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ManadiBuySellStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 9);
		_slowEmaLength = Param(nameof(SlowEmaLength), 21);
		_rsiLength = Param(nameof(RsiLength), 14);
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.03m);
		_stopLossPercent = Param(nameof(StopLossPercent), 0.015m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		_prevEmaFast = 0m;
		_prevEmaSlow = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_rsi.IsFormed)
		{
			_prevEmaFast = fastEma;
			_prevEmaSlow = slowEma;
			return;
		}

		if (_prevEmaFast == 0 || _prevEmaSlow == 0)
		{
			_prevEmaFast = fastEma;
			_prevEmaSlow = slowEma;
			return;
		}

		var bullCross = _prevEmaFast <= _prevEmaSlow && fastEma > slowEma;
		var bearCross = _prevEmaFast >= _prevEmaSlow && fastEma < slowEma;

		var longCondition = bullCross && rsi < 70 && rsi > 40;
		var shortCondition = bearCross && rsi > 30 && rsi < 60;

		if (longCondition && Position <= 0)
		{
			BuyMarket();
			var close = candle.ClosePrice;
			_stopPrice = close * (1m - StopLossPercent);
			_takeProfitPrice = close * (1m + TakeProfitPercent);
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket();
			var close = candle.ClosePrice;
			_stopPrice = close * (1m + StopLossPercent);
			_takeProfitPrice = close * (1m - TakeProfitPercent);
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
				BuyMarket();
		}

		_prevEmaFast = fastEma;
		_prevEmaSlow = slowEma;
	}
}
