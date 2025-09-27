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
/// Tunnel Method strategy converted from MetaTrader 5 implementation.
/// Combines three displaced moving averages and trades breakouts between them with dynamic trailing.
/// </summary>
public class TunnelMethodStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _firstMaPeriod;
	private readonly StrategyParam<int> _firstMaShift;
	private readonly StrategyParam<int> _secondMaPeriod;
	private readonly StrategyParam<int> _secondMaShift;
	private readonly StrategyParam<int> _thirdMaPeriod;
	private readonly StrategyParam<int> _thirdMaShift;
	private readonly StrategyParam<decimal> _indentPips;
	private readonly StrategyParam<int> _pauseSeconds;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _firstMa;
	private SimpleMovingAverage _secondMa;
	private SimpleMovingAverage _thirdMa;

	private decimal?[] _firstBuffer = Array.Empty<decimal?>();
	private decimal?[] _secondBuffer = Array.Empty<decimal?>();
	private decimal?[] _thirdBuffer = Array.Empty<decimal?>();

	private decimal _adjustedPoint;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;
	private decimal _indentDistance;

	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;

	private DateTimeOffset? _nextEntryTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="TunnelMethodStrategy"/> class.
	/// </summary>
	public TunnelMethodStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Trade Volume", "Order volume for each entry", "General")
		.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
		.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
		.SetNotNegative();

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
		.SetNotNegative();

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetDisplay("Trailing Step (pips)", "Minimal distance before trailing adjustment", "Risk")
		.SetNotNegative();

		_firstMaPeriod = Param(nameof(FirstMaPeriod), 160)
		.SetDisplay("First MA Period", "Length of the slow moving average", "Indicators")
		.SetGreaterThanZero();

		_firstMaShift = Param(nameof(FirstMaShift), 0)
		.SetDisplay("First MA Shift", "Forward shift for the slow moving average", "Indicators")
		.SetNotNegative();

		_secondMaPeriod = Param(nameof(SecondMaPeriod), 80)
		.SetDisplay("Second MA Period", "Length of the middle moving average", "Indicators")
		.SetGreaterThanZero();

		_secondMaShift = Param(nameof(SecondMaShift), 1)
		.SetDisplay("Second MA Shift", "Forward shift for the middle moving average", "Indicators")
		.SetNotNegative();

		_thirdMaPeriod = Param(nameof(ThirdMaPeriod), 20)
		.SetDisplay("Third MA Period", "Length of the fast moving average", "Indicators")
		.SetGreaterThanZero();

		_thirdMaShift = Param(nameof(ThirdMaShift), 2)
		.SetDisplay("Third MA Shift", "Forward shift for the fast moving average", "Indicators")
		.SetNotNegative();

		_indentPips = Param(nameof(IndentPips), 1m)
		.SetDisplay("Indentation (pips)", "Minimum spacing between averages to confirm signals", "Indicators")
		.SetNotNegative();

		_pauseSeconds = Param(nameof(PauseSeconds), 45)
		.SetDisplay("Pause (seconds)", "Minimal pause between entry signal checks", "General")
		.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series for indicators", "Data");
	}

	/// <summary>
	/// Trade volume for new positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal price movement required before trailing stop adjustment in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Period length of the slow moving average.
	/// </summary>
	public int FirstMaPeriod
	{
		get => _firstMaPeriod.Value;
		set => _firstMaPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift for the slow moving average.
	/// </summary>
	public int FirstMaShift
	{
		get => _firstMaShift.Value;
		set => _firstMaShift.Value = value;
	}

	/// <summary>
	/// Period length of the middle moving average.
	/// </summary>
	public int SecondMaPeriod
	{
		get => _secondMaPeriod.Value;
		set => _secondMaPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift for the middle moving average.
	/// </summary>
	public int SecondMaShift
	{
		get => _secondMaShift.Value;
		set => _secondMaShift.Value = value;
	}

	/// <summary>
	/// Period length of the fast moving average.
	/// </summary>
	public int ThirdMaPeriod
	{
		get => _thirdMaPeriod.Value;
		set => _thirdMaPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift for the fast moving average.
	/// </summary>
	public int ThirdMaShift
	{
		get => _thirdMaShift.Value;
		set => _thirdMaShift.Value = value;
	}

	/// <summary>
	/// Minimal indentation between moving averages measured in pips.
	/// </summary>
	public decimal IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Pause duration between entry evaluations in seconds.
	/// </summary>
	public int PauseSeconds
	{
		get => _pauseSeconds.Value;
		set => _pauseSeconds.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_firstMa = null;
		_secondMa = null;
		_thirdMa = null;

		_firstBuffer = Array.Empty<decimal?>();
		_secondBuffer = Array.Empty<decimal?>();
		_thirdBuffer = Array.Empty<decimal?>();

		_longStopPrice = null;
		_longTakeProfit = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;

		_nextEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips == 0)
		{
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");
		}

		_adjustedPoint = CalculateAdjustedPoint();
		_stopLossDistance = StopLossPips > 0 ? StopLossPips * _adjustedPoint : 0m;
		_takeProfitDistance = TakeProfitPips > 0 ? TakeProfitPips * _adjustedPoint : 0m;
		_trailingStopDistance = TrailingStopPips > 0 ? TrailingStopPips * _adjustedPoint : 0m;
		_trailingStepDistance = TrailingStepPips > 0 ? TrailingStepPips * _adjustedPoint : 0m;
		_indentDistance = IndentPips > 0 ? IndentPips * _adjustedPoint : 0m;

		_firstBuffer = new decimal?[Math.Max(FirstMaShift, 0) + 2];
		_secondBuffer = new decimal?[Math.Max(SecondMaShift, 0) + 2];
		_thirdBuffer = new decimal?[Math.Max(ThirdMaShift, 0) + 2];

		_firstMa = new SimpleMovingAverage { Length = FirstMaPeriod };
		_secondMa = new SimpleMovingAverage { Length = SecondMaPeriod };
		_thirdMa = new SimpleMovingAverage { Length = ThirdMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_firstMa, _secondMa, _thirdMa, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _firstMa);
			DrawIndicator(area, _secondMa);
			DrawIndicator(area, _thirdMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal firstValue, decimal secondValue, decimal thirdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_firstMa is null || _secondMa is null || _thirdMa is null)
		return;

		if (!_firstMa.IsFormed || !_secondMa.IsFormed || !_thirdMa.IsFormed)
		{
			PushValue(_firstBuffer, firstValue);
			PushValue(_secondBuffer, secondValue);
			PushValue(_thirdBuffer, thirdValue);
			return;
		}

		PushValue(_firstBuffer, firstValue);
		PushValue(_secondBuffer, secondValue);
		PushValue(_thirdBuffer, thirdValue);

		if (UpdateRiskManagement(candle))
		{
			_nextEntryTime = candle.CloseTime + TimeSpan.FromSeconds(PauseSeconds);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var pause = TimeSpan.FromSeconds(PauseSeconds);
		if (_nextEntryTime.HasValue && candle.CloseTime < _nextEntryTime.Value)
		return;

		_nextEntryTime = candle.CloseTime + pause;

		if (Position != 0)
		return;

		if (!TryGetShiftedValues(_firstBuffer, FirstMaShift, out var firstCurrent, out var firstPrevious))
		return;

		if (!TryGetShiftedValues(_secondBuffer, SecondMaShift, out var secondCurrent, out var secondPrevious))
		return;

		if (!TryGetShiftedValues(_thirdBuffer, ThirdMaShift, out var thirdCurrent, out var thirdPrevious))
		return;

		var halfIndent = _indentDistance / 2m;

		var isBuySignal = thirdPrevious < firstPrevious - halfIndent && thirdCurrent > firstCurrent + halfIndent;
		var isSellSignal = thirdPrevious > secondPrevious + halfIndent && thirdCurrent < secondCurrent - halfIndent;

		if (isBuySignal)
		{
			EnterLong(candle);
		}
		else if (isSellSignal)
		{
			EnterShort(candle);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		if (TradeVolume <= 0)
		return;

		var volume = TradeVolume;
		BuyMarket(volume);

		var entryPrice = candle.ClosePrice;
		_longStopPrice = _stopLossDistance > 0m ? entryPrice - _stopLossDistance : null;
		_longTakeProfit = _takeProfitDistance > 0m ? entryPrice + _takeProfitDistance : null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		if (TradeVolume <= 0)
		return;

		var volume = TradeVolume;
		SellMarket(volume);

		var entryPrice = candle.ClosePrice;
		_shortStopPrice = _stopLossDistance > 0m ? entryPrice + _stopLossDistance : null;
		_shortTakeProfit = _takeProfitDistance > 0m ? entryPrice - _takeProfitDistance : null;
		_longStopPrice = null;
		_longTakeProfit = null;
	}

	private bool UpdateRiskManagement(ICandleMessage candle)
	{
		var exited = false;

		if (Position > 0)
		{
			ApplyTrailingForLong(candle);

			if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
			{
				SellMarket(Position);
				ClearLongState();
				exited = true;
			}
			else if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Position);
				ClearLongState();
				exited = true;
			}
		}
		else if (Position < 0)
		{
			ApplyTrailingForShort(candle);

			var volume = Math.Abs(Position);
			if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
			{
				BuyMarket(volume);
				ClearShortState();
				exited = true;
			}
			else if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(volume);
				ClearShortState();
				exited = true;
			}
		}
		else
		{
			ClearLongState();
			ClearShortState();
		}

		return exited;
	}

	private void ApplyTrailingForLong(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice == 0m)
		return;

		var profit = candle.ClosePrice - entryPrice;
		if (profit <= _trailingStopDistance + _trailingStepDistance)
		return;

		var threshold = candle.ClosePrice - (_trailingStopDistance + _trailingStepDistance);
		if (!_longStopPrice.HasValue || _longStopPrice.Value < threshold)
		{
			_longStopPrice = candle.ClosePrice - _trailingStopDistance;
		}
	}

	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice == 0m)
		return;

		var profit = entryPrice - candle.ClosePrice;
		if (profit <= _trailingStopDistance + _trailingStepDistance)
		return;

		var threshold = candle.ClosePrice + (_trailingStopDistance + _trailingStepDistance);
		if (!_shortStopPrice.HasValue || _shortStopPrice.Value > threshold)
		{
			_shortStopPrice = candle.ClosePrice + _trailingStopDistance;
		}
	}

	private static void PushValue(decimal?[] buffer, decimal value)
	{
		if (buffer.Length == 0)
		return;

		for (var i = buffer.Length - 1; i > 0; i--)
		{
			buffer[i] = buffer[i - 1];
		}

		buffer[0] = value;
	}

	private static bool TryGetShiftedValues(decimal?[] buffer, int shift, out decimal current, out decimal previous)
	{
		current = default;
		previous = default;

		if (shift < 0)
		return false;

		if (buffer.Length <= shift + 1)
		return false;

		var currentValue = buffer[shift];
		var previousValue = buffer[shift + 1];

		if (!currentValue.HasValue || !previousValue.HasValue)
		return false;

		current = currentValue.Value;
		previous = previousValue.Value;
		return true;
	}

	private void ClearLongState()
	{
		_longStopPrice = null;
		_longTakeProfit = null;
	}

	private void ClearShortState()
	{
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	private decimal CalculateAdjustedPoint()
	{
		var priceStep = Security.PriceStep ?? 0m;

		if (priceStep <= 0m)
		{
			var decimals = Security.Decimals;
			priceStep = 1m;
			for (var i = 0; i < decimals; i++)
			{
				priceStep /= 10m;
			}
			if (priceStep <= 0m)
			priceStep = 0.0001m;
		}

		var digits = Security.Decimals;
		var multiplier = digits == 3 || digits == 5 ? 10m : 1m;
		return priceStep * multiplier;
	}
}

