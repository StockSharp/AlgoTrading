using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NRTR reversal strategy converted from MetaTrader 5 expert advisor.
/// Switches between long and short bias based on ATR projected support and resistance levels.
/// </summary>
public class NrtrReversStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _volatilityMultiplier;
	private readonly StrategyParam<decimal> _reversePips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _tradeVolume;

	private readonly List<decimal> _lowHistory = new();
	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _closeHistory = new();

	private AverageTrueRange _atr = null!;

	private decimal _adjustedPoint;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;
	private decimal _reverseDistance;
	private int _historyCapacity;

	private TradeDirection _currentTrend = TradeDirection.Long;
	private TradeDirection _desiredTrend = TradeDirection.Long;
	private bool _waitForFlat;

	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;

	private enum TradeDirection
	{
		Long,
		Short
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NrtrReversStrategy"/> class.
	/// </summary>
	public NrtrReversStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Averaging period for the ATR indicator", "Indicators");

		_volatilityMultiplier = Param(nameof(VolatilityMultiplier), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Volatility Multiplier", "ATR multiplier used to build the NRTR bands", "Indicators");

		_reversePips = Param(nameof(ReversePips), 50m)
		.SetNotNegative()
		.SetDisplay("Reverse (pips)", "Minimum band break to flip the bias", "Logic");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance measured in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 1000m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Profit target distance measured in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop activation distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 45m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Additional profit required to move the trailing stop", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume used for entries", "Trading");
	}

	/// <summary>
	/// Candle type used for all calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Averaging period for the ATR indicator.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the ATR value.
	/// </summary>
	public decimal VolatilityMultiplier
	{
		get => _volatilityMultiplier.Value;
		set => _volatilityMultiplier.Value = value;
	}

	/// <summary>
	/// Reverse distance expressed in pips.
	/// </summary>
	public decimal ReversePips
	{
		get => _reversePips.Value;
		set => _reversePips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop activation distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum additional gain required before the trailing stop is moved.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Trading volume used for new positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lowHistory.Clear();
		_highHistory.Clear();
		_closeHistory.Clear();

		_adjustedPoint = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_trailingStopDistance = 0m;
		_trailingStepDistance = 0m;
		_reverseDistance = 0m;
		_historyCapacity = 0;

		_currentTrend = TradeDirection.Long;
		_desiredTrend = TradeDirection.Long;
		_waitForFlat = false;

		_longStopPrice = null;
		_longTakeProfit = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		Volume = TradeVolume;

		_adjustedPoint = CalculateAdjustedPoint();
		_stopLossDistance = StopLossPips > 0m ? StopLossPips * _adjustedPoint : 0m;
		_takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * _adjustedPoint : 0m;
		_trailingStopDistance = TrailingStopPips > 0m ? TrailingStopPips * _adjustedPoint : 0m;
		_trailingStepDistance = TrailingStepPips > 0m ? TrailingStepPips * _adjustedPoint : 0m;
		_reverseDistance = ReversePips > 0m ? ReversePips * _adjustedPoint : 0m;
		_historyCapacity = Math.Max(AtrPeriod + 5, 10);

		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateHistory(candle);

		if (_waitForFlat && Position == 0)
		{
			_waitForFlat = false;

			if (_desiredTrend == TradeDirection.Long)
			{
				EnterLong(candle);
			}
			else
			{
				EnterShort(candle);
			}
		}

		var exited = UpdateRiskManagement(candle);
		if (exited)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_atr.IsFormed)
		return;

		if (AtrPeriod <= 1)
		return;

		var prevClose = GetCloseShift(1);
		if (!prevClose.HasValue)
		return;

		var bandOffset = atrValue * VolatilityMultiplier;
		if (bandOffset <= 0m)
		return;

		var halfPeriod = (int)Math.Round(AtrPeriod / 2m, MidpointRounding.AwayFromZero);
		if (halfPeriod <= 0)
		halfPeriod = 1;

		if (_currentTrend == TradeDirection.Long)
		{
			var primaryLow = GetLowest(2, AtrPeriod - 1);
			if (!primaryLow.HasValue)
			return;

			var line = primaryLow.Value - bandOffset;
			var secondaryStart = AtrPeriod - halfPeriod + 1;
			if (secondaryStart < 0)
			secondaryStart = 0;

			var secondaryLow = GetLowest(secondaryStart, halfPeriod);

			var shouldReverse = prevClose.Value < line - bandOffset;
			if (!shouldReverse && _reverseDistance > 0m && secondaryLow.HasValue)
			shouldReverse = secondaryLow.Value - line >= _reverseDistance;

			if (shouldReverse)
			{
				LogInfo("Change the trend. Now "SELL".");
				_currentTrend = TradeDirection.Short;
				_desiredTrend = TradeDirection.Short;

				if (Position > 0)
				{
					SellMarket(Position);
					ClearLongState();
					_waitForFlat = true;
				}
				else if (Position == 0)
				{
					EnterShort(candle);
				}
			}
		}
		else
		{
			var primaryHigh = GetHighest(2, AtrPeriod - 1);
			if (!primaryHigh.HasValue)
			return;

			var line = primaryHigh.Value + bandOffset;
			var secondaryStart = AtrPeriod - halfPeriod + 1;
			if (secondaryStart < 0)
			secondaryStart = 0;

			var secondaryHigh = GetHighest(secondaryStart, halfPeriod);

			var shouldReverse = prevClose.Value > line + bandOffset;
			if (!shouldReverse && _reverseDistance > 0m && secondaryHigh.HasValue)
			shouldReverse = line - secondaryHigh.Value >= _reverseDistance;

			if (shouldReverse)
			{
				LogInfo("Change the trend. Now "BUY".");
				_currentTrend = TradeDirection.Long;
				_desiredTrend = TradeDirection.Long;

				if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					ClearShortState();
					_waitForFlat = true;
				}
				else if (Position == 0)
				{
					EnterLong(candle);
				}
			}
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = Volume;
		if (volume <= 0m)
		return;

		BuyMarket(volume);

		var entryPrice = candle.ClosePrice;
		_longStopPrice = _stopLossDistance > 0m ? entryPrice - _stopLossDistance : null;
		_longTakeProfit = _takeProfitDistance > 0m ? entryPrice + _takeProfitDistance : null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = Volume;
		if (volume <= 0m)
		return;

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
		var activation = _trailingStopDistance + _trailingStepDistance;
		if (profit <= activation)
		return;

		var threshold = candle.ClosePrice - activation;
		if (!_longStopPrice.HasValue || _longStopPrice.Value < threshold)
		_longStopPrice = candle.ClosePrice - _trailingStopDistance;
	}

	private void ApplyTrailingForShort(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m)
		return;

		var entryPrice = PositionPrice;
		if (entryPrice == 0m)
		return;

		var profit = entryPrice - candle.ClosePrice;
		var activation = _trailingStopDistance + _trailingStepDistance;
		if (profit <= activation)
		return;

		var threshold = candle.ClosePrice + activation;
		if (!_shortStopPrice.HasValue || _shortStopPrice.Value > threshold)
		_shortStopPrice = candle.ClosePrice + _trailingStopDistance;
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

	private void UpdateHistory(ICandleMessage candle)
	{
		_lowHistory.Add(candle.LowPrice);
		_highHistory.Add(candle.HighPrice);
		_closeHistory.Add(candle.ClosePrice);

		var capacity = _historyCapacity;
		if (capacity <= 0)
		capacity = Math.Max(AtrPeriod + 5, 10);

		TrimHistory(_lowHistory, capacity);
		TrimHistory(_highHistory, capacity);
		TrimHistory(_closeHistory, capacity);
	}

	private static void TrimHistory(List<decimal> history, int capacity)
	{
		while (history.Count > capacity)
		history.RemoveAt(0);
	}

	private decimal? GetLowest(int startShift, int length)
	{
		if (length <= 0)
		return null;

		var count = _lowHistory.Count;
		if (count <= startShift)
		return null;

		var maxShift = startShift + length - 1;
		if (count <= maxShift)
		return null;

		var lowest = decimal.MaxValue;
		for (var shift = startShift; shift < startShift + length; shift++)
		{
			var index = count - 1 - shift;
			if (index < 0)
			return null;

			var value = _lowHistory[index];
			if (value < lowest)
			lowest = value;
		}

		return lowest;
	}

	private decimal? GetHighest(int startShift, int length)
	{
		if (length <= 0)
		return null;

		var count = _highHistory.Count;
		if (count <= startShift)
		return null;

		var maxShift = startShift + length - 1;
		if (count <= maxShift)
		return null;

		var highest = decimal.MinValue;
		for (var shift = startShift; shift < startShift + length; shift++)
		{
			var index = count - 1 - shift;
			if (index < 0)
			return null;

			var value = _highHistory[index];
			if (value > highest)
			highest = value;
		}

		return highest;
	}

	private decimal? GetCloseShift(int shift)
	{
		if (shift < 0)
		return null;

		var count = _closeHistory.Count;
		var index = count - 1 - shift;
		if (index < 0 || index >= count)
		return null;

		return _closeHistory[index];
	}

	private decimal CalculateAdjustedPoint()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 1m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}
}
