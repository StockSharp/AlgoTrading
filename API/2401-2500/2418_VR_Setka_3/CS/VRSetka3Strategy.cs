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
/// Grid strategy inspired by VR SETKA 3.
/// Places limit orders at grid levels. When filled, places next level.
/// Closes position on take profit.
/// </summary>
public class VRSetka3Strategy : Strategy
{
	private readonly StrategyParam<decimal> _startOffset;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _gridDistance;
	private readonly StrategyParam<decimal> _stepDistance;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _buyAvgPrice;
	private decimal _buyVolume;
	private decimal _sellAvgPrice;
	private decimal _sellVolume;
	private int _buyCount;
	private int _sellCount;
	private bool _hasBuyPending;
	private bool _hasSellPending;

	public decimal StartOffset
	{
		get => _startOffset.Value;
		set => _startOffset.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal GridDistance
	{
		get => _gridDistance.Value;
		set => _gridDistance.Value = value;
	}

	public decimal StepDistance
	{
		get => _stepDistance.Value;
		set => _stepDistance.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public VRSetka3Strategy()
	{
		_startOffset = Param(nameof(StartOffset), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Start Offset", "Offset for first limit orders", "Parameters");

		_takeProfit = Param(nameof(TakeProfit), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Distance for profit taking", "Parameters");

		_gridDistance = Param(nameof(GridDistance), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Distance", "Base distance between grid levels", "Parameters");

		_stepDistance = Param(nameof(StepDistance), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Step Distance", "Additional distance for next levels", "Parameters");

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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var price = candle.ClosePrice;

		// Check take profit for long grid
		if (_buyVolume > 0 && price >= _buyAvgPrice + TakeProfit)
		{
			SellMarket();
			ResetState();
			return;
		}

		// Check take profit for short grid
		if (_sellVolume > 0 && price <= _sellAvgPrice - TakeProfit)
		{
			BuyMarket();
			ResetState();
			return;
		}

		// Place grid orders
		if (_buyVolume > 0 && !_hasBuyPending)
		{
			var level = _buyAvgPrice - (GridDistance + StepDistance * _buyCount);
			if (level > 0)
			{
				BuyLimit(level);
				_hasBuyPending = true;
			}
		}
		else if (_sellVolume > 0 && !_hasSellPending)
		{
			var level = _sellAvgPrice + (GridDistance + StepDistance * _sellCount);
			SellLimit(level);
			_hasSellPending = true;
		}
		else if (_buyVolume == 0 && _sellVolume == 0)
		{
			if (!_hasBuyPending)
			{
				var buyPrice = price - StartOffset;
				if (buyPrice > 0)
				{
					BuyLimit(buyPrice);
					_hasBuyPending = true;
				}
			}

			if (!_hasSellPending)
			{
				var sellPrice = price + StartOffset;
				SellLimit(sellPrice);
				_hasSellPending = true;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order.Side == Sides.Buy)
		{
			_buyAvgPrice = (_buyAvgPrice * _buyVolume + trade.Trade.Price * trade.Trade.Volume) / (_buyVolume + trade.Trade.Volume);
			_buyVolume += trade.Trade.Volume;
			_buyCount++;
			_hasBuyPending = false;
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			_sellAvgPrice = (_sellAvgPrice * _sellVolume + trade.Trade.Price * trade.Trade.Volume) / (_sellVolume + trade.Trade.Volume);
			_sellVolume += trade.Trade.Volume;
			_sellCount++;
			_hasSellPending = false;
		}
	}

	private void ResetState()
	{
		_buyAvgPrice = _sellAvgPrice = 0;
		_buyVolume = _sellVolume = 0;
		_buyCount = _sellCount = 0;
		_hasBuyPending = _hasSellPending = false;
	}
}
