namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Sample Detect Economic Calendar strategy: Parabolic SAR trend with EMA filter.
/// Buys when close above both SAR and EMA, sells when close below both.
/// </summary>
public class SampleDetectEconomicCalendarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevClose;
	private decimal _prevSar;
	private decimal _prevEma;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public SampleDetectEconomicCalendarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA filter period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var sar = new ParabolicSar();
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sar, ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			var aboveSar = candle.ClosePrice > sarValue;
			var belowSar = candle.ClosePrice < sarValue;
			var aboveEma = candle.ClosePrice > emaValue;
			var belowEma = candle.ClosePrice < emaValue;

			if (aboveSar && aboveEma && !(_prevClose > _prevSar && _prevClose > _prevEma) && Position <= 0)
				BuyMarket();
			else if (belowSar && belowEma && !(_prevClose < _prevSar && _prevClose < _prevEma) && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevSar = sarValue;
		_prevEma = emaValue;
		_hasPrev = true;
	}
}
