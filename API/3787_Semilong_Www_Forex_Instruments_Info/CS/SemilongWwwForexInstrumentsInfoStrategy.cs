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
/// Reimplementation of the Semilong strategy that compares the current price with two historical closes.
/// Opens a position when the price sharply deviates from older levels and manages the trade with configurable stops,
/// take profit, trailing logic, and loss streak based position sizing.
/// </summary>
public class SemilongWwwForexInstrumentsInfoStrategy : Strategy
{
	private readonly StrategyParam<int> _profitPoints;
	private readonly StrategyParam<int> _lossPoints;
	private readonly StrategyParam<int> _shiftOne;
	private readonly StrategyParam<int> _moveOnePoints;
	private readonly StrategyParam<int> _shiftTwo;
	private readonly StrategyParam<int> _moveTwoPoints;
	private readonly StrategyParam<int> _decreaseFactor;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<int> _trailingPoints;
	private readonly StrategyParam<bool> _useAutoLot;
	private readonly StrategyParam<int> _autoMarginDivider;
	private readonly StrategyParam<DataType> _candleType;

	private Shift _shiftCloseOne = null!;
	private Shift _shiftCloseTwo = null!;
	private decimal _pipSize;
	private int _positionDirection;
	private decimal _entryPrice;
	private decimal _bestPrice;
	private int _lossStreak;

