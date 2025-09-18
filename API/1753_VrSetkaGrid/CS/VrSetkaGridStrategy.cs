
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy converted from MetaTrader "VR---SETKAa3hM".
/// Uses percentage deviation from daily range to open buy or sell grids
/// and optionally applies a martingale volume multiplier.
/// </summary>
public class VrSetkaGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _distance;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _correction;
	private readonly StrategyParam<decimal> _signalPercent;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Price distance in points between grid levels.
	/// </summary>
	public decimal Distance
	{
		get => _distance.Value;
		set => _distance.Value = value;
	}

	/// <summary>
	/// Profit target for the first order in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Extra profit added to average price when multiple orders exist.
	/// </summary>
	public decimal Correction
	{
		get => _correction.Value;
		set => _correction.Value = value;
	}

	/// <summary>
	/// Percentage threshold for signal generation.
	/// </summary>
	public decimal SignalPercent
	{
		get => _signalPercent.Value;
		set => _signalPercent.Value = value;
	}

	/// <summary>
	/// Enable martingale multiplier for order volume.
	/// </summary>
	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	// Fields for tracking state of buy and sell grids
	private decimal _prevOpen;
	private decimal _prevClose;

	private int _buyCount;
	private int _sellCount;
	private decimal _buyVolume;
	private decimal _sellVolume;
	private decimal _buySumPrice;
	private decimal _sellSumPrice;
	private decimal _lastBuyPrice = decimal.MaxValue;
	private decimal _lastSellPrice = decimal.MinValue;
	private decimal _targetSell;
	private decimal _targetBuy;

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public VrSetkaGridStrategy()
	{
		_distance = Param(nameof(Distance), 300m)
			.SetDisplay("Distance", "Grid step in points", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100m, 500m, 100m);

		_takeProfit = Param(nameof(TakeProfit), 300m)
			.SetDisplay("Take Profit", "Target in points for initial order", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100m, 500m, 100m);

		_correction = Param(nameof(Correction), 50m)
			.SetDisplay("Correction", "Additional profit in points", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_signalPercent = Param(nameof(SignalPercent), 1.3m)
			.SetDisplay("Signal %", "Percent deviation from daily range", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Martingale", "Use martingale volume multiplier", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Base candle series", "General");
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

		_prevOpen = 0m;
		_prevClose = 0m;
		ResetBuy();
		ResetSell();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevOpen != 0m && _prevClose != 0m)
		{
			var high = candle.HighPrice;
			var low = candle.LowPrice;

			var x = 0m;
			var y = 0m;

			if (candle.ClosePrice > low && low != 0m)
				x = candle.ClosePrice * 100m / low - 100m;

			if (candle.ClosePrice < high && high != 0m)
				y = candle.ClosePrice * 100m / high - 100m;

			var prevBull = _prevClose > _prevOpen;
			var buySignal = (SignalPercent * -1m <= y) && prevBull;
			var sellSignal = (SignalPercent <= x) && !prevBull;

			var price = candle.ClosePrice;

			if (buySignal && _sellCount == 0)
			{
				if (_buyCount == 0)
				{
					var volume = GetVolume(_buyCount);
					BuyMarket(volume);
					_buyCount = 1;
					_buyVolume = volume;
					_buySumPrice = price * volume;
					_lastBuyPrice = price;
					_targetSell = price + TakeProfit;
				}
				else if (price < _lastBuyPrice - Distance)
				{
					var volume = GetVolume(_buyCount);
					BuyMarket(volume);
					_buyCount++;
					_buyVolume += volume;
					_buySumPrice += price * volume;
					_lastBuyPrice = price;
					var average = _buySumPrice / _buyVolume;
					_targetSell = average + Correction;
				}
			}
			else if (sellSignal && _buyCount == 0)
			{
				if (_sellCount == 0)
				{
					var volume = GetVolume(_sellCount);
					SellMarket(volume);
					_sellCount = 1;
					_sellVolume = volume;
					_sellSumPrice = price * volume;
					_lastSellPrice = price;
					_targetBuy = price - TakeProfit;
				}
				else if (price > _lastSellPrice + Distance)
				{
					var volume = GetVolume(_sellCount);
					SellMarket(volume);
					_sellCount++;
					_sellVolume += volume;
					_sellSumPrice += price * volume;
					_lastSellPrice = price;
					var average = _sellSumPrice / _sellVolume;
					_targetBuy = average - Correction;
				}
			}

			if (_buyCount > 0 && price >= _targetSell)
			{
				SellMarket(_buyVolume);
				ResetBuy();
			}
			else if (_sellCount > 0 && price <= _targetBuy)
			{
				BuyMarket(_sellVolume);
				ResetSell();
			}
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}

	private decimal GetVolume(int count)
	{
		var volume = Volume;
		return UseMartingale ? volume * (count + 1) : volume;
	}

	private void ResetBuy()
	{
		_buyCount = 0;
		_buyVolume = 0m;
		_buySumPrice = 0m;
		_lastBuyPrice = decimal.MaxValue;
		_targetSell = 0m;
	}

	private void ResetSell()
	{
		_sellCount = 0;
		_sellVolume = 0m;
		_sellSumPrice = 0m;
		_lastSellPrice = decimal.MinValue;
		_targetBuy = 0m;
	}
}
