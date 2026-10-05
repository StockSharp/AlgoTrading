namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// QQE Signals Strategy.
/// RSI is smoothed by an EMA of RsiSmoothing bars. The bar-to-bar change of that line is double smoothed over
/// 2 * RsiPeriod - 1 bars and multiplied by QqeFactor to build long and short trailing bands. The trailing line follows
/// the long band in an up trend and the short band in a down trend. When the smoothed RSI moves above the trailing
/// line a long is opened; when it falls below it the long is closed. Threshold marks the 50 +/- Threshold zone on the chart.
/// </summary>
public class QqeSignalsStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiSmoothing;
	private readonly StrategyParam<decimal> _qqeFactor;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _rsiMa;
	private ExponentialMovingAverage _atrRsiMa;
	private ExponentialMovingAverage _darMa;

	private decimal? _prevRsiMa;
	private decimal? _longBand;
	private decimal? _shortBand;
	private int _trend;
	private int _aboveCount;
	private int _belowCount;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// EMA smoothing of RSI.
	/// </summary>
	public int RsiSmoothing
	{
		get => _rsiSmoothing.Value;
		set => _rsiSmoothing.Value = value;
	}

	/// <summary>
	/// Multiplier of the smoothed RSI volatility.
	/// </summary>
	public decimal QqeFactor
	{
		get => _qqeFactor.Value;
		set => _qqeFactor.Value = value;
	}

	/// <summary>
	/// Distance of the reference zone from the 50 level.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
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
	public QqeSignalsStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "QQE");

		_rsiSmoothing = Param(nameof(RsiSmoothing), 5)
			.SetGreaterThanZero()
			.SetDisplay("RSI Smoothing", "EMA smoothing of RSI", "QQE");

		_qqeFactor = Param(nameof(QqeFactor), 4.238m)
			.SetGreaterThanZero()
			.SetDisplay("QQE Factor", "Multiplier of the smoothed RSI volatility", "QQE");

		_threshold = Param(nameof(Threshold), 10m)
			.SetNotNegative()
			.SetDisplay("Threshold", "Distance of the reference zone from the 50 level", "QQE");

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
		_prevRsiMa = null;
		_longBand = null;
		_shortBand = null;
		_trend = 1;
		_aboveCount = 0;
		_belowCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var wildersPeriod = RsiPeriod * 2 - 1;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiMa = new ExponentialMovingAverage { Length = RsiSmoothing };
		_atrRsiMa = new ExponentialMovingAverage { Length = wildersPeriod };
		_darMa = new ExponentialMovingAverage { Length = wildersPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsiMaValue = _rsiMa.Process(rsiValue, candle.OpenTime, true);
		if (!_rsiMa.IsFormed)
			return;

		var rsiMa = rsiMaValue.ToDecimal();
		var prevRsiMa = _prevRsiMa;
		_prevRsiMa = rsiMa;

		if (prevRsiMa is not decimal previous)
			return;

		var atrRsi = Math.Abs(previous - rsiMa);
		var maAtrRsi = _atrRsiMa.Process(atrRsi, candle.OpenTime, true);
		if (!_atrRsiMa.IsFormed)
			return;

		var dar = _darMa.Process(maAtrRsi.ToDecimal(), candle.OpenTime, true);
		if (!_darMa.IsFormed)
			return;

		var delta = dar.ToDecimal() * QqeFactor;
		var newLongBand = rsiMa - delta;
		var newShortBand = rsiMa + delta;

		var prevLongBand = _longBand;
		var prevShortBand = _shortBand;

		var longBand = prevLongBand is decimal pl && previous > pl && rsiMa > pl ? Math.Max(pl, newLongBand) : newLongBand;
		var shortBand = prevShortBand is decimal ps && previous < ps && rsiMa < ps ? Math.Min(ps, newShortBand) : newShortBand;

		if (prevShortBand is decimal s && previous <= s && rsiMa > s)
			_trend = 1;
		else if (prevLongBand is decimal l && previous >= l && rsiMa < l)
			_trend = -1;

		_longBand = longBand;
		_shortBand = shortBand;

		var trailingLine = _trend == 1 ? longBand : shortBand;

		_aboveCount = trailingLine < rsiMa ? _aboveCount + 1 : 0;
		_belowCount = trailingLine > rsiMa ? _belowCount + 1 : 0;

		if (prevLongBand is null || prevShortBand is null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_aboveCount == 1 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_belowCount == 1 && Position > 0)
			SellMarket(Position);
	}
}
