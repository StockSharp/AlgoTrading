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
/// AOCCI strategy converted from MetaTrader 4.
/// Uses Awesome Oscillator and CCI filters combined with a daily pivot calculation.
/// Filters out large opening gaps and manages positions with optional trailing stop.
/// </summary>
public class AocciPivotFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _signalCandleOffset;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _bigJumpPoints;
	private readonly StrategyParam<decimal> _doubleJumpPoints;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private decimal? _previousAo;
	private decimal? _previousCci;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _entryPrice;
	private int _positionDirection;
	private int _openHistoryCount;
	private readonly decimal?[] _openHistory = new decimal?[6];
	private readonly DailyPivotInfo?[] _dailyHistory = new DailyPivotInfo?[10];
	private int _dailyHistoryCount;

	private record struct DailyPivotInfo(decimal High, decimal Low, decimal Close)
	{
		public decimal Pivot => (High + Low + Close) / 3m;
	}

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Additional shift applied when referencing previous daily candles.
	/// </summary>
	public int SignalCandleOffset
	{
		get => _signalCandleOffset.Value;
		set => _signalCandleOffset.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Threshold for filtering single bar opening gaps.
	/// </summary>
	public decimal BigJumpPoints
	{
		get => _bigJumpPoints.Value;
		set => _bigJumpPoints.Value = value;
	}

	/// <summary>
	/// Threshold for filtering two-bar combined gaps.
	/// </summary>
	public decimal DoubleJumpPoints
	{
		get => _doubleJumpPoints.Value;
		set => _doubleJumpPoints.Value = value;
	}

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Intraday candle type (default one-hour bars).
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Daily candle type used for pivot calculations.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <summary>
	/// Constructor initializes all configurable parameters.
	/// </summary>
	public AocciPivotFilterStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for the Commodity Channel Index.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(30, 80, 5);

		_signalCandleOffset = Param(nameof(SignalCandleOffset), 0)
			.SetDisplay("Signal Candle Offset", "Additional shift applied when referencing daily candles.", "General");

		_stopLossPoints = Param(nameof(StopLossPoints), 40m)
			.SetDisplay("Stop Loss (points)", "Stop-loss distance expressed in price steps.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetDisplay("Take Profit (points)", "Take-profit distance expressed in price steps.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 150m, 10m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance in price steps.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 10m);

		_bigJumpPoints = Param(nameof(BigJumpPoints), 1000m)
			.SetDisplay("Big Jump Filter", "Maximum allowed single gap in price steps.", "Filters");

		_doubleJumpPoints = Param(nameof(DoubleJumpPoints), 1000m)
			.SetDisplay("Double Jump Filter", "Maximum allowed combined two-bar gap in price steps.", "Filters");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used when submitting market orders.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Intraday Candle", "Time frame for intraday calculations.", "Data");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candle", "Time frame used for pivot calculation.", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DailyCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousAo = null;
		_previousCci = null;
		_stopPrice = null;
		_takePrice = null;
		_entryPrice = 0m;
		_positionDirection = 0;
		_openHistoryCount = 0;
		Array.Clear(_openHistory, 0, _openHistory.Length);
		_dailyHistoryCount = 0;
		Array.Clear(_dailyHistory, 0, _dailyHistory.Length);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var awesomeOscillator = new AwesomeOscillator();
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var hourlySubscription = SubscribeCandles(CandleType);
		hourlySubscription
			.Bind(awesomeOscillator, cci, (candle, aoValue, cciValue) => ProcessHourlyCandle(candle, awesomeOscillator, cci, aoValue, cciValue))
			.Start();

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = _dailyHistory.Length - 1; i > 0; i--)
			_dailyHistory[i] = _dailyHistory[i - 1];

		_dailyHistory[0] = new DailyPivotInfo(candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		if (_dailyHistoryCount < _dailyHistory.Length)
			_dailyHistoryCount++;
	}

	private void ProcessHourlyCandle(ICandleMessage candle, AwesomeOscillator awesomeOscillator, CommodityChannelIndex cci, decimal aoValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!awesomeOscillator.IsFormed || !cci.IsFormed)
		{
			_previousAo = aoValue;
			_previousCci = cciValue;
			return;
		}

		UpdateOpenHistory(candle.OpenPrice);

		var pip = GetPipSize();
		if (!HasEnoughOpenHistory() || pip <= 0m)
		{
			_previousAo = aoValue;
			_previousCci = cciValue;
			return;
		}

		if (IsJumpDetected(pip))
		{
			_previousAo = aoValue;
			_previousCci = cciValue;
			return;
		}

		var pivot = GetPivot();
		if (pivot is null)
		{
			_previousAo = aoValue;
			_previousCci = cciValue;
			return;
		}

		if (ManageOpenPosition(candle, pip))
		{
			_previousAo = aoValue;
			_previousCci = cciValue;
			return;
		}

		var previousAo = _previousAo;
		var previousCci = _previousCci;

		_previousAo = aoValue;
		_previousCci = cciValue;

		if (previousAo is null || previousCci is null)
			return;

		if (Position != 0m)
			return;

		var close = candle.ClosePrice;
		var pivotValue = pivot.Value;

		if (aoValue > 0m && cciValue >= 0m && close > pivotValue && (previousAo < 0m || previousCci <= 0m || close < pivotValue))
		{
			TryOpenLong(close, pip);
			return;
		}

		if (aoValue > 0m && cciValue >= 0m && close > pivotValue && (previousAo < 0m || previousCci <= 0m || close < pivotValue))
			TryOpenShort(close, pip);
	}

	private bool ManageOpenPosition(ICandleMessage candle, decimal pip)
	{
		if (Position == 0m)
		{
			ResetPositionState();
			return false;
		}

		if (_positionDirection > 0)
		{
			if (TrailingStopPoints > 0m && candle.ClosePrice - _entryPrice > TrailingStopPoints * pip)
			{
				var desiredStop = candle.ClosePrice - TrailingStopPoints * pip;
				if (_stopPrice is null || desiredStop > _stopPrice)
					_stopPrice = desiredStop;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
				return ExitPosition();

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
				return ExitPosition();
		}
		else if (_positionDirection < 0)
		{
			if (TrailingStopPoints > 0m && _entryPrice - candle.ClosePrice > TrailingStopPoints * pip)
			{
				var desiredStop = candle.ClosePrice + TrailingStopPoints * pip;
				if (_stopPrice is null || desiredStop < _stopPrice)
					_stopPrice = desiredStop;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
				return ExitPosition();

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
				return ExitPosition();
		}

		return false;
	}

	private void TryOpenLong(decimal close, decimal pip)
	{
		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		SetPositionState(close, 1, pip);
	}

	private void TryOpenShort(decimal close, decimal pip)
	{
		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		SellMarket(volume);
		SetPositionState(close, -1, pip);
	}

	private void SetPositionState(decimal price, int direction, decimal pip)
	{
		_positionDirection = direction;
		_entryPrice = price;
		_stopPrice = StopLossPoints > 0m ? (direction > 0 ? price - StopLossPoints * pip : price + StopLossPoints * pip) : null;
		_takePrice = TakeProfitPoints > 0m ? (direction > 0 ? price + TakeProfitPoints * pip : price - TakeProfitPoints * pip) : null;
	}

	private bool ExitPosition()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		if (_positionDirection > 0)
			SellMarket(volume);
		else if (_positionDirection < 0)
			BuyMarket(volume);

		ResetPositionState();
		return true;
	}

	private void ResetPositionState()
	{
		_positionDirection = 0;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal? GetPivot()
	{
		var index = SignalCandleOffset + 1;
		if (index < 0)
			return null;

		if (index >= _dailyHistoryCount)
			return null;

		var info = _dailyHistory[index];
		return info?.Pivot;
	}

	private void UpdateOpenHistory(decimal openPrice)
	{
		for (var i = _openHistory.Length - 1; i > 0; i--)
			_openHistory[i] = _openHistory[i - 1];

		_openHistory[0] = openPrice;
		if (_openHistoryCount < _openHistory.Length)
			_openHistoryCount++;
	}

	private bool HasEnoughOpenHistory()
	{
		return _openHistoryCount >= _openHistory.Length;
	}

	private bool IsJumpDetected(decimal pip)
	{
		if (pip <= 0m)
			return false;

		for (var i = 0; i < 5; i++)
		{
			var current = _openHistory[i];
			var next = _openHistory[i + 1];
			if (current is null || next is null)
				return false;

			var difference = Math.Abs(current.Value - next.Value) / pip;
			if (difference >= BigJumpPoints)
				return true;
		}

		for (var i = 0; i < 4; i++)
		{
			var current = _openHistory[i];
			var distant = _openHistory[i + 2];
			if (current is null || distant is null)
				return false;

			var difference = Math.Abs(current.Value - distant.Value) / pip;
			if (difference >= DoubleJumpPoints)
				return true;
		}

		return false;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep;
		return step is null or <= 0m ? 1m : step.Value;
	}
}

