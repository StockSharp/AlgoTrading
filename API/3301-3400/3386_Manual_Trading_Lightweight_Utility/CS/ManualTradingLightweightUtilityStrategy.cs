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
	private bool _wasBullish;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int WmaPeriod { get => _wmaPeriod.Value; set => _wmaPeriod.Value = value; }

	public ManualTradingLightweightUtilityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_wmaPeriod = Param(nameof(WmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "Weighted MA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevWma = 0m;
		_hasPrev = false;
		_wasBullish = false;
	}

	/// <inheritdoc />
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
		var isBullish = close > wma && wma > _prevWma;

		if (_hasPrev)
		{
			if (isBullish && !_wasBullish && Position <= 0)
				BuyMarket();
			else if (!isBullish && close < wma && wma < _prevWma && _wasBullish && Position >= 0)
				SellMarket();
		}

		_prevWma = wma;
		_hasPrev = true;
		_wasBullish = isBullish;
	}
}
