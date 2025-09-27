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
/// Port of the "earlyTopProrate_V1" MetaTrader strategy using StockSharp high level API.
/// The strategy trades around the daily open and scales out at predefined profit targets.
/// </summary>
public class EarlyTopProrateV1Strategy : Strategy
{
	private enum MoneyManagementModes
	{
		Fixed = 0,
		FixedAlternate = 1,
		BalanceSquareRoot = 2,
		EquityRisk = 3
	}

	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _closingHour;
	private readonly StrategyParam<int> _timeZoneShift;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _takeProfit1;
	private readonly StrategyParam<int> _takeProfit2;
	private readonly StrategyParam<int> _takeProfit3;
	private readonly StrategyParam<int> _breakEvenTrigger;
	private readonly StrategyParam<int> _stopLoss0;
	private readonly StrategyParam<int> _stopLoss1;
	private readonly StrategyParam<int> _stopLoss2;
	private readonly StrategyParam<int> _ratio1;
	private readonly StrategyParam<int> _ratio2;
	private readonly StrategyParam<int> _ratio3;
	private readonly StrategyParam<int> _moneyManagementType;
	private readonly StrategyParam<decimal> _mmFactor;
	private readonly StrategyParam<int> _mmRiskPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _pointMultiplier;

	private DateTime? _currentDay;
	private decimal _dailyOpen;
	private decimal _dailyHigh;
	private decimal _dailyLow;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private bool _longBreakEvenActive;
	private bool _shortBreakEvenActive;
	private int _phase;

	/// <summary>
	/// Hour when new positions can be opened.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour when entries are disabled.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Hour when all positions must be closed.
	/// </summary>
	public int ClosingHour
	{
		get => _closingHour.Value;
		set => _closingHour.Value = value;
	}

	/// <summary>
	/// Time zone helper used only for documentation (kept for compatibility with the MQL inputs).
	/// </summary>
	public int TimeZoneShift
	{
		get => _timeZoneShift.Value;
		set => _timeZoneShift.Value = value;
	}

	/// <summary>
	/// Base order volume expressed in lots.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous market positions.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Distance of the first take profit target in points (multiplied by <see cref="PointMultiplier"/>).
	/// </summary>
	public int TakeProfit1
	{
		get => _takeProfit1.Value;
		set => _takeProfit1.Value = value;
	}

	/// <summary>
	/// Distance of the second take profit target in points.
	/// </summary>
	public int TakeProfit2
	{
		get => _takeProfit2.Value;
		set => _takeProfit2.Value = value;
	}

	/// <summary>
	/// Distance of the final take profit target in points.
	/// </summary>
	public int TakeProfit3
	{
		get => _takeProfit3.Value;
		set => _takeProfit3.Value = value;
	}

	/// <summary>
	/// Drawdown from the entry that activates the break-even exit.
	/// </summary>
	public int BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	/// <summary>
	/// Maximum allowed adverse excursion before closing the position immediately.
	/// </summary>
	public int StopLoss0
	{
		get => _stopLoss0.Value;
		set => _stopLoss0.Value = value;
	}

	/// <summary>
	/// Profit threshold that moves the stop to the entry price.
	/// </summary>
	public int StopLoss1
	{
		get => _stopLoss1.Value;
		set => _stopLoss1.Value = value;
	}

	/// <summary>
	/// Profit threshold that shifts the stop into profit territory.
	/// </summary>
	public int StopLoss2
	{
		get => _stopLoss2.Value;
		set => _stopLoss2.Value = value;
	}

	/// <summary>
	/// Portion of the position closed at the first target.
	/// </summary>
	public int Ratio1
	{
		get => _ratio1.Value;
		set => _ratio1.Value = value;
	}

	/// <summary>
	/// Portion of the position closed at the second target.
	/// </summary>
	public int Ratio2
	{
		get => _ratio2.Value;
		set => _ratio2.Value = value;
	}

