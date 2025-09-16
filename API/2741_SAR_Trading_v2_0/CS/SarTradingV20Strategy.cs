using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR and shifted SMA trend strategy inspired by the original MQL5 version.
/// Opens long positions when SAR or the shifted close confirm bullish alignment.
/// Opens short positions when SAR or the shifted close confirm bearish alignment.
/// Includes configurable fixed stops, take profit and trailing stop with step filter.
/// </summary>
public class SarTradingV20Strategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma = null!;
	private ParabolicSar _parabolicSar = null!;
	private readonly List<decimal> _closeHistory = new();

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _pipSize;
	private bool _exitPending;

	/// <summary>
	/// SMA length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars to shift the close comparison.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMaxStep
	{
		get => _sarMaxStep.Value;
		set => _sarMaxStep.Value = value;
	}

	/// <summary>
	/// Stop-loss size in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit size in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum advance before the trailing stop moves.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize parameters for the strategy.
	/// </summary>
	public SarTradingV20Strategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Number of bars for the simple moving average.", "Indicators")
			.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 2)
			.SetGreaterOrEqualZero()
			.SetDisplay("MA Shift", "Bars to shift the close comparison against the SMA.", "Indicators");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR.", "Indicators")
			.SetCanOptimize(true);

		_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum acceleration factor for Parabolic SAR.", "Indicators")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Fixed stop-loss distance expressed in pips.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Fixed take-profit distance expressed in pips.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips.", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Additional profit before trailing stop moves.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type subscribed for processing.", "Data");
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

		_ma = null!;
		_parabolicSar = null!;
		_closeHistory.Clear();
		ResetPositionState();
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		{
			// Fallback to a default pip size when the security does not provide one.
			_pipSize = 0.0001m;
		}

		_ma = new SimpleMovingAverage { Length = MaPeriod };
		_parabolicSar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMaxStep
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_ma, _parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal sarValue)
	{
		// Only react on completed candles to mirror the original expert behavior.
		if (candle.State != CandleStates.Finished)
			return;

		// Keep a sliding window of closes for shifted comparisons.
		UpdateCloseHistory(candle.ClosePrice);

		// Clear pending state when the previous exit was filled.
		if (_exitPending && Position == 0)
		{
			ResetPositionState();
		}

		// Manage the active position before looking for new entries.
		if (Position != 0)
		{
			ManageExistingPosition(candle);
			return;
		}

		if (!_ma.IsFormed || !_parabolicSar.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_closeHistory.Count <= MaShift)
			return;

		var shiftedClose = _closeHistory[_closeHistory.Count - 1 - MaShift];

		var sarBelowMa = sarValue < maValue;
		var sarAboveMa = sarValue > maValue;
		var closeBelowMa = shiftedClose < maValue;
		var closeAboveMa = shiftedClose > maValue;

		if (sarBelowMa || closeBelowMa)
		{
			OpenLong(candle.ClosePrice, sarBelowMa, closeBelowMa);
		}
		else if (sarAboveMa || closeAboveMa)
		{
			OpenShort(candle.ClosePrice, sarAboveMa, closeAboveMa);
		}
	}

	private void ManageExistingPosition(ICandleMessage candle)
	{
		if (_exitPending)
			return;

		if (_entryPrice == null)
			return;

		if (Position > 0)
		{
			UpdateTrailingForLong(candle);
			TryExitLong(candle);
		}
		else if (Position < 0)
		{
			UpdateTrailingForShort(candle);
			TryExitShort(candle);
		}
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _entryPrice == null)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var profit = candle.ClosePrice - _entryPrice.Value;

		// Move the stop only after price advanced by trailing distance plus the configured step.
		if (profit <= trailingDistance + trailingStep)
			return;

		var candidate = candle.ClosePrice - trailingDistance;
		var minIncrease = TrailingStepPips > 0 ? trailingStep : 0m;

		if (_stopPrice == null || candidate > _stopPrice.Value + minIncrease)
		{
			_stopPrice = candidate;
			LogInfo($"Updated long trailing stop to {_stopPrice.Value:0.#####}.");
		}
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || _entryPrice == null)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;
		var profit = _entryPrice.Value - candle.ClosePrice;

		// Move the stop only after price advanced by trailing distance plus the configured step.
		if (profit <= trailingDistance + trailingStep)
			return;

		var candidate = candle.ClosePrice + trailingDistance;
		var minDecrease = TrailingStepPips > 0 ? trailingStep : 0m;

		if (_stopPrice == null || candidate < _stopPrice.Value - minDecrease)
		{
			_stopPrice = candidate;
			LogInfo($"Updated short trailing stop to {_stopPrice.Value:0.#####}.");
		}
	}

	private void TryExitLong(ICandleMessage candle)
	{
		var position = Math.Abs(Position);
		if (position <= 0)
			return;

		if (_stopPrice != null && candle.LowPrice <= _stopPrice.Value)
		{
			SellMarket(position);
			_exitPending = true;
			LogInfo($"Exit long via stop at {_stopPrice.Value:0.#####}.");
			return;
		}

		if (_takeProfitPrice != null && candle.HighPrice >= _takeProfitPrice.Value)
		{
			SellMarket(position);
			_exitPending = true;
			LogInfo($"Exit long via take profit at {_takeProfitPrice.Value:0.#####}.");
		}
	}

	private void TryExitShort(ICandleMessage candle)
	{
		var position = Math.Abs(Position);
		if (position <= 0)
			return;

		if (_stopPrice != null && candle.HighPrice >= _stopPrice.Value)
		{
			BuyMarket(position);
			_exitPending = true;
			LogInfo($"Exit short via stop at {_stopPrice.Value:0.#####}.");
			return;
		}

		if (_takeProfitPrice != null && candle.LowPrice <= _takeProfitPrice.Value)
		{
			BuyMarket(position);
			_exitPending = true;
			LogInfo($"Exit short via take profit at {_takeProfitPrice.Value:0.#####}.");
		}
	}

	private void OpenLong(decimal price, bool triggeredBySar, bool triggeredByShiftedClose)
	{
		var volume = Volume;
		if (volume <= 0)
			return;

		BuyMarket(volume);

		InitializePositionState(price, true);

		var reason = triggeredBySar && triggeredByShiftedClose
			? "Parabolic SAR below SMA and shifted close below SMA"
			: triggeredBySar
				? "Parabolic SAR below SMA"
				: "Shifted close below SMA";

		LogInfo($"Open long at {price:0.#####}: {reason}.");
	}

	private void OpenShort(decimal price, bool triggeredBySar, bool triggeredByShiftedClose)
	{
		var volume = Volume;
		if (volume <= 0)
			return;

		SellMarket(volume);

		InitializePositionState(price, false);

		var reason = triggeredBySar && triggeredByShiftedClose
			? "Parabolic SAR above SMA and shifted close above SMA"
			: triggeredBySar
				? "Parabolic SAR above SMA"
				: "Shifted close above SMA";

		LogInfo($"Open short at {price:0.#####}: {reason}.");
	}

	private void InitializePositionState(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;
		_exitPending = false;

		var pip = _pipSize > 0m ? _pipSize : 0.0001m;

		_stopPrice = StopLossPips > 0
			? isLong
				? entryPrice - StopLossPips * pip
				: entryPrice + StopLossPips * pip
			: null;

		_takeProfitPrice = TakeProfitPips > 0
			? isLong
				? entryPrice + TakeProfitPips * pip
				: entryPrice - TakeProfitPips * pip
			: null;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_exitPending = false;
	}

	private void UpdateCloseHistory(decimal closePrice)
	{
		_closeHistory.Add(closePrice);

		var maxCount = Math.Max(MaShift + 1, 1);
		if (_closeHistory.Count > maxCount)
		{
			_closeHistory.RemoveRange(0, _closeHistory.Count - maxCount);
		}
	}
}
