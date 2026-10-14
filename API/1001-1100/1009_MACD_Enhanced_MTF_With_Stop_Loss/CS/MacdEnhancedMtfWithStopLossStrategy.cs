using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD enhanced strategy with an ATR trailing stop line.
/// Every candle gets a MACD score: plus or minus CrossScore on a MACD/signal cross, plus or minus IndicatorScore for the MACD line
/// above or below zero, and plus or minus HistogramScore for a rising or falling histogram. The score turning positive goes long and
/// turning negative goes short, reversing an opposite position. The stop line trails StopLossFactor ATRs (StopLossPeriod) behind the
/// close and only moves in the trade direction; a close through it exits the position.
/// </summary>
public class MacdEnhancedMtfWithStopLossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _crossScore;
	private readonly StrategyParam<decimal> _indicatorScore;
	private readonly StrategyParam<decimal> _histogramScore;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<int> _stopLossPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _prevScore;
	private decimal? _stopLine;

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Score of a MACD/signal cross.
	/// </summary>
	public decimal CrossScore
	{
		get => _crossScore.Value;
		set => _crossScore.Value = value;
	}

	/// <summary>
	/// Score of the MACD line side of zero.
	/// </summary>
	public decimal IndicatorScore
	{
		get => _indicatorScore.Value;
		set => _indicatorScore.Value = value;
	}

	/// <summary>
	/// Score of the histogram direction.
	/// </summary>
	public decimal HistogramScore
	{
		get => _histogramScore.Value;
		set => _histogramScore.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the trailing stop line.
	/// </summary>
	public decimal StopLossFactor
	{
		get => _stopLossFactor.Value;
		set => _stopLossFactor.Value = value;
	}

	/// <summary>
	/// ATR period of the trailing stop line.
	/// </summary>
	public int StopLossPeriod
	{
		get => _stopLossPeriod.Value;
		set => _stopLossPeriod.Value = value;
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
	/// Constructor.
	/// </summary>
	public MacdEnhancedMtfWithStopLossStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "MACD fast period", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "MACD slow period", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "MACD signal period", "MACD");

		_crossScore = Param(nameof(CrossScore), 10m)
			.SetNotNegative()
			.SetDisplay("Cross Score", "Score of a MACD/signal cross", "Score");

		_indicatorScore = Param(nameof(IndicatorScore), 8m)
			.SetNotNegative()
			.SetDisplay("Indicator Score", "Score of the MACD line side of zero", "Score");

		_histogramScore = Param(nameof(HistogramScore), 2m)
			.SetNotNegative()
			.SetDisplay("Histogram Score", "Score of the histogram direction", "Score");

		_stopLossFactor = Param(nameof(StopLossFactor), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Factor", "ATR multiplier of the trailing stop line", "Risk");

		_stopLossPeriod = Param(nameof(StopLossPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Period", "ATR period of the trailing stop line", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_prevMacd = null;
		_prevSignal = null;
		_prevScore = null;
		_stopLine = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};
		var atr = new AverageTrueRange { Length = StopLossPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFormed || !atrValue.IsFormed)
			return;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		_prevMacd = macdLine;
		_prevSignal = signalLine;

		if (prevMacd is not decimal pm || prevSignal is not decimal ps)
			return;

		var crossPart = pm <= ps && macdLine > signalLine ? CrossScore
			: pm >= ps && macdLine < signalLine ? -CrossScore
			: 0m;
		var indicatorPart = macdLine > 0 ? IndicatorScore : macdLine < 0 ? -IndicatorScore : 0m;
		var histogram = macdLine - signalLine;
		var prevHistogram = pm - ps;
		var histogramPart = histogram > prevHistogram ? HistogramScore : histogram < prevHistogram ? -HistogramScore : 0m;
		var score = crossPart + indicatorPart + histogramPart;

		var prevScore = _prevScore;
		_prevScore = score;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var offset = atrValue.GetValue<decimal>() * StopLossFactor;

		if (Position > 0 && _stopLine is decimal longLine)
		{
			if (close < longLine)
			{
				SellMarket(Position);
				_stopLine = null;
				return;
			}

			_stopLine = Math.Max(longLine, close - offset);
		}
		else if (Position < 0 && _stopLine is decimal shortLine)
		{
			if (close > shortLine)
			{
				BuyMarket(-Position);
				_stopLine = null;
				return;
			}

			_stopLine = Math.Min(shortLine, close + offset);
		}

		if (prevScore is not decimal prev)
			return;

		if (prev <= 0 && score > 0 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopLine = close - offset;
		}
		else if (prev >= 0 && score < 0 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopLine = close + offset;
		}
	}
}
