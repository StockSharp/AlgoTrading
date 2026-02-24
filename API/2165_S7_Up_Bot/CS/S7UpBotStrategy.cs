using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// S7 Up Bot breakout strategy.
/// Opens long after double bottom and short after double top.
/// </summary>
public class S7UpBotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _hlDivergence;
	private readonly StrategyParam<decimal> _spanPrice;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevLow;
	private decimal _prevHigh;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private bool _isLong;
	private bool _inPosition;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal HlDivergence { get => _hlDivergence.Value; set => _hlDivergence.Value = value; }
	public decimal SpanPrice { get => _spanPrice.Value; set => _spanPrice.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public S7UpBotStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Absolute take profit", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Absolute stop loss", "Risk");

		_hlDivergence = Param(nameof(HlDivergence), 100m)
			.SetGreaterThanZero()
			.SetDisplay("HL Divergence", "Max difference between highs or lows", "General");

		_spanPrice = Param(nameof(SpanPrice), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Span Price", "Distance from extreme to price", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevLow = 0m;
		_prevHigh = 0m;
		_inPosition = false;

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		if (_inPosition)
			ManagePosition(candle, price);

		if (!_inPosition && _prevLow != 0m && _prevHigh != 0m)
			CheckEntry(candle, price);

		_prevLow = candle.LowPrice;
		_prevHigh = candle.HighPrice;
	}

	private void CheckEntry(ICandleMessage candle, decimal price)
	{
		// Double bottom: consecutive similar lows + bounce
		if (Math.Abs(candle.LowPrice - _prevLow) < HlDivergence &&
			price - candle.LowPrice > SpanPrice)
		{
			if (Position <= 0)
				BuyMarket();
			_inPosition = true;
			_isLong = true;
			_entryPrice = price;
			_stopPrice = price - StopLoss;
			_takeProfitPrice = price + TakeProfit;
		}
		// Double top: consecutive similar highs + drop
		else if (Math.Abs(candle.HighPrice - _prevHigh) < HlDivergence &&
			candle.HighPrice - price > SpanPrice)
		{
			if (Position >= 0)
				SellMarket();
			_inPosition = true;
			_isLong = false;
			_entryPrice = price;
			_stopPrice = price + StopLoss;
			_takeProfitPrice = price - TakeProfit;
		}
	}

	private void ManagePosition(ICandleMessage candle, decimal price)
	{
		if (_isLong)
		{
			if (candle.HighPrice >= _takeProfitPrice || candle.LowPrice <= _stopPrice)
			{
				SellMarket();
				_inPosition = false;
			}
		}
		else
		{
			if (candle.LowPrice <= _takeProfitPrice || candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
				_inPosition = false;
			}
		}
	}
}
