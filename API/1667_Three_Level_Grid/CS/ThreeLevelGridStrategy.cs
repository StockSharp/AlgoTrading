using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy that places symmetric orders with up to three take profit ranks.
/// Each filled order places an opposite limit order to capture profit.
/// </summary>
public class ThreeLevelGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gridSize;
	private readonly StrategyParam<int> _levels;
	private readonly StrategyParam<decimal> _baseTakeProfit;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _enableRank1;
	private readonly StrategyParam<bool> _enableRank2;
	private readonly StrategyParam<bool> _enableRank3;
	private readonly StrategyParam<bool> _allowLongs;
	private readonly StrategyParam<bool> _allowShorts;
	private readonly StrategyParam<DataType> _candleType;
	
	private readonly Dictionary<long, decimal> _takeProfits = new();
	private bool _gridPlaced;
	
	/// <summary>
	/// Distance between grid levels.
	/// </summary>
	public decimal GridSize { get => _gridSize.Value; set => _gridSize.Value = value; }
	
	/// <summary>
	/// Number of grid levels on each side.
	/// </summary>
	public int Levels { get => _levels.Value; set => _levels.Value = value; }
	
	/// <summary>
	/// Base take profit distance.
	/// </summary>
	public decimal BaseTakeProfit { get => _baseTakeProfit.Value; set => _baseTakeProfit.Value = value; }
	
	/// <summary>
	/// Volume for each entry order.
	/// </summary>
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	
	/// <summary>
	/// Enable first take profit rank.
	/// </summary>
	public bool EnableRank1 { get => _enableRank1.Value; set => _enableRank1.Value = value; }
	
	/// <summary>
	/// Enable second take profit rank.
	/// </summary>
	public bool EnableRank2 { get => _enableRank2.Value; set => _enableRank2.Value = value; }
	
	/// <summary>
	/// Enable third take profit rank.
	/// </summary>
	public bool EnableRank3 { get => _enableRank3.Value; set => _enableRank3.Value = value; }
	
	/// <summary>
	/// Allow long side orders.
	/// </summary>
	public bool AllowLongs { get => _allowLongs.Value; set => _allowLongs.Value = value; }
	
	/// <summary>
	/// Allow short side orders.
	/// </summary>
	public bool AllowShorts { get => _allowShorts.Value; set => _allowShorts.Value = value; }
	
	/// <summary>
	/// Candle type used for initial price.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public ThreeLevelGridStrategy()
	{
		_gridSize = Param(nameof(GridSize), 20m)
		.SetDisplay("Grid Size", "Distance between grid levels", "Grid")
		.SetGreaterThanZero();
		
		_levels = Param(nameof(Levels), 5)
		.SetDisplay("Levels", "Number of levels on each side", "Grid")
		.SetGreaterThanZero();
		
		_baseTakeProfit = Param(nameof(BaseTakeProfit), 50m)
		.SetDisplay("Base Take Profit", "Base profit distance", "Profit")
		.SetGreaterThanZero();
		
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Volume per grid order", "General")
		.SetGreaterThanZero();
		
		_enableRank1 = Param(nameof(EnableRank1), true)
		.SetDisplay("Enable Rank1", "Use first take profit rank", "Profit");
		
		_enableRank2 = Param(nameof(EnableRank2), false)
		.SetDisplay("Enable Rank2", "Use second take profit rank", "Profit");
		
		_enableRank3 = Param(nameof(EnableRank3), false)
		.SetDisplay("Enable Rank3", "Use third take profit rank", "Profit");
		
		_allowLongs = Param(nameof(AllowLongs), true)
		.SetDisplay("Allow Longs", "Enable long orders", "General");
		
		_allowShorts = Param(nameof(AllowShorts), true)
		.SetDisplay("Allow Shorts", "Enable short orders", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for initialization", "General");
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
		_takeProfits.Clear();
		_gridPlaced = false;
		}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
		}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		if (_gridPlaced)
			return;
		
		PlaceGrid(candle.ClosePrice);
		_gridPlaced = true;
		}
	
	private void PlaceGrid(decimal centerPrice)
	{
		var step = GridSize;
		
		for (var i = 1; i <= Levels; i++)
		{
			var buyPrice = centerPrice - step * i;
			var sellPrice = centerPrice + step * i;
			
			if (AllowLongs)
			PlaceRankOrders(true, buyPrice);
			
			if (AllowShorts)
			PlaceRankOrders(false, sellPrice);
			}
		}
	
	private void PlaceRankOrders(bool isLong, decimal price)
	{
		if (EnableRank1)
		RegisterEntry(isLong, price, BaseTakeProfit);
		
		if (EnableRank2)
		RegisterEntry(isLong, price, BaseTakeProfit + GridSize);
		
		if (EnableRank3)
		RegisterEntry(isLong, price, BaseTakeProfit + GridSize * 2m);
		}
	
	private void RegisterEntry(bool isLong, decimal price, decimal tpOffset)
	{
		var order = isLong
		? BuyLimit(OrderVolume, price)
		: SellLimit(OrderVolume, price);
		
		if (order != null)
		_takeProfits[order.Id] = isLong ? price + tpOffset : price - tpOffset;
		}
	
	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);
		
		if (!_takeProfits.TryGetValue(trade.Order.Id, out var target))
			return;
		
		var volume = trade.Order.Volume;
		
		if (trade.Order.Direction == Sides.Buy)
		SellLimit(volume, target);
		else
		BuyLimit(volume, target);
		
		_takeProfits.Remove(trade.Order.Id);
		}
	}
