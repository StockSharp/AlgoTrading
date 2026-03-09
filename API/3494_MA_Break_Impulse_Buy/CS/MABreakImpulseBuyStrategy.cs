namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// MA Break Impulse Buy strategy: EMA crossover with volume surge.
/// Buys on EMA cross up with above-average volume, sells on EMA cross down.
/// </summary>
public class MABreakImpulseBuyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevClose;
	private decimal _prevEma;
	private decimal _prevVolume;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public MABreakImpulseBuyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevEma = 0;
		_prevVolume = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevClose = 0;
		_prevEma = 0;
		_prevVolume = 0;
		_hasPrev = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			var volumeUp = candle.TotalVolume > _prevVolume;

			if (_prevClose <= _prevEma && candle.ClosePrice > emaValue && volumeUp && Position <= 0)
				BuyMarket();
			else if (_prevClose >= _prevEma && candle.ClosePrice < emaValue && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevEma = emaValue;
		_prevVolume = candle.TotalVolume;
		_hasPrev = true;
	}
}
