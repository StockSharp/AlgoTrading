using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alert-only strategy converted from the MetaTrader 4 expert Alert_MACD_Slow.mq4.
/// It replicates the MACD slope and EMA trend filters to emit textual notifications when breakout conditions appear.
/// </summary>
public class AlertMacdSlowStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _quickEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd = null!;
	private ExponentialMovingAverage _quickEma = null!;
	private ExponentialMovingAverage _slowEma = null!;

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;
	private decimal? _macdPrev4;

	private decimal? _previousHigh;
	private decimal? _secondPreviousHigh;
	private decimal? _previousLow;
	private decimal? _secondPreviousLow;

	/// <summary>
	/// Initializes strategy parameters with defaults matching the MQL source.
	/// </summary>
	public AlertMacdSlowStrategy()
	{
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 3)
			.SetDisplay("MACD Fast Period", "Fast EMA length used inside the MACD calculation.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 12, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 20)
			.SetDisplay("MACD Slow Period", "Slow EMA length used inside the MACD calculation.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal Period", "Signal smoothing period for the MACD indicator.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_quickEmaPeriod = Param(nameof(QuickEmaPeriod), 20)
			.SetDisplay("Quick EMA Period", "Length of the fast EMA trend filter (Ma_Quick in MQL).", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 65)
			.SetDisplay("Slow EMA Period", "Length of the slow EMA trend filter (Ma_Slow in MQL).", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 100, 5);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(30)))
			.SetDisplay("Candle Type", "Timeframe used for indicator calculations.", "Data")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Fast EMA period for the MACD main line.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD main line.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period used by the MACD indicator.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period used as a trend filter.
	/// </summary>
	public int QuickEmaPeriod
	{
		get => _quickEmaPeriod.Value;
		set => _quickEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used as a trend filter.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the indicator chain.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod,
		};

		_quickEma = new ExponentialMovingAverage { Length = QuickEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _quickEma, _slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _quickEma);
			DrawIndicator(area, _slowEma);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue quickEmaValue, IIndicatorValue slowEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !quickEmaValue.IsFinal || !slowEmaValue.IsFinal)
			return;

		if (!_macd.IsFormed || !_quickEma.IsFormed || !_slowEma.IsFormed)
		{
			UpdateState(((MovingAverageConvergenceDivergenceValue)macdValue).Macd, candle);
			return;
		}

		var macdData = (MovingAverageConvergenceDivergenceValue)macdValue;
		var macdLine = macdData.Macd;
		var quickEma = quickEmaValue.ToDecimal();
		var slowEma = slowEmaValue.ToDecimal();

		var hasHistory = _macdPrev1.HasValue && _macdPrev2.HasValue && _macdPrev3.HasValue && _macdPrev4.HasValue
			&& _previousHigh.HasValue && _secondPreviousHigh.HasValue && _previousLow.HasValue && _secondPreviousLow.HasValue;

		if (hasHistory && IsFormedAndOnlineAndAllowTrading())
		{
			var macd1 = _macdPrev1!.Value;
			var macd2 = _macdPrev2!.Value;
			var macd3 = _macdPrev3!.Value;
			var macd4 = _macdPrev4!.Value;
			var high1 = _previousHigh!.Value;
			var high2 = _secondPreviousHigh!.Value;
			var low1 = _previousLow!.Value;
			var low2 = _secondPreviousLow!.Value;

			var bullishSetup = (macd1 > macd2 && macd2 < macd3 && quickEma > slowEma && candle.ClosePrice > high1 && macd2 < 0m)
				|| (macd2 > macd3 && macd3 < macd4 && quickEma > slowEma && candle.ClosePrice > high2 && macd3 < 0m);

			var bearishSetup = (macd1 < macd2 && macd2 > macd3 && quickEma < slowEma && candle.ClosePrice < low1 && macd2 > 0m)
				|| (macd2 < macd3 && macd3 > macd4 && quickEma < slowEma && candle.ClosePrice < low2 && macd3 > 0m);

			if (bullishSetup)
			{
				AddInfoLog($"SET UP LONG | MACD: [{macd1:F5}, {macd2:F5}, {macd3:F5}, {macd4:F5}] Close={candle.ClosePrice:F5} HighBreak={high1:F5}/{high2:F5}");
			}
			else if (bearishSetup)
			{
				AddInfoLog($"SET UP SHORT_VALUE | MACD: [{macd1:F5}, {macd2:F5}, {macd3:F5}, {macd4:F5}] Close={candle.ClosePrice:F5} LowBreak={low1:F5}/{low2:F5}");
			}
		}

		UpdateState(macdLine, candle);
	}

	private void UpdateState(decimal macdLine, ICandleMessage candle)
	{
		_macdPrev4 = _macdPrev3;
		_macdPrev3 = _macdPrev2;
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = macdLine;

		_secondPreviousHigh = _previousHigh;
		_previousHigh = candle.HighPrice;

		_secondPreviousLow = _previousLow;
		_previousLow = candle.LowPrice;
	}

	private void ResetState()
	{
		_macdPrev1 = null;
		_macdPrev2 = null;
		_macdPrev3 = null;
		_macdPrev4 = null;
		_previousHigh = null;
		_secondPreviousHigh = null;
		_previousLow = null;
		_secondPreviousLow = null;
	}
}
