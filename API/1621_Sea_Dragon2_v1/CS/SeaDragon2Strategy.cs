using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptation of the Sea Dragon 2 hedging strategy.
/// </summary>
public class SeaDragon2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _maxStop;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _altTakeProfit;
	private readonly StrategyParam<int> _volumeScale;
	private readonly StrategyParam<DataType> _candleType;
	
	private readonly List<(decimal price, decimal volume)> _buyTrades = new();
	private readonly List<(decimal price, decimal volume)> _sellTrades = new();
	private decimal _lastOrderPrice;
	
	private static readonly int[] _sequence = { 1, 1, 2, 3, 6, 9, 14, 22, 33, 48, 82, 111, 122, 164, 185 };
	
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public decimal Step { get => _step.Value; set => _step.Value = value; }
	public decimal MaxStop { get => _maxStop.Value; set => _maxStop.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal AltTakeProfit { get => _altTakeProfit.Value; set => _altTakeProfit.Value = value; }
	public int VolumeScale { get => _volumeScale.Value; set => _volumeScale.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public SeaDragon2Strategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
		.SetDisplay("Volume", "Base order size", "Trading");
		
		_step = Param(nameof(Step), 10m)
		.SetDisplay("Step", "Price step to add orders", "Trading");
		
		_maxStop = Param(nameof(MaxStop), 150m)
		.SetDisplay("Max Stop", "Maximum stop distance", "Risk");
		
		_takeProfit = Param(nameof(TakeProfit), 10m)
		.SetDisplay("Take Profit", "Default take profit", "Trading");
		
		_altTakeProfit = Param(nameof(AltTakeProfit), 2m)
		.SetDisplay("Alt Take Profit", "Take profit when side imbalance exists", "Trading");
		
		_volumeScale = Param(nameof(VolumeScale), 1)
		.SetDisplay("Volume Scale", "Scaling factor for sequence", "Trading");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used", "General");
	}
	
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var price = candle.ClosePrice;
		
		if (_buyTrades.Count == 0 && _sellTrades.Count == 0)
		{
			OpenInitialPair(price);
			return;
		}
		
		var point = Security.PriceStep ?? 1m;
		if (Math.Abs(price - _lastOrderPrice) >= Step * point)
		{
			var side = GetSideToTrade();
			if (side > 0)
			OpenBuySide(price);
			else if (side < 0)
			OpenSellSide(price);
		}
		
		CheckTargets(price);
	}
	
	private void OpenInitialPair(decimal price)
	{
		BuyMarket(Volume);
		SellMarket(Volume);
		
		_buyTrades.Add((price, Volume));
		_sellTrades.Add((price, Volume));
		_lastOrderPrice = price;
	}
	
	private int GetSideToTrade()
	{
		if (_buyTrades.Count > _sellTrades.Count)
		return 1;
		if (_buyTrades.Count < _sellTrades.Count)
		return -1;
		return 0;
	}
	
	private decimal GetScaledVolume(bool isBuy)
	{
		var index = isBuy ? _buyTrades.Count : _sellTrades.Count;
		if (index + 1 >= _sequence.Length)
		index = _sequence.Length - 2;
		
		var volume = _sequence[index + 1];
		
		return VolumeScale switch
		{
			1 => volume / 10m,
			2 => volume / 100m,
			_ => volume
		};
	}
	
	private void OpenBuySide(decimal price)
	{
		SellMarket(Volume);
		var vol = GetScaledVolume(true);
		BuyMarket(vol);
		
		_sellTrades.Add((price, Volume));
		_buyTrades.Add((price, vol));
		_lastOrderPrice = price;
	}
	
	private void OpenSellSide(decimal price)
	{
		BuyMarket(Volume);
		var vol = GetScaledVolume(false);
		SellMarket(vol);
		
		_buyTrades.Add((price, Volume));
		_sellTrades.Add((price, vol));
		_lastOrderPrice = price;
	}
	
	private void CheckTargets(decimal price)
	{
		var point = Security.PriceStep ?? 1m;
		
		var buyVol = TotalVolume(_buyTrades);
		var sellVol = TotalVolume(_sellTrades);
		
		if (buyVol > 0)
		{
			var avgBuy = WeightedAverage(_buyTrades);
			var tp = avgBuy + TakeProfit * point;
			var sl = avgBuy - MaxStop * point;
			
			if (_buyTrades.Count > _sellTrades.Count)
			tp = avgBuy + AltTakeProfit * point;
			
			if (price >= tp || price <= sl)
			{
				SellMarket(buyVol);
				_buyTrades.Clear();
			}
		}
		
		if (sellVol > 0)
		{
			var avgSell = WeightedAverage(_sellTrades);
			var tp = avgSell - TakeProfit * point;
			var sl = avgSell + MaxStop * point;
			
			if (_sellTrades.Count > _buyTrades.Count)
			tp = avgSell - AltTakeProfit * point;
			
			if (price <= tp || price >= sl)
			{
				BuyMarket(sellVol);
				_sellTrades.Clear();
			}
		}
	}
	
	private static decimal TotalVolume(List<(decimal price, decimal volume)> trades)
	{
		decimal total = 0m;
		foreach (var (_, vol) in trades)
		total += vol;
		return total;
	}
	
	private static decimal WeightedAverage(List<(decimal price, decimal volume)> trades)
	{
		decimal sumPrice = 0m;
		decimal sumVol = 0m;
		foreach (var (p, v) in trades)
		{
			sumPrice += p * v;
			sumVol += v;
		}
		return sumVol > 0m ? sumPrice / sumVol : 0m;
	}
}
