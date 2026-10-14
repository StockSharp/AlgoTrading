namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Scalping EMA RSI MACD Strategy.
/// A long opens when the fast EMA crosses above the slow EMA with the close above the trend EMA, RSI between RsiOversold and
/// RsiOverbought, the MACD line above its signal and volume above VolumeThreshold times its VolumeMaLength average; a short
/// mirrors this. The stop is AtrMultiplier ATRs from the entry and the target RiskReward times that distance. An opposite
/// signal reverses the position.
/// </summary>
public class ScalpingEmaRsiMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _trendEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeMa;
	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Trend EMA period.
	/// </summary>
	public int TrendEmaLength
	{
		get => _trendEmaLength.Value;
		set => _trendEmaLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Upper RSI bound for entries.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Lower RSI bound for entries.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the stop distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Target distance as a multiple of the stop distance.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Period of the volume average.
	/// </summary>
	public int VolumeMaLength
	{
		get => _volumeMaLength.Value;
		set => _volumeMaLength.Value = value;
	}

	/// <summary>
	/// Volume multiple of its average required for entries.
	/// </summary>
	public decimal VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
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
	public ScalpingEmaRsiMacdStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_trendEmaLength = Param(nameof(TrendEmaLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA", "Trend EMA period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 65m)
			.SetDisplay("RSI Overbought", "Upper RSI bound for entries", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "Lower RSI bound for entries", "Indicators");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA period", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA period", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal line period", "MACD");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the stop distance", "Risk");

		_riskReward = Param(nameof(RiskReward), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Target distance as a multiple of the stop distance", "Risk");

		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Period of the volume average", "Volume");

		_volumeThreshold = Param(nameof(VolumeThreshold), 1.3m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Threshold", "Volume multiple of its average required for entries", "Volume");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_stopPrice = null;
		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var trendEma = new ExponentialMovingAverage { Length = TrendEmaLength };
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
		var atr = new AverageTrueRange { Length = AtrLength };
		_volumeMa = new SimpleMovingAverage { Length = VolumeMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, trendEma, rsi, macd, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, trendEma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue trendValue, IIndicatorValue rsiValue, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeAverage = _volumeMa.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();

		if (!fastValue.IsFormed || !slowValue.IsFormed)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Protective exits are checked first: the stop or the target of the open position.
		if (Position > 0 && ((_stopPrice is decimal longStop && candle.LowPrice <= longStop) || (_targetPrice is decimal longTarget && candle.HighPrice >= longTarget)))
		{
			SellMarket(Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		if (Position < 0 && ((_stopPrice is decimal shortStop && candle.HighPrice >= shortStop) || (_targetPrice is decimal shortTarget && candle.LowPrice <= shortTarget)))
		{
			BuyMarket(-Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		if (prevFast is not decimal pf || prevSlow is not decimal ps || !_volumeMa.IsFormed)
			return;

		if (!trendValue.IsFormed || !rsiValue.IsFormed || !atrValue.IsFormed || !macdValue.IsFormed)
			return;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		var trend = trendValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var rsiInBounds = rsi > RsiOversold && rsi < RsiOverbought;
		var highVolume = candle.TotalVolume > volumeAverage * VolumeThreshold;
		var stopDistance = atr * AtrMultiplier;

		var longSignal = pf <= ps && fast > slow && close > trend && rsiInBounds && macdLine > signalLine && highVolume;
		var shortSignal = pf >= ps && fast < slow && close < trend && rsiInBounds && macdLine < signalLine && highVolume;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - stopDistance;
			_targetPrice = close + stopDistance * RiskReward;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + stopDistance;
			_targetPrice = close - stopDistance * RiskReward;
		}
	}
}
