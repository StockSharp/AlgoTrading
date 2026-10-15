namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// MA Break strategy: EMA breakout with impulse candle confirmation.
/// Buys when close crosses above EMA with strong bullish candle body.
/// Sells when close crosses below EMA with strong bearish candle body.
/// </summary>
public class MaBreakStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _bodyRatio;

	private decimal _prevClose;
	private decimal _prevEma;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public decimal BodyRatio { get => _bodyRatio.Value; set => _bodyRatio.Value = value; }

	public MaBreakStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for breakout", "Indicators");
		_bodyRatio = Param(nameof(BodyRatio), 0.7m)
			.SetDisplay("Body Ratio", "Min body/range ratio for impulse", "Pattern");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevEma = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevClose = 0;
		_prevEma = 0;
		_hasPrev = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var range = candle.HighPrice - candle.LowPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		if (_hasPrev && range > 0)
		{
			var isImpulse = body >= range * BodyRatio;

			if (_prevClose <= _prevEma && candle.ClosePrice > emaValue
				&& candle.ClosePrice > candle.OpenPrice && isImpulse && Position <= 0)
				BuyMarket();
			else if (_prevClose >= _prevEma && candle.ClosePrice < emaValue
				&& candle.ClosePrice < candle.OpenPrice && isImpulse && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevEma = emaValue;
		_hasPrev = true;
	}
}
