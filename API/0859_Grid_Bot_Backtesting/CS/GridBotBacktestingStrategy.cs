namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Grid Bot Backtesting Strategy.
/// </summary>
public class GridBotBacktestingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<bool> _autoBounds;
	private readonly StrategyParam<string> _boundSource;
	private readonly StrategyParam<int> _boundLookback;
	private readonly StrategyParam<decimal> _boundDeviation;
	private readonly StrategyParam<decimal> _upperBoundParam;
	private readonly StrategyParam<decimal> _lowerBoundParam;
	private readonly StrategyParam<int> _gridQty;
	
	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private SimpleMovingAverage _sma = null!;
	
	private decimal[] _gridLines = Array.Empty<decimal>();
	private bool[] _orderPlaced = Array.Empty<bool>();
	private decimal _upperBound;
	private decimal _lowerBound;
	private bool _gridInitialized;
	private decimal _gridWidth;
	private decimal _volumePerGrid;
	
	public GridBotBacktestingStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle type", "Candle type for strategy", "General");
		
		_autoBounds = Param(nameof(AutoBounds), true)
		.SetDisplay("Auto Bounds", "Automatically compute grid bounds", "Grid");
		
		_boundSource = Param(nameof(BoundSource), "Hi & Low")
		.SetDisplay("Bound Source", "Hi & Low or Average", "Grid");
		
		_boundLookback = Param(nameof(BoundLookback), 250)
		.SetDisplay("Bound Lookback", "Lookback for auto bounds", "Grid");
		
		_boundDeviation = Param(nameof(BoundDeviation), 0.10m)
		.SetDisplay("Bound Deviation", "Deviation for auto bounds", "Grid");
		
		_upperBoundParam = Param(nameof(UpperBound), 0.285m)
		.SetDisplay("Upper Bound", "Manual upper bound", "Grid");
		
		_lowerBoundParam = Param(nameof(LowerBound), 0.225m)
		.SetDisplay("Lower Bound", "Manual lower bound", "Grid");
		
		_gridQty = Param(nameof(GridLines), 30)
		.SetDisplay("Grid Lines", "Number of grid lines", "Grid");
	}
	
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}
	
	public bool AutoBounds
	{
		get => _autoBounds.Value;
		set => _autoBounds.Value = value;
	}
	
	public string BoundSource
	{
		get => _boundSource.Value;
		set => _boundSource.Value = value;
	}
	
	public int BoundLookback
	{
		get => _boundLookback.Value;
		set => _boundLookback.Value = value;
	}
	
	public decimal BoundDeviation
	{
		get => _boundDeviation.Value;
		set => _boundDeviation.Value = value;
	}
	
	public decimal UpperBound
	{
		get => _upperBoundParam.Value;
		set => _upperBoundParam.Value = value;
	}
	
	public decimal LowerBound
	{
		get => _lowerBoundParam.Value;
		set => _lowerBoundParam.Value = value;
	}
	
	public int GridLines
	{
		get => _gridQty.Value;
		set => _gridQty.Value = value;
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_gridLines = Array.Empty<decimal>();
		_orderPlaced = Array.Empty<bool>();
		_gridInitialized = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_highest = new Highest { Length = BoundLookback };
		_lowest = new Lowest { Length = BoundLookback };
		_sma = new SimpleMovingAverage { Length = BoundLookback };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_highest, _lowest, _sma, ProcessCandle)
		.Start();
	}
	
	private void InitializeGrid(decimal upper, decimal lower)
	{
		_upperBound = upper;
		_lowerBound = lower;
		_gridWidth = (upper - lower) / (GridLines - 1);
		_gridLines = new decimal[GridLines];
		_orderPlaced = new bool[GridLines];
		
		for (var i = 0; i < GridLines; i++)
		_gridLines[i] = lower + _gridWidth * i;
		
		_volumePerGrid = Volume / (GridLines - 1);
		_gridInitialized = true;
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal avg)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (AutoBounds)
		{
			if (BoundSource == "Hi & Low")
			{
				if (!_highest.IsFormed || !_lowest.IsFormed)
				return;
				
				var newUpper = highest * (1 + BoundDeviation);
				var newLower = lowest * (1 - BoundDeviation);
				
				if (!_gridInitialized || newUpper != _upperBound || newLower != _lowerBound)
				InitializeGrid(newUpper, newLower);
			}
			else
			{
				if (!_sma.IsFormed)
				return;
				
				var newUpper = avg * (1 + BoundDeviation);
				var newLower = avg * (1 - BoundDeviation);
				
				if (!_gridInitialized || newUpper != _upperBound || newLower != _lowerBound)
				InitializeGrid(newUpper, newLower);
			}
		}
		else if (!_gridInitialized)
		{
			InitializeGrid(UpperBound, LowerBound);
		}
		
		if (!_gridInitialized)
		return;
		
		for (var i = 0; i < _gridLines.Length; i++)
		{
			var line = _gridLines[i];
			
			if (candle.ClosePrice < line && !_orderPlaced[i] && i < _gridLines.Length - 1)
			{
				BuyMarket(_volumePerGrid);
				_orderPlaced[i] = true;
			}
			else if (candle.ClosePrice > line && i > 0 && _orderPlaced[i - 1])
			{
				SellMarket(_volumePerGrid);
				_orderPlaced[i - 1] = false;
			}
		}
	}
}
