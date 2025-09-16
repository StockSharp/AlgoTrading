using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R extreme reversal strategy originally designed for the MetaTrader Forex Fraus M1 expert advisor.
/// Buys when Williams %R reaches an ultra-oversold level and sells when it reaches an ultra-overbought level.
/// Includes optional time filtering, classic stop-loss/take-profit management, and a pip-based trailing stop.
/// </summary>
public class ForexFrausM1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useTimeControl;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _closeOppositePositions;
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _pipSize;

	private WilliamsR _williams;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
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
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum additional distance in pips required before the trailing stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enable or disable time filtering.
	/// </summary>
	public bool UseTimeControl
	{
		get => _useTimeControl.Value;
		set => _useTimeControl.Value = value;
	}

	/// <summary>
	/// Trading session start hour (0-23).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading session end hour (0-23, exclusive).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Close opposite positions when a new signal appears.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOppositePositions.Value;
		set => _closeOppositePositions.Value = value;
	}

	/// <summary>
	/// Williams %R calculation period.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Pip size in price units used to convert pip distances to price distances.
	/// </summary>
	public decimal PipSize
	{
		get => _pipSize.Value;
		set => _pipSize.Value = value;
	}

	/// <summary>
	/// Initialize the Forex Fraus M1 strategy.
	/// </summary>
	public ForexFrausM1Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 1m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Trailing Stop (pips)", "Base trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Trailing Step (pips)", "Minimum move before trailing stop updates", "Risk");

		_useTimeControl = Param(nameof(UseTimeControl), true)
			.SetDisplay("Use Time Control", "Enable trading session filter", "Session");

		_startHour = Param(nameof(StartHour), 7)
			.SetDisplay("Start Hour", "Trading session start hour", "Session")
			.SetGreaterOrEqual(0)
			.SetLessOrEqual(23);

		_endHour = Param(nameof(EndHour), 17)
			.SetDisplay("End Hour", "Trading session end hour (exclusive)", "Session")
			.SetGreaterOrEqual(1)
			.SetLessOrEqual(24);

		_closeOppositePositions = Param(nameof(CloseOppositePositions), true)
			.SetDisplay("Close Opposites", "Close opposite positions on new signals", "Trading");

		_williamsPeriod = Param(nameof(WilliamsPeriod), 360)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Williams %R calculation period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "General");

		_pipSize = Param(nameof(PipSize), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Size", "Price value of one pip", "Risk");
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

		_williams = null;

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_williams = new WilliamsR { Length = WilliamsPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_williams, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _williams);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal williamsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateRiskManagement(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_williams == null || !_williams.IsFormed)
			return;

		if (UseTimeControl && !IsWithinTradingHours(candle.OpenTime))
			return;

		if (williamsValue <= -99.9m)
		{
			TryEnterLong(candle);
		}
		else if (williamsValue >= -0.1m)
		{
			TryEnterShort(candle);
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (Position > 0)
			return;

		var volume = OrderVolume;
		if (CloseOppositePositions && Position < 0)
			volume += Math.Abs(Position);

		if (volume <= 0)
			return;

		var expectedPosition = Position + volume;
		BuyMarket(volume);

		if (expectedPosition > 0)
		{
			InitializeLongState(candle.ClosePrice);
			LogInfo($"Long entry at {candle.ClosePrice} triggered by Williams %R extreme value.");
		}
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (Position < 0)
			return;

		var volume = OrderVolume;
		if (CloseOppositePositions && Position > 0)
			volume += Math.Abs(Position);

		if (volume <= 0)
			return;

		var expectedPosition = Position - volume;
		SellMarket(volume);

		if (expectedPosition < 0)
		{
			InitializeShortState(candle.ClosePrice);
			LogInfo($"Short entry at {candle.ClosePrice} triggered by Williams %R extreme value.");
		}
	}

	private void InitializeLongState(decimal entryPrice)
	{
		_longEntryPrice = entryPrice;
		_longStopPrice = CreateStopPrice(entryPrice, StopLossPips, false);
		_longTakeProfitPrice = CreateTakeProfitPrice(entryPrice, TakeProfitPips, false);
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}

	private void InitializeShortState(decimal entryPrice)
	{
		_shortEntryPrice = entryPrice;
		_shortStopPrice = CreateStopPrice(entryPrice, StopLossPips, true);
		_shortTakeProfitPrice = CreateTakeProfitPrice(entryPrice, TakeProfitPips, true);
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private decimal? CreateStopPrice(decimal entryPrice, decimal distancePips, bool isShort)
	{
		var distance = GetPriceDistance(distancePips);
		if (distance <= 0)
			return null;

		return isShort ? entryPrice + distance : entryPrice - distance;
	}

	private decimal? CreateTakeProfitPrice(decimal entryPrice, decimal distancePips, bool isShort)
	{
		var distance = GetPriceDistance(distancePips);
		if (distance <= 0)
			return null;

		return isShort ? entryPrice - distance : entryPrice + distance;
	}

	private decimal GetPriceDistance(decimal distanceInPips)
	{
		if (distanceInPips <= 0)
			return 0m;

		return distanceInPips * PipSize;
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		if (Position > 0 && _longEntryPrice is decimal longEntry)
		{
			UpdateLongTrailing(candle, longEntry);

			if (_longStopPrice is decimal longStop && candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				LogInfo($"Long stop triggered at {longStop}.");
				ResetLongState();
				return;
			}

			if (_longTakeProfitPrice is decimal longTakeProfit && candle.HighPrice >= longTakeProfit)
			{
				SellMarket(Position);
				LogInfo($"Long take-profit reached at {longTakeProfit}.");
				ResetLongState();
				return;
			}
		}
		else if (Position <= 0)
		{
			ResetLongState();
		}

		if (Position < 0 && _shortEntryPrice is decimal shortEntry)
		{
			UpdateShortTrailing(candle, shortEntry);

			if (_shortStopPrice is decimal shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short stop triggered at {shortStop}.");
				ResetShortState();
				return;
			}

			if (_shortTakeProfitPrice is decimal shortTakeProfit && candle.LowPrice <= shortTakeProfit)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short take-profit reached at {shortTakeProfit}.");
				ResetShortState();
				return;
			}
		}
		else if (Position >= 0)
		{
			ResetShortState();
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle, decimal entryPrice)
	{
		var trailingDistance = GetPriceDistance(TrailingStopPips);
		var trailingStep = GetPriceDistance(TrailingStepPips);

		if (trailingDistance <= 0 || trailingStep <= 0)
			return;

		var currentPrice = candle.ClosePrice;
		if (currentPrice - entryPrice <= trailingDistance + trailingStep)
			return;

		var minStop = currentPrice - (trailingDistance + trailingStep);
		var newStop = currentPrice - trailingDistance;

		if (!_longStopPrice.HasValue || _longStopPrice.Value < minStop)
		{
			_longStopPrice = newStop;
			LogInfo($"Adjust long trailing stop to {_longStopPrice.Value}.");
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle, decimal entryPrice)
	{
		var trailingDistance = GetPriceDistance(TrailingStopPips);
		var trailingStep = GetPriceDistance(TrailingStepPips);

		if (trailingDistance <= 0 || trailingStep <= 0)
			return;

		var currentPrice = candle.ClosePrice;
		if (entryPrice - currentPrice <= trailingDistance + trailingStep)
			return;

		var maxStop = currentPrice + (trailingDistance + trailingStep);
		var newStop = currentPrice + trailingDistance;

		if (!_shortStopPrice.HasValue || _shortStopPrice.Value > maxStop)
		{
			_shortStopPrice = newStop;
			LogInfo($"Adjust short trailing stop to {_shortStopPrice.Value}.");
		}
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;
		var start = StartHour;
		var end = EndHour;

		if (start == end)
			return false;

		if (start > end)
		{
			return (hour >= 0 && hour <= end - 1) || (hour >= start && hour <= 23);
		}

		return hour >= start && hour <= end - 1;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}
}
