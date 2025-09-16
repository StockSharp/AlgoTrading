using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Detects a sequence of identical candles and opens trades in the same direction.
/// Implements optional take profit, stop loss, trailing stop and trading hour filter.
/// </summary>
public class NCandlesV5Strategy : Strategy
{
	private readonly StrategyParam<int> _candlesCount;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<decimal> _maxNetVolume;
	private readonly StrategyParam<DataType> _candleType;

	private int _bullishCount;
	private int _bearishCount;

	private decimal? _longEntryPrice;
	private decimal? _longTakeProfit;
	private decimal? _longStopLoss;
	private decimal? _longTrailingStop;

	private decimal? _shortEntryPrice;
	private decimal? _shortTakeProfit;
	private decimal? _shortStopLoss;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Initializes a new instance of <see cref="NCandlesV5Strategy"/>.
	/// </summary>
	public NCandlesV5Strategy()
	{
		_volumeParam = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Trade Volume", "Order volume for entries", "Trading")
		.SetGreaterThanZero();

		_candlesCount = Param(nameof(CandlesCount), 3)
		.SetDisplay("Candles Count", "Number of identical candles required", "General")
		.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop activation distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 4m)
		.SetDisplay("Trailing Step (pips)", "Increment required to tighten trailing stop", "Risk");

		_useTradingHours = Param(nameof(UseTradingHours), true)
		.SetDisplay("Use Trading Hours", "Enable trading session filter", "Trading");

