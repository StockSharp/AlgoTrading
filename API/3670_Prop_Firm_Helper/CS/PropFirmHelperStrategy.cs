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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that recreates the MetaTrader "Prop Firm Helper" expert advisor.
/// Places stop orders around Donchian channel extremes and enforces prop firm challenge limits.
/// </summary>
public class PropFirmHelperStrategy : Strategy
{
	private readonly StrategyParam<int> _entryPeriod;
	private readonly StrategyParam<int> _entryShift;
	private readonly StrategyParam<int> _exitPeriod;
	private readonly StrategyParam<int> _exitShift;
	private readonly StrategyParam<bool> _useChallenge;
	private readonly StrategyParam<decimal> _passCriteria;
	private readonly StrategyParam<decimal> _dailyLossLimit;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _entryUpperHistory = new();
	private readonly List<decimal> _entryLowerHistory = new();
	private readonly List<decimal> _exitLowerHistory = new();
	private readonly List<decimal> _exitUpperHistory = new();

	private DonchianChannels _entryChannel = null!;
	private DonchianChannels _exitChannel = null!;
	private AverageTrueRange _atr = null!;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _longStopOrder;
	private Order _shortStopOrder;

	private decimal _tickSize;
	private DateTime _currentChallengeDay;
	private decimal _dayStartEquity;
	private bool _challengeLocked;

