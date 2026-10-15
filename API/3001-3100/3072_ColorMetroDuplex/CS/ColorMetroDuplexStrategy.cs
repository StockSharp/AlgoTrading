using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MT5 expert "Exp_ColorMETRO_Duplex".
/// Uses RSI with step-based envelopes (fast/slow) to generate long/short signals.
/// </summary>
public class ColorMetroDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _fastStep;
	private readonly StrategyParam<int> _slowStep;
	private readonly StrategyParam<int> _signalCooldownBars;

	// fast envelope state
	private decimal? _fastMin, _fastMax;
	private int _fastTrend;
	private decimal? _prevFastBand;

	// slow envelope state
	private decimal? _slowMin, _slowMax;
	private int _slowTrend;
	private decimal? _prevSlowBand;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int FastStep { get => _fastStep.Value; set => _fastStep.Value = value; }
	public int SlowStep { get => _slowStep.Value; set => _slowStep.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public ColorMetroDuplexStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI lookback", "Indicator");

		_fastStep = Param(nameof(FastStep), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast Step", "Step size for fast envelope", "Indicator");

		_slowStep = Param(nameof(SlowStep), 24)
			.SetGreaterThanZero()
			.SetDisplay("Slow Step", "Step size for slow envelope", "Indicator");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between reversals", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMin = _fastMax = null;
		_slowMin = _slowMax = null;
		_fastTrend = _slowTrend = 0;
		_prevFastBand = _prevSlowBand = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMin = _fastMax = null;
		_slowMin = _slowMax = null;
		_fastTrend = _slowTrend = 0;
		_prevFastBand = _prevSlowBand = null;
		_cooldownRemaining = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var fStep = (decimal)FastStep;
		var sStep = (decimal)SlowStep;

		// fast envelope
		var fastMinCand = rsiVal - 2m * fStep;
		var fastMaxCand = rsiVal + 2m * fStep;

		if (_fastMin == null || _fastMax == null)
		{
			_fastMin = fastMinCand;
			_fastMax = fastMaxCand;
			_fastTrend = 0;

			_slowMin = rsiVal - 2m * sStep;
			_slowMax = rsiVal + 2m * sStep;
			_slowTrend = 0;
			return;
		}

		// fast trend
		if (rsiVal > _fastMax) _fastTrend = 1;
		else if (rsiVal < _fastMin) _fastTrend = -1;

		if (_fastTrend > 0 && fastMinCand < _fastMin) fastMinCand = _fastMin.Value;
		else if (_fastTrend < 0 && fastMaxCand > _fastMax) fastMaxCand = _fastMax.Value;

		// slow envelope
		var slowMinCand = rsiVal - 2m * sStep;
		var slowMaxCand = rsiVal + 2m * sStep;

		if (rsiVal > _slowMax) _slowTrend = 1;
		else if (rsiVal < _slowMin) _slowTrend = -1;

		if (_slowTrend > 0 && slowMinCand < _slowMin) slowMinCand = _slowMin.Value;
		else if (_slowTrend < 0 && slowMaxCand > _slowMax) slowMaxCand = _slowMax.Value;

		// compute band values
		decimal? fastBand = null;
		if (_fastTrend > 0) fastBand = fastMinCand + fStep;
		else if (_fastTrend < 0) fastBand = fastMaxCand - fStep;

		decimal? slowBand = null;
		if (_slowTrend > 0) slowBand = slowMinCand + sStep;
		else if (_slowTrend < 0) slowBand = slowMaxCand - sStep;

		_fastMin = fastMinCand;
		_fastMax = fastMaxCand;
		_slowMin = slowMinCand;
		_slowMax = slowMaxCand;

		if (fastBand == null || slowBand == null)
		{
			_prevFastBand = fastBand;
			_prevSlowBand = slowBand;
			return;
		}

		if (_prevFastBand == null || _prevSlowBand == null)
		{
			_prevFastBand = fastBand;
			_prevSlowBand = slowBand;
			return;
		}

		var up = fastBand.Value;
		var down = slowBand.Value;
		var prevUp = _prevFastBand.Value;
		var prevDown = _prevSlowBand.Value;

		_prevFastBand = fastBand;
		_prevSlowBand = slowBand;

		// Long signal: fast crosses below slow (up crosses down downward)
		var longOpen = prevUp > prevDown && up <= down;
		// Short signal: fast crosses above slow (up crosses down upward)
		var shortOpen = prevUp < prevDown && up >= down;

		if (_cooldownRemaining == 0 && longOpen && Position == 0)
		{
			BuyMarket();
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && shortOpen && Position == 0)
		{
			SellMarket();
			_cooldownRemaining = SignalCooldownBars;
		}
	}
}
