using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands based strategy with optional RSI and Stochastic filters.
/// Replicates the Bollinger Bands RSI expert advisor logic with configurable entry and exit zones.
/// </summary>
public class BollingerBandsRsiStrategy : Strategy
{
	private readonly StrategyParam<BollingerBandsRsiEntryMode> _entryMode;
	private readonly StrategyParam<BollingerBandsRsiClosureMode> _closureMode;
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<bool> _useStochasticFilter;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<decimal> _stochasticLowerLevel;
	private readonly StrategyParam<int> _barShift;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _pipValue;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _teeth = null!;
	private BollingerBands _jaws = null!;
	private BollingerBands _lips = null!;
	private RelativeStrengthIndex _rsi = null!;
	private StochasticOscillator _stochastic = null!;

	private readonly Queue<decimal> _teethMiddleHistory = new();
	private readonly Queue<decimal> _teethUpperHistory = new();
	private readonly Queue<decimal> _teethLowerHistory = new();
	private readonly Queue<decimal> _jawsUpperHistory = new();
	private readonly Queue<decimal> _jawsLowerHistory = new();
	private readonly Queue<decimal> _lipsUpperHistory = new();
	private readonly Queue<decimal> _lipsLowerHistory = new();
	private readonly Queue<decimal> _rsiHistory = new();
	private readonly Queue<decimal> _stochasticHistory = new();

	private bool _longLocked;
	private bool _shortLocked;

	/// <summary>
	/// Entry zone selection.
	/// </summary>
	public BollingerBandsRsiEntryMode EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Exit zone selection.
	/// </summary>
	public BollingerBandsRsiClosureMode ClosureMode
	{
		get => _closureMode.Value;
		set => _closureMode.Value = value;
	}

