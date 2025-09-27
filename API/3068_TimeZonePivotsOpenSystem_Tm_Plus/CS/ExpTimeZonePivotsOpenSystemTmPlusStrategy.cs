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
/// Strategy that replicates the Time Zone Pivots Open System expert from MetaTrader.
/// It follows the session open price and reacts when candles close above or below the
/// upper and lower offset bands while respecting the original money management rules.
/// </summary>
public class ExpTimeZonePivotsOpenSystemTmPlusStrategy : Strategy
{
	// Parameters from the original expert controlling size, stops and permissions.
	private readonly StrategyParam<decimal> _moneyManagement;
	private readonly StrategyParam<MoneyManagementMode> _moneyMode;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _deviationPoints;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<int> _holdingMinutes;
	private readonly StrategyParam<decimal> _offsetPoints;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _startHour;

	// Rolling buffer that stores recent candle states for the indicator recreation.
	private readonly List<ZoneSnapshot> _zoneHistory = new();

	// Session level tracking.
	private DateTime? _lastSessionDate;
	private decimal? _sessionOpenPrice;
	private decimal? _upperBand;
	private decimal? _lowerBand;

	// Pending entry scheduling.
	private bool _pendingLongEntry;
	private bool _pendingShortEntry;
	private DateTimeOffset? _longSignalTime;
	private DateTimeOffset? _shortSignalTime;
	private DateTimeOffset? _lastLongSignalOrigin;
	private DateTimeOffset? _lastShortSignalOrigin;
	private DateTimeOffset? _currentCandleOpen;

	// Position bookkeeping for exit controls.
	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	// Cached timeframe of the selected candle series.
	private TimeSpan? _timeFrame;

