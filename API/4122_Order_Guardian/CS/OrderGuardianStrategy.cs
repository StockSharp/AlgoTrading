using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Order management helper that mirrors the MetaTrader OrderGuardian expert.
/// It tracks the current position and closes it once the configured take-profit or stop-loss level is reached.
/// </summary>
public class OrderGuardianStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TakeProfitMethodOption> _takeProfitMethod;
	private readonly StrategyParam<StopLossMethodOption> _stopLossMethod;
	private readonly StrategyParam<int> _takeProfitPeriod;
	private readonly StrategyParam<int> _stopLossPeriod;
	private readonly StrategyParam<MovingAverageMethodOption> _takeProfitMaMethod;
	private readonly StrategyParam<MovingAverageMethodOption> _stopLossMaMethod;
	private readonly StrategyParam<AppliedPriceOption> _takeProfitPriceType;
	private readonly StrategyParam<AppliedPriceOption> _stopLossPriceType;
	private readonly StrategyParam<decimal> _takeProfitDeviation;
	private readonly StrategyParam<decimal> _stopLossDeviation;
	private readonly StrategyParam<int> _takeProfitShift;
	private readonly StrategyParam<int> _stopLossShift;
	private readonly StrategyParam<decimal> _manualTakeProfitLong;
	private readonly StrategyParam<decimal> _manualTakeProfitShort;
	private readonly StrategyParam<decimal> _manualStopLossLong;
	private readonly StrategyParam<decimal> _manualStopLossShort;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<bool> _showLines;

	private IIndicator? _takeProfitMaIndicator;
	private IIndicator? _stopLossMaIndicator;
	private ParabolicSar? _sarIndicator;

	private decimal[] _takeProfitBuffer = Array.Empty<decimal>();
	private int _takeProfitWriteIndex;
	private int _takeProfitCount;
	private decimal[] _stopLossBuffer = Array.Empty<decimal>();
	private int _stopLossWriteIndex;
	private int _stopLossCount;
	private decimal[] _sarBuffer = Array.Empty<decimal>();
	private int _sarWriteIndex;
	private int _sarCount;

	private ChartArea? _chartArea;
	private DateTimeOffset? _lastGuideTime;
	private decimal? _lastGuideTakeProfit;
	private decimal? _lastGuideStopLoss;
	private string? _lastStatusMessage;

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderGuardianStrategy"/> class.
	/// All parameters match the original MetaTrader inputs to keep behaviour familiar.
	/// </summary>
	public OrderGuardianStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles processed by the strategy.", "General");

		_takeProfitMethod = Param(nameof(TakeProfitMethod), TakeProfitMethodOption.ManualLine)
			.SetDisplay("Take Profit Method", "Source used to compute the take-profit level.", "Take Profit");

		_stopLossMethod = Param(nameof(StopLossMethod), StopLossMethodOption.ManualLine)
			.SetDisplay("Stop Loss Method", "Source used to compute the stop-loss level.", "Stop Loss");

		_takeProfitPeriod = Param(nameof(TakeProfitPeriod), 31)
			.SetGreaterThanZero()
			.SetDisplay("TP MA Period", "Moving average length for the take-profit envelope.", "Take Profit");

		_stopLossPeriod = Param(nameof(StopLossPeriod), 31)
			.SetGreaterThanZero()
			.SetDisplay("SL MA Period", "Moving average length for the stop-loss envelope.", "Stop Loss");

		_takeProfitMaMethod = Param(nameof(TakeProfitMaMethod), MovingAverageMethodOption.Exponential)
			.SetDisplay("TP MA Method", "Moving average calculation used for the take-profit envelope.", "Take Profit");

		_stopLossMaMethod = Param(nameof(StopLossMaMethod), MovingAverageMethodOption.Exponential)
			.SetDisplay("SL MA Method", "Moving average calculation used for the stop-loss envelope.", "Stop Loss");

		_takeProfitPriceType = Param(nameof(TakeProfitPriceType), AppliedPriceOption.Close)
			.SetDisplay("TP Price Source", "Price source fed into the take-profit moving average.", "Take Profit");

		_stopLossPriceType = Param(nameof(StopLossPriceType), AppliedPriceOption.Close)
			.SetDisplay("SL Price Source", "Price source fed into the stop-loss moving average.", "Stop Loss");

		_takeProfitDeviation = Param(nameof(TakeProfitDeviation), 0.2m)
			.SetDisplay("TP Deviation %", "Envelope deviation applied to the take-profit moving average.", "Take Profit");

		_stopLossDeviation = Param(nameof(StopLossDeviation), 0m)
			.SetDisplay("SL Deviation %", "Envelope deviation applied to the stop-loss moving average.", "Stop Loss");

		_takeProfitShift = Param(nameof(TakeProfitShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("TP Shift", "Number of completed candles used as shift for the take-profit MA.", "Take Profit");

		_stopLossShift = Param(nameof(StopLossShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("SL Shift", "Number of completed candles used as shift for the stop-loss MA or SAR.", "Stop Loss");

		_manualTakeProfitLong = Param(nameof(ManualTakeProfitLong), 0m)
			.SetDisplay("Manual TP Long", "Manual take-profit price for long positions (0 disables the level).", "Take Profit");

		_manualTakeProfitShort = Param(nameof(ManualTakeProfitShort), 0m)
			.SetDisplay("Manual TP Short", "Manual take-profit price for short positions (0 disables the level).", "Take Profit");

		_manualStopLossLong = Param(nameof(ManualStopLossLong), 0m)
			.SetDisplay("Manual SL Long", "Manual stop-loss price for long positions (0 disables the level).", "Stop Loss");

		_manualStopLossShort = Param(nameof(ManualStopLossShort), 0m)
			.SetDisplay("Manual SL Short", "Manual stop-loss price for short positions (0 disables the level).", "Stop Loss");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Acceleration factor for the Parabolic SAR based stop.", "Stop Loss");

		_sarMaximum = Param(nameof(SarMaximum), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Maximum", "Maximum acceleration factor for the Parabolic SAR based stop.", "Stop Loss");

		_showLines = Param(nameof(ShowLines), true)
			.SetDisplay("Show Guide Lines", "Draw the current take-profit and stop-loss levels on the chart.", "Visuals");
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take-profit calculation method.
	/// </summary>
	public TakeProfitMethodOption TakeProfitMethod
	{
		get => _takeProfitMethod.Value;
		set => _takeProfitMethod.Value = value;
	}

	/// <summary>
	/// Stop-loss calculation method.
	/// </summary>
	public StopLossMethodOption StopLossMethod
	{
		get => _stopLossMethod.Value;
		set => _stopLossMethod.Value = value;
	}

	/// <summary>
	/// Moving average period for the take-profit envelope.
	/// </summary>
	public int TakeProfitPeriod
	{
		get => _takeProfitPeriod.Value;
		set => _takeProfitPeriod.Value = value;
	}

	/// <summary>
	/// Moving average period for the stop-loss envelope.
	/// </summary>
	public int StopLossPeriod
	{
		get => _stopLossPeriod.Value;
		set => _stopLossPeriod.Value = value;
	}

	/// <summary>
	/// Moving average method used to calculate the take-profit envelope.
	/// </summary>
	public MovingAverageMethodOption TakeProfitMaMethod
	{
		get => _takeProfitMaMethod.Value;
		set => _takeProfitMaMethod.Value = value;
	}

	/// <summary>
	/// Moving average method used to calculate the stop-loss envelope.
	/// </summary>
	public MovingAverageMethodOption StopLossMaMethod
	{
		get => _stopLossMaMethod.Value;
		set => _stopLossMaMethod.Value = value;
	}

	/// <summary>
	/// Price source for the take-profit moving average.
	/// </summary>
	public AppliedPriceOption TakeProfitPriceType
	{
		get => _takeProfitPriceType.Value;
		set => _takeProfitPriceType.Value = value;
	}

	/// <summary>
	/// Price source for the stop-loss moving average.
	/// </summary>
	public AppliedPriceOption StopLossPriceType
	{
		get => _stopLossPriceType.Value;
		set => _stopLossPriceType.Value = value;
	}

	/// <summary>
	/// Envelope deviation for the take-profit level in percent.
	/// </summary>
	public decimal TakeProfitDeviation
	{
		get => _takeProfitDeviation.Value;
		set => _takeProfitDeviation.Value = value;
	}

	/// <summary>
	/// Envelope deviation for the stop-loss level in percent.
	/// </summary>
	public decimal StopLossDeviation
	{
		get => _stopLossDeviation.Value;
		set => _stopLossDeviation.Value = value;
	}

	/// <summary>
	/// Number of completed candles used as shift for the take-profit moving average.
	/// </summary>
	public int TakeProfitShift
	{
		get => _takeProfitShift.Value;
		set => _takeProfitShift.Value = value;
	}

	/// <summary>
	/// Number of completed candles used as shift for the stop-loss moving average or SAR.
	/// </summary>
	public int StopLossShift
	{
		get => _stopLossShift.Value;
		set => _stopLossShift.Value = value;
	}

	/// <summary>
	/// Manual take-profit price for long positions.
	/// </summary>
	public decimal ManualTakeProfitLong
	{
		get => _manualTakeProfitLong.Value;
		set => _manualTakeProfitLong.Value = value;
	}

	/// <summary>
	/// Manual take-profit price for short positions.
	/// </summary>
	public decimal ManualTakeProfitShort
	{
		get => _manualTakeProfitShort.Value;
		set => _manualTakeProfitShort.Value = value;
	}

	/// <summary>
	/// Manual stop-loss price for long positions.
	/// </summary>
	public decimal ManualStopLossLong
	{
		get => _manualStopLossLong.Value;
		set => _manualStopLossLong.Value = value;
	}

	/// <summary>
	/// Manual stop-loss price for short positions.
	/// </summary>
	public decimal ManualStopLossShort
	{
		get => _manualStopLossShort.Value;
		set => _manualStopLossShort.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Enables drawing of guide lines for the active levels.
	/// </summary>
	public bool ShowLines
	{
		get => _showLines.Value;
		set => _showLines.Value = value;
	}

	/// <summary>
	/// Latest formatted status line summarising the active levels.
	/// </summary>
	public string? StatusLine => _lastStatusMessage;

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetBuffers();
		_lastStatusMessage = null;
		ResetGuideLines();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// The original expert enforces a minimum shift of one bar for Parabolic SAR to avoid repainting.
		if (StopLossMethod == StopLossMethodOption.ParabolicSar && StopLossShift < 1)
		{
			StopLossShift = 1;
		}

		InitializeIndicators();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		_chartArea = CreateChartArea();
		if (_chartArea != null)
		{
			DrawCandles(_chartArea, subscription);
			DrawOwnTrades(_chartArea);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with completed candles just like the MetaTrader script.
		if (candle.State != CandleStates.Finished)
			return;

		var (takeProfitLong, takeProfitShort) = CalculateTakeProfit(candle);
		var (stopLossLong, stopLossShort) = CalculateStopLoss(candle);

	decimal? activeTake = null;
	decimal? activeStop = null;

	if (Position > 0m)
	{
		activeTake = takeProfitLong;
		activeStop = stopLossLong;
	}
	else if (Position < 0m)
	{
		activeTake = takeProfitShort;
		activeStop = stopLossShort;
	}
	else
	{
		ResetGuideLines();
	}

	UpdateGuideLines(candle, activeTake, activeStop);

	if (!IsFormedAndOnlineAndAllowTrading())
		return;

	if (Position > 0m)
	{
		if (activeStop.HasValue && candle.LowPrice <= activeStop.Value)
		{
			ClosePosition();
			ResetGuideLines();
			UpdateStatusInfo(null, null);
			return;
		}

		if (activeTake.HasValue && candle.HighPrice >= activeTake.Value)
		{
			ClosePosition();
			ResetGuideLines();
			UpdateStatusInfo(null, null);
			return;
		}
	}
	else if (Position < 0m)
	{
		if (activeStop.HasValue && candle.HighPrice >= activeStop.Value)
		{
			ClosePosition();
			ResetGuideLines();
			UpdateStatusInfo(null, null);
			return;
		}

		if (activeTake.HasValue && candle.LowPrice <= activeTake.Value)
		{
			ClosePosition();
			ResetGuideLines();
			UpdateStatusInfo(null, null);
		}
	}

	if (Position != 0m)
	{
		UpdateStatusInfo(activeStop, activeTake);
	}
	else if (_lastStatusMessage != null)
	{
		UpdateStatusInfo(null, null);
	}
	}

	private (decimal? longPrice, decimal? shortPrice) CalculateTakeProfit(ICandleMessage candle)
	{
		switch (TakeProfitMethod)
		{
			case TakeProfitMethodOption.Envelope:
			{
				if (_takeProfitMaIndicator == null)
					return (null, null);

				var price = GetAppliedPrice(candle, TakeProfitPriceType);
				var value = _takeProfitMaIndicator.Process(price, candle.OpenTime, true);
				if (!_takeProfitMaIndicator.IsFormed)
					return (null, null);

				var ma = value.ToDecimal();
				var shifted = PushValue(ref _takeProfitBuffer, ref _takeProfitWriteIndex, ref _takeProfitCount, ma, TakeProfitShift);
				if (shifted == null)
					return (null, null);

				var envelope = shifted.Value * (1m + TakeProfitDeviation / 100m);
				return (envelope, envelope);
			}
			case TakeProfitMethodOption.ManualLine:
			{
				var longPrice = ManualTakeProfitLong > 0m ? ManualTakeProfitLong : (decimal?)null;
				var shortPrice = ManualTakeProfitShort > 0m ? ManualTakeProfitShort : (decimal?)null;
				return (longPrice, shortPrice);
			}
			default:
				return (null, null);
		}
	}

	private (decimal? longPrice, decimal? shortPrice) CalculateStopLoss(ICandleMessage candle)
	{
		switch (StopLossMethod)
		{
			case StopLossMethodOption.Envelope:
			{
				if (_stopLossMaIndicator == null)
					return (null, null);

				var price = GetAppliedPrice(candle, StopLossPriceType);
				var value = _stopLossMaIndicator.Process(price, candle.OpenTime, true);
				if (!_stopLossMaIndicator.IsFormed)
					return (null, null);

				var ma = value.ToDecimal();
				var shifted = PushValue(ref _stopLossBuffer, ref _stopLossWriteIndex, ref _stopLossCount, ma, StopLossShift);
				if (shifted == null)
					return (null, null);

				var envelope = shifted.Value * (1m + StopLossDeviation / 100m);
				return (envelope, envelope);
			}
			case StopLossMethodOption.ManualLine:
			{
				var longPrice = ManualStopLossLong > 0m ? ManualStopLossLong : (decimal?)null;
				var shortPrice = ManualStopLossShort > 0m ? ManualStopLossShort : (decimal?)null;
				return (longPrice, shortPrice);
			}
			case StopLossMethodOption.ParabolicSar:
			{
				if (_sarIndicator == null)
					return (null, null);

				var value = _sarIndicator.Process(candle);
				if (!_sarIndicator.IsFormed)
					return (null, null);

				var sar = value.ToDecimal();
				var shifted = PushValue(ref _sarBuffer, ref _sarWriteIndex, ref _sarCount, sar, StopLossShift);
				if (shifted == null)
					return (null, null);

				var level = shifted.Value;
				return (level, level);
			}
			default:
				return (null, null);
		}
	}

	private void InitializeIndicators()
	{
		_takeProfitMaIndicator = TakeProfitMethod == TakeProfitMethodOption.Envelope
			? CreateMovingAverage(TakeProfitMaMethod, TakeProfitPeriod)
			: null;

		_stopLossMaIndicator = StopLossMethod == StopLossMethodOption.Envelope
			? CreateMovingAverage(StopLossMaMethod, StopLossPeriod)
			: null;

		_sarIndicator = StopLossMethod == StopLossMethodOption.ParabolicSar
			? new ParabolicSar { AccelerationStep = SarStep, AccelerationMax = SarMaximum }
			: null;

		ResetBuffers();
	}

	private void ResetBuffers()
	{
		_takeProfitBuffer = Array.Empty<decimal>();
		_takeProfitWriteIndex = 0;
		_takeProfitCount = 0;

		_stopLossBuffer = Array.Empty<decimal>();
		_stopLossWriteIndex = 0;
		_stopLossCount = 0;

		_sarBuffer = Array.Empty<decimal>();
		_sarWriteIndex = 0;
		_sarCount = 0;
	}

	private void UpdateGuideLines(ICandleMessage candle, decimal? take, decimal? stop)
	{
		if (!ShowLines || _chartArea == null)
			return;

		if (take == null && stop == null)
		{
			_lastGuideTime = candle.CloseTime;
			_lastGuideTakeProfit = null;
			_lastGuideStopLoss = null;
			return;
		}

		if (_lastGuideTime != null && _lastGuideTakeProfit != null && take != null)
		{
			DrawLine(_lastGuideTime.Value, _lastGuideTakeProfit.Value, candle.CloseTime, take.Value);
		}

		if (_lastGuideTime != null && _lastGuideStopLoss != null && stop != null)
		{
			DrawLine(_lastGuideTime.Value, _lastGuideStopLoss.Value, candle.CloseTime, stop.Value);
		}

		_lastGuideTime = candle.CloseTime;
		_lastGuideTakeProfit = take;
		_lastGuideStopLoss = stop;
	}

	private void ResetGuideLines()
	{
		_lastGuideTime = null;
		_lastGuideTakeProfit = null;
		_lastGuideStopLoss = null;
	}

	private void UpdateStatusInfo(decimal? stop, decimal? take)
	{
		var stopText = stop?.ToString("0.#####") ?? "__";
		var takeText = take?.ToString("0.#####") ?? "__";
		var message = $"S/L @ {stopText}   T/P @ {takeText}";

		if (message == _lastStatusMessage)
			return;

		_lastStatusMessage = take == null && stop == null ? null : message;

		if (_lastStatusMessage != null)
			LogInfo(_lastStatusMessage);
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethodOption method, int period)
	{
		return method switch
		{
			MovingAverageMethodOption.Simple => new SimpleMovingAverage { Length = period },
			MovingAverageMethodOption.Exponential => new ExponentialMovingAverage { Length = period },
			MovingAverageMethodOption.Smoothed => new SmoothedMovingAverage { Length = period },
			MovingAverageMethodOption.LinearWeighted => new WeightedMovingAverage { Length = period },
			_ => new ExponentialMovingAverage { Length = period }
		};
	}

	private static decimal? PushValue(ref decimal[] buffer, ref int writeIndex, ref int count, decimal value, int shift)
	{
		var required = Math.Max(shift + 1, 1);
		if (buffer.Length != required)
		{
			var newBuffer = new decimal[required];
			var toCopy = Math.Min(count, required);

			for (var i = 0; i < toCopy; i++)
			{
				var sourceIndex = writeIndex - count + i;
				while (sourceIndex < 0)
					sourceIndex += buffer.Length;
				sourceIndex %= buffer.Length == 0 ? 1 : buffer.Length;
				newBuffer[i] = buffer.Length == 0 ? 0m : buffer[sourceIndex];
			}

			buffer = newBuffer;
			count = toCopy;
			writeIndex = toCopy % required;
		}

		if (buffer.Length == 0)
		{
			buffer = new decimal[required];
			writeIndex = 0;
			count = 0;
		}

		buffer[writeIndex] = value;
		writeIndex = (writeIndex + 1) % buffer.Length;

		if (count < buffer.Length)
			count++;

		if (shift >= count)
			return null;

		var index = writeIndex - 1 - shift;
		while (index < 0)
			index += buffer.Length;

		return buffer[index];
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceOption priceType)
	{
		return priceType switch
		{
			AppliedPriceOption.Open => candle.OpenPrice,
			AppliedPriceOption.High => candle.HighPrice,
			AppliedPriceOption.Low => candle.LowPrice,
			AppliedPriceOption.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceOption.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceOption.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	/// <summary>
	/// Take-profit calculation choices.
	/// </summary>
	public enum TakeProfitMethodOption
	{
		Envelope = 1,
		ManualLine = 2
	}

	/// <summary>
	/// Stop-loss calculation choices.
	/// </summary>
	public enum StopLossMethodOption
	{
		Envelope = 1,
		ManualLine = 2,
		ParabolicSar = 3
	}

	/// <summary>
	/// Moving average calculation types compatible with the MetaTrader constants.
	/// </summary>
	public enum MovingAverageMethodOption
	{
		Simple = 0,
		Exponential = 1,
		Smoothed = 2,
		LinearWeighted = 3
	}

	/// <summary>
	/// Price sources equivalent to the MetaTrader applied price options.
	/// </summary>
	public enum AppliedPriceOption
	{
		Close = 0,
		Open = 1,
		High = 2,
		Low = 3,
		Median = 4,
		Typical = 5,
		Weighted = 6
	}
}
