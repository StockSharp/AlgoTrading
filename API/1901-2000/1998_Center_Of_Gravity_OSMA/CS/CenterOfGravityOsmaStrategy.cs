using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Center-of-Gravity OSMA: product of a simple and weighted mean,
/// followed by two smoothing stages. Trades on local extrema of the oscillator.
/// </summary>
public class CenterOfGravityOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _smoothPeriod1;
	private readonly StrategyParam<int> _smoothPeriod2;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _closes = new();
	private readonly Queue<decimal> _products = new();
	private readonly Queue<decimal> _stage1 = new();
	private decimal? _prior;
	private decimal? _priorPrior;
	private Order _pendingOrder;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public int SmoothPeriod1 { get => _smoothPeriod1.Value; set => _smoothPeriod1.Value = value; }
	public int SmoothPeriod2 { get => _smoothPeriod2.Value; set => _smoothPeriod2.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CenterOfGravityOsmaStrategy()
	{
		_period = Param(nameof(Period), 10).SetGreaterThanZero()
			.SetDisplay("Period", "Base SMA and WMA period", "Indicator");
		_smoothPeriod1 = Param(nameof(SmoothPeriod1), 3).SetGreaterThanZero()
			.SetDisplay("Smooth Period 1", "First smoothing length", "Indicator");
		_smoothPeriod2 = Param(nameof(SmoothPeriod2), 3).SetGreaterThanZero()
			.SetDisplay("Smooth Period 2", "Second smoothing length", "Indicator");
		_stopLoss = Param(nameof(StopLoss), 0m).SetNotNegative()
			.SetDisplay("Stop Loss", "Optional price-unit loss limit", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 0m).SetNotNegative()
			.SetDisplay("Take Profit", "Optional price-unit profit target", "Risk");
		_buyPosOpen = Param(nameof(BuyPosOpen), true).SetDisplay("Buy Pos Open", "Allow long entry", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true).SetDisplay("Sell Pos Open", "Allow short entry", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true).SetDisplay("Buy Pos Close", "Allow long exit", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true).SetDisplay("Sell Pos Close", "Allow short exit", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetHistory();
	}

	private void ResetHistory()
	{
		_closes.Clear();
		_products.Clear();
		_stage1.Clear();
		_prior = _priorPrior = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetHistory();
		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private static void Append(Queue<decimal> history, decimal price, int period)
	{
		history.Enqueue(price);
		if (history.Count > period)
			history.Dequeue();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		Append(_closes, candle.ClosePrice, Period);
		if (_closes.Count < Period)
			return;

		var weighted = 0m;
		var weight = 0;
		foreach (var price in _closes)
		{
			weight++;
			weighted += weight * price;
		}

		var sma = _closes.Average();
		var wma = weighted / (Period * (Period + 1m) / 2m);
		Append(_products, sma * wma, SmoothPeriod1);
		if (_products.Count < SmoothPeriod1)
			return;

		Append(_stage1, _products.Average(), SmoothPeriod2);
		if (_stage1.Count < SmoothPeriod2)
			return;

		var value = _stage1.Average();
		var turnUp = _priorPrior is decimal older && _prior is decimal previous
			&& previous < older && value > previous;
		var turnDown = _priorPrior is decimal olderDown && _prior is decimal previousDown
			&& previousDown > olderDown && value < previousDown;
		_priorPrior = _prior;
		_prior = value;
		if (!IsFormedAndOnlineAndAllowTrading() ||
			(_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (turnUp)
		{
			if (Position < 0m && SellPosClose)
				_pendingOrder = BuyMarket(-Position + (BuyPosOpen ? Volume : 0m));
			else if (Position == 0m && BuyPosOpen)
				_pendingOrder = BuyMarket(Volume);
		}
		else if (turnDown)
		{
			if (Position > 0m && BuyPosClose)
				_pendingOrder = SellMarket(Position + (SellPosOpen ? Volume : 0m));
			else if (Position == 0m && SellPosOpen)
				_pendingOrder = SellMarket(Volume);
		}
	}
}
