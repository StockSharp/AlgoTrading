namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Port of the MetaTrader expert AEROSPINE.
/// Implements a daily open breakout with optional recovery sizing and break-even management.
/// The strategy observes intraday candles, reacts once per trading day, and emulates the original breakeven behaviour.
/// </summary>
public class AeroSpineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<decimal> _entryOffsetPips;
	private readonly StrategyParam<decimal> _exitOffsetPips;
	private readonly StrategyParam<decimal> _recoveryOffsetPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _volumeFilter;
	private readonly StrategyParam<decimal> _maxSpreadPips;
	private readonly StrategyParam<bool> _enableRecovery;

	private enum TradeDirection
	{
		None,
		Long,
		Short,
	}

	private TradeDirection _currentDirection = TradeDirection.None;
	private TradeDirection _plannedRecoveryDirection = TradeDirection.None;

	private DateTime? _currentDay;
	private DateTime? _lastEntryDay;
	private decimal _dailyOpen;
	private decimal _pipSize;
	private decimal _maxSpread;

	private decimal _currentEntryPrice;
	private decimal _currentVolume;
	private bool _breakEvenArmed;

	private decimal _recoveryVolume;
	private bool _recoveryMode;

	public AeroSpineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Working timeframe", "Parameters");


		_entryHour = Param(nameof(EntryHour), 18)
		.SetDisplay("Entry Hour", "Start hour for signals", "Parameters");

		_entryOffsetPips = Param(nameof(EntryOffsetPips), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Entry Offset", "Distance from daily open in pips", "Parameters");

		_exitOffsetPips = Param(nameof(ExitOffsetPips), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Exit Offset", "Protective exit distance in pips", "Parameters");

		_recoveryOffsetPips = Param(nameof(RecoveryOffsetPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Recovery Offset", "Distance for recovery entries in pips", "Parameters");

		_takeProfitPips = Param(nameof(TakeProfitPips), 900m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Target distance in pips", "Exits");

		_breakEvenPips = Param(nameof(BreakEvenPips), 39m)
		.SetGreaterThanZero()
		.SetDisplay("Break Even", "Distance to arm break-even in pips", "Exits");

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break Even", "Enable break-even management", "Exits");

		_volumeFilter = Param(nameof(VolumeFilter), 10000m)
		.SetDisplay("Volume Filter", "Minimum candle volume", "Filters");

		_maxSpreadPips = Param(nameof(MaxSpreadPips), 25m)
		.SetDisplay("Max Spread", "Maximum allowed spread in pips", "Filters");

		_enableRecovery = Param(nameof(EnableRecovery), true)
		.SetDisplay("Enable Recovery", "Allow recovery sizing", "Filters");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}


	public int EntryHour
	{
		get => _entryHour.Value;
		set => _entryHour.Value = value;
	}

	public decimal EntryOffsetPips
	{
		get => _entryOffsetPips.Value;
		set => _entryOffsetPips.Value = value;
	}

	public decimal ExitOffsetPips
	{
		get => _exitOffsetPips.Value;
		set => _exitOffsetPips.Value = value;
	}

	public decimal RecoveryOffsetPips
	{
		get => _recoveryOffsetPips.Value;
		set => _recoveryOffsetPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	public decimal VolumeFilter
	{
		get => _volumeFilter.Value;
		set => _volumeFilter.Value = value;
	}

	public decimal MaxSpreadPips
	{
		get => _maxSpreadPips.Value;
		set => _maxSpreadPips.Value = value;
	}

	public bool EnableRecovery
	{
		get => _enableRecovery.Value;
		set => _enableRecovery.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security.PriceStep ?? 0.0001m;
		_maxSpread = MaxSpreadPips > 0m ? MaxSpreadPips * _pipSize : 0m;
		_recoveryVolume = Volume;

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateDailyState(candle);

		if (!IsSpreadAcceptable())
		return;

		if (Position == 0m)
		{
			TryEnterPosition(candle);
		}
		else
		{
			ManageOpenPosition(candle);
		}
	}

	private void UpdateDailyState(ICandleMessage candle)
	{
		var candleDay = candle.OpenTime.Date;

		if (_currentDay == candleDay)
		return;

		_currentDay = candleDay;
		_dailyOpen = candle.OpenPrice;
		_breakEvenArmed = false;

		if (_currentDirection == TradeDirection.None)
		{
			_lastEntryDay = null;
		}
	}

	private bool IsSpreadAcceptable()
	{
		if (_maxSpread <= 0m)
		return true;

		if (Security.BestBid is null || Security.BestAsk is null)
		return false;

		return Security.BestAsk.Value - Security.BestBid.Value <= _maxSpread;
	}

	private void TryEnterPosition(ICandleMessage candle)
	{
		if (_currentDay is null)
		return;

		if (_lastEntryDay == _currentDay && !IsRecoveryPending())
		return;

		if (candle.CloseTime.Hour < EntryHour)
		return;

		if (candle.TotalVolume < VolumeFilter)
		return;

		var entryOffset = EntryOffsetPips * _pipSize;
		var recoveryOffset = RecoveryOffsetPips * _pipSize;

		if (IsRecoveryPending())
		{
			if (_plannedRecoveryDirection == TradeDirection.Long)
			{
				if (candle.HighPrice < _dailyOpen + recoveryOffset)
				return;

				EnterLong(candle, _recoveryVolume);
			}
			else if (_plannedRecoveryDirection == TradeDirection.Short)
			{
				if (candle.LowPrice > _dailyOpen - recoveryOffset)
				return;

				EnterShort(candle, _recoveryVolume);
			}

			return;
		}

		if (candle.HighPrice >= _dailyOpen + entryOffset)
		{
			EnterLong(candle, Volume);
			return;
		}

		if (candle.LowPrice <= _dailyOpen - entryOffset)
		{
			EnterShort(candle, Volume);
		}
	}

	private bool IsRecoveryPending()
	{
		return EnableRecovery && _recoveryMode && _plannedRecoveryDirection != TradeDirection.None;
	}

	private void EnterLong(ICandleMessage candle, decimal volume)
	{
		BuyMarket(volume);
		_currentDirection = TradeDirection.Long;
		_currentEntryPrice = candle.ClosePrice;
		_currentVolume = volume;
		_breakEvenArmed = false;
		_lastEntryDay = _currentDay;
	}

	private void EnterShort(ICandleMessage candle, decimal volume)
	{
		SellMarket(volume);
		_currentDirection = TradeDirection.Short;
		_currentEntryPrice = candle.ClosePrice;
		_currentVolume = volume;
		_breakEvenArmed = false;
		_lastEntryDay = _currentDay;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (_currentDirection == TradeDirection.None)
		return;

		var takeProfit = TakeProfitPips * _pipSize;
		var exitOffset = ExitOffsetPips * _pipSize;
		var breakEvenDistance = BreakEvenPips * _pipSize;

		switch (_currentDirection)
		{
		case TradeDirection.Long:
			if (UseBreakEven && !_breakEvenArmed && candle.HighPrice >= _currentEntryPrice + breakEvenDistance)
			{
				_breakEvenArmed = true;
			}

			if (takeProfit > 0m && candle.HighPrice >= _currentEntryPrice + takeProfit)
			{
				CloseWithResult(TradeDirection.Long, _currentEntryPrice + takeProfit);
				return;
			}

			if (_breakEvenArmed && candle.LowPrice <= _currentEntryPrice + breakEvenDistance)
			{
				CloseWithResult(TradeDirection.Long, _currentEntryPrice + breakEvenDistance);
				return;
			}

			if (exitOffset > 0m && candle.LowPrice <= _dailyOpen - exitOffset)
			{
				CloseWithResult(TradeDirection.Long, _dailyOpen - exitOffset);
			}

			break;

		case TradeDirection.Short:
			if (UseBreakEven && !_breakEvenArmed && candle.LowPrice <= _currentEntryPrice - breakEvenDistance)
			{
				_breakEvenArmed = true;
			}

			if (takeProfit > 0m && candle.LowPrice <= _currentEntryPrice - takeProfit)
			{
				CloseWithResult(TradeDirection.Short, _currentEntryPrice - takeProfit);
				return;
			}

			if (_breakEvenArmed && candle.HighPrice >= _currentEntryPrice - breakEvenDistance)
			{
				CloseWithResult(TradeDirection.Short, _currentEntryPrice - breakEvenDistance);
				return;
			}

			if (exitOffset > 0m && candle.HighPrice >= _dailyOpen + exitOffset)
			{
				CloseWithResult(TradeDirection.Short, _dailyOpen + exitOffset);
			}

			break;
		}
	}

	private void CloseWithResult(TradeDirection direction, decimal exitPrice)
	{
		ClosePosition();

		var pnl = CalculatePnl(direction, exitPrice);

		if (EnableRecovery && pnl < 0m)
		{
			_recoveryMode = true;
			_plannedRecoveryDirection = direction == TradeDirection.Long ? TradeDirection.Short : TradeDirection.Long;
			_recoveryVolume = _currentVolume + Volume;
		}
		else
		{
			_recoveryMode = false;
			_plannedRecoveryDirection = TradeDirection.None;
			_recoveryVolume = Volume;
		}

		_currentDirection = TradeDirection.None;
		_currentEntryPrice = 0m;
		_currentVolume = 0m;
		_breakEvenArmed = false;
	}

	private decimal CalculatePnl(TradeDirection direction, decimal exitPrice)
	{
		var priceDelta = direction == TradeDirection.Long
		? exitPrice - _currentEntryPrice
		: _currentEntryPrice - exitPrice;

		return priceDelta * _currentVolume;
	}
}
