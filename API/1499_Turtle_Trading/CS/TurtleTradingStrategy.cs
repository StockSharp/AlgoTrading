using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Turtle Trading breakout strategy with ATR-based position sizing and pyramiding.
/// </summary>
public class TurtleTradingStrategy : Strategy
{
	private readonly StrategyParam<bool> _useMode2;
	private readonly StrategyParam<int> _entryLength;
	private readonly StrategyParam<int> _exitLength;
	private readonly StrategyParam<int> _entryLengthMode2;
	private readonly StrategyParam<int> _exitLengthMode2;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _initialStopAtrMultiple;
	private readonly StrategyParam<decimal> _pyramidAtrMultiple;
	private readonly StrategyParam<int> _maxUnits;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _fromDate;
	private readonly StrategyParam<DateTimeOffset> _toDate;

	private decimal _prevEntryUpper;
	private decimal _prevEntryLower;
	private decimal _prevExitUpper;
	private decimal _prevExitLower;
	private decimal _prevClose;

	private decimal _trailingStopLong;
	private decimal _trailingStopShort;
	private decimal _realEntryPriceLong;
	private decimal _realEntryPriceShort;
	private decimal _addUnitPriceLong;
	private decimal _addUnitPriceShort;
	private int _units;
	private bool _lastTradeWin;

	/// <summary>
	/// Use mode 2 parameters.
	/// </summary>
	public bool UseMode2 { get => _useMode2.Value; set => _useMode2.Value = value; }

	/// <summary>
	/// Entry channel length for mode 1.
	/// </summary>
	public int EntryLength { get => _entryLength.Value; set => _entryLength.Value = value; }

	/// <summary>
	/// Exit channel length for mode 1.
	/// </summary>
	public int ExitLength { get => _exitLength.Value; set => _exitLength.Value = value; }

	/// <summary>
	/// Entry channel length for mode 2.
	/// </summary>
	public int EntryLengthMode2 { get => _entryLengthMode2.Value; set => _entryLengthMode2.Value = value; }