	/// <summary>
	/// Bollinger period for all bands.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for the primary (yellow) band.
	/// </summary>
	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
	}

	/// <summary>
	/// Enable RSI filter.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}

	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI short threshold (long threshold is mirrored from 100).
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// Enable Stochastic filter.
	/// </summary>
	public bool UseStochasticFilter
	{
		get => _useStochasticFilter.Value;
		set => _useStochasticFilter.Value = value;
	}

	/// <summary>
	/// Stochastic main period.
	/// </summary>
	public int StochasticPeriod
	{
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level (long threshold is mirrored from 100).
	/// </summary>
	public decimal StochasticLowerLevel
	{
		get => _stochasticLowerLevel.Value;
		set => _stochasticLowerLevel.Value = value;
	}

	/// <summary>
	/// Number of finished bars used for indicator shift.
	/// </summary>
	public int BarShift
	{
		get => _barShift.Value;
		set => _barShift.Value = value;
	}

	/// <summary>
	/// Allow only one open position at a time.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Trading volume for new orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Value of one pip in price units.
	/// </summary>
	public decimal PipValue
	{
		get => _pipValue.Value;
		set => _pipValue.Value = value;
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
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerBandsRsiStrategy"/> class.
	/// </summary>
	public BollingerBandsRsiStrategy()
	{
		_entryMode = Param(nameof(EntryMode), BollingerBandsRsiEntryMode.BetweenYellowAndBlue)
			.SetDisplay("Entry Mode", "Bollinger zone used for entries", "Trading");

		_closureMode = Param(nameof(ClosureMode), BollingerBandsRsiClosureMode.BetweenBlueAndRed)
			.SetDisplay("Closure Mode", "Bollinger zone used for exits", "Trading");

		_bandsPeriod = Param(nameof(BandsPeriod), 140)
			.SetGreaterThanZero()
			.SetDisplay("Bands Period", "Length of all Bollinger bands", "Indicators")
			.SetCanOptimize();

		_deviation = Param(nameof(Deviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "Standard deviation for yellow band", "Indicators")
			.SetCanOptimize();

		_useRsiFilter = Param(nameof(UseRsiFilter), false)
			.SetDisplay("Use RSI Filter", "Enable RSI confirmation", "Filters");

		_rsiPeriod = Param(nameof(RsiPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of RSI filter", "Filters")
			.SetCanOptimize();

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 70m)
			.SetDisplay("RSI Lower", "Short threshold (long uses 100-threshold)", "Filters")
			.SetCanOptimize();

		_useStochasticFilter = Param(nameof(UseStochasticFilter), true)
			.SetDisplay("Use Stochastic Filter", "Enable Stochastic confirmation", "Filters");

		_stochasticPeriod = Param(nameof(StochasticPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Main %K period", "Filters")
			.SetCanOptimize();

		_stochasticLowerLevel = Param(nameof(StochasticLowerLevel), 95m)
			.SetDisplay("Stochastic Lower", "Overbought threshold (long uses mirror)", "Filters")
			.SetCanOptimize();

		_barShift = Param(nameof(BarShift), 1)
			.SetGreaterThanZero()
			.SetDisplay("Bar Shift", "Number of finished bars for signals", "Trading");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), true)
			.SetDisplay("Only One Position", "Restrict to single open position", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume sent with each market order", "Trading");

		_pipValue = Param(nameof(PipValue), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Value", "Monetary value of one pip", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 200m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 200m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for analysis", "General");
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

		_teethMiddleHistory.Clear();
		_teethUpperHistory.Clear();
		_teethLowerHistory.Clear();
		_jawsUpperHistory.Clear();
		_jawsLowerHistory.Clear();
		_lipsUpperHistory.Clear();
		_lipsLowerHistory.Clear();
		_rsiHistory.Clear();
		_stochasticHistory.Clear();
		_longLocked = false;
		_shortLocked = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_teeth = new BollingerBands
		{
			Length = BandsPeriod,
			Width = Deviation
		};

		_jaws = new BollingerBands
		{
			Length = BandsPeriod,
			Width = Deviation / 2m
		};

		_lips = new BollingerBands
		{
			Length = BandsPeriod,
			Width = Deviation * 2m
		};

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		_stochastic = new StochasticOscillator
		{
			Length = StochasticPeriod,
			K = { Length = 3 },
			D = { Length = 3 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_teeth, _jaws, _lips, _rsi, _stochastic, ProcessCandle)
			.Start();

		var take = TakeProfitPips > 0m ? new Unit(TakeProfitPips * PipValue, UnitTypes.Absolute) : null;
		var stop = StopLossPips > 0m ? new Unit(StopLossPips * PipValue, UnitTypes.Absolute) : null;

		if (take != null || stop != null)
			StartProtection(takeProfit: take, stopLoss: stop);
	}

	private void ProcessCandle(
		ICandleMessage candle,
		decimal teethMiddle,
		decimal teethUpper,
		decimal teethLower,
		decimal jawsMiddle,
		decimal jawsUpper,
		decimal jawsLower,
		decimal lipsMiddle,
		decimal lipsUpper,
		decimal lipsLower,
		decimal rsiValue, decimal stochasticK, decimal _)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bandsReady = _teeth.IsFormed && _jaws.IsFormed && _lips.IsFormed;
		if (!bandsReady)
			return;

		var rsiReady = !UseRsiFilter || _rsi.IsFormed;
		var stochasticReady = !UseStochasticFilter || _stochastic.IsFormed;

		if (!rsiReady || !stochasticReady)
		{
			UpdateHistory(teethMiddle, teethUpper, teethLower, jawsUpper, jawsLower, lipsUpper, lipsLower, rsiValue, stochasticK);
			return;
		}

		if (!TryGetShifted(_teethMiddleHistory, out var baseTeeth) ||
			!TryGetShifted(_teethUpperHistory, out var upperTeeth) ||
			!TryGetShifted(_teethLowerHistory, out var lowerTeeth) ||
			!TryGetShifted(_jawsUpperHistory, out var upperJaws) ||
			!TryGetShifted(_jawsLowerHistory, out var lowerJaws) ||
			!TryGetShifted(_lipsUpperHistory, out var upperLips) ||
			!TryGetShifted(_lipsLowerHistory, out var lowerLips))
		{
			UpdateHistory(teethMiddle, teethUpper, teethLower, jawsUpper, jawsLower, lipsUpper, lipsLower, rsiValue, stochasticK);
			return;
		}

		decimal rsiShifted = 50m;
		if (UseRsiFilter)
		{
			if (!TryGetShifted(_rsiHistory, out rsiShifted))
			{
				UpdateHistory(teethMiddle, teethUpper, teethLower, jawsUpper, jawsLower, lipsUpper, lipsLower, rsiValue, stochasticK);
				return;
			}
		}

		decimal stochasticShifted = 50m;
		if (UseStochasticFilter)
		{
			if (!TryGetShifted(_stochasticHistory, out stochasticShifted))
			{
				UpdateHistory(teethMiddle, teethUpper, teethLower, jawsUpper, jawsLower, lipsUpper, lipsLower, rsiValue, stochasticK);
				return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(teethMiddle, teethUpper, teethLower, jawsUpper, jawsLower, lipsUpper, lipsLower, rsiValue, stochasticK);
			return;
		}

		var longEntryPrice = GetLongEntryPrice(lowerTeeth, lowerJaws, lowerLips);
		var shortEntryPrice = GetShortEntryPrice(upperTeeth, upperJaws, upperLips);

		var (exitLong, exitShort) = GetExitLevels(shortEntryPrice, longEntryPrice, upperJaws, lowerJaws, upperLips, lowerLips);

		if (!OnlyOnePosition)
		{
			if (candle.ClosePrice >= baseTeeth)
				_longLocked = false;

			if (candle.ClosePrice <= baseTeeth)
				_shortLocked = false;
		}

		var priceHitLong = candle.LowPrice <= longEntryPrice;
		var priceHitShort = candle.HighPrice >= shortEntryPrice;

		var rsiLongOk = !UseRsiFilter || rsiShifted <= 100m - RsiLowerLevel;
		var rsiShortOk = !UseRsiFilter || rsiShifted >= RsiLowerLevel;

		var stochLongOk = !UseStochasticFilter || stochasticShifted < 100m - StochasticLowerLevel;
		var stochShortOk = !UseStochasticFilter || stochasticShifted > StochasticLowerLevel;

		var canOpenLong = OnlyOnePosition ? Position == 0m : Position >= 0m;
		var canOpenShort = OnlyOnePosition ? Position == 0m : Position <= 0m;

		if (priceHitShort && rsiShortOk && stochShortOk && canOpenShort)
		{
			if (OnlyOnePosition || !_shortLocked)
			{
				// Sell when price reaches the selected upper band zone and filters confirm overbought state.
				SellMarket(Volume);
				_shortLocked = !OnlyOnePosition;
			}
		}

		if (priceHitLong && rsiLongOk && stochLongOk && canOpenLong)
		{
			if (OnlyOnePosition || !_longLocked)
			{
				// Buy when price reaches the selected lower band zone and filters confirm oversold state.
				BuyMarket(Volume);
				_longLocked = !OnlyOnePosition;
			}
		}

		// Exit logic mirrors the original EA: close longs on selected upper zone, shorts on selected lower zone.
		switch (ClosureMode)
		{
			case BollingerBandsRsiClosureMode.MiddleLine:
				if (Position > 0m && candle.HighPrice >= baseTeeth)
					SellMarket(Position);

				if (Position < 0m && candle.LowPrice <= baseTeeth)
					BuyMarket(Math.Abs(Position));
				break;

			case BollingerBandsRsiClosureMode.BetweenYellowAndBlue:
			case BollingerBandsRsiClosureMode.BetweenBlueAndRed:
				if (Position > 0m && candle.HighPrice >= exitLong)
					SellMarket(Position);

				if (Position < 0m && candle.LowPrice <= exitShort)
					BuyMarket(Math.Abs(Position));
				break;

			case BollingerBandsRsiClosureMode.YellowLine:
				if (Position > 0m && candle.HighPrice >= upperTeeth)
					SellMarket(Position);

				if (Position < 0m && candle.LowPrice <= lowerTeeth)
					BuyMarket(Math.Abs(Position));
				break;

			case BollingerBandsRsiClosureMode.BlueLine:
				if (Position > 0m && candle.HighPrice >= upperJaws)
					SellMarket(Position);

				if (Position < 0m && candle.LowPrice <= lowerJaws)
					BuyMarket(Math.Abs(Position));
				break;

			case BollingerBandsRsiClosureMode.RedLine:
				if (Position > 0m && candle.HighPrice >= upperLips)
					SellMarket(Position);

				if (Position < 0m && candle.LowPrice <= lowerLips)
					BuyMarket(Math.Abs(Position));
				break;
		}

		UpdateHistory(teethMiddle, teethUpper, teethLower, jawsUpper, jawsLower, lipsUpper, lipsLower, rsiValue, stochasticK);
	}

	private decimal GetLongEntryPrice(decimal lowerTeeth, decimal lowerJaws, decimal lowerLips)
	{
		return EntryMode switch
		{
			BollingerBandsRsiEntryMode.BetweenYellowAndBlue => lowerTeeth - (lowerTeeth - lowerJaws) / 2m,
			BollingerBandsRsiEntryMode.BetweenBlueAndRed => lowerJaws - (lowerJaws - lowerLips) / 2m,
			BollingerBandsRsiEntryMode.YellowLine => lowerTeeth,
			BollingerBandsRsiEntryMode.BlueLine => lowerJaws,
			BollingerBandsRsiEntryMode.RedLine => lowerLips,
			_ => lowerTeeth
		};
	}

	private decimal GetShortEntryPrice(decimal upperTeeth, decimal upperJaws, decimal upperLips)
	{
		return EntryMode switch
		{
			BollingerBandsRsiEntryMode.BetweenYellowAndBlue => upperTeeth + (upperJaws - upperTeeth) / 2m,
			BollingerBandsRsiEntryMode.BetweenBlueAndRed => upperJaws + (upperLips - upperJaws) / 2m,
			BollingerBandsRsiEntryMode.YellowLine => upperTeeth,
			BollingerBandsRsiEntryMode.BlueLine => upperJaws,
			BollingerBandsRsiEntryMode.RedLine => upperLips,
			_ => upperTeeth
		};
	}

	private (decimal exitLong, decimal exitShort) GetExitLevels(decimal shortEntryPrice, decimal longEntryPrice, decimal upperJaws, decimal lowerJaws, decimal upperLips, decimal lowerLips)
	{
		if ((ClosureMode == BollingerBandsRsiClosureMode.BetweenYellowAndBlue && EntryMode == BollingerBandsRsiEntryMode.BetweenYellowAndBlue) ||
			(ClosureMode == BollingerBandsRsiClosureMode.BetweenBlueAndRed && EntryMode == BollingerBandsRsiEntryMode.BetweenBlueAndRed))
		{
			return (shortEntryPrice, longEntryPrice);
		}

		var defaultLong = upperJaws + (upperLips - upperJaws) / 2m;
		var defaultShort = lowerJaws - (lowerJaws - lowerLips) / 2m;
		return (defaultLong, defaultShort);
	}

	private bool TryGetShifted(Queue<decimal> history, out decimal value)
	{
		if (BarShift <= 0)
		{
			value = 0m;
			return false;
		}

		if (history.Count < BarShift)
		{
			value = 0m;
			return false;
		}

		value = history.Peek();
		return true;
	}

	private void UpdateHistory(
		decimal teethMiddle,
		decimal teethUpper,
		decimal teethLower,
		decimal jawsUpper,
		decimal jawsLower,
		decimal lipsUpper,
		decimal lipsLower,
		decimal rsiValue,
		decimal stochasticK)
	{
		if (BarShift <= 0)
			return;

		Enqueue(_teethMiddleHistory, teethMiddle);
		Enqueue(_teethUpperHistory, teethUpper);
		Enqueue(_teethLowerHistory, teethLower);
		Enqueue(_jawsUpperHistory, jawsUpper);
		Enqueue(_jawsLowerHistory, jawsLower);
		Enqueue(_lipsUpperHistory, lipsUpper);
		Enqueue(_lipsLowerHistory, lipsLower);

		if (_rsi.IsFormed)
			Enqueue(_rsiHistory, rsiValue);

		if (_stochastic.IsFormed)
			Enqueue(_stochasticHistory, stochasticK);
	}

	private void Enqueue(Queue<decimal> history, decimal value)
	{
		history.Enqueue(value);

		while (history.Count > BarShift)
			history.Dequeue();
	}
}

/// <summary>
/// Entry location for Bollinger Bands RSI strategy.
/// </summary>
public enum BollingerBandsRsiEntryMode
{
	/// <summary>
	/// Midpoint between yellow (primary) and blue (narrow) bands.
	/// </summary>
	BetweenYellowAndBlue,

	/// <summary>
	/// Midpoint between blue (narrow) and red (wide) bands.
	/// </summary>
	BetweenBlueAndRed,

	/// <summary>
	/// Yellow band itself.
	/// </summary>
	YellowLine,

	/// <summary>
	/// Blue band (narrow deviation).
	/// </summary>
	BlueLine,

	/// <summary>
	/// Red band (wide deviation).
	/// </summary>
	RedLine
}

/// <summary>
/// Exit location for Bollinger Bands RSI strategy.
/// </summary>
public enum BollingerBandsRsiClosureMode
{
	/// <summary>
	/// Exit on the middle Bollinger band.
	/// </summary>
	MiddleLine,

	/// <summary>
	/// Exit between yellow and blue bands.
	/// </summary>
	BetweenYellowAndBlue,

	/// <summary>
	/// Exit between blue and red bands.
	/// </summary>
	BetweenBlueAndRed,

	/// <summary>
	/// Exit on the yellow band.
	/// </summary>
	YellowLine,

	/// <summary>
	/// Exit on the blue band.
	/// </summary>
	BlueLine,

	/// <summary>
	/// Exit on the red band.
	/// </summary>
	RedLine
}
