using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Momentum Reversal strategy.
/// Shorts when bullish candle grows while MACD histogram decreases.
/// Buys when bearish candle grows while MACD histogram increases.
/// </summary>
public class MacdMomentumReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevBodySize;
	private decimal? _prevHist1;
	private decimal? _prevHist2;

	/// <summary>
	/// Fast length for MACD.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow length for MACD.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal length for MACD.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MacdMomentumReversalStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast Length", "Fast length for MACD", "Indicators");
		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "Slow length for MACD", "Indicators");
		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("Signal Length", "Signal length for MACD", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevBodySize = null;
		_prevHist1 = null;
		_prevHist2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceHistogram
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceHistogramValue)macdValue;
		if (macdTyped.Macd is not decimal hist || macdTyped.Signal is not decimal _)
			return;

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var candleBigger = _prevBodySize != null && bodySize > _prevBodySize;
		var bullishCandle = candle.ClosePrice > candle.OpenPrice;
		var bearishCandle = candle.ClosePrice < candle.OpenPrice;

		var macdLossBullish = _prevHist2 != null && _prevHist1 != null &&
			_prevHist2 > _prevHist1 && _prevHist1 > hist;
		var macdLossBearish = _prevHist2 != null && _prevHist1 != null &&
			_prevHist2 < _prevHist1 && _prevHist1 < hist;

		if (bullishCandle && candleBigger && macdLossBullish && Position >= 0)
		{
			SellMarket(Volume + Position);
		}
		else if (bearishCandle && candleBigger && macdLossBearish && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}

		_prevBodySize = bodySize;
		_prevHist2 = _prevHist1;
		_prevHist1 = hist;
	}
}
