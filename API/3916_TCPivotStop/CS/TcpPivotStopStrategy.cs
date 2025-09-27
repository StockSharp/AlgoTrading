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
/// Daily pivot breakout strategy converted from the MetaTrader "gpfTCPivotStop" expert.
/// Calculates classical floor pivots using the previous day's range and monitors the daily open
/// to enter trades when price crosses the pivot level from above or below.
/// Protective stop and target levels are mapped to the original support and resistance tiers.
/// </summary>
public class TcpPivotStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitTarget;
	private readonly StrategyParam<bool> _closeAtSessionEnd;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _currentDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;

	private decimal? _previousClose;
	private decimal? _lastClose;

	private decimal? _pivot;
	private decimal? _resistance1;
	private decimal? _resistance2;
	private decimal? _resistance3;
	private decimal? _support1;
	private decimal? _support2;
	private decimal? _support3;

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="TcpPivotStopStrategy"/> class.
	/// </summary>
	public TcpPivotStopStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Position size used for market orders.", "General");

		_takeProfitTarget = Param(nameof(TakeProfitTarget), 1)
			.SetDisplay("Target Level", "Selects which support/resistance tier becomes the take profit.", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(1, 3, 1);

		_closeAtSessionEnd = Param(nameof(CloseAtSessionEnd), false)
			.SetDisplay("Close At Session End", "Flat the position when a new daily session begins.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate daily pivots.", "General");
	}

	/// <summary>
	/// Position size used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Target tier mapped to support or resistance levels (1 - nearest, 3 - farthest).
	/// </summary>
	public int TakeProfitTarget
	{
		get => _takeProfitTarget.Value;
		set => _takeProfitTarget.Value = value;
	}

	/// <summary>
	/// Close any open position at the start of a new daily session.
	/// </summary>
	public bool CloseAtSessionEnd
	{
		get => _closeAtSessionEnd.Value;
		set => _closeAtSessionEnd.Value = value;
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TakeProfitTarget is < 1 or > 3)
			throw new InvalidOperationException("Take profit target must be between 1 and 3.");

		Volume = OrderVolume;

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleDay = candle.OpenTime.Date;

		if (_currentDay != null && candleDay != _currentDay.Value)
		{
			FinalizePreviousDay();
			_currentDay = candleDay;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
		}
		else if (_currentDay == null)
		{
			_currentDay = candleDay;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
		}
		else
		{
			_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
			_dayLow = Math.Min(_dayLow, candle.LowPrice);
		}

		_dayClose = candle.ClosePrice;

		ManageActivePosition(candle);

		_previousClose = _lastClose;
		_lastClose = candle.ClosePrice;
	}

	private void FinalizePreviousDay()
	{
		if (_currentDay == null)
			return;

		CalculatePivotLevels();

		if (CloseAtSessionEnd)
			CloseActivePosition();

		TryEnterAtSessionOpen();
	}

	private void CalculatePivotLevels()
	{
		var high = _dayHigh;
		var low = _dayLow;
		var close = _dayClose;

		if (high == 0m && low == 0m && close == 0m)
			return;

		_pivot = (high + low + close) / 3m;
		_resistance1 = 2m * _pivot - low;
		_support1 = 2m * _pivot - high;
		_resistance2 = _pivot + (_resistance1 - _support1);
		_support2 = _pivot - (_resistance1 - _support1);
		_resistance3 = high + 2m * (_pivot - low);
		_support3 = low - 2m * (high - _pivot);
	}

	private void TryEnterAtSessionOpen()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pivot == null || _previousClose == null || _lastClose == null)
			return;

		if (Position != 0m)
			return;

		var pivot = _pivot.Value;
		var previousClose = _previousClose.Value;
		var lastClose = _lastClose.Value;

		if (lastClose < pivot && previousClose >= pivot)
		{
			EnterShort();
		}
		else if (lastClose > pivot && previousClose <= pivot)
		{
			EnterLong();
		}
	}

	private void EnterLong()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_support1 == null || _support2 == null || _support3 == null ||
			_resistance1 == null || _resistance2 == null || _resistance3 == null)
			return;

		var stop = TakeProfitTarget switch
		{
			1 => _support1,
			2 => _support1,
			3 => _support2,
			_ => _support1,
		};

		var take = TakeProfitTarget switch
		{
			1 => _resistance1,
			2 => _resistance2,
			3 => _resistance3,
			_ => _resistance1,
		};

		BuyMarket(Volume);
		_stopLossPrice = stop;
		_takeProfitPrice = take;
	}

	private void EnterShort()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_support1 == null || _support2 == null || _support3 == null ||
			_resistance1 == null || _resistance2 == null || _resistance3 == null)
			return;

		var stop = TakeProfitTarget switch
		{
			1 => _resistance1,
			2 => _resistance1,
			3 => _resistance2,
			_ => _resistance1,
		};

		var take = TakeProfitTarget switch
		{
			1 => _support1,
			2 => _support2,
			3 => _support3,
			_ => _support1,
		};

		SellMarket(Volume);
		_stopLossPrice = stop;
		_takeProfitPrice = take;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopLossPrice is { } longStop && candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				ResetProtectionLevels();
				return;
			}

			if (_takeProfitPrice is { } longTake && candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				ResetProtectionLevels();
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);

			if (_stopLossPrice is { } shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(absPosition);
				ResetProtectionLevels();
				return;
			}

			if (_takeProfitPrice is { } shortTake && candle.LowPrice <= shortTake)
			{
				BuyMarket(absPosition);
				ResetProtectionLevels();
			}
		}
	}

	private void CloseActivePosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
			ResetProtectionLevels();
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			ResetProtectionLevels();
		}
	}

	private void ResetProtectionLevels()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private void ResetState()
	{
		_currentDay = null;
		_dayHigh = 0m;
		_dayLow = 0m;
		_dayClose = 0m;
		_previousClose = null;
		_lastClose = null;
		_pivot = null;
		_resistance1 = null;
		_resistance2 = null;
		_resistance3 = null;
		_support1 = null;
		_support2 = null;
		_support3 = null;
		ResetProtectionLevels();
	}
}