	/// <summary>
	/// Portion of the position closed at the final target.
	/// </summary>
	public int Ratio3
	{
		get => _ratio3.Value;
		set => _ratio3.Value = value;
	}

	/// <summary>
	/// Selected money management mode.
	/// </summary>
	public int MoneyManagementType
	{
		get => _moneyManagementType.Value;
		set => _moneyManagementType.Value = value;
	}

	/// <summary>
	/// Multiplier used by the square root balance model.
	/// </summary>
	public decimal MoneyManagementFactor
	{
		get => _mmFactor.Value;
		set => _mmFactor.Value = value;
	}

	/// <summary>
	/// Risk percentage used by the equity based model.
	/// </summary>
	public int MoneyManagementRiskPercent
	{
		get => _mmRiskPercent.Value;
		set => _mmRiskPercent.Value = value;
	}

	/// <summary>
	/// Candle type used for the main decision logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Multiplier applied to price steps to emulate the 10*Point arithmetic from MetaTrader.
	/// </summary>
	public decimal PointMultiplier
	{
		get => _pointMultiplier.Value;
		set => _pointMultiplier.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EarlyTopProrateV1Strategy"/> class.
	/// </summary>
	public EarlyTopProrateV1Strategy()
	{
		_startHour = Param(nameof(StartHour), 5)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "Hour when entries become available", "Trading Hours");

		_endHour = Param(nameof(EndHour), 10)
		.SetRange(0, 23)
		.SetDisplay("End Hour", "Hour when new entries stop", "Trading Hours");

		_closingHour = Param(nameof(ClosingHour), 18)
		.SetRange(0, 23)
		.SetDisplay("Closing Hour", "Hour when the strategy forces a flat position", "Trading Hours");

		_timeZoneShift = Param(nameof(TimeZoneShift), 0)
		.SetRange(-12, 12)
		.SetDisplay("Time Zone Shift", "Reference offset kept for documentation", "Trading Hours");

		_baseVolume = Param(nameof(BaseVolume), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Default trade volume in lots", "Money Management")
		.SetCanOptimize(true);

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetRange(1, 5)
		.SetDisplay("Max Positions", "Maximum simultaneous positions", "Money Management");

		_takeProfit1 = Param(nameof(TakeProfit1), 25)
		.SetRange(0, 500)
		.SetDisplay("Take Profit 1", "Distance of the first take profit target", "Trade Management");

		_takeProfit2 = Param(nameof(TakeProfit2), 50)
		.SetRange(0, 500)
		.SetDisplay("Take Profit 2", "Distance of the second take profit target", "Trade Management");

		_takeProfit3 = Param(nameof(TakeProfit3), 75)
		.SetRange(0, 500)
		.SetDisplay("Take Profit 3", "Distance of the final take profit target", "Trade Management");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 35)
		.SetRange(0, 500)
		.SetDisplay("Break-Even Trigger", "Loss threshold that activates the break-even exit", "Trade Management");

		_stopLoss0 = Param(nameof(StopLoss0), 100)
		.SetRange(0, 1000)
		.SetDisplay("Emergency Stop", "Loss threshold that closes the position immediately", "Trade Management");

		_stopLoss1 = Param(nameof(StopLoss1), 35)
		.SetRange(0, 1000)
		.SetDisplay("Stop To Entry", "Profit required to move the stop to the entry price", "Trade Management");

		_stopLoss2 = Param(nameof(StopLoss2), 60)
		.SetRange(0, 1000)
		.SetDisplay("Stop In Profit", "Profit required to trail the stop beyond the entry", "Trade Management");

		_ratio1 = Param(nameof(Ratio1), 30)
		.SetRange(0, 100)
		.SetDisplay("Ratio 1", "Percentage closed at the first target", "Trade Management");

		_ratio2 = Param(nameof(Ratio2), 70)
		.SetRange(0, 100)
		.SetDisplay("Ratio 2", "Percentage closed at the second target", "Trade Management");

		_ratio3 = Param(nameof(Ratio3), 100)
		.SetRange(0, 100)
		.SetDisplay("Ratio 3", "Percentage closed at the final target", "Trade Management");

		_moneyManagementType = Param(nameof(MoneyManagementType), (int)MoneyManagementModes.Fixed)
		.SetRange(0, 3)
		.SetDisplay("Money Management Mode", "Selects how the order volume is calculated", "Money Management");

		_mmFactor = Param(nameof(MoneyManagementFactor), 3m)
		.SetNotNegative()
		.SetDisplay("MM Factor", "Multiplier used by the square root balance model", "Money Management");

		_mmRiskPercent = Param(nameof(MoneyManagementRiskPercent), 50)
		.SetRange(0, 1000)
		.SetDisplay("MM Risk %", "Risk percent applied to the equity based sizing", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series used by the logic", "General");

		_pointMultiplier = Param(nameof(PointMultiplier), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Point Multiplier", "Multiplier applied to price steps when converting points to prices", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateDailyLevels(candle);

		if (Position > 0m)
		ManageLongPosition(candle);
		else if (Position < 0m)
		ManageShortPosition(candle);

		if (ClosingHour > 0 && Position != 0m && IsClosingTime(candle.CloseTime))
		{
			ExitAtClose();
			return;
		}

		if (Position == 0m)
		{
			ResetPositionState();

			if (!IsWithinTradingWindow(candle.CloseTime))
			return;

			if (!HasDailyContext())
			return;

			var trendDirection = GetTrendDirection();
			var price = candle.ClosePrice;

			if (trendDirection > 0 && price > _dailyOpen)
			EnterLong(candle);
			else if (trendDirection < 0 && price < _dailyOpen)
			EnterShort(candle);
		}
	}

	private void UpdateDailyLevels(ICandleMessage candle)
	{
		var candleDate = candle.CloseTime.Date;

		if (_currentDay != candleDate)
		{
			_currentDay = candleDate;
			_dailyOpen = candle.OpenPrice;
			_dailyHigh = candle.HighPrice;
			_dailyLow = candle.LowPrice;
		}
		else
		{
			if (candle.HighPrice > _dailyHigh)
			_dailyHigh = candle.HighPrice;

			if (candle.LowPrice < _dailyLow)
			_dailyLow = candle.LowPrice;
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (Position <= 0m)
		return;

		var entryPrice = _longEntryPrice ?? candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		// Activate the break-even exit after a predefined drawdown.
		if (!_longBreakEvenActive && BreakEvenTrigger > 0)
		{
			var triggerPrice = entryPrice - GetPriceOffset(BreakEvenTrigger);
			if (low <= triggerPrice)
			_longBreakEvenActive = true;
		}

		// Close at the entry price when the market recovers after the drawdown.
		if (_longBreakEvenActive && high >= entryPrice)
		{
			SellMarket(Position);
			return;
		}

		// Emergency stop closes the position immediately.
		if (StopLoss0 > 0)
		{
			var emergency = entryPrice - GetPriceOffset(StopLoss0);
			if (low <= emergency)
			{
				SellMarket(Position);
				return;
			}
		}

		// Determine the most protective stop level.
		decimal? desiredStop = null;

		if (StopLoss1 > 0)
		{
			var trigger = entryPrice + GetPriceOffset(StopLoss1);
			if (high >= trigger)
			desiredStop = entryPrice;
		}

		if (StopLoss2 > 0)
		{
			var trigger = entryPrice + GetPriceOffset(StopLoss2);
			if (high >= trigger)
			{
				var offset = Math.Max(StopLoss2 - StopLoss1, 0);
				var candidate = entryPrice + GetPriceOffset(offset);
				if (desiredStop == null || candidate > desiredStop.Value)
				desiredStop = candidate;
			}
		}

		if (desiredStop != null)
		{
			if (_longStopPrice == null || desiredStop.Value > _longStopPrice.Value)
			_longStopPrice = desiredStop;
		}

		if (_longStopPrice != null)
		{
			if (low <= _longStopPrice.Value)
			{
				SellMarket(Position);
				return;
			}
		}

		// Scale out using the three profit targets.
		if (TakeProfit1 > 0 && _phase == 0)
		{
			var target = entryPrice + GetPriceOffset(TakeProfit1);
			if (high >= target)
			{
				CloseLongFraction(Ratio1);
				_phase = 1;
			}
		}

		if (TakeProfit2 > 0 && _phase == 1)
		{
			var target = entryPrice + GetPriceOffset(TakeProfit2);
			if (high >= target)
			{
				CloseLongFraction(Ratio2);
				_phase = 2;
			}
		}

		if (TakeProfit3 > 0 && _phase == 2)
		{
			var target = entryPrice + GetPriceOffset(TakeProfit3);
			if (high >= target)
			{
				SellMarket(Position);
				_phase = 3;
			}
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (Position >= 0m)
		return;

		var entryPrice = _shortEntryPrice ?? candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var shortVolume = Math.Abs(Position);

		// Activate the break-even exit after a predefined drawdown.
		if (!_shortBreakEvenActive && BreakEvenTrigger > 0)
		{
			var triggerPrice = entryPrice + GetPriceOffset(BreakEvenTrigger);
			if (high >= triggerPrice)
			_shortBreakEvenActive = true;
		}

		// Close at the entry price when the market returns after the drawdown.
		if (_shortBreakEvenActive && low <= entryPrice)
		{
			BuyMarket(shortVolume);
			return;
		}

		// Emergency stop closes the position immediately.
		if (StopLoss0 > 0)
		{
			var emergency = entryPrice + GetPriceOffset(StopLoss0);
			if (high >= emergency)
			{
				BuyMarket(shortVolume);
				return;
			}
		}

		// Determine the protective stop level for the short side.
		decimal? desiredStop = null;

		if (StopLoss1 > 0)
		{
			var trigger = entryPrice - GetPriceOffset(StopLoss1);
			if (low <= trigger)
			desiredStop = entryPrice;
		}

		if (StopLoss2 > 0)
		{
			var trigger = entryPrice - GetPriceOffset(StopLoss2);
			if (low <= trigger)
			{
				var offset = Math.Max(StopLoss2 - StopLoss1, 0);
				var candidate = entryPrice - GetPriceOffset(offset);
				if (desiredStop == null || candidate < desiredStop.Value)
				desiredStop = candidate;
			}
		}

		if (desiredStop != null)
		{
			if (_shortStopPrice == null || desiredStop.Value < _shortStopPrice.Value)
			_shortStopPrice = desiredStop;
		}

		if (_shortStopPrice != null)
		{
			if (high >= _shortStopPrice.Value)
			{
				BuyMarket(shortVolume);
				return;
			}
		}

		// Scale out using the three profit targets.
		if (TakeProfit1 > 0 && _phase == 0)
		{
			var target = entryPrice - GetPriceOffset(TakeProfit1);
			if (low <= target)
			{
				CloseShortFraction(Ratio1);
				_phase = 1;
			}
		}

		if (TakeProfit2 > 0 && _phase == 1)
		{
			var target = entryPrice - GetPriceOffset(TakeProfit2);
			if (low <= target)
			{
				CloseShortFraction(Ratio2);
				_phase = 2;
			}
		}

		if (TakeProfit3 > 0 && _phase == 2)
		{
			var target = entryPrice - GetPriceOffset(TakeProfit3);
			if (low <= target)
			{
				BuyMarket(shortVolume);
				_phase = 3;
			}
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		if (MaxPositions <= 0)
		return;

		var volume = CalculateEntryVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		BuyMarket(volume);

		_longEntryPrice = candle.ClosePrice;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longBreakEvenActive = false;
		_shortBreakEvenActive = false;
		_phase = 0;
	}

	private void EnterShort(ICandleMessage candle)
	{
		if (MaxPositions <= 0)
		return;

		var volume = CalculateEntryVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		SellMarket(volume);

		_shortEntryPrice = candle.ClosePrice;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longBreakEvenActive = false;
		_shortBreakEvenActive = false;
		_phase = 0;
	}

	private decimal CalculateEntryVolume(decimal price)
	{
		var mode = (MoneyManagementModes)MoneyManagementType;
		var volume = BaseVolume;

		if (mode == MoneyManagementModes.BalanceSquareRoot)
		{
			var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
			if (balance != null && balance.Value > 0m)
			{
				var sqrt = (decimal)Math.Sqrt((double)(balance.Value / 1000m));
				volume = 0.1m * sqrt * MoneyManagementFactor;
			}
		}
		else if (mode == MoneyManagementModes.EquityRisk)
		{
			var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
			if (equity != null && equity.Value > 0m && price > 0m)
			{
				volume = equity.Value / price / 1000m * MoneyManagementRiskPercent / 100m;
			}
		}

		var normalized = NormalizeVolume(volume, true);
		return normalized;
	}

	private decimal NormalizeVolume(decimal volume, bool applyMinLimit)
	{
		var step = Security?.VolumeStep;
		if (step != null && step.Value > 0m)
		{
			var rounded = Math.Round(volume / step.Value, MidpointRounding.AwayFromZero) * step.Value;
			if (rounded <= 0m && volume > 0m)
			rounded = step.Value;
			volume = rounded;
		}

		if (applyMinLimit)
		{
			var min = Security?.MinVolume;
			if (min != null && min.Value > 0m && volume < min.Value)
			volume = min.Value;
		}

		var max = Security?.MaxVolume;
		if (max != null && max.Value > 0m && volume > max.Value)
		volume = max.Value;

		return volume;
	}

	private void CloseLongFraction(int ratio)
	{
		if (ratio <= 0)
		return;

		var positionVolume = Position;
		if (positionVolume <= 0m)
		return;

		var rawVolume = positionVolume * ratio / 100m;
		var normalized = NormalizeVolume(rawVolume, false);

		if (normalized <= 0m || normalized > positionVolume)
		normalized = rawVolume;

		if (normalized <= 0m)
		return;

		SellMarket(normalized);
	}

	private void CloseShortFraction(int ratio)
	{
		if (ratio <= 0)
		return;

		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
		return;

		var rawVolume = positionVolume * ratio / 100m;
		var normalized = NormalizeVolume(rawVolume, false);

		if (normalized <= 0m || normalized > positionVolume)
		normalized = rawVolume;

		if (normalized <= 0m)
		return;

		BuyMarket(normalized);
	}

	private decimal GetPriceOffset(int points)
	{
		if (points <= 0)
		return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 0.0001m;

		return points * step * PointMultiplier;
	}

	private void ResetPositionState()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longBreakEvenActive = false;
		_shortBreakEvenActive = false;
		_phase = 0;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = ClampHour(StartHour);
		var end = ClampHour(EndHour);
		var hour = time.Hour;

		if (start == end)
		return true;

		if (start < end)
		return hour >= start && hour < end;

		return hour >= start || hour < end;
	}

	private bool IsClosingTime(DateTimeOffset time)
	{
		return time.Hour >= ClampHour(ClosingHour);
	}

	private void ExitAtClose()
	{
		if (Position > 0m)
		SellMarket(Position);
		else if (Position < 0m)
		BuyMarket(Math.Abs(Position));
	}

	private bool HasDailyContext()
	{
		return _currentDay.HasValue && _dailyOpen > 0m && _dailyHigh > 0m && _dailyLow > 0m;
	}

	private int GetTrendDirection()
	{
		var upMove = _dailyHigh - _dailyOpen;
		var downMove = _dailyOpen - _dailyLow;

		if (upMove > downMove)
		return 1;

		if (downMove > upMove)
		return -1;

		return 0;
	}

	private static int ClampHour(int hour)
	{
		if (hour < 0)
		return 0;
		if (hour > 23)
		return 23;
		return hour;
	}
}
