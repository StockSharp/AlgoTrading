using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum Alligator strategy for Bitcoin.
/// </summary>
public class MomentumAlligator4hBitcoinStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAo;
	private bool _hasPrev;
	private decimal _entryPrice;

	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MomentumAlligator4hBitcoinStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 0.02m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAo = 0;
		_hasPrev = false;
		_entryPrice = 0;

		var jaw = new SmoothedMovingAverage { Length = 13 };
		var teeth = new SmoothedMovingAverage { Length = 8 };
		var lips = new SmoothedMovingAverage { Length = 5 };
		var ao = new AwesomeOscillator { ShortMa = { Length = 5 }, LongMa = { Length = 34 } };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ao, jaw, teeth, lips, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ao, decimal jaw, decimal teeth, decimal lips)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_hasPrev)
		{
			// AO increasing and positive - momentum building
			var aoMomentum = ao > _prevAo && ao > 0;

			if (aoMomentum && close > jaw && close > lips && Position <= 0)
			{
				BuyMarket();
				_entryPrice = close;
			}

			// AO decreasing and negative - bearish momentum
			var aoBearish = ao < _prevAo && ao < 0;
			if (aoBearish && close < jaw && close < lips && Position >= 0)
			{
				SellMarket();
				_entryPrice = close;
			}
		}

		if (Position > 0 && _entryPrice > 0)
		{
			var percentStop = _entryPrice * (1m - StopLossPercent);
			if (close < percentStop || close < teeth)
			{
				SellMarket();
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var percentStop = _entryPrice * (1m + StopLossPercent);
			if (close > percentStop || close > teeth)
			{
				BuyMarket();
			}
		}

		_prevAo = ao;
		_hasPrev = true;
	}
}
