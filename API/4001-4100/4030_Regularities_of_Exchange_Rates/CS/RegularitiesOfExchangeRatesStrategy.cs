using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time-based breakout strategy converted from the "Strategy of Regularities of Exchange Rates" MQL expert advisor.
/// At a scheduled hour captures reference price, then enters on breakout above/below offset levels.
/// Exits at a closing hour or on take-profit/stop-loss hit.
/// </summary>
public class RegularitiesOfExchangeRatesStrategy : Strategy
{
	private readonly StrategyParam<int> _openingHour;
	private readonly StrategyParam<int> _closingHour;
	private readonly StrategyParam<decimal> _entryOffsetPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _dummySma;
	private decimal _pointSize;
	private DateTime? _lastEntryDate;
	private decimal _referencePrice;
	private decimal _entryPrice;
	private bool _waitingForBreakout;

	public int OpeningHour
	{
		get => _openingHour.Value;
		set => _openingHour.Value = value;
	}

	public int ClosingHour
	{
		get => _closingHour.Value;
		set => _closingHour.Value = value;
	}

	public decimal EntryOffsetPoints
	{
		get => _entryOffsetPoints.Value;
		set => _entryOffsetPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RegularitiesOfExchangeRatesStrategy()
	{
		_openingHour = Param(nameof(OpeningHour), 9)
			.SetDisplay("Opening Hour", "Hour (0-23) when breakout levels are set", "Schedule")
			.SetRange(0, 23);

		_closingHour = Param(nameof(ClosingHour), 2)
			.SetDisplay("Closing Hour", "Hour (0-23) when the strategy exits", "Schedule")
			.SetRange(0, 23);

		_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 20m)
			.SetDisplay("Entry Offset (points)", "Distance from reference price for breakout", "Orders")
			.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
			.SetDisplay("Take Profit (points)", "Profit target distance in points", "Risk")
			.SetNotNegative();

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetDisplay("Stop Loss (points)", "Stop-loss distance in points", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate trading hours", "General");
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_pointSize = 0m;
		_lastEntryDate = null;
		_referencePrice = 0m;
		_entryPrice = 0m;
		_waitingForBreakout = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pointSize = Security?.PriceStep ?? 0.01m;
		if (_pointSize <= 0m)
			_pointSize = 0.01m;

		_dummySma = new SimpleMovingAverage { Length = 2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_dummySma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		// At closing hour: flatten position and cancel breakout watch
		if (hour == ClosingHour)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			_waitingForBreakout = false;
			_entryPrice = 0m;
		}

		// Manage take-profit and stop-loss for existing position
		if (Position != 0 && _entryPrice > 0m)
		{
			var tp = TakeProfitPoints * _pointSize;
			var sl = StopLossPoints * _pointSize;

			if (Position > 0)
			{
				if ((tp > 0m && close - _entryPrice >= tp) || (sl > 0m && _entryPrice - close >= sl))
				{
					SellMarket(Position);
					_entryPrice = 0m;
					_waitingForBreakout = false;
				}
			}
			else if (Position < 0)
			{
				if ((tp > 0m && _entryPrice - close >= tp) || (sl > 0m && close - _entryPrice >= sl))
				{
					BuyMarket(-Position);
					_entryPrice = 0m;
					_waitingForBreakout = false;
				}
			}
		}

		// At opening hour: set reference price for breakout
		if (hour == OpeningHour && Position == 0)
		{
			var date = candle.OpenTime.Date;
			if (!_lastEntryDate.HasValue || _lastEntryDate.Value != date)
			{
				_referencePrice = close;
				_waitingForBreakout = true;
				_lastEntryDate = date;
			}
		}

		// Check for breakout entry
		if (_waitingForBreakout && Position == 0 && _referencePrice > 0m)
		{
			var offset = EntryOffsetPoints * _pointSize;
			var buyLevel = _referencePrice + offset;
			var sellLevel = _referencePrice - offset;

			if (high >= buyLevel)
			{
				BuyMarket();
				_entryPrice = close;
				_waitingForBreakout = false;
			}
			else if (low <= sellLevel)
			{
				SellMarket();
				_entryPrice = close;
				_waitingForBreakout = false;
			}
		}
	}
}
