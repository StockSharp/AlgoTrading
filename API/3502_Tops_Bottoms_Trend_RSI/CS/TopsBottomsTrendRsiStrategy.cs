namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Tops Bottoms Trend RSI strategy: RSI trend direction with EMA filter.
/// Buys when RSI above 50 and close above EMA, sells when RSI below 50 and close below EMA.
/// </summary>
public class TopsBottomsTrendRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevRsi;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public TopsBottomsTrendRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevRsi = 0;
		_hasPrev = false;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevRsi <= 45 && rsiValue > 45 && candle.ClosePrice > emaValue && Position <= 0)
				BuyMarket();
			else if (_prevRsi >= 55 && rsiValue < 55 && candle.ClosePrice < emaValue && Position >= 0)
				SellMarket();
		}

		_prevRsi = rsiValue;
		_hasPrev = true;
	}
}
