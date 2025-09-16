using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy opening simultaneous buy and sell orders.
/// Each time take profit is reached on one side the remaining position is closed
/// and the next grid level is opened with increased volume.
/// </summary>
public class BuySellGridStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _buyEntryPrice;
	private decimal _sellEntryPrice;
	private int _gridLevel;
	private bool _hasPositions;
	
	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}
	
	/// <summary>
	/// Initial order volume.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}
	
	/// <summary>
	/// Multiplier for volume on each new grid level.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}
	
	/// <summary>
	/// Maximum number of grid levels.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}
	
	/// <summary>
	/// Candle type used to trigger strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="BuySellGridStrategy"/>.
	/// </summary>
	public BuySellGridStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
		.SetDisplay("Take Profit", "Take profit in price steps", "General")
		.SetGreaterThanZero();
		
		_initialVolume = Param(nameof(InitialVolume), 0.1m)
		.SetDisplay("Initial Volume", "Volume of first orders", "Trading")
		.SetGreaterThanZero();
		
		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
		.SetDisplay("Volume Multiplier", "Multiplier for next grid level", "Trading")
		.SetGreaterThanZero();
		
		_maxTrades = Param(nameof(MaxTrades), 20)
		.SetDisplay("Max Trades", "Maximum number of grid levels", "Trading")
		.SetGreaterThanZero();
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type for processing", "General");
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
		
		if (!_hasPositions)
		{
			OpenGridLevel(candle.ClosePrice);
			return;
		}
		
		var step = Security.PriceStep * TakeProfitPoints;
		
		// Check buy position take profit
		if (Position > 0 && candle.ClosePrice - _buyEntryPrice >= step)
		{
			SellMarket(Math.Abs(Position));
			CloseGrid();
			return;
		}
		
		// Check sell position take profit
		if (Position < 0 && _sellEntryPrice - candle.ClosePrice >= step)
		{
			BuyMarket(Math.Abs(Position));
			CloseGrid();
		}
	}
	
	private void OpenGridLevel(decimal price)
	{
		if (_gridLevel >= MaxTrades)
		return;
		
		var volume = InitialVolume * (decimal)Math.Pow((double)VolumeMultiplier, _gridLevel);
		BuyMarket(volume);
		SellMarket(volume);
		
		_buyEntryPrice = price;
		_sellEntryPrice = price;
		_hasPositions = true;
		_gridLevel++;
	}
	
	private void CloseGrid()
	{
		_buyEntryPrice = 0m;
		_sellEntryPrice = 0m;
		_hasPositions = false;
		
		OpenGridLevel(LastTrade?.Price ?? 0m);
	}
}
