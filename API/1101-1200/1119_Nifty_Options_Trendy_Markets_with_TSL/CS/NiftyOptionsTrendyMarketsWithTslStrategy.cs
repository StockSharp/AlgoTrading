using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Nifty options trendy markets with trailing stop strategy.
/// A long opens when the close crosses above the upper Bollinger Band while ADX is above AdxEntryThreshold, volume spikes above
/// VolumeSpikeMultiplier times its average and the close is above SuperTrend; a short mirrors this at the lower band.
/// A position closes on an opposite MACD/signal cross, when ADX falls below AdxExitThreshold, or at an ATR trailing stop.
/// </summary>
public class NiftyOptionsTrendyMarketsWithTslStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxEntryThreshold;
	private readonly StrategyParam<decimal> _adxExitThreshold;
	private readonly StrategyParam<int> _superTrendLength;
	private readonly StrategyParam<decimal> _superTrendMultiplier;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _volumeSpikeMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _volumes = new();
	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _trailStop;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	/// <summary>
	/// Minimum ADX to enter.
	/// </summary>
	public decimal AdxEntryThreshold
	{
		get => _adxEntryThreshold.Value;
		set => _adxEntryThreshold.Value = value;
	}

	/// <summary>
	/// ADX level below which positions close.
	/// </summary>
	public decimal AdxExitThreshold
	{
		get => _adxExitThreshold.Value;
		set => _adxExitThreshold.Value = value;
	}

	/// <summary>
	/// SuperTrend ATR period.
	/// </summary>
	public int SuperTrendLength
	{
		get => _superTrendLength.Value;
		set => _superTrendLength.Value = value;
	}

	/// <summary>
	/// SuperTrend ATR multiplier.
	/// </summary>
	public decimal SuperTrendMultiplier
	{
		get => _superTrendMultiplier.Value;
		set => _superTrendMultiplier.Value = value;
	}

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// ATR period for the trailing stop.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the trailing stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Volume must exceed its average by this factor.
	/// </summary>
	public decimal VolumeSpikeMultiplier
	{
		get => _volumeSpikeMultiplier.Value;
		set => _volumeSpikeMultiplier.Value = value;
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
	public NiftyOptionsTrendyMarketsWithTslStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "Bollinger");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Bollinger Bands deviation multiplier", "Bollinger");

		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "ADX");

		_adxEntryThreshold = Param(nameof(AdxEntryThreshold), 25m)
			.SetNotNegative()
			.SetDisplay("ADX Entry", "Minimum ADX to enter", "ADX");

		_adxExitThreshold = Param(nameof(AdxExitThreshold), 20m)
			.SetNotNegative()
			.SetDisplay("ADX Exit", "ADX level below which positions close", "ADX");

		_superTrendLength = Param(nameof(SuperTrendLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend Length", "SuperTrend ATR period", "SuperTrend");

		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend Multiplier", "SuperTrend ATR multiplier", "SuperTrend");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast period", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow period", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal period", "MACD");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for the trailing stop", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for the trailing stop", "Risk");

		_volumeSpikeMultiplier = Param(nameof(VolumeSpikeMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Spike", "Volume must exceed its average by this factor", "Volume");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_volumes.Clear();
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
		_prevMacd = null;
		_prevSignal = null;
		_trailStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerMultiplier };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var supertrend = new SuperTrend { Length = SuperTrendLength, Multiplier = SuperTrendMultiplier };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, adx, supertrend, macd, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, adx);
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue adxValue,
		IIndicatorValue supertrendValue, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// The volume average covers the BollingerPeriod candles before this one.
		var avgVolume = _volumes.Count >= BollingerPeriod ? _volumes.Average() : (decimal?)null;
		_volumes.Enqueue(candle.TotalVolume);
		while (_volumes.Count > BollingerPeriod)
			_volumes.Dequeue();

		var prevClose = _prevClose;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;
		_prevClose = close;

		if (!bollingerValue.IsFormed || bollingerValue is not BollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		_prevUpper = upper;
		_prevLower = lower;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		_prevMacd = macdLine;
		_prevSignal = signalLine;

		if (!adxValue.IsFormed || !supertrendValue.IsFormed || !macdValue.IsFormed || !atrValue.IsFormed)
			return;

		if (adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal adx })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var trailDistance = atr * AtrMultiplier;
		var macdCrossDown = prevMacd is decimal pm && prevSignal is decimal ps && pm >= ps && macdLine < signalLine;
		var macdCrossUp = prevMacd is decimal pm2 && prevSignal is decimal ps2 && pm2 <= ps2 && macdLine > signalLine;

		if (Position > 0)
		{
			if ((_trailStop is decimal stop && candle.LowPrice <= stop) || macdCrossDown || adx < AdxExitThreshold)
			{
				SellMarket(Position);
				_trailStop = null;
				return;
			}

			var candidate = close - trailDistance;
			_trailStop = _trailStop is decimal s ? Math.Max(s, candidate) : candidate;
		}
		else if (Position < 0)
		{
			if ((_trailStop is decimal stop && candle.HighPrice >= stop) || macdCrossUp || adx < AdxExitThreshold)
			{
				BuyMarket(-Position);
				_trailStop = null;
				return;
			}

			var candidate = close + trailDistance;
			_trailStop = _trailStop is decimal s ? Math.Min(s, candidate) : candidate;
		}

		if (prevClose is not decimal pc || prevUpper is not decimal pu || prevLower is not decimal pl || avgVolume is not decimal avgVol)
			return;

		var st = supertrendValue.GetValue<decimal>();
		var volumeSpike = candle.TotalVolume > avgVol * VolumeSpikeMultiplier;
		var strongTrend = adx > AdxEntryThreshold;

		if (pc <= pu && close > upper && strongTrend && volumeSpike && close > st && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_trailStop = close - trailDistance;
		}
		else if (pc >= pl && close < lower && strongTrend && volumeSpike && close < st && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_trailStop = close + trailDistance;
		}
	}
}