	public decimal MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	public MoneyManagementMode MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal DeviationPoints
	{
		get => _deviationPoints.Value;
		set => _deviationPoints.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	public bool TimeTrade
	{
		get => _useTimeExit.Value;
		set => _useTimeExit.Value = value;
	}

	public int HoldingMinutes
	{
		get => _holdingMinutes.Value;
		set => _holdingMinutes.Value = value;
	}

	public decimal OffsetPoints
	{
		get => _offsetPoints.Value;
		set => _offsetPoints.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public ExpTimeZonePivotsOpenSystemTmPlusStrategy()
	{
		_moneyManagement = Param(nameof(MoneyManagement), 0.1m)
			.SetDisplay("Money Management", "Base value used for position sizing", "Trading")
			.SetGreaterThanZero();

		_moneyMode = Param(nameof(MoneyMode), MoneyManagementMode.Lot)
			.SetDisplay("Money Mode", "Position sizing model", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss (points)", "Distance from entry to stop loss expressed in points", "Risk")
			.SetNotNegative();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit (points)", "Distance from entry to take profit expressed in points", "Risk")
			.SetNotNegative();

		_deviationPoints = Param(nameof(DeviationPoints), 10m)
			.SetDisplay("Allowed Deviation", "Maximum acceptable price deviation for entries", "Risk")
			.SetNotNegative();

		_allowBuyOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_allowSellOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_allowBuyClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions on opposite signals", "Trading");

		_allowSellClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions on opposite signals", "Trading");

		_useTimeExit = Param(nameof(TimeTrade), true)
			.SetDisplay("Use Time Exit", "Close positions after a fixed holding time", "Risk");

		_holdingMinutes = Param(nameof(HoldingMinutes), 720)
			.SetDisplay("Holding Minutes", "Maximum position lifetime in minutes", "Risk")
			.SetNotNegative();

		_offsetPoints = Param(nameof(OffsetPoints), 200m)
			.SetDisplay("Offset (points)", "Distance from session open that defines the pivot zones", "Indicator")
			.SetNotNegative();

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Number of bars to delay the signal evaluation", "Indicator")
			.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for calculations", "Indicator");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Session Start Hour", "Hour of day used to anchor the session open price", "Indicator")
			.SetNotNegative()
			.SetLessOrEqual(23);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_zoneHistory.Clear();
		_lastSessionDate = null;
		_sessionOpenPrice = null;
		_upperBand = null;
		_lowerBand = null;
		_pendingLongEntry = false;
		_pendingShortEntry = false;
		_longSignalTime = null;
		_shortSignalTime = null;
		_lastLongSignalOrigin = null;
		_lastShortSignalOrigin = null;
		_currentCandleOpen = null;
		_longEntryTime = null;
		_shortEntryTime = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_timeFrame = CandleType.Arg as TimeSpan?;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_timeFrame = CandleType.Arg as TimeSpan?;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		// Enable loss protection guard as required by the framework.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Detect new candle openings to trigger delayed entries on the first update.
		if (_currentCandleOpen != candle.OpenTime)
		{
			_currentCandleOpen = candle.OpenTime;
			TryExecutePendingEntries(candle);
		}

		// Only finished candles contribute to the indicator logic and trade decisions.
		if (candle.State != CandleStates.Finished)
			return;

		// Refresh the session reference and pre-compute band levels.
		UpdateSessionReference(candle);

		// Memorise the current candle classification for delayed evaluations.
		var snapshot = new ZoneSnapshot
		{
			State = DetermineState(candle),
			OpenTime = candle.OpenTime,
			CloseTime = candle.CloseTime
		};

		_zoneHistory.Insert(0, snapshot);

		var maxHistory = Math.Max(5, SignalBar + 3);
		while (_zoneHistory.Count > maxHistory)
			_zoneHistory.RemoveAt(_zoneHistory.Count - 1);

		// Ensure we have enough candles to evaluate the signal and confirmation offsets.
		if (_zoneHistory.Count <= SignalBar + 1)
		{
			ManageStops(candle);
			HandleTimeExit(candle.CloseTime);
			return;
		}

		var signalSnapshot = _zoneHistory[SignalBar];
		var confirmSnapshot = _zoneHistory[SignalBar + 1];

		var closeLong = false;
		var closeShort = false;

		// Previous candle closed above the upper band – schedule long entry and close shorts.
		if (confirmSnapshot.State == ZoneSignal.Above)
		{
			if (SellPosClose)
				closeShort = true;

			if (BuyPosOpen && signalSnapshot.State != ZoneSignal.Above && (_lastLongSignalOrigin != confirmSnapshot.CloseTime))
			{
				_pendingLongEntry = true;
				_longSignalTime = confirmSnapshot.CloseTime + (_timeFrame ?? TimeSpan.Zero);
				_lastLongSignalOrigin = confirmSnapshot.CloseTime;
			}
		}
		// Previous candle closed below the lower band – schedule short entry and close longs.
		else if (confirmSnapshot.State == ZoneSignal.Below)
		{
			if (BuyPosClose)
				closeLong = true;

			if (SellPosOpen && signalSnapshot.State != ZoneSignal.Below && (_lastShortSignalOrigin != confirmSnapshot.CloseTime))
			{
				_pendingShortEntry = true;
				_shortSignalTime = confirmSnapshot.CloseTime + (_timeFrame ?? TimeSpan.Zero);
				_lastShortSignalOrigin = confirmSnapshot.CloseTime;
			}
		}

		if (closeLong && Position > 0m)
		{
			SellMarket(Position);
			_longEntryTime = null;
			_longEntryPrice = null;
			_longStopPrice = null;
			_longTakePrice = null;
		}

		if (closeShort && Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			_shortEntryTime = null;
			_shortEntryPrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}

		ManageStops(candle);
		HandleTimeExit(candle.CloseTime);
	}

	// Execute pending entries once the new candle that should host the trade begins.
	private void TryExecutePendingEntries(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return;

		var opened = false;

		if (_pendingLongEntry && BuyPosOpen)
		{
			if (!_longSignalTime.HasValue || candle.OpenTime >= _longSignalTime.Value)
			{
				var entryPrice = candle.OpenPrice;
				var volume = GetEntryVolume(true, entryPrice);

				if (volume > 0m)
				{
					_longEntryPrice = entryPrice;
					BuyMarket(volume);
					_pendingLongEntry = false;
					_longSignalTime = null;
					opened = true;
				}
			}
		}

		if (!opened && _pendingShortEntry && SellPosOpen)
		{
			if (!_shortSignalTime.HasValue || candle.OpenTime >= _shortSignalTime.Value)
			{
				var entryPrice = candle.OpenPrice;
				var volume = GetEntryVolume(false, entryPrice);

				if (volume > 0m)
				{
					_shortEntryPrice = entryPrice;
					SellMarket(volume);
					_pendingShortEntry = false;
					_shortSignalTime = null;
				}
			}
		}
	}

	// Monitor stop-loss and take-profit levels intrabar using candle extremes.
	private void ManageStops(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		if (Position > 0m)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(volume);
				_longEntryTime = null;
				_longEntryPrice = null;
				_longStopPrice = null;
				_longTakePrice = null;
			}
			else if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(volume);
				_longEntryTime = null;
				_longEntryPrice = null;
				_longStopPrice = null;
				_longTakePrice = null;
			}
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(volume);
				_shortEntryTime = null;
				_shortEntryPrice = null;
				_shortStopPrice = null;
				_shortTakePrice = null;
			}
			else if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(volume);
				_shortEntryTime = null;
				_shortEntryPrice = null;
				_shortStopPrice = null;
				_shortTakePrice = null;
			}
		}
	}

	// Implement the time based exit from the MQL5 code.
	private void HandleTimeExit(DateTimeOffset time)
	{
		if (!TimeTrade)
			return;

		var holdMinutes = HoldingMinutes;
		if (holdMinutes <= 0)
			return;

		var threshold = TimeSpan.FromMinutes(holdMinutes);
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		if (Position > 0m && _longEntryTime.HasValue && time - _longEntryTime.Value >= threshold)
		{
			SellMarket(volume);
			_longEntryTime = null;
			_longEntryPrice = null;
			_longStopPrice = null;
			_longTakePrice = null;
		}
		else if (Position < 0m && _shortEntryTime.HasValue && time - _shortEntryTime.Value >= threshold)
		{
			BuyMarket(volume);
			_shortEntryTime = null;
			_shortEntryPrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
	}

	// Update the session open reference when the configured hour is reached.
	private void UpdateSessionReference(ICandleMessage candle)
	{
		var openTime = candle.OpenTime;
		var currentDate = openTime.Date;

		if ((!_lastSessionDate.HasValue || _lastSessionDate.Value != currentDate) && openTime.Hour == StartHour)
		{
			_sessionOpenPrice = candle.OpenPrice;
			_lastSessionDate = currentDate;
			_zoneHistory.Clear();
			_pendingLongEntry = false;
			_pendingShortEntry = false;
			_longSignalTime = null;
			_shortSignalTime = null;
			_lastLongSignalOrigin = null;
			_lastShortSignalOrigin = null;
		}

		if (_sessionOpenPrice.HasValue)
		{
			var step = GetPriceStep();
			var offset = OffsetPoints * step;

			_upperBand = _sessionOpenPrice + offset;
			_lowerBand = _sessionOpenPrice - offset;
		}
		else
		{
			_upperBand = null;
			_lowerBand = null;
		}
	}

	// Classify the candle relative to the offset bands.
	private ZoneSignal DetermineState(ICandleMessage candle)
	{
		if (!_sessionOpenPrice.HasValue || !_upperBand.HasValue || !_lowerBand.HasValue)
			return ZoneSignal.Inside;

		if (candle.ClosePrice > _upperBand.Value)
			return ZoneSignal.Above;

		if (candle.ClosePrice < _lowerBand.Value)
			return ZoneSignal.Below;

		return ZoneSignal.Inside;
	}

	// Translate the money management mode into an executable volume.
	private decimal GetEntryVolume(bool isLong, decimal price)
	{
		if (price <= 0m)
			return 0m;

		var step = GetPriceStep();
		var stopDistance = StopLossPoints > 0m ? StopLossPoints * step : 0m;
		var capital = Portfolio?.CurrentValue ?? 0m;
		var mmValue = MoneyManagement;

		switch (MoneyMode)
		{
			case MoneyManagementMode.Lot:
				return mmValue;
			case MoneyManagementMode.Balance:
			case MoneyManagementMode.FreeMargin:
				return capital > 0m ? capital * mmValue / price : 0m;
			case MoneyManagementMode.LossBalance:
			case MoneyManagementMode.LossFreeMargin:
				if (stopDistance > 0m)
					return capital > 0m ? capital * mmValue / stopDistance : 0m;

				return capital > 0m ? capital * mmValue / price : 0m;
			default:
				return mmValue;
		}
	}

	// Retrieve the minimum price step for the configured security.
	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		if (security.PriceStep > 0m)
			return security.PriceStep;

		if (security.MinStep > 0m)
			return security.MinStep;

		return 0.0001m;
	}

	// Helper to compute stop-loss and take-profit levels around the fill price.
	private decimal? CalculateStopPrice(bool isLong, decimal? entryPrice)
	{
		if (!entryPrice.HasValue || StopLossPoints <= 0m)
			return null;

		var distance = StopLossPoints * GetPriceStep();
		return isLong ? entryPrice - distance : entryPrice + distance;
	}

	private decimal? CalculateTakePrice(bool isLong, decimal? entryPrice)
	{
		if (!entryPrice.HasValue || TakeProfitPoints <= 0m)
			return null;

		var distance = TakeProfitPoints * GetPriceStep();
		return isLong ? entryPrice + distance : entryPrice - distance;
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position > 0m && trade.OrderDirection == Sides.Buy)
		{
			_longEntryTime = trade.ServerTime;
			_longEntryPrice = trade.Price;
			_longStopPrice = CalculateStopPrice(true, _longEntryPrice);
			_longTakePrice = CalculateTakePrice(true, _longEntryPrice);
		}
		else if (Position < 0m && trade.OrderDirection == Sides.Sell)
		{
			_shortEntryTime = trade.ServerTime;
			_shortEntryPrice = trade.Price;
			_shortStopPrice = CalculateStopPrice(false, _shortEntryPrice);
			_shortTakePrice = CalculateTakePrice(false, _shortEntryPrice);
		}

		if (Position == 0m)
		{
			_longEntryTime = null;
			_shortEntryTime = null;
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longStopPrice = null;
			_shortStopPrice = null;
			_longTakePrice = null;
			_shortTakePrice = null;
		}
	}

	public enum MoneyManagementMode
	{
		FreeMargin,
		Balance,
		LossFreeMargin,
		LossBalance,
		Lot
	}

	private enum ZoneSignal
	{
		Inside,
		Above,
		Below
	}

	private sealed class ZoneSnapshot
	{
		public ZoneSignal State { get; init; }
		public DateTimeOffset OpenTime { get; init; }
		public DateTimeOffset CloseTime { get; init; }
	}
}

