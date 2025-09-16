using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale strategy that increases position size after a loss and manages risk using money targets and trailing stops.
/// </summary>
public class MartingaleBoneCrusherStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTakeProfitMoney;
	private readonly StrategyParam<decimal> _takeProfitMoney;
	private readonly StrategyParam<bool> _useTakeProfitPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingTakeProfitMoney;
	private readonly StrategyParam<decimal> _trailingStopMoney;
	private readonly StrategyParam<MartingaleMode> _martingaleMode;
	private readonly StrategyParam<bool> _useMoveToBreakeven;
	private readonly StrategyParam<decimal> _moveToBreakevenTrigger;
	private readonly StrategyParam<decimal> _breakevenOffset;
	private readonly StrategyParam<decimal> _multiply;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<bool> _doubleLotSize;
	private readonly StrategyParam<decimal> _lotSizeIncrement;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _fastMa;
	private SMA _slowMa;
	private decimal _averagePrice;
	private decimal _positionVolume;
	private decimal _currentVolume;
	private decimal _lastOrderVolume;
	private decimal _lastTradeResult;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal? _breakevenPrice;
	private decimal _maxFloatingProfit;
	private decimal _initialCapital;
	private Sides? _lastPositionSide;
	private Sides? _lastLosingSide;

	/// <summary>
	/// Initializes a new instance of <see cref="MartingaleBoneCrusherStrategy"/>.
	/// </summary>
	public MartingaleBoneCrusherStrategy()
	{
		_useTakeProfitMoney = Param(nameof(UseTakeProfitMoney), false)
			.SetDisplay("Use Money TP", "Enable fixed money take profit", "Risk Management");

		_takeProfitMoney = Param(nameof(TakeProfitMoney), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Money TP", "Take profit in money", "Risk Management");

		_useTakeProfitPercent = Param(nameof(UseTakeProfitPercent), false)
			.SetDisplay("Use Percent TP", "Enable percentage take profit", "Risk Management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Percent TP", "Take profit percentage", "Risk Management");

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Trailing Enabled", "Use money trailing stop", "Risk Management");

		_trailingTakeProfitMoney = Param(nameof(TrailingTakeProfitMoney), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Start", "Profit to activate trailing", "Risk Management");

		_trailingStopMoney = Param(nameof(TrailingStopMoney), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Allowed profit pullback", "Risk Management");

		_martingaleMode = Param(nameof(MartingaleMode), MartingaleMode.Martingale2)
			.SetDisplay("Mode", "Martingale logic variant", "General");

		_useMoveToBreakeven = Param(nameof(UseMoveToBreakeven), true)
			.SetDisplay("Use Breakeven", "Enable breakeven stop", "Risk Management");

		_moveToBreakevenTrigger = Param(nameof(MoveToBreakevenTrigger), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Breakeven Trigger", "Steps to move stop", "Risk Management");

		_breakevenOffset = Param(nameof(BreakevenOffset), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Breakeven Offset", "Offset from entry", "Risk Management");

		_multiply = Param(nameof(Multiply), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiply", "Lot multiplier after loss", "Position Sizing");

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Base order volume", "Position Sizing");

		_doubleLotSize = Param(nameof(DoubleLotSize), false)
			.SetDisplay("Double Volume", "Multiply volume after loss", "Position Sizing");

		_lotSizeIncrement = Param(nameof(LotSizeIncrement), 0.01m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Lot Increment", "Volume increment after loss", "Position Sizing");

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Steps", "Trailing distance in steps", "Price Targets");

		_stopLossSteps = Param(nameof(StopLossSteps), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Steps", "Stop-loss distance in steps", "Price Targets");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Steps", "Take-profit distance in steps", "Price Targets");

		_fastPeriod = Param(nameof(FastPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average length", "Signals");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average length", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
	}

	/// <summary>
	/// Enable fixed take profit in money.
	/// </summary>
	public bool UseTakeProfitMoney
	{
		get => _useTakeProfitMoney.Value;
		set => _useTakeProfitMoney.Value = value;
	}

	/// <summary>
	/// Take profit amount in money.
	/// </summary>
	public decimal TakeProfitMoney
	{
		get => _takeProfitMoney.Value;
		set => _takeProfitMoney.Value = value;
	}

	/// <summary>
	/// Enable take profit measured in percent.
	/// </summary>
	public bool UseTakeProfitPercent
	{
		get => _useTakeProfitPercent.Value;
		set => _useTakeProfitPercent.Value = value;
	}

	/// <summary>
	/// Percentage profit target.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Enable trailing stop in money.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit required to activate money trailing.
	/// </summary>
	public decimal TrailingTakeProfitMoney
	{
		get => _trailingTakeProfitMoney.Value;
		set => _trailingTakeProfitMoney.Value = value;
	}

	/// <summary>
	/// Allowed profit pullback while trailing.
	/// </summary>
	public decimal TrailingStopMoney
	{
		get => _trailingStopMoney.Value;
		set => _trailingStopMoney.Value = value;
	}

	/// <summary>
	/// Selected martingale mode.
	/// </summary>
	public MartingaleMode MartingaleMode
	{
		get => _martingaleMode.Value;
		set => _martingaleMode.Value = value;
	}

	/// <summary>
	/// Enable automatic move to breakeven.
	/// </summary>
	public bool UseMoveToBreakeven
	{
		get => _useMoveToBreakeven.Value;
		set => _useMoveToBreakeven.Value = value;
	}

	/// <summary>
	/// Distance in steps required to activate breakeven.
	/// </summary>
	public decimal MoveToBreakevenTrigger
	{
		get => _moveToBreakevenTrigger.Value;
		set => _moveToBreakevenTrigger.Value = value;
	}

	/// <summary>
	/// Offset added to entry price when moving stop to breakeven.
	/// </summary>
	public decimal BreakevenOffset
	{
		get => _breakevenOffset.Value;
		set => _breakevenOffset.Value = value;
	}

	/// <summary>
	/// Multiplier applied to volume after a loss.
	/// </summary>
	public decimal Multiply
	{
		get => _multiply.Value;
		set => _multiply.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Use multiplication instead of addition for martingale.
	/// </summary>
	public bool DoubleLotSize
	{
		get => _doubleLotSize.Value;
		set => _doubleLotSize.Value = value;
	}

	/// <summary>
	/// Additional volume added after a loss when doubling is disabled.
	/// </summary>
	public decimal LotSizeIncrement
	{
		get => _lotSizeIncrement.Value;
		set => _lotSizeIncrement.Value = value;
	}

	/// <summary>
	/// Trailing distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
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

		_averagePrice = 0m;
		_positionVolume = 0m;
		_currentVolume = AlignVolume(InitialVolume);
		_lastOrderVolume = _currentVolume;
		_lastTradeResult = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_breakevenPrice = null;
		_maxFloatingProfit = 0m;
		_lastPositionSide = null;
		_lastLosingSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = AlignVolume(InitialVolume);
		_currentVolume = Volume;
		_lastOrderVolume = Volume;

		_initialCapital = Portfolio?.BeginValue ?? Portfolio?.CurrentValue ?? 0m;

		_fastMa = new SMA { Length = FastPeriod };
		_slowMa = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		if (Position != 0)
		{
			UpdateExtremes(candle);

			if (TryApplyStopAndTake(candle))
				return;

			if (TryApplyBreakeven(candle.ClosePrice))
				return;

			if (TryApplyMoneyTargets(candle.ClosePrice))
				return;

			TryActivateBreakeven(candle.ClosePrice);
			return;
		}

		var entrySide = DetermineEntrySide(fastValue, slowValue);
		if (entrySide is null)
			return;

		var volume = AlignVolume(_currentVolume);
		if (volume <= 0m)
			return;

		if (entrySide == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_averagePrice = candle.ClosePrice;
		_positionVolume = volume;
		_lastOrderVolume = volume;
		_lastPositionSide = entrySide;
		_highestPrice = candle.ClosePrice;
		_lowestPrice = candle.ClosePrice;
		_breakevenPrice = null;
		_maxFloatingProfit = 0m;
	}

	private Sides? DetermineEntrySide(decimal fastValue, decimal slowValue)
	{
		Sides? signal = null;
		if (fastValue < slowValue)
		signal = Sides.Buy;
		else if (fastValue > slowValue)
		signal = Sides.Sell;

		if (_lastTradeResult < 0m)
		{
		if (MartingaleMode == MartingaleMode.Martingale2 && _lastLosingSide.HasValue)
		return _lastLosingSide == Sides.Buy ? Sides.Sell : Sides.Buy;

		return signal;
		}

		return signal;
	}

	private bool TryApplyStopAndTake(ICandleMessage candle)
	{
		if (_positionVolume <= 0m || !_lastPositionSide.HasValue)
			return false;

		var stopDistance = StepsToPrice(StopLossSteps);
		var takeDistance = StepsToPrice(TakeProfitSteps);
		var trailingDistance = StepsToPrice(TrailingStopSteps);
		var closePrice = candle.ClosePrice;

		if (_lastPositionSide == Sides.Buy)
		{
			if (stopDistance > 0m && candle.LowPrice <= _averagePrice - stopDistance)
			{
				ClosePosition(_averagePrice - stopDistance);
				return true;
			}

			if (takeDistance > 0m && candle.HighPrice >= _averagePrice + takeDistance)
			{
				ClosePosition(_averagePrice + takeDistance);
				return true;
			}

			if (TrailingStopSteps > 0m && trailingDistance > 0m && closePrice <= _highestPrice - trailingDistance)
			{
				ClosePosition(closePrice);
				return true;
			}
		}
		else
		{
			if (stopDistance > 0m && candle.HighPrice >= _averagePrice + stopDistance)
			{
				ClosePosition(_averagePrice + stopDistance);
				return true;
			}

			if (takeDistance > 0m && candle.LowPrice <= _averagePrice - takeDistance)
			{
				ClosePosition(_averagePrice - takeDistance);
				return true;
			}

			if (TrailingStopSteps > 0m && trailingDistance > 0m && closePrice >= _lowestPrice + trailingDistance)
			{
				ClosePosition(closePrice);
				return true;
			}
		}

		return false;
	}

	private bool TryApplyBreakeven(decimal closePrice)
	{
		if (!UseMoveToBreakeven || !_breakevenPrice.HasValue || !_lastPositionSide.HasValue)
			return false;

		if (_lastPositionSide == Sides.Buy && closePrice <= _breakevenPrice.Value)
		{
			ClosePosition(closePrice);
			return true;
		}

		if (_lastPositionSide == Sides.Sell && closePrice >= _breakevenPrice.Value)
		{
			ClosePosition(closePrice);
			return true;
		}

		return false;
	}

	private bool TryApplyMoneyTargets(decimal closePrice)
	{
		var profit = GetFloatingProfit(closePrice);

		if (UseTakeProfitMoney && profit >= TakeProfitMoney)
		{
			ClosePosition(closePrice);
			return true;
		}

		if (UseTakeProfitPercent && _initialCapital > 0m)
		{
			var target = _initialCapital * TakeProfitPercent / 100m;
			if (profit >= target)
			{
				ClosePosition(closePrice);
				return true;
			}
		}

		if (EnableTrailing && profit > 0m)
		{
			if (profit >= TrailingTakeProfitMoney)
				_maxFloatingProfit = Math.Max(_maxFloatingProfit, profit);

			if (_maxFloatingProfit > 0m && _maxFloatingProfit - profit >= TrailingStopMoney)
			{
				ClosePosition(closePrice);
				return true;
			}
		}

		return false;
	}

	private void TryActivateBreakeven(decimal closePrice)
	{
		if (!UseMoveToBreakeven || _breakevenPrice.HasValue || !_lastPositionSide.HasValue)
			return;

		var trigger = StepsToPrice(MoveToBreakevenTrigger);
		if (trigger <= 0m)
			return;

		var offset = StepsToPrice(BreakevenOffset);
		if (_lastPositionSide == Sides.Buy)
		{
			if (closePrice >= _averagePrice + trigger)
			_breakevenPrice = _averagePrice + offset;
		}
		else if (closePrice <= _averagePrice - trigger)
		{
			_breakevenPrice = _averagePrice - offset;
		}
	}

	private decimal GetFloatingProfit(decimal currentPrice)
	{
		if (_positionVolume <= 0m || !_lastPositionSide.HasValue)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
			return 0m;

		var direction = _lastPositionSide == Sides.Buy ? 1m : -1m;
		var priceDiff = (currentPrice - _averagePrice) * direction;
		var steps = priceDiff / priceStep;
		return steps * stepPrice * _positionVolume;
	}

	private void ClosePosition(decimal exitPrice)
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(-Position);

		ComputeTradeResult(exitPrice);
		ResetPositionState();
		UpdateNextVolume();
	}

	private void ComputeTradeResult(decimal exitPrice)
	{
		if (_positionVolume <= 0m || !_lastPositionSide.HasValue)
		{
			_lastTradeResult = 0m;
			_lastLosingSide = null;
			return;
		}

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		{
			_lastTradeResult = 0m;
			_lastLosingSide = null;
			return;
		}

		var direction = _lastPositionSide == Sides.Buy ? 1m : -1m;
		var priceDiff = (exitPrice - _averagePrice) * direction;
		var steps = priceDiff / priceStep;
		var pnl = steps * stepPrice * _positionVolume;

		_lastTradeResult = pnl;
		_lastLosingSide = pnl < 0m ? _lastPositionSide : null;
	}

	private void ResetPositionState()
	{
		_averagePrice = 0m;
		_positionVolume = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_breakevenPrice = null;
		_maxFloatingProfit = 0m;
		_lastPositionSide = null;
	}

	private void UpdateExtremes(ICandleMessage candle)
	{
		if (!_lastPositionSide.HasValue)
			return;

		if (_lastPositionSide == Sides.Buy)
		{
			if (candle.HighPrice > _highestPrice)
			_highestPrice = candle.HighPrice;
		}
		else
		{
			if (_lowestPrice == 0m || candle.LowPrice < _lowestPrice)
			_lowestPrice = candle.LowPrice;
		}
	}

	private void UpdateNextVolume()
	{
		decimal nextVolume;
		if (_lastTradeResult < 0m)
		nextVolume = DoubleLotSize ? _lastOrderVolume * Multiply : _lastOrderVolume + LotSizeIncrement;
		else
		nextVolume = InitialVolume;

		_currentVolume = AlignVolume(nextVolume);
		_lastOrderVolume = _currentVolume;
	}

	private decimal StepsToPrice(decimal steps)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 0m;

		return steps * priceStep;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (Security is null)
		return volume;

		var step = Security.VolumeStep ?? 0m;
		var min = Security.VolumeMin ?? 0m;
		var max = Security.VolumeMax ?? decimal.MaxValue;

		if (step > 0m)
		{
		var ratio = Math.Round(volume / step, MidpointRounding.AwayFromZero);
		if (ratio == 0m && volume > 0m)
		ratio = 1m;
		volume = ratio * step;
		}

		if (min > 0m && volume < min)
		volume = min;

		if (volume > max)
		volume = max;

		return volume;
	}

	/// <summary>
	/// Supported martingale variants.
	/// </summary>
	public enum MartingaleMode
	{
		/// <summary>
		/// Follow moving average direction after every loss.
		/// </summary>
		Martingale1,

		/// <summary>
		/// Reverse direction after a loss while using martingale sizing.
		/// </summary>
		Martingale2
	}
}
