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
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevAo;
	private bool _hasPrev;
	private decimal _entryPrice;
	private int _barsFromSignal;

	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MomentumAlligator4hBitcoinStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 0.02m).SetGreaterThanZero();
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 8).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevAo = 0m;
		_hasPrev = false;
		_entryPrice = 0m;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAo = 0;
		_hasPrev = false;
		_entryPrice = 0;
		_barsFromSignal = SignalCooldownBars;

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

		var close = candle.ClosePrice;
		_barsFromSignal++;

		if (_hasPrev)
		{
			var aoCrossUp = _prevAo <= 0m && ao > 0m;
			var aoCrossDown = _prevAo >= 0m && ao < 0m;
			var alligatorBull = lips > teeth && close > jaw;
			var alligatorBear = lips < teeth && close < jaw;

			if (_barsFromSignal >= SignalCooldownBars && aoCrossUp && alligatorBull && close > lips && Position <= 0)
			{
				BuyMarket();
				_entryPrice = close;
				_barsFromSignal = 0;
			}

			if (_barsFromSignal >= SignalCooldownBars && aoCrossDown && alligatorBear && close < lips && Position >= 0)
			{
				SellMarket();
				_entryPrice = close;
				_barsFromSignal = 0;
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
