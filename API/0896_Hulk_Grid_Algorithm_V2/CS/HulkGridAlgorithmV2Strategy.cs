using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy that places ten layered buy limit orders around a mid price.
/// </summary>
public class HulkGridAlgorithmV2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _midPrice;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _gridStep;
	private readonly StrategyParam<decimal> _lot;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _grid1;
	private decimal _grid2;
	private decimal _grid3;
	private decimal _grid4;
	private decimal _grid5;
	private decimal _grid6;
	private decimal _grid7;
	private decimal _grid8;
	private decimal _grid9;
	private decimal _grid10;
	
	private decimal _stopLevel;
	private decimal _takeProfitLevel;
	private bool _ordersPlaced;
	
	/// <summary>
	/// Mid price around which the grid is built.
	/// </summary>
	public decimal MidPrice { get => _midPrice.Value; set => _midPrice.Value = value; }
	
	/// <summary>
	/// Stop-loss percentage below the lowest grid.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	
	/// <summary>
	/// Take-profit percentage above the upper grid.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	
	/// <summary>
	/// Distance between grid levels.
	/// </summary>
	public decimal GridStep { get => _gridStep.Value; set => _gridStep.Value = value; }
	
	/// <summary>
	/// Base cash amount used to calculate order volume.
	/// </summary>
	public decimal Lot { get => _lot.Value; set => _lot.Value = value; }
	
	/// <summary>
	/// Candle type for price updates.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public HulkGridAlgorithmV2Strategy()
	{
		_midPrice = Param(nameof(MidPrice), 0m)
		.SetDisplay("Mid Price", "Mid price", "Grid");
		
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetDisplay("Stop Loss %", "Stop loss percent", "Grid");
		
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
		.SetDisplay("Take Profit %", "Take profit percent", "Grid");
		
		_gridStep = Param(nameof(GridStep), 200m)
		.SetDisplay("Grid Step", "Grid step", "Grid");
		
		_lot = Param(nameof(Lot), 50m)
		.SetDisplay("Lot", "Base cash per level", "Risk");
		
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
		_ordersPlaced = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		CalculateGrid();
		StartProtection();
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}
	
	private void CalculateGrid()
	{
		var mid = MidPrice;
		var step = GridStep;
		
		_grid1 = mid;
		_grid2 = mid + step;
		_grid3 = mid + step * 2m;
		_grid4 = mid + step * 3m;
		_grid5 = mid + step * 4m;
		_grid6 = mid - step;
		_grid7 = mid - step * 2m;
		_grid8 = mid - step * 3m;
		_grid9 = mid - step * 4m;
		_grid10 = mid - step * 5m;
		
		_stopLevel = _grid10 - _grid10 * StopLossPercent / 100m;
		_takeProfitLevel = _grid5 + _grid5 * TakeProfitPercent / 100m;
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (Position == 0 && !_ordersPlaced)
		PlaceGridOrders();
		
		if (candle.ClosePrice < _grid10 || candle.ClosePrice < MidPrice)
		CancelGrid();
		
		if (candle.ClosePrice <= _stopLevel)
		CloseGridPosition();
		else if (candle.ClosePrice >= _takeProfitLevel)
		CloseGridPosition();
	}
	
	private void PlaceGridOrders()
	{
		var lot = Lot;
		
		BuyLimit(lot * 1m / _grid10, _grid10);
		BuyLimit(lot * 2m / _grid9, _grid9);
		BuyLimit(lot * 3m / _grid8, _grid8);
		BuyLimit(lot * 4m / _grid7, _grid7);
		BuyLimit(lot * 5m / _grid6, _grid6);
		BuyLimit(lot * 6m / _grid1, _grid1);
		BuyLimit(lot * 7m / _grid2, _grid2);
		BuyLimit(lot * 8m / _grid3, _grid3);
		BuyLimit(lot * 9m / _grid4, _grid4);
		BuyLimit(lot * 10m / _grid5, _grid5);
		
		_ordersPlaced = true;
	}
	
	private void CancelGrid()
	{
		if (!_ordersPlaced)
		return;
		
		CancelActiveOrders();
		_ordersPlaced = false;
	}
	
	private void CloseGridPosition()
	{
		CancelGrid();
		
		if (Position > 0)
		SellMarket(Position);
		else if (Position < 0)
		BuyMarket(Math.Abs(Position));
	}
}

