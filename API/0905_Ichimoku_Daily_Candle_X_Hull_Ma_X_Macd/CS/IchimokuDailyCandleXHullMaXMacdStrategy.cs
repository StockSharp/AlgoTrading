using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Ichimoku cloud, Hull Moving Average trend and MACD crossover.
/// </summary>
public class IchimokuDailyCandleXHullMaXMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private HullMovingAverage _hma;
	private MovingAverageConvergenceDivergence _macd;
	private ExponentialMovingAverage _macdSignal;

	private decimal _prevHma;
	private decimal _prevMacdLine;
	private decimal _prevSignalLine;
	private bool _isReady;

	/// <summary>HMA Period.</summary>
	public int HmaPeriod { get => _hmaPeriod.Value; set => _hmaPeriod.Value = value; }
	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public IchimokuDailyCandleXHullMaXMacdStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 14)
			.SetDisplay("HMA Period", "Hull MA period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Main candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hma = new HullMovingAverage { Length = HmaPeriod };
		_macd = new MovingAverageConvergenceDivergence();
		_macdSignal = new ExponentialMovingAverage { Length = 9 };

		var ichimoku = new Ichimoku();

		_prevHma = 0;
		_prevMacdLine = 0;
		_prevSignalLine = 0;
		_isReady = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuRaw)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Process HMA manually
		var hmaResult = _hma.Process(new DecimalIndicatorValue(_hma, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		if (!_hma.IsFormed)
			return;
		var hmaVal = hmaResult.ToDecimal();

		// Process MACD manually
		var macdResult = _macd.Process(new DecimalIndicatorValue(_macd, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var macdLine = macdResult.ToDecimal();
		var signalResult = _macdSignal.Process(new DecimalIndicatorValue(_macdSignal, macdLine, candle.OpenTime) { IsFinal = true });
		var signalLine = signalResult.ToDecimal();

		// Get Ichimoku values
		var ichi = (IIchimokuValue)ichimokuRaw;
		if (ichi.SenkouA is not decimal senkouA || ichi.SenkouB is not decimal senkouB)
		{
			_prevHma = hmaVal;
			_prevMacdLine = macdLine;
			_prevSignalLine = signalLine;
			return;
		}

		if (!_isReady)
		{
			_prevHma = hmaVal;
			_prevMacdLine = macdLine;
			_prevSignalLine = signalLine;
			_isReady = true;
			return;
		}

		var price = candle.ClosePrice;
		var hmaBull = hmaVal > _prevHma;
		var hmaBear = hmaVal < _prevHma;

		// Ichimoku cloud filter
		var aboveCloud = price > Math.Max(senkouA, senkouB);
		var belowCloud = price < Math.Min(senkouA, senkouB);

		// MACD crossover
		var macdCrossUp = _prevMacdLine <= _prevSignalLine && macdLine > signalLine;
		var macdCrossDown = _prevMacdLine >= _prevSignalLine && macdLine < signalLine;

		if (hmaBull && aboveCloud && macdCrossUp && Position <= 0)
			BuyMarket();
		else if (hmaBear && belowCloud && macdCrossDown && Position >= 0)
			SellMarket();

		_prevHma = hmaVal;
		_prevMacdLine = macdLine;
		_prevSignalLine = signalLine;
	}
}
