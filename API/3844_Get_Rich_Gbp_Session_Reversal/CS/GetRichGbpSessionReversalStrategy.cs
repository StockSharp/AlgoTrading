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
/// Mean-reversion strategy that counts bullish and bearish candles within a rolling window.
/// Opens positions during two specific trading windows when the majority of the recent candles were in the opposite direction.
/// Applies a virtual take profit, stop loss and trailing stop that operate in points relative to the instrument price step.
/// </summary>
public class GetRichGbpSessionReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _partialTakeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _firstEntryHour;
	private readonly StrategyParam<int> _secondEntryHour;
	private readonly StrategyParam<int> _hourShift;
	private readonly StrategyParam<int> _entryWindowMinutes;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<int> _directionBuffer = new();
	private int _directionSum;
	private decimal _longTrailHigh;
	private decimal _shortTrailLow;

	/// <summary>
	/// Take profit distance in points (price steps).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Intermediate take profit distance in points (price steps).
	/// </summary>
	public decimal PartialTakeProfitPoints
	{
		get => _partialTakeProfitPoints.Value;
		set => _partialTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points (price steps).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points (price steps).
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Fixed trading volume (lots).
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Flag that enables basic percentage risk money management.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk percentage used when money management is active.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Number of finished candles that participate in the direction count.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// First trading hour in exchange time before applying the hour shift.
	/// </summary>
	public int FirstEntryHour
	{
		get => _firstEntryHour.Value;
		set => _firstEntryHour.Value = value;
	}

	/// <summary>
	/// Second trading hour in exchange time before applying the hour shift.
	/// </summary>
	public int SecondEntryHour
	{
		get => _secondEntryHour.Value;
		set => _secondEntryHour.Value = value;
	}

	/// <summary>
	/// Hour shift that aligns server time with the desired trading session.
	/// </summary>
	public int HourShift
	{
		get => _hourShift.Value;
		set => _hourShift.Value = value;
	}

	/// <summary>
	/// Number of minutes after the hour when entries are permitted.
	/// </summary>
	public int EntryWindowMinutes
	{
		get => _entryWindowMinutes.Value;
		set => _entryWindowMinutes.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
/// Constructor.
/// </summary>
public GetRichGbpSessionReversalStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Points", "Final take profit distance in points", "Risk Management")
		.SetCanOptimize(true);

		_partialTakeProfitPoints = Param(nameof(PartialTakeProfitPoints), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Partial Take Profit Points", "Virtual take profit distance in points", "Risk Management")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Points", "Stop loss distance in points", "Risk Management")
		.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop Points", "Trailing stop distance in points", "Risk Management")
		.SetCanOptimize(true);

		_fixedVolume = Param(nameof(FixedVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Fixed Volume", "Base order volume in lots", "Money Management")
		.SetCanOptimize(true);

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Use Money Management", "Enable dynamic position sizing", "Money Management");

		_riskPercent = Param(nameof(RiskPercent), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Percent", "Risk percentage used for dynamic sizing", "Money Management")
		.SetCanOptimize(true);

		_lookback = Param(nameof(Lookback), 18)
		.SetGreaterThanZero()
		.SetDisplay("Lookback", "Number of candles used in direction count", "Logic")
		.SetCanOptimize(true);

		_firstEntryHour = Param(nameof(FirstEntryHour), 22)
		.SetDisplay("First Entry Hour", "Primary trading hour before shift", "Schedule")
		.SetCanOptimize(true);

		_secondEntryHour = Param(nameof(SecondEntryHour), 19)
		.SetDisplay("Second Entry Hour", "Secondary trading hour before shift", "Schedule")
		.SetCanOptimize(true);

		_hourShift = Param(nameof(HourShift), 2)
		.SetDisplay("Hour Shift", "Offset applied to trading hours", "Schedule")
		.SetCanOptimize(true);

		_entryWindowMinutes = Param(nameof(EntryWindowMinutes), 5)
		.SetGreaterThanZero()
		.SetDisplay("Entry Window Minutes", "How many minutes after the hour orders are allowed", "Schedule")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to analyze", "General");
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

		_directionBuffer.Clear();
		_directionSum = 0;
		_longTrailHigh = 0m;
		_shortTrailLow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			_longTrailHigh = Position.AveragePrice ?? 0m;
			_shortTrailLow = 0m;
		}
		else if (Position < 0m)
		{
			_shortTrailLow = Position.AveragePrice ?? 0m;
			_longTrailHigh = 0m;
		}
		else
		{
			_longTrailHigh = 0m;
			_shortTrailLow = 0m;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateDirections(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		ManageOpenPosition(candle);

		if (Position != 0m)
		return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		if (_directionBuffer.Count < Lookback)
		return;

		var candleTime = candle.OpenTime;
		if (!IsWithinTradingWindow(candleTime))
		return;

		var minute = candleTime.Minute;
		if (minute >= EntryWindowMinutes)
		return;

		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		if (_directionSum > 0)
		{
			BuyMarket(volume);
		}
		else if (_directionSum < 0)
		{
			SellMarket(volume);
		}
	}

	private void UpdateDirections(ICandleMessage candle)
	{
		var direction = 0;

		if (candle.OpenPrice > candle.ClosePrice)
		{
			direction = 1;
		}
		else if (candle.OpenPrice < candle.ClosePrice)
		{
			direction = -1;
		}

		_directionBuffer.Enqueue(direction);
		_directionSum += direction;

		while (_directionBuffer.Count > Lookback)
		{
			_directionSum -= _directionBuffer.Dequeue();
		}
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var adjustedHour1 = Mod24(FirstEntryHour + HourShift);
		var adjustedHour2 = Mod24(SecondEntryHour + HourShift);
		var currentHour = time.Hour;

		return currentHour == adjustedHour1 || currentHour == adjustedHour2;
	}

	private static int Mod24(int hour)
	{
		var result = hour % 24;
		return result < 0 ? result + 24 : result;
	}

	private decimal CalculateVolume()
	{
		if (!UseMoneyManagement)
		return FixedVolume;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		if (portfolioValue <= 0m)
		return FixedVolume;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
		return FixedVolume;

		var riskAmount = portfolioValue * RiskPercent / 100m;
		var stopDistance = StopLossPoints > 0m ? StopLossPoints : 1m;
		var moneyPerLot = stepPrice * stopDistance;
		if (moneyPerLot <= 0m)
		return FixedVolume;

		var volume = riskAmount / moneyPerLot;

		var lotStep = Security?.LotStep ?? 1m;
		if (lotStep <= 0m)
		lotStep = 1m;

		var minVolume = Security?.MinVolume ?? lotStep;
		if (minVolume <= 0m)
		minVolume = lotStep;

		volume = Math.Floor(volume / lotStep) * lotStep;
		if (volume < minVolume)
		volume = minVolume;

		return volume;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0m)
		return;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var entryPrice = Position.AveragePrice;
		if (entryPrice is null)
		return;

		if (Position > 0m)
		{
			_longTrailHigh = Math.Max(_longTrailHigh <= 0m ? entryPrice.Value : _longTrailHigh, candle.HighPrice);

			var takeProfitPrice = entryPrice.Value + PartialTakeProfitPoints * priceStep;
			if (PartialTakeProfitPoints > 0m && candle.ClosePrice >= takeProfitPrice)
			{
				ClosePosition();
				return;
			}

			var stopLossPrice = entryPrice.Value - StopLossPoints * priceStep;
			if (StopLossPoints > 0m && candle.ClosePrice <= stopLossPrice)
			{
				ClosePosition();
				return;
			}

			if (TrailingStopPoints > 0m)
			{
				var trailingStopPrice = _longTrailHigh - TrailingStopPoints * priceStep;
				if (candle.ClosePrice <= trailingStopPrice)
				{
					ClosePosition();
					return;
				}
			}

			var finalTakeProfit = entryPrice.Value + TakeProfitPoints * priceStep;
			if (TakeProfitPoints > 0m && candle.ClosePrice >= finalTakeProfit)
			{
				ClosePosition();
			}
		}
		else if (Position < 0m)
		{
			_shortTrailLow = _shortTrailLow <= 0m ? entryPrice.Value : Math.Min(_shortTrailLow, candle.LowPrice);

			var takeProfitPrice = entryPrice.Value - PartialTakeProfitPoints * priceStep;
			if (PartialTakeProfitPoints > 0m && candle.ClosePrice <= takeProfitPrice)
			{
				ClosePosition();
				return;
			}

			var stopLossPrice = entryPrice.Value + StopLossPoints * priceStep;
			if (StopLossPoints > 0m && candle.ClosePrice >= stopLossPrice)
			{
				ClosePosition();
				return;
			}

			if (TrailingStopPoints > 0m)
			{
				_shortTrailLow = Math.Min(_shortTrailLow, candle.LowPrice);
				var trailingStopPrice = _shortTrailLow + TrailingStopPoints * priceStep;
				if (candle.ClosePrice >= trailingStopPrice)
				{
					ClosePosition();
					return;
				}
			}

			var finalTakeProfit = entryPrice.Value - TakeProfitPoints * priceStep;
			if (TakeProfitPoints > 0m && candle.ClosePrice <= finalTakeProfit)
			{
				ClosePosition();
			}
		}
	}
}

