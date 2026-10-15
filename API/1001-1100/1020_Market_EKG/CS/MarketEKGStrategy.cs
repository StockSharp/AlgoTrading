using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MarketEKGStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _deviationThresholdPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private ICandleMessage _prev3;
	private int _barsFromSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal DeviationThresholdPercent { get => _deviationThresholdPercent.Value; set => _deviationThresholdPercent.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public MarketEKGStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
		_deviationThresholdPercent = Param(nameof(DeviationThresholdPercent), 0.15m);
		_cooldownBars = Param(nameof(CooldownBars), 16);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev1 = null;
		_prev2 = null;
		_prev3 = null;
		_barsFromSignal = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prev1 = null;
		_prev2 = null;
		_prev3 = null;
		_barsFromSignal = CooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prev1 != null && _prev2 != null && _prev3 != null)
		{
			var avgClose = (_prev3.ClosePrice + _prev2.ClosePrice) / 2m;
			var diffClose = avgClose - _prev1.ClosePrice;
			var basePrice = _prev1.ClosePrice;
			var diffPercent = basePrice == 0m ? 0m : Math.Abs(diffClose) / basePrice * 100m;
			_barsFromSignal++;
			var canSignal = _barsFromSignal >= CooldownBars;

			if (canSignal && diffClose > 0 && diffPercent >= DeviationThresholdPercent && Position <= 0)
			{
				BuyMarket();
				_barsFromSignal = 0;
			}
			else if (canSignal && diffClose < 0 && diffPercent >= DeviationThresholdPercent && Position >= 0)
			{
				SellMarket();
				_barsFromSignal = 0;
			}
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = candle;
	}
}