		_startHour = Param(nameof(StartHour), 11)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "Hour when trading is allowed to start", "Trading");

		_endHour = Param(nameof(EndHour), 18)
		.SetRange(0, 23)
		.SetDisplay("End Hour", "Hour when trading is allowed to stop", "Trading");

		_maxNetVolume = Param(nameof(MaxNetVolume), 2m)
		.SetDisplay("Max Net Volume", "Maximum absolute net position", "Risk")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to analyze", "General");

		Volume = _volumeParam.Value;
	}

	/// <summary>
	/// Trade volume used for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volumeParam.Value;
		set
		{
			_volumeParam.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Number of consecutive identical candles required for a signal.
	/// </summary>
	public int CandlesCount
	{
		get => _candlesCount.Value;
		set => _candlesCount.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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
	/// Trailing step distance in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enables the trading hour filter.
	/// </summary>
	public bool UseTradingHours
	{
		get => _useTradingHours.Value;
		set => _useTradingHours.Value = value;
	}

	/// <summary>
	/// First hour of the allowed trading window.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Last hour of the allowed trading window.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Maximum absolute net position allowed.
	/// </summary>
	public decimal MaxNetVolume
	{
		get => _maxNetVolume.Value;
		set => _maxNetVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for pattern detection.
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

		Volume = TradeVolume;
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (UseTradingHours && StartHour >= EndHour)
		throw new InvalidOperationException("Start hour must be less than end hour when trading hours filter is enabled.");

		Volume = TradeVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with completed candles to avoid premature signals.
		if (candle.State != CandleStates.Finished)
		return;

		// Refresh trailing and exit logic before looking for new opportunities.
		UpdateRiskManagement(candle);

		var direction = GetDirection(candle);
		// Track bullish and bearish streak length.


		if (direction == 1)
		{
			_bullishCount++;
			_bearishCount = 0;
		}
		else if (direction == -1)
		{
			_bearishCount++;
			_bullishCount = 0;
		}
		else
		{
			_bullishCount = 0;
			_bearishCount = 0;
		}

		var tradingAllowed = !UseTradingHours || (candle.OpenTime.Hour >= StartHour && candle.OpenTime.Hour <= EndHour);
		// Skip entries outside the configured session window.

		if (!tradingAllowed)
		return;

		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		var step = Security?.PriceStep ?? 1m;
		// Use instrument price step to translate pip distances to absolute prices.

		if (_bullishCount >= CandlesCount && Position <= 0m)
		{
			// Enter long after detecting the required number of bullish candles in a row.
			var orderVolume = volume + Math.Max(0m, -Position);
			if (orderVolume > 0m && Math.Abs(Position + orderVolume) <= MaxNetVolume)
			{
				BuyMarket(orderVolume);
				SetupLongState(candle, step);
			}

			ResetCounters();
		}
		else if (_bearishCount >= CandlesCount && Position >= 0m)
		{
			// Enter short after detecting the required number of bearish candles in a row.
			var orderVolume = volume + Math.Max(0m, Position);
			if (orderVolume > 0m && Math.Abs(Position - orderVolume) <= MaxNetVolume)
			{
				SellMarket(orderVolume);
				SetupShortState(candle, step);
			}

			ResetCounters();
		}
	}

	private void UpdateRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			ManageLongPosition(candle);
		}
		else
		{
			ClearLongState();
		}

		if (Position < 0m)
		{
			ManageShortPosition(candle);
		}
		else
		{
			ClearShortState();
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (_longEntryPrice is null)
			// Capture entry price if it was not stored yet (for example after restart).
			_longEntryPrice = candle.ClosePrice;

		var step = Security?.PriceStep ?? 1m;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		var trailingDistance = TrailingStopPips > 0m ? TrailingStopPips * step : 0m;
		var trailingStep = TrailingStepPips > 0m ? TrailingStepPips * step : 0m;

		if (TrailingStopPips > 0m && _longEntryPrice is decimal entry)
		{
			// Update trailing stop level according to the latest candle.

			if (_longTrailingStop is null)
			{
				if (close - trailingDistance > entry)
				_longTrailingStop = entry;
			}
			else
			{
				var newLevel = close - trailingDistance;
				if (newLevel - trailingStep > _longTrailingStop)
				_longTrailingStop = newLevel;
			}
		}
		else
		{
			_longTrailingStop = null;
		}

		var exitVolume = Position > 0m ? Position : 0m;
		var closed = false;

		// Exit the long position when any protective target is triggered.
		if (!closed && _longTakeProfit is decimal takeProfit && high >= takeProfit)
		{
			if (exitVolume > 0m)
			SellMarket(exitVolume);
			closed = true;
		}

		if (!closed && _longStopLoss is decimal stopLoss && low <= stopLoss)
		{
			if (exitVolume > 0m)
			SellMarket(exitVolume);
			closed = true;
		}

		if (!closed && _longTrailingStop is decimal trailingStop && low <= trailingStop)
		{
			if (exitVolume > 0m)
			SellMarket(exitVolume);
			closed = true;
		}

		if (closed)
			ClearLongState();
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (_shortEntryPrice is null)
			// Capture entry price if it was not stored yet (for example after restart).
			_shortEntryPrice = candle.ClosePrice;

		var step = Security?.PriceStep ?? 1m;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		var trailingDistance = TrailingStopPips > 0m ? TrailingStopPips * step : 0m;
		var trailingStep = TrailingStepPips > 0m ? TrailingStepPips * step : 0m;

		if (TrailingStopPips > 0m && _shortEntryPrice is decimal entry)
		{
			// Update trailing stop level for the active short position.

			if (_shortTrailingStop is null)
			{
				if (close + trailingDistance < entry)
				_shortTrailingStop = entry;
			}
			else
			{
				var newLevel = close + trailingDistance;
				if (newLevel + trailingStep < _shortTrailingStop)
				_shortTrailingStop = newLevel;
			}
		}
		else
		{
			_shortTrailingStop = null;
		}

		var exitVolume = Position < 0m ? -Position : 0m;
		var closed = false;

		// Exit the short position when any protective target is triggered.
		if (!closed && _shortTakeProfit is decimal takeProfit && low <= takeProfit)
		{
			if (exitVolume > 0m)
			BuyMarket(exitVolume);
			closed = true;
		}

		if (!closed && _shortStopLoss is decimal stopLoss && high >= stopLoss)
		{
			if (exitVolume > 0m)
			BuyMarket(exitVolume);
			closed = true;
		}

		if (!closed && _shortTrailingStop is decimal trailingStop && high >= trailingStop)
		{
			if (exitVolume > 0m)
			BuyMarket(exitVolume);
			closed = true;
		}

		if (closed)
			ClearShortState();
	}

	private static int GetDirection(ICandleMessage candle)
	{
		if (candle.ClosePrice > candle.OpenPrice)
		return 1;

		if (candle.ClosePrice < candle.OpenPrice)
		return -1;

		return 0;
	}

	private void SetupLongState(ICandleMessage candle, decimal step)
	{
		var entryPrice = candle.ClosePrice;
		// Store reference levels for long-side risk management.
		_longEntryPrice = entryPrice;
		_longTakeProfit = TakeProfitPips > 0m ? entryPrice + TakeProfitPips * step : null;
		_longStopLoss = StopLossPips > 0m ? entryPrice - StopLossPips * step : null;
		_longTrailingStop = null;

		ClearShortState();
	}

	private void SetupShortState(ICandleMessage candle, decimal step)
	{
		var entryPrice = candle.ClosePrice;
		// Store reference levels for short-side risk management.
		_shortEntryPrice = entryPrice;
		_shortTakeProfit = TakeProfitPips > 0m ? entryPrice - TakeProfitPips * step : null;
		_shortStopLoss = StopLossPips > 0m ? entryPrice + StopLossPips * step : null;
		_shortTrailingStop = null;

		ClearLongState();
	}

	private void ClearLongState()
	{
		_longEntryPrice = null;
		_longTakeProfit = null;
		_longStopLoss = null;
		_longTrailingStop = null;
	}

	private void ClearShortState()
	{
		_shortEntryPrice = null;
		_shortTakeProfit = null;
		_shortStopLoss = null;
		_shortTrailingStop = null;
	}

	private void ResetState()
	{
		ResetCounters();
		ClearLongState();
		ClearShortState();
	}

	private void ResetCounters()
	{
		_bullishCount = 0;
		_bearishCount = 0;
	}
}
