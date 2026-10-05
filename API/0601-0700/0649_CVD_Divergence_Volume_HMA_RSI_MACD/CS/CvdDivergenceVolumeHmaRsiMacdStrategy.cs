using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CVD divergence strategy with HMA trend, RSI, MACD and volume filters.
/// Each candle's volume delta is its volume signed by the candle direction; the running sum is the CVD.
/// A long needs HMA20 above HMA50 with price above HMA20, RSI between 40 and RsiOverbought, MACD above its signal with a rising histogram,
/// volume above VolumeMultiplier times its average, and a bullish CVD divergence or CVD above its CvdLength average. Shorts mirror this.
/// A long exits when price drops below HMA20, RSI rises above RsiOverbought or MACD crosses below its signal; shorts mirror this.
/// </summary>
public class CvdDivergenceVolumeHmaRsiMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _hma20Length;
	private readonly StrategyParam<int> _hma50Length;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _cvdLength;
	private readonly StrategyParam<int> _divergenceLookback;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _cvdSma;
	private readonly List<decimal> _closes = [];
	private readonly List<decimal> _cvds = [];
	private decimal _cvd;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _prevHistogram;

	/// <summary>
	/// Fast HMA length.
	/// </summary>
	public int Hma20Length { get => _hma20Length.Value; set => _hma20Length.Value = value; }

	/// <summary>
	/// Slow HMA length.
	/// </summary>
	public int Hma50Length { get => _hma50Length.Value; set => _hma50Length.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// Volume average length.
	/// </summary>
	public int VolumeMaLength { get => _volumeMaLength.Value; set => _volumeMaLength.Value = value; }

	/// <summary>
	/// Volume must exceed its average times this multiplier.
	/// </summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }

	/// <summary>
	/// Length of the CVD average that defines a rising or falling CVD.
	/// </summary>
	public int CvdLength { get => _cvdLength.Value; set => _cvdLength.Value = value; }

	/// <summary>
	/// Bars between the two points compared for a divergence.
	/// </summary>
	public int DivergenceLookback { get => _divergenceLookback.Value; set => _divergenceLookback.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public CvdDivergenceVolumeHmaRsiMacdStrategy()
	{
		_hma20Length = Param(nameof(Hma20Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("HMA20 Length", "Fast HMA length", "Trend");

		_hma50Length = Param(nameof(Hma50Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("HMA50 Length", "Slow HMA length", "Trend");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast period", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow period", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal period", "MACD");

		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Volume average length", "Volume");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Volume must exceed its average times this", "Volume");

		_cvdLength = Param(nameof(CvdLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("CVD Length", "Length of the CVD average", "CVD");

		_divergenceLookback = Param(nameof(DivergenceLookback), 5)
			.SetGreaterThanZero()
			.SetDisplay("Divergence Lookback", "Bars between the compared divergence points", "CVD");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_closes.Clear();
		_cvds.Clear();
		_cvd = 0m;
		_prevMacd = null;
		_prevSignal = null;
		_prevHistogram = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var hma20 = new HullMovingAverage { Length = Hma20Length };
		var hma50 = new HullMovingAverage { Length = Hma50Length };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal },
		};
		_volumeSma = new SimpleMovingAverage { Length = VolumeMaLength };
		_cvdSma = new SimpleMovingAverage { Length = CvdLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(hma20, hma50, rsi, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma20);
			DrawIndicator(area, hma50);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue hma20Value, IIndicatorValue hma50Value, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;

		var delta = close > candle.OpenPrice ? volume : close < candle.OpenPrice ? -volume : 0m;
		_cvd += delta;

		var volumeAvg = _volumeSma.Process(volume, candle.ServerTime, true);
		var cvdAvg = _cvdSma.Process(_cvd, candle.ServerTime, true);

		_closes.Add(close);
		_cvds.Add(_cvd);
		var maxHistory = DivergenceLookback + 1;
		if (_closes.Count > maxHistory)
		{
			_closes.RemoveAt(0);
			_cvds.RemoveAt(0);
		}

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		var histogram = macdLine - signalLine;
		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		var prevHistogram = _prevHistogram;
		_prevMacd = macdLine;
		_prevSignal = signalLine;
		_prevHistogram = histogram;

		if (!hma20Value.IsFormed || !hma50Value.IsFormed || !rsiValue.IsFormed || !volumeAvg.IsFormed || !cvdAvg.IsFormed)
			return;

		if (prevMacd is not decimal pm || prevSignal is not decimal ps || prevHistogram is not decimal ph || _closes.Count < maxHistory)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hma20 = hma20Value.GetValue<decimal>();
		var hma50 = hma50Value.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var cvdMa = cvdAvg.GetValue<decimal>();

		var macdCrossDown = pm >= ps && macdLine < signalLine;
		var macdCrossUp = pm <= ps && macdLine > signalLine;

		if (Position > 0)
		{
			if (close < hma20 || rsi > RsiOverbought || macdCrossDown)
				SellMarket(Position);
			return;
		}

		if (Position < 0)
		{
			if (close > hma20 || rsi < RsiOversold || macdCrossUp)
				BuyMarket(-Position);
			return;
		}

		var oldClose = _closes[0];
		var oldCvd = _cvds[0];

		// Price makes a lower point while CVD makes a higher one, or the reverse.
		var bullishDivergence = close < oldClose && _cvd > oldCvd;
		var bearishDivergence = close > oldClose && _cvd < oldCvd;

		var volumeOk = volume > volumeAvg.GetValue<decimal>() * VolumeMultiplier;

		var longSignal = hma20 > hma50 && close > hma20
			&& rsi > 40m && rsi < RsiOverbought
			&& macdLine > signalLine && histogram > ph
			&& volumeOk
			&& (bullishDivergence || _cvd > cvdMa);

		var shortSignal = hma20 < hma50 && close < hma20
			&& rsi > RsiOversold && rsi < 60m
			&& macdLine < signalLine && histogram < ph
			&& volumeOk
			&& (bearishDivergence || _cvd < cvdMa);

		if (longSignal)
			BuyMarket();
		else if (shortSignal)
			SellMarket();
	}
}
