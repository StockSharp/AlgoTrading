namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo.Candles;

/// <summary>
/// Virtual Robot grid strategy ported from MetaTrader.
/// </summary>
public class VirtualRobotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _minTakeProfitPips;
	private readonly StrategyParam<decimal> _averageTakeProfitPips;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _realStepper;
	private readonly StrategyParam<int> _virtualStepper;
	private readonly StrategyParam<decimal> _pipStepPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _startingRealOrders;
	private readonly StrategyParam<int> _realAverageThreshold;
	private readonly StrategyParam<bool> _visualMode;

	private ISubscriptionHandler<ICandleMessage> _series;
	private decimal _pointSize;
	private decimal _pipDistance;
	private decimal _stopDistance;
	private decimal _singleTakeDistance;
	private decimal _virtualTakeDistance;
	private decimal _averageTakeDistance;

	private readonly List<PositionEntry> _virtualBuys = new();
	private readonly List<PositionEntry> _virtualSells = new();

	private readonly PositionState _buyState = new();
	private readonly PositionState _sellState = new();

	private decimal? _buyStopPrice;
	private decimal? _buyTakePrice;
	private decimal? _sellStopPrice;
	private decimal? _sellTakePrice;

	private bool _buyStarted;
	private bool _sellStarted;

	public VirtualRobotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
		.SetDisplay("Candle type", "Time-frame used for virtual levels", "General");

		_stopLossPips = Param(nameof(StopLossPips), 400m)
		.SetDisplay("Stop Loss (pips)", "Distance in pips for the protective stop", "Trading")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
		.SetDisplay("Take Profit (pips)", "Target in pips for a single real order", "Trading")
		.SetCanOptimize(true);

		_minTakeProfitPips = Param(nameof(MinTakeProfitPips), 15m)
		.SetDisplay("Minimum TP (pips)", "Profit in pips required to close the first virtual order", "Virtual")
		.SetCanOptimize(true);

		_averageTakeProfitPips = Param(nameof(AverageTakeProfitPips), 10m)
		.SetDisplay("Average TP (pips)", "Profit in pips applied to the averaged basket", "Virtual")
		.SetCanOptimize(true);

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
		.SetDisplay("Base volume", "Initial trade size", "Money")
		.SetCanOptimize(true);

		_maxVolume = Param(nameof(MaxVolume), 2m)
		.SetDisplay("Max volume", "Upper bound for any order size", "Money")
		.SetCanOptimize(true);

		_multiplier = Param(nameof(Multiplier), 1.5m)
		.SetDisplay("Multiplier", "Lot multiplier applied when averaging", "Money")
		.SetCanOptimize(true);

		_realStepper = Param(nameof(RealStepper), 1)
		.SetDisplay("Real stepper", "Number of filled orders before applying the multiplier", "Trading")
		.SetCanOptimize(true);

		_virtualStepper = Param(nameof(VirtualStepper), 2)
		.SetDisplay("Virtual stepper", "Virtual orders filled with the base volume before scaling", "Virtual")
		.SetCanOptimize(true);

		_pipStepPips = Param(nameof(PipStepPips), 18m)
		.SetDisplay("Pip step", "Minimum adverse excursion between grid orders (pips)", "Virtual")
		.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 16)
		.SetDisplay("Max trades", "Maximum number of real orders per side", "Trading");

		_startingRealOrders = Param(nameof(StartingRealOrders), 6)
		.SetDisplay("Virtual threshold", "Number of virtual orders required before sending the first real trade", "Virtual");

		_realAverageThreshold = Param(nameof(RealAverageThreshold), 2)
		.SetDisplay("Real average", "Real order count required to switch to true average price", "Trading");

		_visualMode = Param(nameof(VisualMode), true)
		.SetDisplay("Visual mode", "Retained for compatibility with the MT4 template", "Misc");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal MinTakeProfitPips
	{
		get => _minTakeProfitPips.Value;
		set => _minTakeProfitPips.Value = value;
	}

	public decimal AverageTakeProfitPips
	{
		get => _averageTakeProfitPips.Value;
		set => _averageTakeProfitPips.Value = value;
	}

	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	public int RealStepper
	{
		get => _realStepper.Value;
		set => _realStepper.Value = value;
	}

	public int VirtualStepper
	{
		get => _virtualStepper.Value;
		set => _virtualStepper.Value = value;
	}

	public decimal PipStepPips
	{
		get => _pipStepPips.Value;
		set => _pipStepPips.Value = value;
	}

	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	public int StartingRealOrders
	{
		get => _startingRealOrders.Value;
		set => _startingRealOrders.Value = value;
	}

	public int RealAverageThreshold
	{
		get => _realAverageThreshold.Value;
		set => _realAverageThreshold.Value = value;
	}

	public bool VisualMode
	{
		get => _visualMode.Value;
		set => _visualMode.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = GetPointSize();
		_pipDistance = PipStepPips * _pointSize;
		_stopDistance = StopLossPips > 0m ? StopLossPips * _pointSize : 0m;
		_singleTakeDistance = TakeProfitPips > 0m ? TakeProfitPips * _pointSize : 0m;
		_virtualTakeDistance = MinTakeProfitPips > 0m ? MinTakeProfitPips * _pointSize : 0m;
		_averageTakeDistance = AverageTakeProfitPips > 0m ? AverageTakeProfitPips * _pointSize : 0m;

		_series = SubscribeCandles(CandleType);
		_series
		.Bind(ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = candle.ClosePrice;
		var open = candle.OpenPrice;

		UpdateVirtualTakeProfits(price);
		UpdateVirtualOrders(open, price);
		ProcessLongSide(price);
		ProcessShortSide(price);
		CheckStopsAndTakes(price);
	}

	private void UpdateVirtualOrders(decimal openPrice, decimal closePrice)
	{
		if (_pipDistance <= 0m)
		return;

		// Build the virtual buy ladder following candle direction and spacing rules.
		if ((_virtualBuys.Count == 0 && closePrice > openPrice) ||
		(_virtualBuys.Count > 0 && _virtualBuys[^1].Price - closePrice > _pipDistance))
		{
			var volume = CalculateVirtualVolume(_virtualBuys);
			if (volume > 0m)
			_virtualBuys.Add(new PositionEntry(closePrice, volume));
		}

		// Build the virtual sell ladder symmetrically.
		if ((_virtualSells.Count == 0 && closePrice < openPrice) ||
		(_virtualSells.Count > 0 && closePrice - _virtualSells[^1].Price > _pipDistance))
		{
			var volume = CalculateVirtualVolume(_virtualSells);
			if (volume > 0m)
			_virtualSells.Add(new PositionEntry(closePrice, volume));
		}
	}

	private void ProcessLongSide(decimal price)
	{
		if (_buyState.Count == 0)
		{
			if (_buyStarted)
			{
				ResetVirtualBuys();
				_buyStarted = false;
			}
		}

		var needNewOrder = (_virtualBuys.Count >= StartingRealOrders && _buyState.Count == 0) ||
		(_buyState.Count > 0 && ShouldOpenNext(true, price));

		if (!needNewOrder)
		return;

		var volume = CalculateNextRealVolume(true, price);
		if (volume <= 0m)
		return;

		BuyMarket(volume);
		_buyState.Add(price, volume);
		_buyStarted = true;

		UpdateProtectionLevels(true);
	}

	private void ProcessShortSide(decimal price)
	{
		if (_sellState.Count == 0)
		{
			if (_sellStarted)
			{
				ResetVirtualSells();
				_sellStarted = false;
			}
		}

		var needNewOrder = (_virtualSells.Count >= StartingRealOrders && _sellState.Count == 0) ||
		(_sellState.Count > 0 && ShouldOpenNext(false, price));

		if (!needNewOrder)
		return;

		var volume = CalculateNextRealVolume(false, price);
		if (volume <= 0m)
		return;

		SellMarket(volume);
		_sellState.Add(price, volume);
		_sellStarted = true;

		UpdateProtectionLevels(false);
	}

	private void UpdateVirtualTakeProfits(decimal price)
	{
		if (_virtualTakeDistance > 0m)
		{
			if (_virtualBuys.Count == 1 && price - _virtualBuys[0].Price > _virtualTakeDistance)
			{
				CloseLongPositions();
				ResetVirtualBuys();
			}

			if (_virtualSells.Count == 1 && _virtualSells[0].Price - price > _virtualTakeDistance)
			{
				CloseShortPositions();
				ResetVirtualSells();
			}
		}

		if (_averageTakeDistance > 0m)
		{
			if (_virtualBuys.Count > 1)
			{
				var avgPrice = GetVirtualAveragePrice(true);
				if (avgPrice > 0m && price > avgPrice + _averageTakeDistance)
				{
					CloseLongPositions();
					ResetVirtualBuys();
				}
			}

			if (_virtualSells.Count > 1)
			{
				var avgPrice = GetVirtualAveragePrice(false);
				if (avgPrice > 0m && price < avgPrice - _averageTakeDistance)
				{
					CloseShortPositions();
					ResetVirtualSells();
				}
			}
		}
	}

	private bool ShouldOpenNext(bool isBuy, decimal price)
	{
		if (_pipDistance <= 0m)
		return false;

		var state = isBuy ? _buyState : _sellState;
		if (state.Count >= MaxTrades)
		return false;

		var lastPrice = state.LastPrice;
		if (lastPrice == 0m)
		return false;

		var diff = isBuy ? lastPrice - price : price - lastPrice;
		return diff > _pipDistance;
	}

	private decimal CalculateNextRealVolume(bool isBuy, decimal price)
	{
		var baseVolume = AdjustVolume(BaseVolume);
		if (baseVolume <= 0m)
		return 0m;

		var state = isBuy ? _buyState : _sellState;
		if (state.Count == 0)
		return LimitVolume(baseVolume);

		if (state.Count < RealStepper)
		return LimitVolume(baseVolume);

		var control = isBuy ? state.LastPrice - price : price - state.LastPrice;
		if (control <= 0m || _pipDistance <= 0m)
		return 0m;

		var factor = control / _pipDistance;
		var nextVolume = state.LastVolume * Multiplier * factor;
		nextVolume = AdjustVolume(nextVolume);
		return LimitVolume(nextVolume);
	}

	private decimal CalculateVirtualVolume(List<PositionEntry> entries)
	{
		var baseVolume = AdjustVolume(BaseVolume);
		if (baseVolume <= 0m)
		return 0m;

		if (entries.Count < VirtualStepper)
		return LimitVolume(baseVolume);

		var lastVolume = entries[^1].Volume;
		var nextVolume = AdjustVolume(lastVolume * Multiplier);
		return LimitVolume(nextVolume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		volume = step * Math.Round(volume / step, MidpointRounding.AwayFromZero);

		var min = security.MinVolume ?? 0m;
		if (min > 0m && volume < min)
		return 0m;

		var max = security.MaxVolume ?? 0m;
		if (max > 0m && volume > max)
		volume = max.Value;

		return volume;
	}

	private decimal LimitVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var max = MaxVolume;
		if (max > 0m && volume > max)
		volume = max;

		var security = Security;
		var secMax = security?.MaxVolume ?? 0m;
		if (secMax > 0m && volume > secMax)
		volume = secMax.Value;

		return volume;
	}

	private void UpdateProtectionLevels(bool isBuy)
	{
		if (isBuy)
		{
			if (_buyState.Count == 0)
			{
				_buyStopPrice = null;
				_buyTakePrice = null;
				return;
			}

			var reference = GetProtectionReferencePrice(true);
			_buyTakePrice = null;
			_buyStopPrice = null;

			if (_buyState.Count > 1 && _averageTakeDistance > 0m)
			_buyTakePrice = reference + _averageTakeDistance;
			else if (_singleTakeDistance > 0m)
			_buyTakePrice = _buyState.LastPrice + _singleTakeDistance;

			if (_stopDistance > 0m)
			_buyStopPrice = _buyState.LastPrice - _stopDistance;
		}
		else
		{
			if (_sellState.Count == 0)
			{
				_sellStopPrice = null;
				_sellTakePrice = null;
				return;
			}

			var reference = GetProtectionReferencePrice(false);
			_sellTakePrice = null;
			_sellStopPrice = null;

			if (_sellState.Count > 1 && _averageTakeDistance > 0m)
			_sellTakePrice = reference - _averageTakeDistance;
			else if (_singleTakeDistance > 0m)
			_sellTakePrice = _sellState.LastPrice - _singleTakeDistance;

			if (_stopDistance > 0m)
			_sellStopPrice = _sellState.LastPrice + _stopDistance;
		}
	}

	private decimal GetProtectionReferencePrice(bool isBuy)
	{
		var realCount = isBuy ? _buyState.Count : _sellState.Count;
		if (realCount >= RealAverageThreshold)
		{
			var average = isBuy ? _buyState.GetAveragePrice() : _sellState.GetAveragePrice();
			if (average > 0m)
			return average;
		}

		var virtualAverage = GetVirtualAveragePrice(isBuy);
		if (virtualAverage > 0m)
		return virtualAverage;

		return isBuy ? _buyState.LastPrice : _sellState.LastPrice;
	}

	private void CheckStopsAndTakes(decimal price)
	{
		if (_buyState.Count > 0)
		{
			if (_buyStopPrice.HasValue && price <= _buyStopPrice.Value)
			{
				CloseLongPositions();
				ResetVirtualBuys();
			}
			else if (_buyTakePrice.HasValue && price >= _buyTakePrice.Value)
			{
				CloseLongPositions();
				ResetVirtualBuys();
			}
		}

		if (_sellState.Count > 0)
		{
			if (_sellStopPrice.HasValue && price >= _sellStopPrice.Value)
			{
				CloseShortPositions();
				ResetVirtualSells();
			}
			else if (_sellTakePrice.HasValue && price <= _sellTakePrice.Value)
			{
				CloseShortPositions();
				ResetVirtualSells();
			}
		}
	}

	private void CloseLongPositions()
	{
		if (Position <= 0m)
		return;

		SellMarket(Position);
		_buyState.Reset();
		_buyStopPrice = null;
		_buyTakePrice = null;
		_buyStarted = false;
	}

	private void CloseShortPositions()
	{
		if (Position >= 0m)
		return;

		BuyMarket(Math.Abs(Position));
		_sellState.Reset();
		_sellStopPrice = null;
		_sellTakePrice = null;
		_sellStarted = false;
	}

	private void ResetVirtualBuys()
	{
		_virtualBuys.Clear();
	}

	private void ResetVirtualSells()
	{
		_virtualSells.Clear();
	}

	private decimal GetVirtualAveragePrice(bool isBuy)
	{
		var entries = isBuy ? _virtualBuys : _virtualSells;
		if (entries.Count == 0)
		return 0m;

		decimal totalVolume = 0m;
		decimal weightedSum = 0m;

		foreach (var entry in entries)
		{
			weightedSum += entry.Price * entry.Volume;
			totalVolume += entry.Volume;
		}

		return totalVolume > 0m ? weightedSum / totalVolume : 0m;
	}

	private decimal GetPointSize()
	{
		var security = Security;
		if (security == null)
		return 1m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		step = security.Step ?? 0m;
		if (step <= 0m)
		return 1m;

		var value = step;
		var digits = 0;
		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
		step *= 10m;

		return step;
	}

	private readonly struct PositionEntry
	{
		public PositionEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }
		public decimal Volume { get; }
	}

	private sealed class PositionState
	{
		public decimal LastPrice { get; private set; }
		public decimal LastVolume { get; private set; }
		public decimal TotalVolume { get; private set; }
		public decimal WeightedSum { get; private set; }
		public int Count { get; private set; }

		public void Add(decimal price, decimal volume)
		{
			LastPrice = price;
			LastVolume = volume;
			TotalVolume += volume;
			WeightedSum += price * volume;
			Count++;
		}

		public decimal GetAveragePrice()
		{
			return TotalVolume > 0m ? WeightedSum / TotalVolume : 0m;
		}

		public void Reset()
		{
			LastPrice = 0m;
			LastVolume = 0m;
			TotalVolume = 0m;
			WeightedSum = 0m;
			Count = 0;
		}
	}
}

