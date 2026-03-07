namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy converted from the MetaTrader expert Exp_XWPR_Histogram_Vol.
/// Computes a volume-weighted Williams %R histogram inline and trades on strong colour transitions.
/// </summary>
public class ExpXwprHistogramVolStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<decimal> _highLevel2;
	private readonly StrategyParam<decimal> _lowLevel2;
	private readonly StrategyParam<int> _signalCooldownBars;

	private WilliamsR _wpr;
	private SimpleMovingAverage _histSma;
	private SimpleMovingAverage _volSma;
	private int? _prevColor;
	private int _cooldownRemaining;
	private DateTimeOffset? _lastEntryTime;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public decimal HighLevel2 { get => _highLevel2.Value; set => _highLevel2.Value = value; }
	public decimal LowLevel2 { get => _lowLevel2.Value; set => _lowLevel2.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }

	public ExpXwprHistogramVolStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_wprPeriod = Param(nameof(WprPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R lookback", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "Smoothing length", "Indicator");

		_highLevel2 = Param(nameof(HighLevel2), 17m)
			.SetDisplay("High Level 2", "Strong bullish zone", "Indicator");

		_lowLevel2 = Param(nameof(LowLevel2), -17m)
			.SetDisplay("Low Level 2", "Strong bearish zone", "Indicator");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 48)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait after a new entry", "Trading");
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
		_wpr = null;
		_histSma = null;
		_volSma = null;
		_prevColor = null;
		_cooldownRemaining = 0;
		_lastEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevColor = null;
		_cooldownRemaining = 0;
		_lastEntryTime = null;

		_wpr = new WilliamsR { Length = WprPeriod };
		_histSma = new SimpleMovingAverage { Length = SmoothingLength };
		_volSma = new SimpleMovingAverage { Length = SmoothingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var wprValue = _wpr.Process(candle);
		if (!wprValue.IsFormed)
			return;

		var wpr = wprValue.ToDecimal();
		var volume = candle.TotalVolume > 0m ? candle.TotalVolume : 1m;
		var histRaw = (wpr + 50m) * volume;
		var histSmoothed = _histSma.Process(new DecimalIndicatorValue(_histSma, histRaw, candle.OpenTime) { IsFinal = true });
		var volSmoothed = _volSma.Process(new DecimalIndicatorValue(_volSma, volume, candle.OpenTime) { IsFinal = true });

		if (!histSmoothed.IsFormed || !volSmoothed.IsFormed)
			return;

		var baseline = volSmoothed.ToDecimal();
		if (baseline == 0m)
			return;

		var hist = histSmoothed.ToDecimal();
		var strongBullLevel = HighLevel2 * baseline;
		var strongBearLevel = LowLevel2 * baseline;

		var color = hist >= strongBullLevel ? 0 : hist <= strongBearLevel ? 4 : 2;

		if (_prevColor == null)
		{
			_prevColor = color;
			return;
		}

		var previousColor = _prevColor.Value;
		_prevColor = color;

		if (_cooldownRemaining > 0 || HasRecentEntry(candle))
			return;

		if (previousColor != 0 && color == 0 && Position <= 0)
		{
			var volumeToBuy = Volume + Math.Abs(Position);
			BuyMarket(volumeToBuy);
			_cooldownRemaining = SignalCooldownBars;
			_lastEntryTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		}
		else if (previousColor != 4 && color == 4 && Position >= 0)
		{
			var volumeToSell = Volume + Math.Abs(Position);
			SellMarket(volumeToSell);
			_cooldownRemaining = SignalCooldownBars;
			_lastEntryTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		}
	}

	private bool HasRecentEntry(ICandleMessage candle)
	{
		if (!_lastEntryTime.HasValue)
			return false;

		var candleTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		return candleTime.Date == _lastEntryTime.Value.Date;
	}
}
