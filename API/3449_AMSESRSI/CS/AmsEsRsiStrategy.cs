namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Morning/Evening Star + RSI strategy.
/// Buys on morning star with low RSI, sells on evening star with high RSI.
/// </summary>
public class AmsEsRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLow;
	private readonly StrategyParam<decimal> _rsiHigh;

	private ICandleMessage _prevCandle;
	private ICandleMessage _prevPrevCandle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiLow { get => _rsiLow.Value; set => _rsiLow.Value = value; }
	public decimal RsiHigh { get => _rsiHigh.Value; set => _rsiHigh.Value = value; }

	public AmsEsRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_rsiLow = Param(nameof(RsiLow), 40m)
			.SetDisplay("RSI Low", "RSI oversold threshold", "Signals");
		_rsiHigh = Param(nameof(RsiHigh), 60m)
			.SetDisplay("RSI High", "RSI overbought threshold", "Signals");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCandle = null;
		_prevPrevCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		_prevPrevCandle = null;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_prevCandle != null && _prevPrevCandle != null)
		{
			var prevBody = Math.Abs(_prevCandle.ClosePrice - _prevCandle.OpenPrice);
			var prevRange = _prevCandle.HighPrice - _prevCandle.LowPrice;
			var isSmallBody = prevRange > 0 && prevBody < prevRange * 0.3m;

			var firstBearish = _prevPrevCandle.OpenPrice > _prevPrevCandle.ClosePrice;
			var currBullish = candle.ClosePrice > candle.OpenPrice;
			var isMorningStar = firstBearish && isSmallBody && currBullish;

			var firstBullish = _prevPrevCandle.ClosePrice > _prevPrevCandle.OpenPrice;
			var currBearish = candle.OpenPrice > candle.ClosePrice;
			var isEveningStar = firstBullish && isSmallBody && currBearish;

			if (isMorningStar && rsiValue < RsiLow && Position <= 0)
				BuyMarket();
			else if (isEveningStar && rsiValue > RsiHigh && Position >= 0)
				SellMarket();
		}

		_prevPrevCandle = _prevCandle;
		_prevCandle = candle;
	}
}
