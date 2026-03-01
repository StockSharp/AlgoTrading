using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades when price breaks statistical trend levels using Z-Score and Fibonacci ranges.
/// </summary>
public class IronBotStatisticalTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _zLength;
	private readonly StrategyParam<int> _analysisWindow;
	private readonly StrategyParam<decimal> _highTrendLimit;
	private readonly StrategyParam<decimal> _lowTrendLimit;
	private readonly StrategyParam<decimal> _slRatio;
	private readonly StrategyParam<decimal> _tp1Ratio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _highestHigh;
	private decimal _lowestLow;
	private int _barCount;

	/// <summary>
	/// Z-score length.
	/// </summary>
	public int ZLength
	{
		get => _zLength.Value;
		set => _zLength.Value = value;
	}

	/// <summary>
	/// Trend analysis window.
	/// </summary>
	public int AnalysisWindow
	{
		get => _analysisWindow.Value;
		set => _analysisWindow.Value = value;
	}

	/// <summary>
	/// High trend Fibonacci level.
	/// </summary>
	public decimal HighTrendLimit
	{
		get => _highTrendLimit.Value;
		set => _highTrendLimit.Value = value;
	}

	/// <summary>
	/// Low trend Fibonacci level.
	/// </summary>
	public decimal LowTrendLimit
	{
		get => _lowTrendLimit.Value;
		set => _lowTrendLimit.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal SlRatio
	{
		get => _slRatio.Value;
		set => _slRatio.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal Tp1Ratio
	{
		get => _tp1Ratio.Value;
		set => _tp1Ratio.Value = value;
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
	/// Initializes a new instance of the <see cref="IronBotStatisticalTrendFilterStrategy"/> class.
	/// </summary>
	public IronBotStatisticalTrendFilterStrategy()
	{
		_zLength = Param(nameof(ZLength), 40)
			.SetDisplay("Z Length", "Length for Z-score.", "General");

		_analysisWindow = Param(nameof(AnalysisWindow), 44)
			.SetDisplay("Analysis Window", "Lookback for trend.", "General");

		_highTrendLimit = Param(nameof(HighTrendLimit), 0.236m)
			.SetDisplay("Fibo High", "High trend Fibonacci.", "General");

		_lowTrendLimit = Param(nameof(LowTrendLimit), 0.786m)
			.SetDisplay("Fibo Low", "Low trend Fibonacci.", "General");

		_slRatio = Param(nameof(SlRatio), 0.008m)
			.SetDisplay("Stop %", "Stop loss percent.", "Risk");

		_tp1Ratio = Param(nameof(Tp1Ratio), 0.0075m)
			.SetDisplay("TP1 %", "Take profit level.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles.", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0m;
		_highestHigh = 0m;
		_lowestLow = decimal.MaxValue;
		_barCount = 0;

		var sma = new SimpleMovingAverage { Length = ZLength };
		var std = new StandardDeviation { Length = ZLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, std, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track highest/lowest over analysis window manually
		_barCount++;

		if (_barCount <= AnalysisWindow)
		{
			if (candle.HighPrice > _highestHigh) _highestHigh = candle.HighPrice;
			if (candle.LowPrice < _lowestLow) _lowestLow = candle.LowPrice;
			if (_barCount < AnalysisWindow) return;
		}
		else
		{
			// Simple rolling update
			if (candle.HighPrice > _highestHigh) _highestHigh = candle.HighPrice;
			if (candle.LowPrice < _lowestLow) _lowestLow = candle.LowPrice;
		}

		var zScore = stdValue == 0m ? 0m : (candle.ClosePrice - smaValue) / stdValue;

		var range = _highestHigh - _lowestLow;
		if (range <= 0) return;

		var highTrendLevel = _highestHigh - range * HighTrendLimit;
		var trendLine = _highestHigh - range * 0.5m;
		var lowTrendLevel = _highestHigh - range * LowTrendLimit;

		// Exit logic
		if (Position > 0)
		{
			var pnlPct = (_entryPrice > 0) ? (candle.ClosePrice - _entryPrice) / _entryPrice : 0m;
			if (pnlPct <= -SlRatio || pnlPct >= Tp1Ratio)
			{
				SellMarket();
				_entryPrice = 0m;
			}
			return;
		}
		else if (Position < 0)
		{
			var pnlPct = (_entryPrice > 0) ? (_entryPrice - candle.ClosePrice) / _entryPrice : 0m;
			if (pnlPct <= -SlRatio || pnlPct >= Tp1Ratio)
			{
				BuyMarket();
				_entryPrice = 0m;
			}
			return;
		}

		// Entry logic
		var canLong = candle.ClosePrice >= trendLine && candle.ClosePrice >= highTrendLevel && zScore >= 0m;
		var canShort = candle.ClosePrice <= trendLine && candle.ClosePrice <= lowTrendLevel && zScore <= 0m;

		if (canLong)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (canShort)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
	}
}
