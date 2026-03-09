using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bezier re-open strategy based on a custom Bezier curve calculation.
/// Opens positions when the Bezier value changes direction and re-enters
/// after price moves by a specified step.
/// </summary>
public class BezierReOpenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bPeriod;
	private readonly StrategyParam<decimal> _t;
	private readonly StrategyParam<decimal> _priceStep;
	private readonly StrategyParam<int> _posTotal;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private readonly List<decimal> _prices = new();
	private decimal _prev1;
	private decimal _prev2;
	private int _orderCount;
	private decimal _lastEntryPrice;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars for Bezier calculation.
	/// </summary>
	public int BPeriod
	{
		get => _bPeriod.Value;
		set => _bPeriod.Value = value;
	}

	/// <summary>
	/// Bezier curve tension.
	/// </summary>
	public decimal T
	{
		get => _t.Value;
		set => _t.Value = value;
	}

	/// <summary>
	/// Price distance for additional entries.
	/// </summary>
	public decimal PriceStep
	{
		get => _priceStep.Value;
		set => _priceStep.Value = value;
	}

	/// <summary>
	/// Maximum number of positions in sequence.
	/// </summary>
	public int PosTotal
	{
		get => _posTotal.Value;
		set => _posTotal.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Close longs on opposite signal.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Close shorts on opposite signal.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Stop-loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BezierReOpenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bPeriod = Param(nameof(BPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Bezier Period", "Number of bars for Bezier calculation", "Indicator");

		_t = Param(nameof(T), 0.5m)
			.SetDisplay("T", "Bezier curve tension", "Indicator");

		_priceStep = Param(nameof(PriceStep), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Price Step", "Price distance for additional entries", "Trading");

		_posTotal = Param(nameof(PosTotal), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of positions", "Trading");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Enabled", "Allow long entries", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Enabled", "Allow short entries", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Long", "Close longs on opposite signal", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Short", "Close shorts on opposite signal", "Trading");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop-loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take-profit in price units", "Risk");
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

		_prices.Clear();
		_prev1 = 0m;
		_prev2 = 0m;
		_orderCount = 0;
		_lastEntryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private decimal ComputeBezier()
	{
		var n = BPeriod;
		if (_prices.Count < n + 1)
			return 0m;

		var t = (double)T;
		double result = 0;
		for (var i = 0; i <= n; i++)
		{
			var priceVal = (double)_prices[_prices.Count - n - 1 + i];
			result += priceVal * Binomial(n, i) * Math.Pow(t, i) * Math.Pow(1 - t, n - i);
		}

		return (decimal)result;
	}

	private static double Binomial(int n, int k)
	{
		return Factorial(n) / (Factorial(k) * Factorial(n - k));
	}

	private static double Factorial(int value)
	{
		var res = 1d;
		for (var j = 2; j <= value; j++)
			res *= j;
		return res;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m;
		_prices.Add(price);
		if (_prices.Count > BPeriod + 2)
			_prices.RemoveAt(0);

		var bezierValue = ComputeBezier();
		if (bezierValue == 0m)
			return;

		_prev2 = _prev1;
		_prev1 = bezierValue;

		if (_prev2 == 0m)
			return;

		var openLong = false;
		var openShort = false;
		var closeLong = false;
		var closeShort = false;

		if (_prev1 > _prev2)
		{
			if (BuyPosOpen)
				openLong = true;
			if (SellPosClose && Position < 0)
				closeShort = true;
		}
		else if (_prev1 < _prev2)
		{
			if (SellPosOpen)
				openShort = true;
			if (BuyPosClose && Position > 0)
				closeLong = true;
		}

		if (closeLong)
			SellMarket();

		if (closeShort)
			BuyMarket();

		if (openLong && Position <= 0)
		{
			BuyMarket();
			_orderCount = 1;
			_lastEntryPrice = candle.ClosePrice;
			return;
		}

		if (openShort && Position >= 0)
		{
			SellMarket();
			_orderCount = 1;
			_lastEntryPrice = candle.ClosePrice;
			return;
		}

		if (Position > 0 && _orderCount < PosTotal)
		{
			if (candle.ClosePrice - _lastEntryPrice >= PriceStep)
			{
				BuyMarket();
				_lastEntryPrice = candle.ClosePrice;
				_orderCount++;
			}
		}
		else if (Position < 0 && _orderCount < PosTotal)
		{
			if (_lastEntryPrice - candle.ClosePrice >= PriceStep)
			{
				SellMarket();
				_lastEntryPrice = candle.ClosePrice;
				_orderCount++;
			}
		}

		CheckStops(candle.ClosePrice);
	}

	private void CheckStops(decimal price)
	{
		if (Position > 0)
		{
			if (StopLoss > 0 && price <= _lastEntryPrice - StopLoss)
				SellMarket();
			if (TakeProfit > 0 && price >= _lastEntryPrice + TakeProfit)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (StopLoss > 0 && price >= _lastEntryPrice + StopLoss)
				BuyMarket();
			if (TakeProfit > 0 && price <= _lastEntryPrice - TakeProfit)
				BuyMarket();
		}
	}
}
