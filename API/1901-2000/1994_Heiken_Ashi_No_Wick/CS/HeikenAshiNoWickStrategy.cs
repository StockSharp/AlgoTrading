using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian Heiken Ashi pattern: enter against a stronger candle with no
/// lower/upper wick, exit on the opposite no-wick candle.
/// </summary>
public class HeikenAshiNoWickStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private decimal? _previousHaOpen;
	private decimal? _previousHaClose;
	private decimal? _previousBody;
	private Order _pendingOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public HeikenAshiNoWickStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousHaOpen = _previousHaClose = _previousBody = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousHaOpen = _previousHaClose = _previousBody = null;
		_pendingOrder = null;
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;
		var haClose = (open + high + low + close) / 4m;
		var haOpen = _previousHaOpen is decimal prevOpen
			? (prevOpen + _previousHaClose.Value) / 2m
			: (open + close) / 2m;
		var body = Math.Abs(haClose - haOpen);
		var larger = _previousBody is decimal previousBody && body > previousBody;
		var bullishNoLower = haClose > haOpen && low >= haOpen;
		var bearishNoUpper = haClose < haOpen && high <= haOpen;

		_previousHaOpen = haOpen;
		_previousHaClose = haClose;
		_previousBody = body;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (bullishNoLower)
		{
			if (Position > 0m)
				_pendingOrder = SellMarket(Position + (larger ? Volume : 0m));
			else if (Position == 0m && larger)
				_pendingOrder = SellMarket(Volume);
		}
		else if (bearishNoUpper)
		{
			if (Position < 0m)
				_pendingOrder = BuyMarket(-Position + (larger ? Volume : 0m));
			else if (Position == 0m && larger)
				_pendingOrder = BuyMarket(Volume);
		}
	}
}
