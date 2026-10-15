namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Volume Calculator strategy: EMA + volume confirmation.
/// Buys when price above EMA with increasing volume, sells below EMA with increasing volume.
/// </summary>
public class VolumeCalculatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevVolume;
	private bool _wasBullishSignal;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public VolumeCalculatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevVolume = 0;
		_wasBullishSignal = false;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevVolume = 0;
		_wasBullishSignal = false;
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
			var bullishSignal = candle.ClosePrice > emaValue && volumeUp;
			var bearishSignal = candle.ClosePrice < emaValue && volumeUp;
			var crossedUp = bullishSignal && !_wasBullishSignal;
			var crossedDown = bearishSignal && _wasBullishSignal;

			if (crossedUp && Position <= 0)
				BuyMarket();
			else if (crossedDown && Position >= 0)
				SellMarket();

			if (bullishSignal || bearishSignal)
				_wasBullishSignal = bullishSignal;
		}

		_prevVolume = candle.TotalVolume;
		_hasPrev = true;
	}
}
