using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based grid strategy.
/// Opens the first trade when RSI reaches overbought/oversold zones
/// and then adds counter trades as price moves by configurable steps.
/// All positions are closed together on defined profit target.
/// </summary>
public class RecoRsiGridStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiSellZone;
	private readonly StrategyParam<decimal> _rsiBuyZone;
	private readonly StrategyParam<decimal> _gridStep;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastOrderPrice;
	private bool _lastOrderIsBuy;
	private int _ordersTotal;
	private decimal _entryPrice;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiSellZone { get => _rsiSellZone.Value; set => _rsiSellZone.Value = value; }
	public decimal RsiBuyZone { get => _rsiBuyZone.Value; set => _rsiBuyZone.Value = value; }
	public decimal GridStep { get => _gridStep.Value; set => _gridStep.Value = value; }
	public int MaxOrders { get => _maxOrders.Value; set => _maxOrders.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RecoRsiGridStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI indicator period", "Signal");

		_rsiSellZone = Param(nameof(RsiSellZone), 70m)
			.SetDisplay("RSI Sell Zone", "RSI level to sell", "Signal");

		_rsiBuyZone = Param(nameof(RsiBuyZone), 30m)
			.SetDisplay("RSI Buy Zone", "RSI level to buy", "Signal");

		_gridStep = Param(nameof(GridStep), 200m)
			.SetDisplay("Grid Step", "Distance between grid orders", "Grid");

		_maxOrders = Param(nameof(MaxOrders), 5)
			.SetDisplay("Max Orders", "Maximum number of grid orders", "Grid");

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

		_lastOrderPrice = 0;
		_lastOrderIsBuy = false;
		_ordersTotal = 0;
		_entryPrice = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		// Check if we should close all on profit
		if (_ordersTotal > 0 && _entryPrice > 0)
		{
			var unrealized = Position > 0
				? price - _entryPrice
				: _entryPrice - price;

			if (unrealized > GridStep * 0.5m)
			{
				// Close all
				if (Position > 0)
					SellMarket();
				else if (Position < 0)
					BuyMarket();

				_ordersTotal = 0;
				_lastOrderPrice = 0;
				_entryPrice = 0;
				return;
			}
		}

		var signal = GetSignal(price, rsiValue);

		if (signal > 0 && Position <= 0)
		{
			BuyMarket();
			_lastOrderIsBuy = true;
			_lastOrderPrice = price;
			_entryPrice = price;
			_ordersTotal++;
		}
		else if (signal < 0 && Position >= 0)
		{
			SellMarket();
			_lastOrderIsBuy = false;
			_lastOrderPrice = price;
			_entryPrice = price;
			_ordersTotal++;
		}
	}

	private int GetSignal(decimal price, decimal rsiValue)
	{
		if (_ordersTotal == 0)
		{
			if (rsiValue >= RsiSellZone)
				return -1;
			if (rsiValue <= RsiBuyZone)
				return 1;
			return 0;
		}

		if (MaxOrders > 0 && _ordersTotal >= MaxOrders)
		{
			// Reset grid when max orders reached
			_ordersTotal = 0;
			_lastOrderPrice = 0;
			return 0;
		}

		// Add counter-trend orders at grid steps
		if (_lastOrderIsBuy && price <= _lastOrderPrice - GridStep)
			return 1;
		else if (!_lastOrderIsBuy && price >= _lastOrderPrice + GridStep)
			return -1;

		return 0;
	}
}
