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
/// Strategy demonstrating monthly profit tracking with scheduled trades.
/// </summary>
public class StxMonthlyTradesProfitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _firstTarget;
	private decimal _stopPrice;
	private bool _exitPending;

	/// <summary>Candle type used by the strategy.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="StxMonthlyTradesProfitStrategy"/>.
	/// </summary>
	public StxMonthlyTradesProfitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_entryPrice = 0m;
		_firstTarget = 0m;
		_stopPrice = 0m;
		_exitPending = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.OpenTime;
		var longCondition = time.Day == 1 && time.Hour >= 10 && time.Hour < 12;
		var shortCondition = time.Day == 10 && time.Hour >= 10 && time.Hour < 12;

		// Exit logic
		if (Position > 0 && !_exitPending)
		{
			if (candle.ClosePrice >= _firstTarget || candle.ClosePrice <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
				_exitPending = true;
			}
		}
		else if (Position < 0 && !_exitPending)
		{
			if (candle.ClosePrice <= _firstTarget || candle.ClosePrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_exitPending = true;
			}
		}

		// Reset exit pending when position is flat
		if (Position == 0)
			_exitPending = false;

		// Entry logic
		if (longCondition && Position == 0 && !_exitPending)
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice * (1m - 0.02m);
			_firstTarget = _entryPrice * (1m + 0.03m);
		}
		else if (shortCondition && Position == 0 && !_exitPending)
		{
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice * (1m + 0.02m);
			_firstTarget = _entryPrice * (1m - 0.03m);
		}
	}
}
