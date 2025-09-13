using System;
using System.Collections.Generic;

using StockSharp.Algo;
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

	/// <summary>
	/// Fast LWMA period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	/// <summary>
	/// Slow LWMA period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	/// <summary>
	/// Fast EMA for MACD.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	/// <summary>
	/// Slow EMA for MACD.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	/// <summary>
	/// Signal EMA for MACD.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	/// <summary>
	/// KAMA length.
	/// </summary>
	public int KamaLength { get => _kamaLength.Value; set => _kamaLength.Value = value; }
	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SnowiesoStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Fast LWMA period", "LWMAs")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Slow LWMA period", "LWMAs")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 2);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 4);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 2);

		_kamaLength = Param(nameof(KamaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("KAMA Length", "Lookback period for KAMA", "KAMA")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 20m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 200m, 50m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_prevKama = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastLwma = new WeightedMovingAverage { Length = FastLength };
		var slowLwma = new WeightedMovingAverage { Length = SlowLength };
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFast,
			LongPeriod = MacdSlow,
			SignalPeriod = MacdSignal
		};
		var kama = new KaufmanAdaptiveMovingAverage { Length = KamaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastLwma, slowLwma, macd, kama, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints, UnitTypes.Price),
			stopLoss: new Unit(StopLossPoints, UnitTypes.Price));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastLwma);
			DrawIndicator(area, slowLwma);
			DrawIndicator(area, kama);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal macdValue, decimal macdSignal, decimal macdHistogram, decimal kamaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

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

		if (crossUp && macdHistogram > 0 && kamaRising)
		{
			if (Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket();
			}
		}
		else if (crossDown && macdHistogram < 0 && kamaFalling)
		{
			if (Position >= 0)
			{
				if (Position > 0)
					SellMarket(Position);
				SellMarket();
			}
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
		_prevKama = kamaValue;
	}
}
