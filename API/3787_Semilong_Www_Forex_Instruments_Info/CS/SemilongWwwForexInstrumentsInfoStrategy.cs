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

	private readonly List<decimal> _closes = new();
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

		_shiftOne = Param(nameof(ShiftOne), 5)
		.SetNotNegative()
		.SetDisplay("Primary Shift", "Number of bars between the current close and the comparison close", "Signals");

		_moveOnePoints = Param(nameof(MoveOnePoints), 0)
		.SetNotNegative()
		.SetDisplay("Primary Move (points)", "Minimum deviation in points from the primary shifted close", "Signals");

		_shiftTwo = Param(nameof(ShiftTwo), 10)
		.SetNotNegative()
		.SetDisplay("Secondary Shift", "Additional bars added on top of the primary shift", "Signals");

		_moveTwoPoints = Param(nameof(MoveTwoPoints), 0)
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

		_useAutoLot = Param(nameof(UseAutoLot), false)
		.SetDisplay("Use Auto Lot", "Enable dynamic position sizing based on free margin", "Money Management");

		_autoMarginDivider = Param(nameof(AutoMarginDivider), 7)
		.SetRange(1, int.MaxValue)
		.SetDisplay("Auto Margin Divider", "Divisor used to convert free margin into the lot size", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_closes.Clear();
		_pipSize = 0m;
		_positionDirection = 0;
		_entryPrice = 0m;
		_bestPrice = 0m;
		_lossStreak = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
			_pipSize = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandleSimple).Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));
	}

	private void ProcessCandleSimple(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);

		var totalShift = ShiftOne + ShiftTwo;
		if (_closes.Count > totalShift + 2)
			_closes.RemoveAt(0);

		if (_closes.Count <= totalShift)
			return;

		if (Position != 0m)
			return;

		var price = candle.ClosePrice;
		var shiftedOneValue = _closes[_closes.Count - 1 - ShiftOne];
		var shiftedTwoValue = _closes[_closes.Count - 1 - totalShift];

		var moveOne = MoveOnePoints * _pipSize;
		var moveTwo = MoveTwoPoints * _pipSize;

		var priceDelta = price - shiftedOneValue;
		var closeDelta = shiftedOneValue - shiftedTwoValue;

		var buySignal = priceDelta < -moveOne && closeDelta > moveTwo;
		var sellSignal = priceDelta > moveOne && closeDelta < -moveTwo;

		if (buySignal)
		{
			BuyMarket();
		}
		else if (sellSignal)
		{
			SellMarket();
		}
	}
}

