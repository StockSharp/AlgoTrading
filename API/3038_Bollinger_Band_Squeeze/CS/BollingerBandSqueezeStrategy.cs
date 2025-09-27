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
/// Bollinger Band squeeze breakout strategy converted from the original MetaTrader 4 expert advisor.
/// Looks for Bollinger band contractions followed by momentum supported breakouts.
/// </summary>
public class BollingerBandSqueezeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<decimal> _squeezeRatio;
	private readonly StrategyParam<int> _retraceCandles;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;

	private readonly List<decimal> _bandWidths = new();
	private readonly List<CandleSnapshot> _recentCandles = new();
	private readonly Queue<decimal> _momentumDiffs = new();

	private WeightedMovingAverage _fastWma = null!;
	private WeightedMovingAverage _slowWma = null!;
	private MovingAverageConvergenceDivergenceSignal _baseMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _monthlyMacd = null!;
	private Momentum _higherMomentum;
	private DataType _higherCandleType;
	private readonly DataType _monthlyCandleType = TimeSpan.FromDays(30).TimeFrame();

	private decimal? _monthlyMacdMain;
	private decimal? _monthlyMacdSignal;

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerBandSqueezeStrategy"/> class.
	/// </summary>
	public BollingerBandSqueezeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for trading", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("BB Period", "Length of Bollinger Bands", "Bollinger Bands");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
		.SetGreaterThanZero()
		.SetDisplay("BB Width", "Standard deviation multiplier", "Bollinger Bands");

		_squeezeRatio = Param(nameof(SqueezeRatio), 1.1m)
		.SetGreaterThanZero()
		.SetDisplay("Squeeze Ratio", "Expansion ratio after squeeze", "Logic");

		_retraceCandles = Param(nameof(RetraceCandles), 10)
		.SetGreaterThanZero()
		.SetDisplay("Lookback", "Candles used for squeeze comparison", "Logic");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast WMA", "Length of the fast weighted MA", "Trend Filter");

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow WMA", "Length of the slow weighted MA", "Trend Filter");

		_momentumLength = Param(nameof(MomentumLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Period for higher timeframe momentum", "Momentum");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Momentum Buy", "Minimum deviation from 100 for long entries", "Momentum");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetNotNegative()
		.SetDisplay("Momentum Sell", "Minimum deviation from 100 for short entries", "Momentum");
	}

	/// <summary>
	/// Primary candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the Bollinger Bands.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Width multiplier for the Bollinger Bands.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Required expansion ratio after the squeeze.
	/// </summary>
	public decimal SqueezeRatio
	{
		get => _squeezeRatio.Value;
		set => _squeezeRatio.Value = value;
	}

	/// <summary>
	/// Number of candles used for historical squeeze comparison.
	/// </summary>
	public int RetraceCandles
	{
		get => _retraceCandles.Value;
		set => _retraceCandles.Value = value;
	}

	/// <summary>
	/// Length of the fast weighted moving average.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow weighted moving average.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Period for the higher timeframe momentum indicator.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimum deviation from 100 required for long entries.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum deviation from 100 required for short entries.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		var higher = GetHigherCandleType();
		if (higher != null)
		yield return (Security, higher);

		yield return (Security, _monthlyCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bandWidths.Clear();
		_recentCandles.Clear();
		_momentumDiffs.Clear();
		_monthlyMacdMain = null;
		_monthlyMacdSignal = null;
		_higherCandleType = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastWma = new WeightedMovingAverage { Length = FastMaLength };
		_slowWma = new WeightedMovingAverage { Length = SlowMaLength };

		_baseMacd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};

		_monthlyMacd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(bollinger, _baseMacd, ProcessBaseCandle)
		.Start();

		_higherCandleType = GetHigherCandleType();
		if (_higherCandleType is not null)
		{
			_higherMomentum = new Momentum { Length = MomentumLength };

			SubscribeCandles(_higherCandleType.Value)
			.Bind(_higherMomentum, ProcessHigherMomentum)
			.Start();
		}
		else
		{
			_higherMomentum = null;
		}

		SubscribeCandles(_monthlyCandleType)
		.BindEx(_monthlyMacd, ProcessMonthlyMacd)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, _baseMacd);

			if (_higherMomentum != null)
			{
				DrawIndicator(area, _higherMomentum);
			}

			DrawOwnTrades(area);
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper ||
		bb.LowBand is not decimal lower ||
		bb.MovingAverage is not decimal middle ||
		middle == 0)
		{
			return;
		}

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdMain ||
		macdTyped.Signal is not decimal macdSignal)
		{
			return;
		}

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var fastValue = _fastWma.Process(new DecimalIndicatorValue(_fastWma, typicalPrice, candle.OpenTime));
		var slowValue = _slowWma.Process(new DecimalIndicatorValue(_slowWma, typicalPrice, candle.OpenTime));

		if (!fastValue.IsFormed || !slowValue.IsFormed)
		{
			return;
		}

		var fastMa = fastValue.ToDecimal();
		var slowMa = slowValue.ToDecimal();

		var width = (upper - lower) / middle;
		UpdateBandWidths(width);
		UpdateRecentCandles(candle);

		// Manage exits first to respect the original MT4 behaviour.
		if (Position > 0 && candle.ClosePrice >= upper)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && candle.ClosePrice <= lower)
		{
			BuyMarket(-Position);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (_monthlyMacdMain is not decimal monthlyMacdMain ||
		_monthlyMacdSignal is not decimal monthlyMacdSignal)
		{
			return;
		}

		if (_recentCandles.Count < 3)
		{
			return;
		}

		var hasExpansion = HasSqueezeExpansion();
		if (!hasExpansion)
		{
			return;
		}

		if (_higherMomentum != null && _momentumDiffs.Count < 3)
		{
			return;
		}

		var momentumDeviation = _momentumDiffs.Count > 0 ? _momentumDiffs.Max() : 0m;
		var momentumLongOk = _higherMomentum == null || momentumDeviation > MomentumBuyThreshold;
		var momentumShortOk = _higherMomentum == null || momentumDeviation > MomentumSellThreshold;

		var previous = _recentCandles[^2];
		var earlier = _recentCandles[^3];

		var monthlyUp = monthlyMacdMain > monthlyMacdSignal;
		var monthlyDown = monthlyMacdMain < monthlyMacdSignal;

		if (momentumLongOk && monthlyUp && macdMain > macdSignal && fastMa > slowMa && earlier.Low < previous.High && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (momentumShortOk && monthlyDown && macdMain < macdSignal && fastMa < slowMa && previous.Low < earlier.High && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}

	private void ProcessHigherMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished || _higherMomentum == null)
		{
			return;
		}

		if (!_higherMomentum.IsFormed)
		{
			return;
		}

		var deviation = Math.Abs(momentumValue - 100m);
		_momentumDiffs.Enqueue(deviation);

		while (_momentumDiffs.Count > 3)
		{
			_momentumDiffs.Dequeue();
		}
	}

	private void ProcessMonthlyMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd ||
		macdTyped.Signal is not decimal signal)
		{
			return;
		}

		_monthlyMacdMain = macd;
		_monthlyMacdSignal = signal;
	}

	private bool HasSqueezeExpansion()
	{
		if (_bandWidths.Count <= RetraceCandles)
		{
			return false;
		}

		var currentWidth = _bandWidths[^1];
		var compareIndex = _bandWidths.Count - 1 - RetraceCandles;

		if (compareIndex < 0)
		{
			return false;
		}

		var referenceWidth = _bandWidths[compareIndex];

		if (referenceWidth == 0)
		{
			return false;
		}

		return currentWidth / referenceWidth > SqueezeRatio;
	}

	private void UpdateBandWidths(decimal width)
	{
		_bandWidths.Add(width);

		var maxSize = Math.Max(2, RetraceCandles + 2);
		while (_bandWidths.Count > maxSize)
		{
			_bandWidths.RemoveAt(0);
		}
	}

	private void UpdateRecentCandles(ICandleMessage candle)
	{
		_recentCandles.Add(new CandleSnapshot(candle.HighPrice, candle.LowPrice, candle.ClosePrice));

		while (_recentCandles.Count > 3)
		{
			_recentCandles.RemoveAt(0);
		}
	}

	private DataType GetHigherCandleType()
	{
		if (CandleType.Arg is not TimeSpan baseSpan)
		{
			return null;
		}

		var minutes = (int)baseSpan.TotalMinutes;

		return minutes switch
		{
			1 => TimeSpan.FromMinutes(15).TimeFrame(),
			5 => TimeSpan.FromMinutes(30).TimeFrame(),
			15 => TimeSpan.FromHours(1).TimeFrame(),
			30 => TimeSpan.FromHours(4).TimeFrame(),
			60 => TimeSpan.FromDays(1).TimeFrame(),
			240 => TimeSpan.FromDays(7).TimeFrame(),
			1440 => TimeSpan.FromDays(30).TimeFrame(),
			10080 => TimeSpan.FromDays(30).TimeFrame(),
			43200 => null,
			_ => null
		};
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal high, decimal low, decimal close)
		{
			High = high;
			Low = low;
			Close = close;
		}

		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}
}

