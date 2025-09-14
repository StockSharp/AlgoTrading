using System;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Zerolag DeMarker indicator.
/// Combines several DeMarker indicators into fast and slow trend lines.
/// Generates signals on line crossovers.
/// </summary>
public class ColorZerolagDeMarkerStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _candleTimeframe;
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _period1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<int> _period2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<int> _period3;
	private readonly StrategyParam<decimal> _factor4;
	private readonly StrategyParam<int> _period4;
	private readonly StrategyParam<decimal> _factor5;
	private readonly StrategyParam<int> _period5;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<bool> _closeBuy;
	private readonly StrategyParam<bool> _closeSell;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private decimal _smoothConst;
	private decimal? _stopLoss;
	private decimal? _takeProfit;
	
	/// <summary>
	/// Candle timeframe used for indicator calculation.
	/// </summary>
	public TimeSpan CandleTimeframe
	{
		get => _candleTimeframe.Value;
		set => _candleTimeframe.Value = value;
	}
	
	/// <summary>
	/// Smoothing factor for slow trend line.
	/// </summary>
	public int Smoothing
	{
		get => _smoothing.Value;
		set => _smoothing.Value = value;
	}
	
	/// <summary>
	/// Weight for first DeMarker.
	/// </summary>
	public decimal Factor1
	{
		get => _factor1.Value;
		set => _factor1.Value = value;
	}
	
	/// <summary>
	/// Period for first DeMarker.
	/// </summary>
	public int DeMarkerPeriod1
	{
		get => _period1.Value;
		set => _period1.Value = value;
	}
	
	/// <summary>
	/// Weight for second DeMarker.
	/// </summary>
	public decimal Factor2
	{
		get => _factor2.Value;
		set => _factor2.Value = value;
	}
	
	/// <summary>
	/// Period for second DeMarker.
	/// </summary>
	public int DeMarkerPeriod2
	{
		get => _period2.Value;
		set => _period2.Value = value;
	}
	
	/// <summary>
	/// Weight for third DeMarker.
	/// </summary>
	public decimal Factor3
	{
		get => _factor3.Value;
		set => _factor3.Value = value;
	}
	
	/// <summary>
	/// Period for third DeMarker.
	/// </summary>
	public int DeMarkerPeriod3
	{
		get => _period3.Value;
		set => _period3.Value = value;
	}
	
	/// <summary>
	/// Weight for fourth DeMarker.
	/// </summary>
	public decimal Factor4
	{
		get => _factor4.Value;
		set => _factor4.Value = value;
	}
	
	/// <summary>
	/// Period for fourth DeMarker.
	/// </summary>
	public int DeMarkerPeriod4
	{
		get => _period4.Value;
		set => _period4.Value = value;
	}
	
	/// <summary>
	/// Weight for fifth DeMarker.
	/// </summary>
	public decimal Factor5
	{
		get => _factor5.Value;
		set => _factor5.Value = value;
	}
	
	/// <summary>
	/// Period for fifth DeMarker.
	/// </summary>
	public int DeMarkerPeriod5
	{
		get => _period5.Value;
		set => _period5.Value = value;
	}
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}
	
	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool OpenBuy
	{
		get => _openBuy.Value;
		set => _openBuy.Value = value;
	}
	
	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool OpenSell
	{
		get => _openSell.Value;
		set => _openSell.Value = value;
	}
	
	/// <summary>
	/// Enable closing long positions.
	/// </summary>
	public bool CloseBuy
	{
		get => _closeBuy.Value;
		set => _closeBuy.Value = value;
	}
	
	/// <summary>
	/// Enable closing short positions.
	/// </summary>
	public bool CloseSell
	{
		get => _closeSell.Value;
		set => _closeSell.Value = value;
	}
	
	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}
	
	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}
	
	/// <summary>
	/// Initializes <see cref="ColorZerolagDeMarkerStrategy"/>.
	/// </summary>
	public ColorZerolagDeMarkerStrategy()
	{
		_candleTimeframe = Param(nameof(CandleTimeframe), TimeSpan.FromHours(3))
		.SetDisplay("Candle TF", "Timeframe for indicator", "General");
		
		_smoothing = Param(nameof(Smoothing), 15)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing", "Smoothing factor", "Indicator");
		
		_factor1 = Param(nameof(Factor1), 0.05m)
		.SetDisplay("Factor 1", "Weight for period 1", "Indicator");
		_period1 = Param(nameof(DeMarkerPeriod1), 8)
		.SetGreaterThanZero()
		.SetDisplay("Period 1", "DeMarker period 1", "Indicator");
		
		_factor2 = Param(nameof(Factor2), 0.10m)
		.SetDisplay("Factor 2", "Weight for period 2", "Indicator");
		_period2 = Param(nameof(DeMarkerPeriod2), 21)
		.SetGreaterThanZero()
		.SetDisplay("Period 2", "DeMarker period 2", "Indicator");
		
		_factor3 = Param(nameof(Factor3), 0.16m)
		.SetDisplay("Factor 3", "Weight for period 3", "Indicator");
		_period3 = Param(nameof(DeMarkerPeriod3), 34)
		.SetGreaterThanZero()
		.SetDisplay("Period 3", "DeMarker period 3", "Indicator");
		
		_factor4 = Param(nameof(Factor4), 0.26m)
		.SetDisplay("Factor 4", "Weight for period 4", "Indicator");
		_period4 = Param(nameof(DeMarkerPeriod4), 55)
		.SetGreaterThanZero()
		.SetDisplay("Period 4", "DeMarker period 4", "Indicator");
		
		_factor5 = Param(nameof(Factor5), 0.43m)
		.SetDisplay("Factor 5", "Weight for period 5", "Indicator");
		_period5 = Param(nameof(DeMarkerPeriod5), 89)
		.SetGreaterThanZero()
		.SetDisplay("Period 5", "DeMarker period 5", "Indicator");
		
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");
		
		_openBuy = Param(nameof(OpenBuy), true)
		.SetDisplay("Open Buy", "Enable buy entries", "Trading");
		_openSell = Param(nameof(OpenSell), true)
		.SetDisplay("Open Sell", "Enable sell entries", "Trading");
		_closeBuy = Param(nameof(CloseBuy), true)
		.SetDisplay("Close Buy", "Enable buy exits", "Trading");
		_closeSell = Param(nameof(CloseSell), true)
		.SetDisplay("Close Sell", "Enable sell exits", "Trading");
		
		_stopLossPct = Param(nameof(StopLossPct), 0m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
		.SetCanOptimize(true);
		_takeProfitPct = Param(nameof(TakeProfitPct), 0m)
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
		.SetCanOptimize(true);
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_smoothConst = (Smoothing - 1m) / Smoothing;
		
		var de1 = new DeMarker { Length = DeMarkerPeriod1 };
		var de2 = new DeMarker { Length = DeMarkerPeriod2 };
		var de3 = new DeMarker { Length = DeMarkerPeriod3 };
		var de4 = new DeMarker { Length = DeMarkerPeriod4 };
		var de5 = new DeMarker { Length = DeMarkerPeriod5 };
		
		var subscription = SubscribeCandles(new TimeFrameCandleMessage { TimeFrame = CandleTimeframe });
		subscription.Bind(de1, de2, de3, de4, de5, ProcessCandle).Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2, decimal d3, decimal d4, decimal d5)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var fast = Factor1 * d1 + Factor2 * d2 + Factor3 * d3 + Factor4 * d4 + Factor5 * d5;
		var slow = fast / Smoothing + _prevSlow * _smoothConst;
		
		if (_hasPrev)
		{
			if (_prevFast > _prevSlow)
			{
				if (OpenBuy && fast < slow && Position <= 0)
				{
					var volume = Volume + (Position < 0 ? -Position : 0m);
					BuyMarket(volume);
					SetStops(candle.ClosePrice, true);
				}
				
				if (CloseSell && Position < 0)
				ClosePosition();
			}
			else if (_prevFast < _prevSlow)
			{
				if (OpenSell && fast > slow && Position >= 0)
				{
					var volume = Volume + (Position > 0 ? Position : 0m);
					SellMarket(volume);
					SetStops(candle.ClosePrice, false);
				}
				
				if (CloseBuy && Position > 0)
				ClosePosition();
			}
		}
		
		if (_stopLoss != null && _takeProfit != null)
		{
			if (Position > 0)
			{
				if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
				SellMarket(Position);
			}
			else if (Position < 0)
			{
				if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
				BuyMarket(-Position);
			}
		}
		
		_prevFast = fast;
		_prevSlow = slow;
		_hasPrev = true;
	}
	
	private void SetStops(decimal price, bool isBuy)
	{
		if (StopLossPct > 0m && TakeProfitPct > 0m)
		{
			if (isBuy)
			{
				_stopLoss = price * (1 - StopLossPct / 100m);
				_takeProfit = price * (1 + TakeProfitPct / 100m);
			}
			else
			{
				_stopLoss = price * (1 + StopLossPct / 100m);
				_takeProfit = price * (1 - TakeProfitPct / 100m);
			}
		}
		else
		{
			_stopLoss = null;
			_takeProfit = null;
		}
	}
}
