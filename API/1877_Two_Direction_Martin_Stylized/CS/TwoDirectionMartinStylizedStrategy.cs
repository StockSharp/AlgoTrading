using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale-style hedging strategy placing buy and sell positions with adaptive limits.
/// </summary>
public class TwoDirectionMartinStylizedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _volumeToOrder;
	private readonly StrategyParam<decimal> _volumeLimit;
	private readonly StrategyParam<decimal> _percentSame;
	private readonly StrategyParam<DataType> _candleType;

	private Order? _buyLimitOrder;
	private Order? _sellLimitOrder;

	private decimal _currentBid;
	private decimal _currentAsk;
	private decimal _spread;
	private decimal _tp;

	private decimal _buyLimitVolume;
	private decimal _sellLimitVolume;
	private decimal _buyLimitPrice;
	private decimal _sellLimitPrice;

	/// <summary>
	/// Take profit percentage from current price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Minimal order volume.
	/// </summary>
	public decimal VolumeToOrder
	{
		get => _volumeToOrder.Value;
		set => _volumeToOrder.Value = value;
	}

	/// <summary>
	/// Max volume for a single order.
	/// </summary>
	public decimal VolumeLimitOrder
	{
		get => _volumeLimit.Value;
		set => _volumeLimit.Value = value;
	}

	/// <summary>
	/// Percentage for dominant side volume.
	/// </summary>
	public decimal PercentSame
	{
		get => _percentSame.Value;
		set => _percentSame.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TwoDirectionMartinStylizedStrategy()
	{
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.35m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit as percent of price", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.05m);

		_volumeToOrder = Param(nameof(VolumeToOrder), 0.10m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Minimal volume to send", "General");

		_volumeLimit = Param(nameof(VolumeLimitOrder), 0.75m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Limit", "Maximum volume per order", "General");

		_percentSame = Param(nameof(PercentSame), 75m)
			.SetGreaterThanZero()
			.SetDisplay("Same Side %", "Percent of dominant side", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_buyLimitOrder = null;
		_sellLimitOrder = null;
		_currentBid = 0m;
		_currentAsk = 0m;
		_spread = 0m;
		_tp = 0m;
		_buyLimitVolume = 0m;
		_sellLimitVolume = 0m;
		_buyLimitPrice = 0m;
		_sellLimitPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_currentBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_currentAsk = (decimal)ask;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_currentBid == 0m || _currentAsk == 0m)
			return;

		DataForSymbol();

		if (_buyLimitOrder is null && _sellLimitOrder is null)
		{
			InitializeOrders();
			return;
		}

		if (_buyLimitOrder is null || _sellLimitOrder is null)
		{
			ShrinkRange();
			return;
		}

		if ((_sellLimitPrice > _currentAsk + 2m * _tp && _buyLimitPrice < _currentBid - _tp && _sellLimitVolume <= _buyLimitVolume) ||
		(_buyLimitPrice < _currentBid - 2m * _tp && _sellLimitPrice > _currentAsk + _tp && _sellLimitVolume >= _buyLimitVolume))
			ShrinkRange();
	}

	private void DataForSymbol()
	{
		_spread = _currentAsk - _currentBid;
		_tp = Math.Max(TakeProfitPercent * _currentAsk / 100m, _spread);
	}

	private void InitializeOrders()
	{
		AddSell(VolumeToOrder);
		var buyPrice = _currentBid - _tp;
		BuyLimitOrder(VolumeToOrder, buyPrice);
		_buyLimitVolume = VolumeToOrder;
		_buyLimitPrice = buyPrice;

		AddBuy(VolumeToOrder);
		var sellPrice = _currentAsk + _tp;
		SellLimitOrder(VolumeToOrder, sellPrice);
		_sellLimitVolume = VolumeToOrder;
		_sellLimitPrice = sellPrice;
	}

	private void ShrinkRange()
	{
		var oldLoss = (_sellLimitPrice - _currentBid) * _sellLimitVolume +
		(_currentAsk - _buyLimitPrice) * _buyLimitVolume;
		var newTotalVol = Math.Max(2m * VolumeToOrder,
		Math.Ceiling(((oldLoss + _spread) / _tp) * 100m) / 100m);

		decimal newSellLimitVol;
		decimal newBuyLimitVol;
		if (_sellLimitVolume >= _buyLimitVolume)
		{
			newSellLimitVol = Math.Max(VolumeToOrder, Math.Ceiling(newTotalVol * PercentSame) / 100m);
			newBuyLimitVol = Math.Max(VolumeToOrder, Math.Round((newTotalVol - newSellLimitVol) * 100m) / 100m);
		}
		else
		{
			newBuyLimitVol = Math.Max(VolumeToOrder, Math.Ceiling(newTotalVol * PercentSame) / 100m);
			newSellLimitVol = Math.Max(VolumeToOrder, Math.Round((newTotalVol - newBuyLimitVol) * 100m) / 100m);
		}

		var addVolume = (newSellLimitVol - newBuyLimitVol) - (_sellLimitVolume - _buyLimitVolume);

		DeleteOld();

		var buyPrice = _currentBid - _tp;
		BuyLimitOrder(newBuyLimitVol, buyPrice);
		_buyLimitVolume = newBuyLimitVol;
		_buyLimitPrice = buyPrice;

		var sellPrice = _currentAsk + _tp;
		SellLimitOrder(newSellLimitVol, sellPrice);
		_sellLimitVolume = newSellLimitVol;
		_sellLimitPrice = sellPrice;

		if (addVolume > 0.005m)
			AddBuy(addVolume);
		if (addVolume < -0.005m)
			AddSell(-addVolume);
	}

	private void DeleteOld()
	{
		if (_buyLimitOrder != null)
		{
			CancelOrder(_buyLimitOrder);
			_buyLimitOrder = null;
		}
		if (_sellLimitOrder != null)
		{
			CancelOrder(_sellLimitOrder);
			_sellLimitOrder = null;
		}
	}

	private void BuyLimitOrder(decimal volume, decimal price)
	{
		var remaining = volume;
		var p = price;
		while (remaining > VolumeLimitOrder)
		{
			BuyLimitOrderSend(VolumeLimitOrder, p);
			remaining -= VolumeLimitOrder;
			p -= _spread;
		}
		BuyLimitOrderSend(remaining, p);
	}

	private void SellLimitOrder(decimal volume, decimal price)
	{
		var remaining = volume;
		var p = price;
		while (remaining > VolumeLimitOrder)
		{
			SellLimitOrderSend(VolumeLimitOrder, p);
			remaining -= VolumeLimitOrder;
			p += _spread;
		}
		SellLimitOrderSend(remaining, p);
	}

	private void AddBuy(decimal volume)
	{
		var remaining = volume;
		while (remaining > VolumeLimitOrder)
		{
			AddBuySend(VolumeLimitOrder);
			remaining -= VolumeLimitOrder;
		}
		AddBuySend(remaining);
	}

	private void AddSell(decimal volume)
	{
		var remaining = volume;
		while (remaining > VolumeLimitOrder)
		{
			AddSellSend(VolumeLimitOrder);
			remaining -= VolumeLimitOrder;
		}
		AddSellSend(remaining);
	}

	private void BuyLimitOrderSend(decimal volume, decimal price)
	{
		_buyLimitOrder = BuyLimit(volume, price);
	}

	private void SellLimitOrderSend(decimal volume, decimal price)
	{
		_sellLimitOrder = SellLimit(volume, price);
	}

	private void AddBuySend(decimal volume)
	{
		BuyMarket(volume);
	}

	private void AddSellSend(decimal volume)
	{
		SellMarket(volume);
	}
}
