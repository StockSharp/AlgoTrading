using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Blau True Volume Index.
/// Buys when the indicator turns up and closes short positions.
/// Sells when the indicator turns down and closes long positions.
/// Includes optional stop-loss and take-profit in points.
/// </summary>
public class BlauTviStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<int> _length3;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _volume;
	
	private ExponentialMovingAverage _emaUp1;
	private ExponentialMovingAverage _emaDown1;
	private ExponentialMovingAverage _emaUp2;
	private ExponentialMovingAverage _emaDown2;
	private ExponentialMovingAverage _emaTvi;
	
	private decimal _tviPrev;
	private decimal _tviPrev2;
	private bool _isFirst;
	
	/// <summary>First smoothing length.</summary>
	public int Length1 { get => _length1.Value; set => _length1.Value = value; }
	
	/// <summary>Second smoothing length.</summary>
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }
	
	/// <summary>Final smoothing length.</summary>
	public int Length3 { get => _length3.Value; set => _length3.Value = value; }
	
	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>Enable opening long positions.</summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	
	/// <summary>Enable opening short positions.</summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	
	/// <summary>Enable closing long positions.</summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	
	/// <summary>Enable closing short positions.</summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }
	
	/// <summary>Use stop-loss protection.</summary>
	public bool EnableStopLoss { get => _enableStopLoss.Value; set => _enableStopLoss.Value = value; }
	
	/// <summary>Stop-loss in points.</summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	
	/// <summary>Use take-profit protection.</summary>
	public bool EnableTakeProfit { get => _enableTakeProfit.Value; set => _enableTakeProfit.Value = value; }
	
	/// <summary>Take-profit in points.</summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	
	/// <summary>Order volume in lots.</summary>
	public decimal TradeVolume { get => _volume.Value; set => _volume.Value = value; }
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public BlauTviStrategy()
	{
		_length1 = Param(nameof(Length1), 12)
		.SetGreaterThanZero()
		.SetDisplay("Length 1", "First smoothing length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);
		
		_length2 = Param(nameof(Length2), 12)
		.SetGreaterThanZero()
		.SetDisplay("Length 2", "Second smoothing length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 5);
		
		_length3 = Param(nameof(Length3), 5)
		.SetGreaterThanZero()
		.SetDisplay("Length 3", "Final smoothing length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Allow Buy Open", "Enable opening long positions", "Trading");
		
		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Allow Sell Open", "Enable opening short positions", "Trading");
		
		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Allow Buy Close", "Enable closing long positions", "Trading");
		
		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Allow Sell Close", "Enable closing short positions", "Trading");
		
		_enableStopLoss = Param(nameof(EnableStopLoss), true)
		.SetDisplay("Enable Stop Loss", "Use stop-loss protection", "Risk");
		
		_stopLoss = Param(nameof(StopLoss), 1000m)
		.SetDisplay("Stop Loss", "Stop-loss in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 3000m, 100m);
		
		_enableTakeProfit = Param(nameof(EnableTakeProfit), true)
		.SetDisplay("Enable Take Profit", "Use take-profit protection", "Risk");
		
		_takeProfit = Param(nameof(TakeProfit), 2000m)
		.SetDisplay("Take Profit", "Take-profit in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 5000m, 100m);
		
		_volume = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Volume", "Order volume in lots", "Trading")
		.SetGreaterThanZero();
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
		_emaUp1 = null;
		_emaDown1 = null;
		_emaUp2 = null;
		_emaDown2 = null;
		_emaTvi = null;
		_tviPrev = 0m;
		_tviPrev2 = 0m;
		_isFirst = true;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_emaUp1 = new ExponentialMovingAverage { Length = Length1 };
		_emaDown1 = new ExponentialMovingAverage { Length = Length1 };
		_emaUp2 = new ExponentialMovingAverage { Length = Length2 };
		_emaDown2 = new ExponentialMovingAverage { Length = Length2 };
		_emaTvi = new ExponentialMovingAverage { Length = Length3 };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
		
		StartProtection(
		EnableTakeProfit ? new Unit(TakeProfit, UnitTypes.Point) : new Unit(),
		EnableStopLoss ? new Unit(StopLoss, UnitTypes.Point) : new Unit());
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaTvi);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var volume = candle.TotalVolume;
		var priceStep = Security?.PriceStep ?? 1m;
		var upTicks = (volume + (candle.ClosePrice - candle.OpenPrice) / priceStep) / 2m;
		var dnTicks = volume - upTicks;
		
		var up1 = _emaUp1.Process(upTicks);
		var dn1 = _emaDown1.Process(dnTicks);
		if (!up1.IsFinal || !dn1.IsFinal)
		return;
		
		var up2 = _emaUp2.Process(up1.GetValue<decimal>());
		var dn2 = _emaDown2.Process(dn1.GetValue<decimal>());
		if (!up2.IsFinal || !dn2.IsFinal)
		return;
		
		var xxUp = up2.GetValue<decimal>();
		var xxDn = dn2.GetValue<decimal>();
		if (xxUp + xxDn == 0)
		return;
		
		var raw = 100m * (xxUp - xxDn) / (xxUp + xxDn);
		var tviVal = _emaTvi.Process(raw);
		if (!tviVal.IsFinal)
		return;
		
		var current = tviVal.GetValue<decimal>();
		
		if (_isFirst)
		{
			_tviPrev2 = current;
			_tviPrev = current;
			_isFirst = false;
			return;
		}
		
		if (_tviPrev < _tviPrev2 && current > _tviPrev)
		{
			if (SellPosClose && Position < 0)
			BuyMarket(-Position);
			
			if (BuyPosOpen && Position <= 0)
			BuyMarket(TradeVolume + Math.Abs(Position));
		}
		else if (_tviPrev > _tviPrev2 && current < _tviPrev)
		{
			if (BuyPosClose && Position > 0)
			SellMarket(Position);
			
			if (SellPosOpen && Position >= 0)
			SellMarket(TradeVolume + Position);
		}
		
		_tviPrev2 = _tviPrev;
		_tviPrev = current;
	}
}