	/// <summary>
	/// Initializes a new instance of the <see cref="SemilongWwwForexInstrumentsInfoStrategy"/> class.
	/// </summary>
	public SemilongWwwForexInstrumentsInfoStrategy()
	{
		_profitPoints = Param(nameof(ProfitPoints), 120)
		.SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk");

		_lossPoints = Param(nameof(LossPoints), 60)
		.SetDisplay("Stop Loss (points)", "Distance in points for the protective stop", "Risk");

		_shiftOne = Param(nameof(ShiftOne), 100)
		.SetNotNegative()
		.SetDisplay("Primary Shift", "Number of bars between the current close and the comparison close", "Signals");

		_moveOnePoints = Param(nameof(MoveOnePoints), 60)
		.SetNotNegative()
		.SetDisplay("Primary Move (points)", "Minimum deviation in points from the primary shifted close", "Signals");

		_shiftTwo = Param(nameof(ShiftTwo), 10)
		.SetNotNegative()
		.SetDisplay("Secondary Shift", "Additional bars added on top of the primary shift", "Signals");

		_moveTwoPoints = Param(nameof(MoveTwoPoints), 30)
		.SetNotNegative()
		.SetDisplay("Secondary Move (points)", "Minimum distance between the two shifted closes", "Signals");

		_decreaseFactor = Param(nameof(DecreaseFactor), 14)
		.SetNotNegative()
		.SetDisplay("Decrease Factor", "Divisor applied when shrinking the auto lot after losses", "Money Management");

		_fixedVolume = Param(nameof(FixedVolume), 1m)
		.SetDisplay("Fixed Volume", "Base volume used when auto lot is disabled", "Money Management");

		_trailingPoints = Param(nameof(TrailingPoints), 0)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (points)", "Trailing stop distance in points", "Risk");

		_useAutoLot = Param(nameof(UseAutoLot), true)
		.SetDisplay("Use Auto Lot", "Enable dynamic position sizing based on free margin", "Money Management");

		_autoMarginDivider = Param(nameof(AutoMarginDivider), 7)
		.SetRange(1, int.MaxValue)
		.SetDisplay("Auto Margin Divider", "Divisor used to convert free margin into the lot size", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for signal calculations", "General");
	}

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public int ProfitPoints
	{
		get => _profitPoints.Value;
		set => _profitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points.
	/// </summary>
	public int LossPoints
	{
		get => _lossPoints.Value;
		set => _lossPoints.Value = value;
	}

	/// <summary>
	/// Number of bars between the current candle and the primary comparison candle.
	/// </summary>
	public int ShiftOne
	{
		get => _shiftOne.Value;
		set => _shiftOne.Value = value;
	}

	/// <summary>
	/// Minimum deviation from the primary shifted close required before a trade is allowed.
	/// </summary>
	public int MoveOnePoints
	{
		get => _moveOnePoints.Value;
		set => _moveOnePoints.Value = value;
	}

	/// <summary>
	/// Additional bars added on top of the the primary shift for the secondary comparison.
	/// </summary>
	public int ShiftTwo
	{
		get => _shiftTwo.Value;
		set => _shiftTwo.Value = value;
	}

	/// <summary>
	/// Minimum distance in points between the two shifted closes.
	/// </summary>
	public int MoveTwoPoints
	{
		get => _moveTwoPoints.Value;
		set => _moveTwoPoints.Value = value;
	}

	/// <summary>
	/// Divisor used to reduce the calculated auto lot size after consecutive losses.
	/// </summary>
	public int DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Fixed trade volume used whenever auto lot sizing is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public int TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the strategy calculates the lot size from free margin.
	/// </summary>
	public bool UseAutoLot
	{
		get => _useAutoLot.Value;
		set => _useAutoLot.Value = value;
	}

	/// <summary>
	/// Divider applied to free margin when auto lot sizing is enabled.
	/// </summary>
	public int AutoMarginDivider
	{
		get => _autoMarginDivider.Value;
		set => _autoMarginDivider.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
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

		_positionDirection = 0;
		_entryPrice = 0m;
		_bestPrice = 0m;
		_lossStreak = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shiftCloseOne = new Shift { Length = Math.Max(0, ShiftOne) };
		_shiftCloseTwo = new Shift { Length = Math.Max(0, ShiftOne + ShiftTwo) };

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
			_pipSize = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var shiftedOneValue = _shiftCloseOne.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
		var shiftedTwoValue = _shiftCloseTwo.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

		if (!_shiftCloseOne.IsFormed || !_shiftCloseTwo.IsFormed)
			return;

		var bidPrice = Security?.BestBid?.Price ?? candle.ClosePrice;
		var askPrice = Security?.BestAsk?.Price ?? candle.ClosePrice;
		var spreadPoints = 0m;

		if (_pipSize > 0m)
		{
			var spread = askPrice - bidPrice;
			if (spread > 0m)
				spreadPoints = spread / _pipSize;
		}

		ManageOpenPosition(candle.ClosePrice, spreadPoints);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return;

		if (HasActiveOrders())
			return;

		var moveOne = MoveOnePoints * _pipSize;
		var moveTwo = MoveTwoPoints * _pipSize;

		var bidDelta = bidPrice - shiftedOneValue;
		var closeDelta = shiftedOneValue - shiftedTwoValue;

		var buySignal = bidDelta < -moveOne && closeDelta > moveTwo;
		var sellSignal = bidDelta > moveOne && closeDelta < -moveTwo;

		if (!buySignal && !sellSignal)
			return;

		var volume = CalculateTradeVolume();
		if (volume <= 0m)
			return;

		if (GetFreeMargin() < volume * 2000m)
			return;

		if (buySignal)
		{
			BuyMarket(volume);
			_positionDirection = 1;
			_entryPrice = askPrice;
			_bestPrice = askPrice;
		}
		else if (sellSignal)
		{
			SellMarket(volume);
			_positionDirection = -1;
			_entryPrice = bidPrice;
			_bestPrice = bidPrice;
		}
	}

	private void ManageOpenPosition(decimal currentPrice, decimal spreadPoints)
	{
		if (Position == 0m || _positionDirection == 0)
			return;

		var lossDistance = LossPoints > 0 ? LossPoints * _pipSize : 0m;
		var takeDistance = ProfitPoints > 0 ? (ProfitPoints + spreadPoints) * _pipSize : 0m;
		var trailingDistance = TrailingPoints > 0 ? TrailingPoints * _pipSize : 0m;

		if (_positionDirection > 0)
		{
			if (currentPrice > _bestPrice)
				_bestPrice = currentPrice;

			if (lossDistance > 0m && _entryPrice - currentPrice >= lossDistance)
			{
				ClosePosition(currentPrice);
				return;
			}

			if (takeDistance > 0m && currentPrice - _entryPrice >= takeDistance)
			{
				ClosePosition(currentPrice);
				return;
			}

			if (trailingDistance > 0m)
			{
				var trailingStop = _bestPrice - trailingDistance;
				if (currentPrice <= trailingStop)
				{
					ClosePosition(currentPrice);
					return;
				}
			}
		}
		else if (_positionDirection < 0)
		{
			if (_bestPrice == 0m || currentPrice < _bestPrice)
				_bestPrice = currentPrice;

			if (lossDistance > 0m && currentPrice - _entryPrice >= lossDistance)
			{
				ClosePosition(currentPrice);
				return;
			}

			if (takeDistance > 0m && _entryPrice - currentPrice >= takeDistance)
			{
				ClosePosition(currentPrice);
				return;
			}

			if (trailingDistance > 0m)
			{
				var trailingStop = _bestPrice + trailingDistance;
				if (currentPrice >= trailingStop)
				{
					ClosePosition(currentPrice);
					return;
				}
			}
		}
	}

	private void ClosePosition(decimal currentPrice)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		{
			ResetTradeTracking();
			return;
		}

		if (Position > 0m)
			SellMarket(volume);
		else
			BuyMarket(volume);

		if (_positionDirection != 0 && _entryPrice > 0m)
		{
			var profit = _positionDirection > 0 ? currentPrice - _entryPrice : _entryPrice - currentPrice;
			if (profit < 0m)
				_lossStreak++;
			else if (profit > 0m)
				_lossStreak = 0;
		}

		ResetTradeTracking();
	}

	private void ResetTradeTracking()
	{
		_positionDirection = 0;
		_entryPrice = 0m;
		_bestPrice = 0m;
	}

	private decimal CalculateTradeVolume()
	{
		var volume = FixedVolume;

		if (UseAutoLot)
		{
			var freeMargin = GetFreeMargin();
			var divider = Math.Max(1, AutoMarginDivider) * 1000m;
			if (divider > 0m)
			{
				var raw = freeMargin / divider;
				volume = Math.Round(raw, 0, MidpointRounding.AwayFromZero);
			}

			if (DecreaseFactor > 0 && _lossStreak > 1)
			{
				var reduction = volume * _lossStreak / DecreaseFactor;
				var adjusted = volume - Math.Round(reduction, 0, MidpointRounding.AwayFromZero);
				if (adjusted > 0m)
					volume = adjusted;
			}

			if (volume < FixedVolume)
				volume = FixedVolume;

			if (volume > 99m)
				volume = 99m;
		}

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security is null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			if (steps <= 0m)
				steps = 1m;
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = security.MaxVolume;
		if (maxVolume.HasValue && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal GetFreeMargin()
	{
		var portfolio = Portfolio;
		if (portfolio is null)
			return 0m;

		var current = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		var blocked = portfolio.BlockedValue ?? 0m;
		var free = current - blocked;
		if (free <= 0m)
			free = current;
		return Math.Max(free, 0m);
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order.State.IsActive())
				return true;
		}

		return false;
	}
}

