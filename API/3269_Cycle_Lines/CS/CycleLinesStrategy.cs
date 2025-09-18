using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy inspired by the MQL "Cycle Lines" expert advisor.
/// Trades MACD crossovers and manages open positions with stop loss,
/// take profit, break-even and trailing protection logic.
/// </summary>
public class CycleLinesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingOffset;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _breakEvenOffset;
	private readonly StrategyParam<DataType> _candleType;

	private MACD _macd;

	private decimal _prevMacd;
	private decimal _prevSignal;

	private decimal? _entryPrice;
	private decimal _maxPrice;
	private decimal _minPrice;
	private decimal? _breakEvenLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="CycleLinesStrategy"/> class.
	/// </summary>
	public CycleLinesStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD fast EMA", "Fast EMA period for MACD", "Indicators")
			.SetCanOptimize(true, 6, 18, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD slow EMA", "Slow EMA period for MACD", "Indicators")
			.SetCanOptimize(true, 20, 40, 2);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD signal", "Signal line period", "Indicators")
			.SetCanOptimize(true, 6, 18, 1);

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop loss", "Absolute stop loss distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take profit", "Absolute take profit distance", "Risk");

		_trailingOffset = Param(nameof(TrailingOffset), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing offset", "Distance between peak and trailing stop", "Risk");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break-even trigger", "Profit required to arm break-even", "Risk");

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break-even offset", "Offset applied when moving stop to break-even", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Working candle series", "General");
	}

	/// <summary>
	/// Trade volume per signal.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Fast EMA period used inside MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used inside MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Absolute stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Absolute take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing distance maintained from the best price in favor of the trade.
	/// </summary>
	public decimal TrailingOffset
	{
		get => _trailingOffset.Value;
		set => _trailingOffset.Value = value;
	}

	/// <summary>
	/// Profit threshold that activates break-even logic.
	/// </summary>
	public decimal BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	/// <summary>
	/// Additional offset applied to the break-even stop level.
	/// </summary>
	public decimal BreakEvenOffset
	{
		get => _breakEvenOffset.Value;
		set => _breakEvenOffset.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MACD
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_macd, ProcessCandle).Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		// Reset state after position is fully closed.
		if (Position == 0m)
		{
			_entryPrice = null;
			_breakEvenLevel = null;
			_maxPrice = 0m;
			_minPrice = 0m;
			return;
		}

		if (trade.Order is null)
			return;

		// Capture the fill price when a fresh position is opened.
		if (Position > 0m && trade.Order.Direction == Sides.Buy)
		{
			_entryPrice = trade.Trade.Price;
			_maxPrice = trade.Trade.Price;
			_minPrice = trade.Trade.Price;
			_breakEvenLevel = null;
		}
		else if (Position < 0m && trade.Order.Direction == Sides.Sell)
		{
			_entryPrice = trade.Trade.Price;
			_maxPrice = trade.Trade.Price;
			_minPrice = trade.Trade.Price;
			_breakEvenLevel = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal signalLine)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store previous values before the indicator becomes fully formed.
		if (!_macd.IsFormed)
		{
			_prevMacd = macdLine;
			_prevSignal = signalLine;
			return;
		}

		// Manage existing positions using risk rules before looking for new entries.
		if (ManagePosition(candle, macdLine, signalLine))
		{
			_prevMacd = macdLine;
			_prevSignal = signalLine;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMacd = macdLine;
			_prevSignal = signalLine;
			return;
		}

		var crossUp = _prevMacd <= _prevSignal && macdLine > signalLine;
		var crossDown = _prevMacd >= _prevSignal && macdLine < signalLine;

		if (crossUp)
		{
			// MACD crosses above its signal line: open a long position.
			BuyMarket(Volume);
		}
		else if (crossDown)
		{
			// MACD crosses below its signal line: open a short position.
			SellMarket(Volume);
		}

		_prevMacd = macdLine;
		_prevSignal = signalLine;
	}

	private bool ManagePosition(ICandleMessage candle, decimal macdLine, decimal signalLine)
	{
		if (Position == 0m || _entryPrice is null)
			return false;

		var entryPrice = _entryPrice.Value;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		if (Position > 0m)
		{
			_maxPrice = Math.Max(_maxPrice, high);

			// Hard stop loss protection for long trades.
			if (StopLoss > 0m && low <= entryPrice - StopLoss)
			{
				ClosePosition();
				return true;
			}

			// Hard take profit protection for long trades.
			if (TakeProfit > 0m && high >= entryPrice + TakeProfit)
			{
				ClosePosition();
				return true;
			}

			// Arm break-even once the candle moved far enough.
			if (BreakEvenTrigger > 0m && _breakEvenLevel is null && high - entryPrice >= BreakEvenTrigger)
				_breakEvenLevel = entryPrice + BreakEvenOffset;

			if (_breakEvenLevel is decimal breakEven && low <= breakEven)
			{
				ClosePosition();
				return true;
			}

			if (TrailingOffset > 0m)
			{
				var trailingLevel = _maxPrice - TrailingOffset;

				if (trailingLevel > entryPrice && low <= trailingLevel)
				{
					ClosePosition();
					return true;
				}
			}

			// Exit long positions when MACD turns bearish.
			if (macdLine < signalLine)
			{
				ClosePosition();
				return true;
			}
		}
		else
		{
			_minPrice = _minPrice == 0m ? low : Math.Min(_minPrice, low);

			// Hard stop loss protection for short trades.
			if (StopLoss > 0m && high >= entryPrice + StopLoss)
			{
				ClosePosition();
				return true;
			}

			// Hard take profit protection for short trades.
			if (TakeProfit > 0m && low <= entryPrice - TakeProfit)
			{
				ClosePosition();
				return true;
			}

			// Arm break-even once the candle moved far enough in favor.
			if (BreakEvenTrigger > 0m && _breakEvenLevel is null && entryPrice - low >= BreakEvenTrigger)
				_breakEvenLevel = entryPrice - BreakEvenOffset;

			if (_breakEvenLevel is decimal breakEven && high >= breakEven)
			{
				ClosePosition();
				return true;
			}

			if (TrailingOffset > 0m)
			{
				var trailingLevel = _minPrice + TrailingOffset;

				if (trailingLevel < entryPrice && high >= trailingLevel)
				{
					ClosePosition();
					return true;
				}
			}

			// Exit short positions when MACD turns bullish.
			if (macdLine > signalLine)
			{
				ClosePosition();
				return true;
			}
		}

		return false;
	}
}
