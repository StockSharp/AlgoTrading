namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Sample Trailingstop MT5 strategy: EMA + RSI confirmation.
/// Buys when close above EMA and RSI above 50, sells when close below EMA and RSI below 50.
/// </summary>
public class SampleTrailingstopMt5Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _prevClose;
	private decimal _prevEma;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public SampleTrailingstopMt5Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
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
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevClose <= _prevEma && candle.ClosePrice > emaValue && rsiValue > 55 && Position <= 0)
				BuyMarket();
			else if (_prevClose >= _prevEma && candle.ClosePrice < emaValue && rsiValue < 45 && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevEma = emaValue;
		_hasPrev = true;
	}
}
