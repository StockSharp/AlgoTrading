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
/// Implements the gpfTCPivotStop MetaTrader strategy using StockSharp high level API.
/// Uses previous day's pivot levels for breakout entries with configurable targets.
/// Optionally forces flat positions at a specified hour of the new trading day.
/// </summary>
public class TcpPivotSessionStopStrategy : Strategy
{
	private readonly StrategyParam<int> _targetLevel;
	private readonly StrategyParam<bool> _closeAtSessionStart;
	private readonly StrategyParam<int> _sessionCloseHour;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pivot;
	private decimal _res1;
	private decimal _res2;
	private decimal _res3;
	private decimal _sup1;
	private decimal _sup2;
	private decimal _sup3;

	private decimal _previousClose;
	private decimal _targetPrice;
	private decimal _stopPrice;

	/// <summary>
	/// Target/support level used for take-profit and stop-loss calculation (1-3).
	/// </summary>
	public int TargetLevel
	{
		get => _targetLevel.Value;
		set => _targetLevel.Value = value;
	}

	/// <summary>
	/// Close all positions when the specified session hour starts.
	/// </summary>
	public bool CloseAtSessionStart
	{
		get => _closeAtSessionStart.Value;
		set => _closeAtSessionStart.Value = value;
	}

	/// <summary>
	/// Hour of the day (0-23) when positions should be closed if enabled.
	/// </summary>
	public int SessionCloseHour
	{
		get => _sessionCloseHour.Value;
		set => _sessionCloseHour.Value = value;
	}

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TcpPivotSessionStopStrategy"/> class.
	/// </summary>
	public TcpPivotSessionStopStrategy()
	{
		_targetLevel = Param(nameof(TargetLevel), 1)
			.SetGreaterThanZero()
			.SetDisplay("Target Level", "Pivot level used for stop-loss and take-profit (1-3)", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 3, 1);

		_closeAtSessionStart = Param(nameof(CloseAtSessionStart), false)
			.SetDisplay("Close At Session Start", "Close positions at the start of the configured session hour", "General");

		_sessionCloseHour = Param(nameof(SessionCloseHour), 0)
			.SetDisplay("Session Close Hour", "Hour of the day (0-23) used when closing at session start", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for pivot breakout signals", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pivot = 0m;
		_res1 = 0m;
		_res2 = 0m;
		_res3 = 0m;
		_sup1 = 0m;
		_sup2 = 0m;
		_sup3 = 0m;
		_previousClose = 0m;
		_targetPrice = 0m;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());

		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();

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

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		_pivot = (high + low + close) / 3m;
		_res1 = 2m * _pivot - low;
		_sup1 = 2m * _pivot - high;
		var diff = _res1 - _sup1;
		_res2 = _pivot + diff;
		_sup2 = _pivot - diff;
		_res3 = high + 2m * (_pivot - low);
		_sup3 = low - 2m * (high - _pivot);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CloseAtSessionStart && candle.OpenTime.Hour == SessionCloseHour && Position != 0)
		{
			if (Position > 0)
			{
				SellMarket(Math.Abs(Position));
			}
			else
			{
				BuyMarket(Math.Abs(Position));
			}

			_targetPrice = 0m;
			_stopPrice = 0m;
		}

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (_targetPrice != 0m && close >= _targetPrice)
			{
				SellMarket(Math.Abs(Position));
				_targetPrice = 0m;
				_stopPrice = 0m;
			}
			else if (_stopPrice != 0m && close <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
				_targetPrice = 0m;
				_stopPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (_targetPrice != 0m && close <= _targetPrice)
			{
				BuyMarket(Math.Abs(Position));
				_targetPrice = 0m;
				_stopPrice = 0m;
			}
			else if (_stopPrice != 0m && close >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_targetPrice = 0m;
				_stopPrice = 0m;
			}
		}
		else if (_pivot != 0m && _previousClose != 0m)
		{
			if (_previousClose <= _pivot && close > _pivot)
			{
				(_targetPrice, _stopPrice) = GetLevels(true);
				BuyMarket(Volume);
			}
			else if (_previousClose >= _pivot && close < _pivot)
			{
				(_targetPrice, _stopPrice) = GetLevels(false);
				SellMarket(Volume);
			}
		}

		_previousClose = close;
	}

	private (decimal target, decimal stop) GetLevels(bool isLong)
	{
		return TargetLevel switch
		{
			1 => isLong ? (_res1, _sup1) : (_sup1, _res1),
			2 => isLong ? (_res2, _sup2) : (_sup2, _res2),
			_ => isLong ? (_res3, _sup3) : (_sup3, _res3),
		};
	}
}

