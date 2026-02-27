namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Manual Trading Lightweight Utility strategy: WMA trend following.
/// Buys when price is above WMA and rising, sells when below and falling.
/// </summary>
public class ManualTradingLightweightUtilityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wmaPeriod;

	private decimal _prevWma;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int WmaPeriod { get => _wmaPeriod.Value; set => _wmaPeriod.Value = value; }

	public ManualTradingLightweightUtilityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_wmaPeriod = Param(nameof(WmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "Weighted MA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var wma = new WeightedMovingAverage { Length = WmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wma)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;

		if (_hasPrev)
		{
			if (close > wma && wma > _prevWma && Position <= 0) BuyMarket();
			else if (close < wma && wma < _prevWma && Position >= 0) SellMarket();
		}

		_prevWma = wma;
		_hasPrev = true;
	}
}
