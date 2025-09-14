using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that follows the color change of a zero lag moving average.
/// </summary>
public class ColorZeroLagMaStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	
	private ZeroLagExponentialMovingAverage _zlma = null!;
	private decimal? _prev1;
	private decimal? _prev2;
	
	/// <summary>
	/// Length of the zero lag moving average.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}
	
	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}
	
	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}
	
	/// <summary>
	/// Close long positions when the indicator turns down.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}
	
	/// <summary>
	/// Close short positions when the indicator turns up.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ColorZeroLagMaStrategy"/>.
	/// </summary>
	public ColorZeroLagMaStrategy()
	{
		_length = Param(nameof(Length), 12).SetDisplay("Length", "Zero lag length", "Indicators").SetCanOptimize();
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
		_buyOpen = Param(nameof(BuyOpen), true).SetDisplay("Open Buy", "Allow opening long", "Trading");
		_sellOpen = Param(nameof(SellOpen), true).SetDisplay("Open Sell", "Allow opening short", "Trading");
		_buyClose = Param(nameof(BuyClose), true).SetDisplay("Close Buy", "Close long on reverse", "Trading");
		_sellClose = Param(nameof(SellClose), true).SetDisplay("Close Sell", "Close short on reverse", "Trading");
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_zlma = new ZeroLagExponentialMovingAverage { Length = Length };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_zlma, ProcessCandle)
		.Start();
		
		base.OnStarted(time);
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal zlma)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (_prev1 is null)
		{
			_prev1 = zlma;
			return;
		}
		
		if (_prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = zlma;
			return;
		}
		
		if (_prev1 < _prev2)
		{
			if (SellClose && Position < 0)
			BuyMarket(-Position);
			
			if (BuyOpen && zlma > _prev1 && Position <= 0)
			BuyMarket();
		}
		else if (_prev1 > _prev2)
		{
			if (BuyClose && Position > 0)
			SellMarket(Position);
			
			if (SellOpen && zlma < _prev1 && Position >= 0)
			SellMarket();
		}
		
		_prev2 = _prev1;
		_prev1 = zlma;
	}
}
