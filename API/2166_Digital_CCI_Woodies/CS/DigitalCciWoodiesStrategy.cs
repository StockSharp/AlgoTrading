namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on crossover of fast and slow CCI indicators.
/// </summary>
public class DigitalCciWoodiesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirst = true;
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Fast CCI length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	
	/// <summary>
	/// Slow CCI length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}
	
	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}
	
	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}
	
	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}
	
	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}
	
	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public DigitalCciWoodiesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
		
		_fastLength = Param(nameof(FastLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Fast CCI Length", "Length of the fast CCI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);
		
		_slowLength = Param(nameof(SlowLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Slow CCI Length", "Length of the slow CCI", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);
		
		_buyOpen = Param(nameof(BuyOpen), true)
		.SetDisplay("Buy Open", "Allow long entries", "Trading");
		
		_sellOpen = Param(nameof(SellOpen), true)
		.SetDisplay("Sell Open", "Allow short entries", "Trading");
		
		_buyClose = Param(nameof(BuyClose), true)
		.SetDisplay("Buy Close", "Allow closing longs", "Trading");
		
		_sellClose = Param(nameof(SellClose), true)
		.SetDisplay("Sell Close", "Allow closing shorts", "Trading");
	}
	
	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = default;
		_prevSlow = default;
		_isFirst = true;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fastCci = new CommodityChannelIndex { Length = FastLength };
		var slowCci = new CommodityChannelIndex { Length = SlowLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(fastCci, slowCci, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
		
		var indicatorArea = CreateChartArea();
		if (indicatorArea != null)
		{
			DrawIndicator(indicatorArea, fastCci);
			DrawIndicator(indicatorArea, slowCci);
		}
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
		return;
		
		// Ensure trading is allowed
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (_isFirst)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}
		
		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;
		
		if (crossUp)
		{
			if (Position < 0 && SellClose)
			{
				var volume = BuyOpen ? Volume + Math.Abs(Position) : Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (BuyOpen && Position <= 0)
			{
				BuyMarket(Volume);
			}
		}
		else if (crossDown)
		{
			if (Position > 0 && BuyClose)
			{
				var volume = SellOpen ? Volume + Position : Position;
				SellMarket(volume);
			}
			else if (SellOpen && Position >= 0)
			{
				SellMarket(Volume);
			}
		}
		else
		{
			if (fast > slow && SellClose && Position < 0)
			BuyMarket(-Position);
			if (fast < slow && BuyClose && Position > 0)
			SellMarket(Position);
		}
		
		_prevFast = fast;
		_prevSlow = slow;
	}
}
