using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ICT Master Suite session breakout strategy.
/// The session is the UTC day. A close above the high of the session's earlier candles goes long and a close below their low goes short,
/// reversing an opposite position. An open position is protected by a trailing stop AtrMultiplier ATRs away from the close that only moves
/// in the trade's favour; the position closes when a candle touches it.
/// </summary>
public class IctMasterSuiteTradingIqStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _sessionDate;
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private decimal? _stopPrice;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the trailing stop distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IctMasterSuiteTradingIqStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Risk Management")
			.SetOptimize(5, 30, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for the trailing stop", "Risk Management")
			.SetOptimize(1m, 3m, 0.5m);

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Enable long trades", "General");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Enable short trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_sessionDate = null;
		_sessionHigh = 0m;
		_sessionLow = 0m;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;

		// The first candle of a session only opens the range; breakouts need earlier candles of the same session.
		if (_sessionDate != date)
		{
			_sessionDate = date;
			_sessionHigh = candle.HighPrice;
			_sessionLow = candle.LowPrice;
			ManageStop(candle, atrValue);
			return;
		}

		var sessionHigh = _sessionHigh;
		var sessionLow = _sessionLow;

		_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
		_sessionLow = Math.Min(_sessionLow, candle.LowPrice);

		if (ManageStop(candle, atrValue))
			return;

		if (!atrValue.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (AllowLong && close > sessionHigh && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - atr * AtrMultiplier;
		}
		else if (AllowShort && close < sessionLow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + atr * AtrMultiplier;
		}
	}

	// Returns true when the position was closed by the trailing stop on this candle.
	private bool ManageStop(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (Position == 0)
		{
			_stopPrice = null;
			return false;
		}

		if (_stopPrice is not decimal stop)
			return false;

		if (Position > 0 && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			_stopPrice = null;
			return true;
		}

		if (Position < 0 && candle.HighPrice >= stop)
		{
			BuyMarket(-Position);
			_stopPrice = null;
			return true;
		}

		if (atrValue.IsFormed)
		{
			var distance = atrValue.GetValue<decimal>() * AtrMultiplier;

			_stopPrice = Position > 0
				? Math.Max(stop, candle.ClosePrice - distance)
				: Math.Min(stop, candle.ClosePrice + distance);
		}

		return false;
	}
}
