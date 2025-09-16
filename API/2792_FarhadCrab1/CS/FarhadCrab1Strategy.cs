using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy converted from the FarhadCrab1 MT5 expert advisor.
/// The strategy enters on pullbacks to an EMA, manages risk with pip-based levels,
/// and closes positions based on a daily EMA crossover filter.
/// </summary>
public class FarhadCrab1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<int> _dailyMaLength;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _orderVolume;

	private readonly Queue<decimal> _maValues = new();

	private readonly DataType _dailyCandleType = TimeSpan.FromDays(1).TimeFrame();

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	private decimal? _prevDailyClose;
	private decimal? _prevDailyMa;
	private decimal? _prevPrevDailyClose;
	private decimal? _prevPrevDailyMa;

	private ICandleMessage _previousCandle;

	/// <summary>
	/// Working candle type for the execution timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the EMA used on the working timeframe.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Number of completed candles to shift the EMA backwards.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Period of the daily EMA that closes positions on crossovers.
	/// </summary>
	public int DailyMaLength
	{
		get => _dailyMaLength.Value;
		set => _dailyMaLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional pip distance required before updating the trailing stop again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Base order volume in lots/contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FarhadCrab1Strategy"/> class.
	/// </summary>
	public FarhadCrab1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Execution timeframe", "General");

		_maLength = Param(nameof(MaLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period on the working timeframe", "Indicators");

		_maShift = Param(nameof(MaShift), 0)
			.SetRange(0, 100)
			.SetDisplay("EMA Shift", "Shift EMA value backwards by N candles", "Indicators");

		_dailyMaLength = Param(nameof(DailyMaLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("Daily EMA Length", "EMA period used on daily candles", "Indicators");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetRange(0m, 500m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Protection");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetRange(0m, 500m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Protection");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetRange(0m, 500m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Protection");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetRange(0m, 500m)
			.SetDisplay("Trailing Step (pips)", "Extra gain in pips before updating the trailing stop", "Protection");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, _dailyCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_maValues.Clear();
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_prevDailyClose = null;
		_prevDailyMa = null;
		_prevPrevDailyClose = null;
		_prevPrevDailyMa = null;
		_previousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Ensure the base strategy volume reflects the configured parameter.
		base.Volume = OrderVolume;

		// Subscribe to the working timeframe candles with an EMA for entry decisions.
		var ema = new ExponentialMovingAverage { Length = MaLength };
		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.Bind(ema, ProcessWorkingCandle)
			.Start();

		// Subscribe to daily candles with another EMA for exit filtering.
		var dailyEma = new ExponentialMovingAverage { Length = DailyMaLength };
		var dailySubscription = SubscribeCandles(_dailyCandleType);
		dailySubscription
			.Bind(dailyEma, ProcessDailyCandle)
			.Start();

		// Draw candles, indicator, and trades on the chart if charting is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
			DrawIndicator(area, candleSubscription, ema);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle, decimal emaValue)
	{
		// Process only finished daily candles.
		if (candle.State != CandleStates.Finished)
			return;

		_prevPrevDailyClose = _prevDailyClose;
		_prevPrevDailyMa = _prevDailyMa;
		_prevDailyClose = candle.ClosePrice;
		_prevDailyMa = emaValue;
	}

	private void ProcessWorkingCandle(ICandleMessage candle, decimal emaValue)
	{
		// Use only completed candles for decision making.
		if (candle.State != CandleStates.Finished)
			return;

		// Skip processing when the environment is not ready for trading.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCandle = candle;
			return;
		}

		// Store EMA values so we can apply the configured shift.
		UpdateMaBuffer(emaValue);
		var shiftedMa = GetShiftedMaValue();
		if (shiftedMa == null)
		{
			_previousCandle = candle;
			return;
		}

		// Require at least one previous candle to evaluate entry conditions.
		if (_previousCandle == null)
		{
			_previousCandle = candle;
			return;
		}

		// Close positions when the daily EMA filter signals a crossover against us.
		if (TryCloseByDailyFilter())
		{
			_previousCandle = candle;
			return;
		}

		var pipSize = GetPipSize();

		// Check for stop-loss or take-profit triggers before adjusting trailing stops.
		if (CheckStopsAndTargets(candle))
		{
			_previousCandle = candle;
			return;
		}

		// Update trailing stop levels if the position has moved far enough.
		ApplyTrailingStop(candle, pipSize);

		// Evaluate long entry condition: previous low above the EMA.
		if (Position <= 0 && _previousCandle.LowPrice > shiftedMa.Value)
		{
			var volume = OrderVolume + (Position < 0 ? -Position : 0m);
			if (volume > 0)
			{
				BuyMarket(volume);
				SetRiskLevels(candle.ClosePrice, pipSize, true);
			}
			_previousCandle = candle;
			return;
		}

		// Evaluate short entry condition: previous high below the EMA.
		if (Position >= 0 && _previousCandle.HighPrice < shiftedMa.Value)
		{
			var volume = OrderVolume + (Position > 0 ? Position : 0m);
			if (volume > 0)
			{
				SellMarket(volume);
				SetRiskLevels(candle.ClosePrice, pipSize, false);
			}
			_previousCandle = candle;
			return;
		}

		// Store the current candle for the next iteration.
		_previousCandle = candle;
	}

	private bool TryCloseByDailyFilter()
	{
		if (_prevDailyClose == null || _prevDailyMa == null || _prevPrevDailyClose == null || _prevPrevDailyMa == null)
			return false;

		var prevClose = _prevDailyClose.Value;
		var prevMa = _prevDailyMa.Value;
		var prev2Close = _prevPrevDailyClose.Value;
		var prev2Ma = _prevPrevDailyMa.Value;

		// Bearish crossover: EMA moved above the daily close -> exit long positions.
		if (Position > 0 && prevMa > prevClose && prev2Ma < prev2Close)
		{
			SellMarket(Position);
			ResetRiskLevels();
			return true;
		}

		// Bullish crossover: EMA moved below the daily close -> exit short positions.
		if (Position < 0 && prevMa < prevClose && prev2Ma > prev2Close)
		{
			BuyMarket(-Position);
			ResetRiskLevels();
			return true;
		}

		return false;
	}

	private bool CheckStopsAndTargets(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(Position);
				ResetRiskLevels();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				ResetRiskLevels();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(-Position);
				ResetRiskLevels();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(-Position);
				ResetRiskLevels();
				return true;
			}
		}
		else if (_stopLossPrice.HasValue || _takeProfitPrice.HasValue)
		{
			// Clear stored levels when the position is flat.
			ResetRiskLevels();
		}

		return false;
	}

	private void ApplyTrailingStop(ICandleMessage candle, decimal pipSize)
	{
		if (TrailingStopPips <= 0m)
			return;

		var entryPrice = PositionPrice ?? 0m;
		var threshold = (TrailingStopPips + TrailingStepPips) * pipSize;

		if (Position > 0)
		{
			var profit = candle.ClosePrice - entryPrice;
			if (profit > threshold)
			{
				var minStop = candle.ClosePrice - threshold;
				var candidate = candle.ClosePrice - TrailingStopPips * pipSize;
				if (!_stopLossPrice.HasValue || _stopLossPrice.Value < minStop)
					_stopLossPrice = candidate;
			}
		}
		else if (Position < 0)
		{
			var profit = entryPrice - candle.ClosePrice;
			if (profit > threshold)
			{
				var maxStop = candle.ClosePrice + threshold;
				var candidate = candle.ClosePrice + TrailingStopPips * pipSize;
				if (!_stopLossPrice.HasValue || _stopLossPrice.Value > maxStop)
					_stopLossPrice = candidate;
			}
		}
	}

	private void SetRiskLevels(decimal executionPrice, decimal pipSize, bool isLong)
	{
		if (StopLossPips > 0m && pipSize > 0m)
			_stopLossPrice = isLong
				? executionPrice - StopLossPips * pipSize
				: executionPrice + StopLossPips * pipSize;
		else
			_stopLossPrice = null;

		if (TakeProfitPips > 0m && pipSize > 0m)
			_takeProfitPrice = isLong
				? executionPrice + TakeProfitPips * pipSize
				: executionPrice - TakeProfitPips * pipSize;
		else
			_takeProfitPrice = null;
	}

	private void ResetRiskLevels()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals;

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private void UpdateMaBuffer(decimal emaValue)
	{
		_maValues.Enqueue(emaValue);

		var maxCount = Math.Max(MaShift + 1, 1);
		while (_maValues.Count > maxCount)
			_maValues.Dequeue();
	}

	private decimal? GetShiftedMaValue()
	{
		var count = _maValues.Count;
		var targetIndex = count - MaShift - 1;
		if (targetIndex < 0)
			return null;

		var index = 0;
		foreach (var value in _maValues)
		{
			if (index == targetIndex)
				return value;
			index++;
		}

		return null;
	}
}