	/// <summary>
	/// Initializes a new instance of <see cref="PropFirmHelperStrategy"/>.
	/// </summary>
	public PropFirmHelperStrategy()
	{
		_entryPeriod = Param(nameof(EntryPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Entry Period", "Number of candles used for breakout Donchian channel", "Entries")
			.SetCanOptimize(true);

		_entryShift = Param(nameof(EntryShift), 1)
			.SetRange(0, 20)
			.SetDisplay("Entry Shift", "Number of finished candles ignored when reading Donchian breakout", "Entries")
			.SetCanOptimize(true);

		_exitPeriod = Param(nameof(ExitPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Exit Period", "Number of candles used for trailing Donchian channel", "Exits")
			.SetCanOptimize(true);

		_exitShift = Param(nameof(ExitShift), 1)
			.SetRange(0, 20)
			.SetDisplay("Exit Shift", "Number of finished candles ignored when reading trailing Donchian", "Exits")
			.SetCanOptimize(true);

		_useChallenge = Param(nameof(UseChallenge), false)
			.SetDisplay("Use Challenge Rules", "Enable prop firm pass and daily loss checks", "Challenge");

		_passCriteria = Param(nameof(PassCriteria), 110100m)
			.SetDisplay("Pass Criteria", "Equity level that stops the strategy", "Challenge");

		_dailyLossLimit = Param(nameof(DailyLossLimit), 4500m)
			.SetDisplay("Daily Loss Limit", "Maximum daily drawdown allowed before trading stops", "Challenge");

		_riskPerTrade = Param(nameof(RiskPerTrade), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Per Trade %", "Percentage of equity risked per position", "Risk")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Average True Range lookback used for trailing filters", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Donchian calculations", "General");
	}

	/// <summary>
	/// Donchian breakout lookback length.
	/// </summary>
	public int EntryPeriod
	{
		get => _entryPeriod.Value;
		set => _entryPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles to shift breakout reading back in time.
	/// </summary>
	public int EntryShift
	{
		get => _entryShift.Value;
		set => _entryShift.Value = value;
	}

	/// <summary>
	/// Donchian trailing lookback length.
	/// </summary>
	public int ExitPeriod
	{
		get => _exitPeriod.Value;
		set => _exitPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles to shift trailing channel readings back in time.
	/// </summary>
	public int ExitShift
	{
		get => _exitShift.Value;
		set => _exitShift.Value = value;
	}

	/// <summary>
	/// Enables prop firm helper risk checks.
	/// </summary>
	public bool UseChallenge
	{
		get => _useChallenge.Value;
		set => _useChallenge.Value = value;
	}

	/// <summary>
	/// Equity level that finishes the challenge.
	/// </summary>
	public decimal PassCriteria
	{
		get => _passCriteria.Value;
		set => _passCriteria.Value = value;
	}

	/// <summary>
	/// Maximum allowed daily drawdown before trading stops.
	/// </summary>
	public decimal DailyLossLimit
	{
		get => _dailyLossLimit.Value;
		set => _dailyLossLimit.Value = value;
	}

	/// <summary>
	/// Percentage of equity risked on each position.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	/// <summary>
	/// Lookback period for ATR trailing filter.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator subscriptions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryUpperHistory.Clear();
		_entryLowerHistory.Clear();
		_exitLowerHistory.Clear();
		_exitUpperHistory.Clear();

		_buyStopOrder = null;
		_sellStopOrder = null;
		_longStopOrder = null;
		_shortStopOrder = null;

		_tickSize = 0m;
		_currentChallengeDay = default;
		_dayStartEquity = 0m;
		_challengeLocked = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_tickSize = Security?.PriceStep ?? 0.0001m;

		_entryChannel = new DonchianChannels { Length = EntryPeriod };
		_exitChannel = new DonchianChannels { Length = ExitPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		var portfolio = Portfolio;
		if (portfolio != null)
		{
			_currentChallengeDay = time.Date;
			_dayStartEquity = portfolio.CurrentValue;
		}

		subscription
			.BindEx(_entryChannel, _exitChannel, _atr, ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelOrder(ref _longStopOrder);
			CancelOrder(ref _shortStopOrder);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue entryValue, IIndicatorValue exitValue, IIndicatorValue atrValue)
	{
		// Work only with finished candles to mimic the original BarOpen check.
		if (candle.State != CandleStates.Finished)
			return;

		UpdateChallengeState(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (entryValue is not DonchianChannelsValue entryBands ||
			exitValue is not DonchianChannelsValue exitBands)
			return;

		if (entryBands.UpBand is not decimal entryUpper ||
			entryBands.LowBand is not decimal entryLower ||
			exitBands.LowBand is not decimal exitLower ||
			exitBands.UpBand is not decimal exitUpper)
			return;

		if (!atrValue.IsFinal)
			return;

		var atr = atrValue.GetValue<decimal>();

		UpdateHistory(_entryUpperHistory, entryUpper, EntryShift);
		UpdateHistory(_entryLowerHistory, entryLower, EntryShift);
		UpdateHistory(_exitLowerHistory, exitLower, ExitShift);
		UpdateHistory(_exitUpperHistory, exitUpper, ExitShift);

		var shiftedEntryUpper = GetShiftedValue(_entryUpperHistory, EntryShift);
		var shiftedEntryLower = GetShiftedValue(_entryLowerHistory, EntryShift);
		var shiftedExitLower = GetShiftedValue(_exitLowerHistory, ExitShift);
		var shiftedExitUpper = GetShiftedValue(_exitUpperHistory, ExitShift);

		if (shiftedEntryUpper is null || shiftedEntryLower is null || shiftedExitLower is null || shiftedExitUpper is null)
			return;

		var triggerLong = shiftedEntryUpper.Value + _tickSize;
		var triggerShort = shiftedEntryLower.Value - _tickSize;
		var exitLong = shiftedExitLower.Value;
		var exitShort = shiftedExitUpper.Value;

		ManagePositions(candle, exitLong, exitShort, atr);

		if (_challengeLocked)
		{
			CancelOrder(ref _buyStopOrder);
			CancelOrder(ref _sellStopOrder);
			return;
		}

		UpdateEntryOrders(candle.ClosePrice, triggerLong, triggerShort, exitLong, exitShort);
	}

	private void UpdateEntryOrders(decimal closePrice, decimal triggerLong, decimal triggerShort, decimal exitLong, decimal exitShort)
	{
		if (Position <= 0m)
		{
			var stopDistance = triggerLong - exitLong;
			if (stopDistance > _tickSize && triggerLong - closePrice >= _tickSize)
			{
				var volume = CalculateVolume(stopDistance);
				EnsureOrder(ref _buyStopOrder, Sides.Buy, volume, triggerLong);
			}
			else
			{
				CancelOrder(ref _buyStopOrder);
			}
		}
		else
		{
			CancelOrder(ref _buyStopOrder);
		}

		if (Position >= 0m)
		{
			var stopDistance = exitShort - triggerShort;
			if (stopDistance > _tickSize && closePrice - triggerShort >= _tickSize)
			{
				var volume = CalculateVolume(stopDistance);
				EnsureOrder(ref _sellStopOrder, Sides.Sell, volume, triggerShort);
			}
			else
			{
				CancelOrder(ref _sellStopOrder);
			}
		}
		else
		{
			CancelOrder(ref _sellStopOrder);
		}
	}

	private void ManagePositions(ICandleMessage candle, decimal exitLong, decimal exitShort, decimal atr)
	{
		var atrBuffer = atr * 0.1m;

		if (Position > 0m)
		{
			// Exit long if price falls below trailing Donchian low.
			if (candle.ClosePrice < exitLong)
			{
				SellMarket(Position);
				CancelOrder(ref _longStopOrder);
				return;
			}

			var newStop = exitLong;
			if (candle.ClosePrice - newStop >= _tickSize)
			{
				var currentStop = _longStopOrder?.Price ?? decimal.MinValue;
				if (newStop > currentStop + atrBuffer)
					MoveProtectiveStop(ref _longStopOrder, Sides.Sell, newStop, Position);
			}
		}
		else if (Position < 0m)
		{
			// Exit short if price rallies above trailing Donchian high.
			if (candle.ClosePrice > exitShort)
			{
				BuyMarket(-Position);
				CancelOrder(ref _shortStopOrder);
				return;
			}

			var newStop = exitShort;
			if (newStop - candle.ClosePrice >= _tickSize)
			{
				var currentStop = _shortStopOrder?.Price ?? decimal.MaxValue;
				if (newStop < currentStop - atrBuffer)
					MoveProtectiveStop(ref _shortStopOrder, Sides.Buy, newStop, -Position);
			}
		}
		else
		{
			CancelOrder(ref _longStopOrder);
			CancelOrder(ref _shortStopOrder);
		}
	}

	private void UpdateChallengeState(ICandleMessage candle)
	{
		if (!UseChallenge || _challengeLocked)
			return;

		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var equity = portfolio.CurrentValue;
		if (equity >= PassCriteria && PassCriteria > 0m)
		{
			LogInfo("Prop firm challenge passed. Clearing all positions and orders.");
			ClearAll();
			_challengeLocked = true;
			return;
		}

		var candleDay = candle.CloseTime.Date;
		if (_currentChallengeDay != candleDay)
		{
			_currentChallengeDay = candleDay;
			_dayStartEquity = equity;
		}

		if (DailyLossLimit > 0m && _dayStartEquity - equity >= DailyLossLimit)
		{
			LogInfo("Daily loss limit exceeded. Clearing all positions and orders.");
			ClearAll();
			_challengeLocked = true;
		}
	}

	private void ClearAll()
	{
		CancelOrder(ref _buyStopOrder);
		CancelOrder(ref _sellStopOrder);
		CancelOrder(ref _longStopOrder);
		CancelOrder(ref _shortStopOrder);

		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(-Position);
	}

	private void EnsureOrder(ref Order order, Sides side, decimal volume, decimal price)
	{
		if (volume <= 0m)
		{
			CancelOrder(ref order);
			return;
		}

		var adjustedVolume = AdjustVolume(volume);
		if (adjustedVolume <= 0m)
		{
			CancelOrder(ref order);
			return;
		}

		if (order != null && order.State.IsActive())
		{
			var samePrice = order.Price == price;
			var sameVolume = order.Volume.HasValue && order.Volume.Value == adjustedVolume;
			if (samePrice && sameVolume)
				return;

			CancelOrder(order);
		}

		order = side == Sides.Buy
			? BuyStop(adjustedVolume, price)
			: SellStop(adjustedVolume, price);
	}

	private void MoveProtectiveStop(ref Order order, Sides side, decimal price, decimal volume)
	{
		if (order != null && order.State.IsActive())
			CancelOrder(order);

		order = side == Sides.Buy
			? BuyStop(volume, price)
			: SellStop(volume, price);
	}

	private void CancelOrder(ref Order order)
	{
		if (order == null)
			return;

		if (order.State.IsActive())
			CancelOrder(order);

		order = null;
	}

	private decimal CalculateVolume(decimal stopDistance)
	{
		if (stopDistance <= 0m)
			return 0m;

		var portfolio = Portfolio;
		if (portfolio == null)
			return Volume;

		var riskPercent = RiskPerTrade / 100m;
		if (riskPercent <= 0m)
			return Volume;

		var equity = portfolio.CurrentValue;
		if (equity <= 0m)
			return Volume;

		var priceStep = Security?.PriceStep ?? 0.0001m;
		var stepPrice = Security?.StepPrice ?? priceStep;
		if (priceStep <= 0m || stepPrice <= 0m)
			return Volume;

		var riskMoney = equity * riskPercent;
		var lossPerUnit = stopDistance / priceStep * stepPrice;
		if (lossPerUnit <= 0m)
			return Volume;

		var volume = riskMoney / lossPerUnit;
		return volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var volumeStep = Security?.VolumeStep ?? 1m;
		if (volumeStep <= 0m)
			volumeStep = 1m;

		var minVolume = Security?.MinVolume ?? volumeStep;
		var maxVolume = Security?.MaxVolume ?? decimal.MaxValue;

		var adjusted = Math.Floor(volume / volumeStep) * volumeStep;
		if (adjusted < minVolume)
			adjusted = minVolume;

		if (adjusted > maxVolume)
			adjusted = maxVolume;

		return adjusted;
	}

	private static void UpdateHistory(List<decimal> history, decimal value, int shift)
	{
		history.Add(value);

		var maxCount = shift + 1;
		if (maxCount <= 0)
			maxCount = 1;

		while (history.Count > maxCount)
			history.RemoveAt(0);
	}

	private static decimal? GetShiftedValue(List<decimal> history, int shift)
	{
		var index = history.Count - 1 - shift;
		if (index < 0 || index >= history.Count)
			return null;

		return history[index];
	}
}

