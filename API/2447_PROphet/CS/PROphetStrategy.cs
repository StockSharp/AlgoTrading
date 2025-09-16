using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PROphet strategy translated from MQL.
/// Uses price ranges of previous candles to generate intraday signals
/// and applies trailing stops.
/// </summary>
public class PROphetStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<int> _x1;
	private readonly StrategyParam<int> _x2;
	private readonly StrategyParam<int> _x3;
	private readonly StrategyParam<int> _x4;
	private readonly StrategyParam<int> _y1;
	private readonly StrategyParam<int> _y2;
	private readonly StrategyParam<int> _y3;
	private readonly StrategyParam<int> _y4;
	private readonly StrategyParam<int> _buyStopPoints;
	private readonly StrategyParam<int> _sellStopPoints;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _bestBid;
	private decimal _bestAsk;
	
	private decimal _buyStopPrice;
	private decimal _sellStopPrice;
	
	private decimal _prevHigh1;
	private decimal _prevLow1;
	private decimal _prevHigh2;
	private decimal _prevLow2;
	private decimal _prevHigh3;
	private decimal _prevLow3;
	private int _historyCount;
	
	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableBuy { get => _enableBuy.Value; set => _enableBuy.Value = value; }
	
	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableSell { get => _enableSell.Value; set => _enableSell.Value = value; }
	
	/// <summary>First coefficient for the buy signal.</summary>
	public int X1 { get => _x1.Value; set => _x1.Value = value; }
	/// <summary>Second coefficient for the buy signal.</summary>
	public int X2 { get => _x2.Value; set => _x2.Value = value; }
	/// <summary>Third coefficient for the buy signal.</summary>
	public int X3 { get => _x3.Value; set => _x3.Value = value; }
	/// <summary>Fourth coefficient for the buy signal.</summary>
	public int X4 { get => _x4.Value; set => _x4.Value = value; }
	
	/// <summary>First coefficient for the sell signal.</summary>
	public int Y1 { get => _y1.Value; set => _y1.Value = value; }
	/// <summary>Second coefficient for the sell signal.</summary>
	public int Y2 { get => _y2.Value; set => _y2.Value = value; }
	/// <summary>Third coefficient for the sell signal.</summary>
	public int Y3 { get => _y3.Value; set => _y3.Value = value; }
	/// <summary>Fourth coefficient for the sell signal.</summary>
	public int Y4 { get => _y4.Value; set => _y4.Value = value; }
	
	/// <summary>Stop distance in points for long positions.</summary>
	public int BuyStopPoints { get => _buyStopPoints.Value; set => _buyStopPoints.Value = value; }
	
	/// <summary>Stop distance in points for short positions.</summary>
	public int SellStopPoints { get => _sellStopPoints.Value; set => _sellStopPoints.Value = value; }
	
	/// <summary>The candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public PROphetStrategy()
	{
		_enableBuy = Param(nameof(EnableBuy), true)
		.SetDisplay("Enable Buy", "Allow opening long positions", "General");
		_enableSell = Param(nameof(EnableSell), true)
		.SetDisplay("Enable Sell", "Allow opening short positions", "General");
		
		_x1 = Param(nameof(X1), 9).SetDisplay("X1", "First coefficient for buy", "Buy");
		_x2 = Param(nameof(X2), 29).SetDisplay("X2", "Second coefficient for buy", "Buy");
		_x3 = Param(nameof(X3), 94).SetDisplay("X3", "Third coefficient for buy", "Buy");
		_x4 = Param(nameof(X4), 125).SetDisplay("X4", "Fourth coefficient for buy", "Buy");
		
		_y1 = Param(nameof(Y1), 61).SetDisplay("Y1", "First coefficient for sell", "Sell");
		_y2 = Param(nameof(Y2), 100).SetDisplay("Y2", "Second coefficient for sell", "Sell");
		_y3 = Param(nameof(Y3), 117).SetDisplay("Y3", "Third coefficient for sell", "Sell");
		_y4 = Param(nameof(Y4), 31).SetDisplay("Y4", "Fourth coefficient for sell", "Sell");
		
		_buyStopPoints = Param(nameof(BuyStopPoints), 68)
		.SetDisplay("Buy Stop Points", "Stop distance for long (points)", "Risk");
		_sellStopPoints = Param(nameof(SellStopPoints), 72)
		.SetDisplay("Sell Stop Points", "Stop distance for short (points)", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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
		_bestBid = 0;
		_bestAsk = 0;
		_buyStopPrice = 0;
		_sellStopPrice = 0;
		_prevHigh1 = _prevHigh2 = _prevHigh3 = 0;
		_prevLow1 = _prevLow2 = _prevLow3 = 0;
		_historyCount = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
		SubscribeOrderBook()
		.Bind(d =>
		{
			_bestBid = d.GetBestBid()?.Price ?? _bestBid;
			_bestAsk = d.GetBestAsk()?.Price ?? _bestAsk;
		})
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var step = Security.StepPrice ?? 1m;
		var spread = _bestAsk - _bestBid;
		var hour = candle.OpenTime.Hour;
		
		if (_historyCount >= 3)
		{
			if (Position > 0)
			{
				if (hour > 18)
				{
					SellMarket(Math.Abs(Position));
				}
				else
				{
					if (_bestBid > _buyStopPrice + spread + 2 * BuyStopPoints * step)
					_buyStopPrice = _bestBid - BuyStopPoints * step;
					
					if (_bestBid <= _buyStopPrice)
					SellMarket(Math.Abs(Position));
				}
			}
			else if (Position < 0)
			{
				if (hour > 18)
				{
					BuyMarket(Math.Abs(Position));
				}
				else
				{
					if (_bestAsk < _sellStopPrice - spread - 2 * SellStopPoints * step)
					_sellStopPrice = _bestAsk + SellStopPoints * step;
					
					if (_bestAsk >= _sellStopPrice)
					BuyMarket(Math.Abs(Position));
				}
			}
			else
			{
				if (EnableBuy && hour >= 10 && hour <= 18 && Qu(X1, X2, X3, X4) > 0)
				{
					_buyStopPrice = _bestAsk - BuyStopPoints * step;
					BuyMarket(Volume + Math.Abs(Position));
				}
				if (EnableSell && hour >= 10 && hour <= 18 && Qu(Y1, Y2, Y3, Y4) > 0)
				{
					_sellStopPrice = _bestBid + SellStopPoints * step;
					SellMarket(Volume + Math.Abs(Position));
				}
			}
		}
		
		UpdateHistory(candle);
	}
	
	private void UpdateHistory(ICandleMessage candle)
	{
		_prevHigh3 = _prevHigh2;
		_prevLow3 = _prevLow2;
		_prevHigh2 = _prevHigh1;
		_prevLow2 = _prevLow1;
		_prevHigh1 = candle.HighPrice;
		_prevLow1 = candle.LowPrice;
		
		if (_historyCount < 3)
		_historyCount++;
	}
	
	private decimal Qu(int q1, int q2, int q3, int q4)
	{
		return (q1 - 100) * Math.Abs(_prevHigh1 - _prevLow2)
		+ (q2 - 100) * Math.Abs(_prevHigh3 - _prevLow2)
		+ (q3 - 100) * Math.Abs(_prevHigh2 - _prevLow1)
		+ (q4 - 100) * Math.Abs(_prevHigh2 - _prevLow3);
	}
}
