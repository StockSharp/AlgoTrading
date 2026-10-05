using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Kaufman Trend strategy.
/// A two-state Kalman filter estimates price and its velocity. Trend strength is the velocity divided by the largest absolute
/// velocity of the last OscBufferLength candles, in percent. A long opens when strength reaches TrendStrengthEntry with the close above
/// the filtered price and a short in the mirror case, reversing an opposite position. The stop sits at the SwingLookback low (high)
/// minus (plus) ATR. While in profit, TakeProfit1Percent of the entry volume closes when strength falls below TrendStrengthEntry and
/// TakeProfit2Percent when it falls below the midpoint of the two thresholds; the rest closes below TrendStrengthExit.
/// </summary>
public class KaufmanTrendStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit1Percent;
	private readonly StrategyParam<decimal> _takeProfit2Percent;
	private readonly StrategyParam<decimal> _takeProfit3Percent;
	private readonly StrategyParam<int> _swingLookback;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _trendStrengthEntry;
	private readonly StrategyParam<decimal> _trendStrengthExit;
	private readonly StrategyParam<decimal> _processNoise;
	private readonly StrategyParam<decimal> _measurementNoise;
	private readonly StrategyParam<int> _oscBufferLength;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _swingHigh;
	private Lowest _swingLow;
	private Highest _oscMax;
	private decimal? _filtered;
	private decimal _velocity;
	private decimal _p00;
	private decimal _p01;
	private decimal _p10;
	private decimal _p11;
	private decimal? _stopPrice;
	private decimal _entryPrice;
	private decimal _entryVolume;
	private int _stage;

	/// <summary>
	/// Percent of the entry volume closed at the first stage.
	/// </summary>
	public decimal TakeProfit1Percent
	{
		get => _takeProfit1Percent.Value;
		set => _takeProfit1Percent.Value = value;
	}

	/// <summary>
	/// Percent of the entry volume closed at the second stage.
	/// </summary>
	public decimal TakeProfit2Percent
	{
		get => _takeProfit2Percent.Value;
		set => _takeProfit2Percent.Value = value;
	}

	/// <summary>
	/// Percent of the entry volume closed at the final stage.
	/// </summary>
	public decimal TakeProfit3Percent
	{
		get => _takeProfit3Percent.Value;
		set => _takeProfit3Percent.Value = value;
	}

	/// <summary>
	/// Candles the swing high/low spans.
	/// </summary>
	public int SwingLookback
	{
		get => _swingLookback.Value;
		set => _swingLookback.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Trend strength required for entries.
	/// </summary>
	public decimal TrendStrengthEntry
	{
		get => _trendStrengthEntry.Value;
		set => _trendStrengthEntry.Value = value;
	}

	/// <summary>
	/// Trend strength below which the position closes.
	/// </summary>
	public decimal TrendStrengthExit
	{
		get => _trendStrengthExit.Value;
		set => _trendStrengthExit.Value = value;
	}

	/// <summary>
	/// Kalman process noise.
	/// </summary>
	public decimal ProcessNoise
	{
		get => _processNoise.Value;
		set => _processNoise.Value = value;
	}

	/// <summary>
	/// Kalman measurement noise.
	/// </summary>
	public decimal MeasurementNoise
	{
		get => _measurementNoise.Value;
		set => _measurementNoise.Value = value;
	}

	/// <summary>
	/// Candles the trend strength normalization spans.
	/// </summary>
	public int OscBufferLength
	{
		get => _oscBufferLength.Value;
		set => _oscBufferLength.Value = value;
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
	/// Initialize <see cref="KaufmanTrendStrategy"/>.
	/// </summary>
	public KaufmanTrendStrategy()
	{
		_takeProfit1Percent = Param(nameof(TakeProfit1Percent), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit 1 %", "Percent of the entry volume closed at the first stage", "Exits");

		_takeProfit2Percent = Param(nameof(TakeProfit2Percent), 25m)
			.SetNotNegative()
			.SetDisplay("Take Profit 2 %", "Percent of the entry volume closed at the second stage", "Exits");

		_takeProfit3Percent = Param(nameof(TakeProfit3Percent), 25m)
			.SetNotNegative()
			.SetDisplay("Take Profit 3 %", "Percent of the entry volume closed at the final stage", "Exits");

		_swingLookback = Param(nameof(SwingLookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Swing Lookback", "Candles the swing high/low spans", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

		_trendStrengthEntry = Param(nameof(TrendStrengthEntry), 60m)
			.SetDisplay("Trend Strength Entry", "Trend strength required for entries", "Trend");

		_trendStrengthExit = Param(nameof(TrendStrengthExit), 40m)
			.SetDisplay("Trend Strength Exit", "Trend strength below which the position closes", "Trend");

		_processNoise = Param(nameof(ProcessNoise), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Process Noise", "Kalman process noise", "Kalman");

		_measurementNoise = Param(nameof(MeasurementNoise), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Measurement Noise", "Kalman measurement noise", "Kalman");

		_oscBufferLength = Param(nameof(OscBufferLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Oscillator Buffer", "Candles the trend strength normalization spans", "Trend");

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
		_filtered = null;
		_velocity = 0m;
		_p00 = 1m;
		_p01 = 0m;
		_p10 = 0m;
		_p11 = 1m;
		_stopPrice = null;
		_entryPrice = 0m;
		_entryVolume = 0m;
		_stage = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrPeriod };
		_swingHigh = new Highest { Length = SwingLookback };
		_swingLow = new Lowest { Length = SwingLookback };
		_oscMax = new Highest { Length = OscBufferLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var close = candle.ClosePrice;

		UpdateKalman(close);

		var maxValue = _oscMax.Process(new DecimalIndicatorValue(_oscMax, Math.Abs(_velocity), time) { IsFinal = true });
		var highValue = _swingHigh.Process(new DecimalIndicatorValue(_swingHigh, candle.HighPrice, time) { IsFinal = true });
		var lowValue = _swingLow.Process(new DecimalIndicatorValue(_swingLow, candle.LowPrice, time) { IsFinal = true });

		if (!_oscMax.IsFormed || !_swingHigh.IsFormed || !_swingLow.IsFormed || _filtered is not decimal filtered)
			return;

		var maxAbs = maxValue.GetValue<decimal>();
		var strength = maxAbs > 0m ? _velocity / maxAbs * 100m : 0m;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var midLevel = (TrendStrengthEntry + TrendStrengthExit) / 2m;

		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop || strength < TrendStrengthExit)
			{
				SellMarket(Position);
				_stopPrice = null;
				return;
			}

			if (close > _entryPrice && TryTakePartial(strength < TrendStrengthEntry, strength < midLevel, false))
				return;
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop || strength > -TrendStrengthExit)
			{
				BuyMarket(-Position);
				_stopPrice = null;
				return;
			}

			if (close < _entryPrice && TryTakePartial(strength > -TrendStrengthEntry, strength > -midLevel, true))
				return;
		}

		if (strength >= TrendStrengthEntry && close > filtered && Position <= 0)
		{
			var stop = lowValue.GetValue<decimal>() - atr;
			if (stop >= close)
				return;

			BuyMarket(Volume + Math.Abs(Position));
			OnEntered(close, stop);
		}
		else if (strength <= -TrendStrengthEntry && close < filtered && Position >= 0)
		{
			var stop = highValue.GetValue<decimal>() + atr;
			if (stop <= close)
				return;

			SellMarket(Volume + Math.Abs(Position));
			OnEntered(close, stop);
		}
	}

	private void OnEntered(decimal price, decimal stop)
	{
		_entryPrice = price;
		_entryVolume = Volume;
		_stopPrice = stop;
		_stage = 0;
	}

	private bool TryTakePartial(bool firstStage, bool secondStage, bool isShort)
	{
		decimal percent;

		if (_stage == 0 && firstStage)
			percent = TakeProfit1Percent;
		else if (_stage == 1 && secondStage)
			percent = TakeProfit2Percent;
		else
			return false;

		_stage++;

		var volume = Math.Min(Math.Abs(Position), _entryVolume * percent / 100m);
		if (volume <= 0m)
			return false;

		if (isShort)
			BuyMarket(volume);
		else
			SellMarket(volume);

		return true;
	}

	private void UpdateKalman(decimal price)
	{
		if (_filtered is not decimal filtered)
		{
			_filtered = price;
			return;
		}

		// Constant-velocity model: predict, then correct with the new close.
		var predicted = filtered + _velocity;

		var p00 = _p00 + _p01 + _p10 + _p11 + ProcessNoise;
		var p01 = _p01 + _p11;
		var p10 = _p10 + _p11;
		var p11 = _p11 + ProcessNoise;

		var s = p00 + MeasurementNoise;
		var k0 = p00 / s;
		var k1 = p10 / s;
		var innovation = price - predicted;

		_filtered = predicted + k0 * innovation;
		_velocity += k1 * innovation;

		_p00 = (1 - k0) * p00;
		_p01 = (1 - k0) * p01;
		_p10 = p10 - k1 * p00;
		_p11 = p11 - k1 * p01;
	}
}
