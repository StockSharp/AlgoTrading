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
/// Simple breakout strategy that enters on momentum moves
/// with fixed take profit and stop loss protection.
/// </summary>
public class TenPipsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>Take profit distance.</summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	/// <summary>Stop loss distance.</summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	/// <summary>Lookback period for momentum.</summary>
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TenPipsStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk")
			.SetGreaterThanZero();
		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk")
			.SetGreaterThanZero();
		_lookback = Param(nameof(Lookback), 10)
			.SetDisplay("Lookback", "Momentum lookback period", "Indicators")
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;

		var roc = new RateOfChange { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, roc);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position == 0)
		{
			if (rocValue > 0.5m)
			{
				BuyMarket();
				_entryPrice = close;
			}
			else if (rocValue < -0.5m)
			{
				SellMarket();
				_entryPrice = close;
			}
		}
		else if (Position > 0)
		{
			if (close >= _entryPrice + TakeProfit || close <= _entryPrice - StopLoss)
			{
				SellMarket(Math.Abs(Position));
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - TakeProfit || close >= _entryPrice + StopLoss)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