	/// <summary>
	/// Exit channel length for mode 2.
	/// </summary>
	public int ExitLengthMode2 { get => _exitLengthMode2.Value; set => _exitLengthMode2.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Fraction of equity risked per trade.
	/// </summary>
	public decimal RiskPerTrade { get => _riskPerTrade.Value; set => _riskPerTrade.Value = value; }

	/// <summary>
	/// ATR multiple for stop loss.
	/// </summary>
	public decimal InitialStopAtrMultiple { get => _initialStopAtrMultiple.Value; set => _initialStopAtrMultiple.Value = value; }

	/// <summary>
	/// ATR multiple for adding units.
	/// </summary>
	public decimal PyramidAtrMultiple { get => _pyramidAtrMultiple.Value; set => _pyramidAtrMultiple.Value = value; }

	/// <summary>
	/// Maximum number of units.
	/// </summary>
	public int MaxUnits { get => _maxUnits.Value; set => _maxUnits.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Start date for trading.
	/// </summary>
	public DateTimeOffset FromDate { get => _fromDate.Value; set => _fromDate.Value = value; }

	/// <summary>
	/// End date for trading.
	/// </summary>
	public DateTimeOffset ToDate { get => _toDate.Value; set => _toDate.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TurtleTradingStrategy()
	{
		_useMode2 = Param(nameof(UseMode2), false)
			.SetDisplay("Use Mode2", "Switch to Mode2 parameters", "General");

		_entryLength = Param(nameof(EntryLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Mode1 Entry Length", "Entry channel length for mode 1", "Mode1");

		_exitLength = Param(nameof(ExitLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Mode1 Exit Length", "Exit channel length for mode 1", "Mode1");

		_entryLengthMode2 = Param(nameof(EntryLengthMode2), 55)
			.SetGreaterThanZero()
			.SetDisplay("Mode2 Entry Length", "Entry channel length for mode 2", "Mode2");

		_exitLengthMode2 = Param(nameof(ExitLengthMode2), 20)
			.SetGreaterThanZero()
			.SetDisplay("Mode2 Exit Length", "Exit channel length for mode 2", "Mode2");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

		_riskPerTrade = Param(nameof(RiskPerTrade), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Per Trade", "Fraction of equity risked per trade", "Risk");

		_initialStopAtrMultiple = Param(nameof(InitialStopAtrMultiple), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Stop ATR Multiple", "ATR multiple for stop loss", "Risk");

		_pyramidAtrMultiple = Param(nameof(PyramidAtrMultiple), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Pyramid ATR Multiple", "ATR multiple for adding units", "Risk");

		_maxUnits = Param(nameof(MaxUnits), 4)
			.SetGreaterThanZero()
			.SetDisplay("Max Units", "Maximum number of units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fromDate = Param(nameof(FromDate), new DateTimeOffset(new DateTime(2013, 1, 1)))
			.SetDisplay("From Date", "Start date for trading", "General");

		_toDate = Param(nameof(ToDate), new DateTimeOffset(new DateTime(2024, 8, 1)))
			.SetDisplay("To Date", "End date for trading", "General");
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

		_prevEntryUpper = 0;
		_prevEntryLower = 0;
		_prevExitUpper = 0;
		_prevExitLower = 0;
		_prevClose = 0;
		ResetLong();
		ResetShort();
		_lastTradeWin = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var entryLen = UseMode2 ? EntryLengthMode2 : EntryLength;
		var exitLen = UseMode2 ? ExitLengthMode2 : ExitLength;

		var entryChannel = new DonchianChannels { Length = entryLen };
		var exitChannel = new DonchianChannels { Length = exitLen };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(entryChannel, exitChannel, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, entryChannel);
			DrawIndicator(area, exitChannel);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue entryValue, IIndicatorValue exitValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var entry = (DonchianChannelsValue)entryValue;
		var exit = (DonchianChannelsValue)exitValue;

		if (entry.UpperBand is not decimal entryUpper || entry.LowerBand is not decimal entryLower)
			return;

		if (exit.UpperBand is not decimal exitUpper || exit.LowerBand is not decimal exitLower)
			return;

		var time = candle.OpenTime;
		var validDate = time >= FromDate && time <= ToDate;
		var modeSignal = UseMode2 || !_lastTradeWin;

		var longSignal = validDate && Position == 0 && _prevEntryUpper != 0 &&
			candle.ClosePrice > _prevEntryUpper && _prevClose <= _prevEntryUpper;

		var shortSignal = validDate && Position == 0 && _prevEntryLower != 0 &&
			candle.ClosePrice < _prevEntryLower && _prevClose >= _prevEntryLower;

		var exitLongSignal = validDate && Position > 0 && _prevExitLower != 0 &&
			candle.ClosePrice < _prevExitLower && _prevClose >= _prevExitLower;

		var exitShortSignal = validDate && Position < 0 && _prevExitUpper != 0 &&
			candle.ClosePrice > _prevExitUpper && _prevClose <= _prevExitUpper;

		var trailingStopLongSignal = validDate && Position > 0 && _trailingStopLong != 0 &&
			candle.ClosePrice < _trailingStopLong;

		var trailingStopShortSignal = validDate && Position < 0 && _trailingStopShort != 0 &&
			candle.ClosePrice > _trailingStopShort;

		var addUnitSignal = validDate && _units < MaxUnits &&
			((Position > 0 && _addUnitPriceLong != 0 && candle.ClosePrice > _addUnitPriceLong) ||
			(Position < 0 && _addUnitPriceShort != 0 && candle.ClosePrice < _addUnitPriceShort));

		if (longSignal && modeSignal)
		{
			var volume = CalculateUnitSize(atrValue);
			BuyMarket(volume);
			_units = 1;
			_realEntryPriceLong = candle.ClosePrice;
			_trailingStopLong = _realEntryPriceLong - InitialStopAtrMultiple * atrValue;
			_addUnitPriceLong = _realEntryPriceLong + PyramidAtrMultiple * atrValue;
		}
		else if (shortSignal && modeSignal)
		{
			var volume = CalculateUnitSize(atrValue);
			SellMarket(volume);
			_units = 1;
			_realEntryPriceShort = candle.ClosePrice;
			_trailingStopShort = _realEntryPriceShort + InitialStopAtrMultiple * atrValue;
			_addUnitPriceShort = _realEntryPriceShort - PyramidAtrMultiple * atrValue;
		}
		else if (exitLongSignal)
		{
			_lastTradeWin = candle.ClosePrice > _realEntryPriceLong;
			SellMarket(Position);
			ResetLong();
		}
		else if (exitShortSignal)
		{
			_lastTradeWin = candle.ClosePrice < _realEntryPriceShort;
			BuyMarket(Math.Abs(Position));
			ResetShort();
		}
		else if (addUnitSignal)
		{
			var volume = CalculateUnitSize(atrValue);

			if (Position > 0)
			{
				BuyMarket(volume);
				_realEntryPriceLong = candle.ClosePrice;
				_trailingStopLong = _realEntryPriceLong - InitialStopAtrMultiple * atrValue;
				_addUnitPriceLong = _realEntryPriceLong + PyramidAtrMultiple * atrValue;
			}
			else if (Position < 0)
			{
				SellMarket(volume);
				_realEntryPriceShort = candle.ClosePrice;
				_trailingStopShort = _realEntryPriceShort + InitialStopAtrMultiple * atrValue;
				_addUnitPriceShort = _realEntryPriceShort - PyramidAtrMultiple * atrValue;
			}

			_units++;
		}
		else if (trailingStopLongSignal)
		{
			_lastTradeWin = candle.ClosePrice > _realEntryPriceLong;
			SellMarket(Position);
			ResetLong();
		}
		else if (trailingStopShortSignal)
		{
			_lastTradeWin = candle.ClosePrice < _realEntryPriceShort;
			BuyMarket(Math.Abs(Position));
			ResetShort();
		}

		_prevEntryUpper = entryUpper;
		_prevEntryLower = entryLower;
		_prevExitUpper = exitUpper;
		_prevExitLower = exitLower;
		_prevClose = candle.ClosePrice;
	}

	private decimal CalculateUnitSize(decimal atrValue)
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = equity * RiskPerTrade;
		var unit = Volume;

		if (atrValue > 0)
		{
			var calc = riskAmount / (InitialStopAtrMultiple * atrValue);
			if (calc > 0)
				unit = calc;
		}

		return unit;
	}

	private void ResetLong()
	{
		_units = 0;
		_trailingStopLong = 0;
		_realEntryPriceLong = 0;
		_addUnitPriceLong = 0;
	}

	private void ResetShort()
	{
		_units = 0;
		_trailingStopShort = 0;
		_realEntryPriceShort = 0;
		_addUnitPriceShort = 0;
	}
}
