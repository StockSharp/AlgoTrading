
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
/// Moving average grid strategy converted from the MetaTrader MAGrid expert.
/// It manages a symmetric basket of long and short orders around an EMA-based anchor level.
/// </summary>
public class MaGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeTolerance;

	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _gridAmount;
	private readonly StrategyParam<decimal> _distance;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<Order, OrderIntent> _orderIntents = new();

	private ExponentialMovingAverage _ema;
	private int _effectiveGridAmount;
	private int _currentGrid;
	private decimal _nextGridPrice;
	private decimal _lastGridPrice;
	private bool _isGridInitialized;
	private decimal _longExposure;
	private decimal _shortExposure;

	private enum OrderIntent
	{
		OpenLong,
		OpenShort,
		CloseLong,
		CloseShort
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MaGridStrategy"/> class.
	/// </summary>
	public MaGridStrategy()
	{
		_volumeTolerance = Param(nameof(VolumeTolerance), 0.0000001m)
			.SetGreaterOrEqualThanZero()
			.SetDisplay("Volume Tolerance", "Small tolerance applied when balancing grid exposure.", "Risk");

		_maPeriod = Param(nameof(MaPeriod), 48)
		.SetRange(5, 400)
		.SetDisplay("MA Period", "Exponential moving average length", "Grid")
		.SetCanOptimize(true);

		_gridAmount = Param(nameof(GridAmount), 6)
		.SetRange(2, 40)
		.SetDisplay("Grid Amount", "Number of grid steps (will be forced to an even value)", "Grid")
		.SetCanOptimize(true);

		_distance = Param(nameof(Distance), 0.005m)
		.SetGreaterThanZero()
		.SetDisplay("Distance", "Relative spacing between grid levels", "Grid")
		.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume per grid order", "Risk")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle type used by the strategy", "Data");
	}

	/// <summary>
	/// Small tolerance used when comparing accumulated exposure.
	/// </summary>
	public decimal VolumeTolerance
	{
		get => _volumeTolerance.Value;
		set => _volumeTolerance.Value = value;
	}

	/// <summary>
	/// EMA period used for the anchor level.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Total number of grid steps that will be mirrored around the EMA.
	/// </summary>
	public int GridAmount
	{
		get => _gridAmount.Value;
		set => _gridAmount.Value = value;
	}

	/// <summary>
	/// Relative distance between consecutive grid levels.
	/// </summary>
	public decimal Distance
	{
		get => _distance.Value;
		set => _distance.Value = value;
	}

	/// <summary>
	/// Volume submitted with each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for data subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_orderIntents.Clear();
		_ema = null;
		_effectiveGridAmount = 0;
		_currentGrid = 0;
		_nextGridPrice = 0m;
		_lastGridPrice = 0m;
		_isGridInitialized = false;
		_longExposure = 0m;
		_shortExposure = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_effectiveGridAmount = GetEffectiveGridAmount();
		_currentGrid = 0;
		_nextGridPrice = 0m;
		_lastGridPrice = 0m;
		_isGridInitialized = false;
		_longExposure = 0m;
		_shortExposure = 0m;
		_orderIntents.Clear();

		_ema = new ExponentialMovingAverage
		{
			Length = MaPeriod
		};

		SubscribeCandles(CandleType)
		.Bind(_ema, ProcessCandle)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order is not { } order || !_orderIntents.TryGetValue(order, out var intent))
		return;

		var volume = trade.Trade.Volume;

		switch (intent)
		{
		case OrderIntent.OpenLong:
		_longExposure += volume;
		break;
		case OrderIntent.OpenShort:
		_shortExposure += volume;
		break;
		case OrderIntent.CloseLong:
		_longExposure = Math.Max(0m, _longExposure - volume);
		break;
		case OrderIntent.CloseShort:
		_shortExposure = Math.Max(0m, _shortExposure - volume);
		break;
		}

		if (order.Balance <= VolumeTolerance || IsOrderCompleted(order))
		_orderIntents.Remove(order);
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_ema?.IsFormed != true)
		return;

		CleanupCompletedOrders();

		if (!_isGridInitialized)
		{
		InitializeGrid(candle.ClosePrice, emaValue);
		return;
		}

		UpdateGridLevels(emaValue);

		if (_nextGridPrice > 0m && candle.ClosePrice >= _nextGridPrice && _nextGridPrice < decimal.MaxValue)
		{
		_currentGrid++;
		CloseLongExposure();
		OpenShortExposure();
		UpdateGridLevels(emaValue);
		}
		else if (_lastGridPrice > 0m && candle.ClosePrice <= _lastGridPrice && _lastGridPrice > decimal.MinValue)
		{
		_currentGrid--;
		CloseShortExposure();
		OpenLongExposure();
		UpdateGridLevels(emaValue);
		}
	}

	private int GetEffectiveGridAmount()
	{
		var amount = GridAmount;
		if (amount < 2)
		amount = 2;

		if (amount % 2 != 0)
		amount++;

		return amount;
	}

	private void InitializeGrid(decimal closePrice, decimal ema)
	{
		_isGridInitialized = true;
		_currentGrid = DetermineInitialGrid(closePrice, ema);

		var half = _effectiveGridAmount / 2;
		var buyCount = Math.Max(0, half - _currentGrid);
		var sellCount = Math.Max(0, _effectiveGridAmount - buyCount);

		for (var i = 0; i < buyCount; i++)
		OpenLongExposure();

		for (var i = 0; i < sellCount; i++)
		OpenShortExposure();

		UpdateGridLevels(ema);
	}

	private int DetermineInitialGrid(decimal price, decimal ema)
	{
		var half = _effectiveGridAmount / 2;
		var distance = Distance;

		if (price < ema)
		{
		for (var i = 1; i <= half; i++)
		{
		var level = ema * (1m - distance * i);
		if (price > level)
		return 1 - i;
		}

		return -half;
		}

		for (var i = 1; i <= half; i++)
		{
		var level = ema * (1m + distance * i);
		if (price < level)
		return i - 1;
		}

		return half;
	}

	private void UpdateGridLevels(decimal ema)
	{
		var distance = Distance;

		if (_currentGrid < _effectiveGridAmount - 1)
		_nextGridPrice = ema * (1m + distance * (1m + _currentGrid));
		else
		_nextGridPrice = 0m;

		if (_currentGrid > 1 - _effectiveGridAmount)
		_lastGridPrice = ema * (1m - distance * (1m - _currentGrid));
		else
		_lastGridPrice = 0m;

		if (_longExposure <= VolumeTolerance)
		_nextGridPrice = decimal.MaxValue;

		if (_shortExposure <= VolumeTolerance)
		_lastGridPrice = decimal.MinValue;
	}

	private void OpenLongExposure()
	{
		if (OrderVolume <= 0m)
		return;

		RegisterOrder(BuyMarket(OrderVolume), OrderIntent.OpenLong);
	}

	private void OpenShortExposure()
	{
		if (OrderVolume <= 0m)
		return;

		RegisterOrder(SellMarket(OrderVolume), OrderIntent.OpenShort);
	}

	private void CloseLongExposure()
	{
		if (_longExposure <= VolumeTolerance)
		return;

		var volume = Math.Min(OrderVolume, _longExposure);
		if (volume <= VolumeTolerance)
		return;

		RegisterOrder(SellMarket(volume), OrderIntent.CloseLong);
	}

	private void CloseShortExposure()
	{
		if (_shortExposure <= VolumeTolerance)
		return;

		var volume = Math.Min(OrderVolume, _shortExposure);
		if (volume <= VolumeTolerance)
		return;

		RegisterOrder(BuyMarket(volume), OrderIntent.CloseShort);
	}

	private void RegisterOrder(Order order, OrderIntent intent)
	{
		if (order == null)
		return;

		_orderIntents[order] = intent;
	}

	private void CleanupCompletedOrders()
	{
		if (_orderIntents.Count == 0)
		return;

		List<Order>? completed = null;

		foreach (var pair in _orderIntents)
		{
		if (!IsOrderCompleted(pair.Key))
		continue;

		completed ??= new List<Order>();
		completed.Add(pair.Key);
		}

		if (completed == null)
		return;

		foreach (var order in completed)
		_orderIntents.Remove(order);
	}
}

