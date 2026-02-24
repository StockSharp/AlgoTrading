using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on LWMA crossover with MACD and KAMA confirmation.
/// Enters long when fast LWMA crosses above slow LWMA, MACD histogram is positive and KAMA is rising.
/// Enters short when fast LWMA crosses below slow LWMA, MACD histogram is negative and KAMA is falling.
/// Uses fixed stop loss and take profit.
/// </summary>
public class SnowiesoStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _kamaLength;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevKama;
	private bool _isInitialized;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public int KamaLength { get => _kamaLength.Value; set => _kamaLength.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SnowiesoStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Fast LWMA period", "LWMAs");

		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Slow LWMA period", "LWMAs");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA for MACD", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA for MACD", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA for MACD", "MACD");

		_kamaLength = Param(nameof(KamaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("KAMA Length", "Lookback period for KAMA", "KAMA");

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastLwma = new WeightedMovingAverage { Length = FastLength };
		var slowLwma = new WeightedMovingAverage { Length = SlowLength };
		var macd = new MovingAverageConvergenceDivergenceSignal();
		macd.Macd.ShortMa.Length = MacdFast;
		macd.Macd.LongMa.Length = MacdSlow;
		macd.SignalMa.Length = MacdSignal;
		var kama = new KaufmanAdaptiveMovingAverage { Length = KamaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { fastLwma, slowLwma, macd, kama }, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPoints, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (values[0].IsEmpty || values[1].IsEmpty || values[2].IsEmpty || values[3].IsEmpty)
			return;

		var fastValue = values[0].GetValue<decimal>();
		var slowValue = values[1].GetValue<decimal>();

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)values[2];
		var macdVal = macdTyped.Macd;
		var signalVal = macdTyped.Signal;

		if (macdVal is not decimal macdLine || signalVal is not decimal signalLine)
			return;

		var histogram = macdLine - signalLine;
		var kamaValue = values[3].GetValue<decimal>();

		if (!_isInitialized)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_prevKama = kamaValue;
			_isInitialized = true;
			return;
		}

		var wasFastAbove = _prevFast > _prevSlow;
		var isFastAbove = fastValue > slowValue;
		var crossUp = !wasFastAbove && isFastAbove;
		var crossDown = wasFastAbove && !isFastAbove;
		var kamaRising = kamaValue > _prevKama;
		var kamaFalling = kamaValue < _prevKama;

		if (crossUp && histogram > 0 && kamaRising)
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (crossDown && histogram < 0 && kamaFalling)
		{
			if (Position >= 0)
				SellMarket();
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
		_prevKama = kamaValue;
	}
}
