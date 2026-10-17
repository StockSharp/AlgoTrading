using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades the Ehlers CyclePeriod indicator (not a fast/slow EMA crossover).
/// The price input is the median of the finished candle's high and low;
/// period estimation follows the published Ehlers phase/instant-period filter.
/// https://www.mql5.com/en/articles/288
/// </summary>
public class ExpCyclePeriodStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _alpha;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private readonly List<double> _prices = [];
	private readonly List<double> _smooth = [];
	private readonly List<double> _cycle = [];
	private readonly List<double> _quadrature = [];
	private readonly List<double> _inPhase = [];
	private readonly List<double> _deltaPhase = [];
	private readonly List<double> _instantPeriod = [];
	private readonly List<double> _cyclePeriod = [];
	private int _validPhaseCount;
	private Order _pendingOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal Alpha { get => _alpha.Value; set => _alpha.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	public ExpCyclePeriodStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_alpha = Param(nameof(Alpha), 0.07m).SetGreaterThanZero()
			.SetDisplay("Alpha", "Ehlers cycle filter coefficient", "Indicator");
		_signalBar = Param(nameof(SignalBar), 1).SetGreaterThanZero()
			.SetDisplay("Signal Bar", "Cycle-period comparison lag", "Indicator");
		_takeProfit = Param(nameof(TakeProfit), 2000m).SetGreaterThanZero()
			.SetDisplay("Take Profit", "Absolute price-distance target", "Risk");
		_stopLoss = Param(nameof(StopLoss), 1000m).SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Absolute price-distance stop", "Risk");
		_buyPosOpen = Param(nameof(BuyPosOpen), true).SetDisplay("Buy Open", "Allow long entries", "Logic");
		_sellPosOpen = Param(nameof(SellPosOpen), true).SetDisplay("Sell Open", "Allow short entries", "Logic");
		_buyPosClose = Param(nameof(BuyPosClose), true).SetDisplay("Buy Close", "Allow long closes", "Logic");
		_sellPosClose = Param(nameof(SellPosClose), true).SetDisplay("Sell Close", "Allow short closes", "Logic");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	private void ResetCycle()
	{
		_prices.Clear(); _smooth.Clear(); _cycle.Clear(); _quadrature.Clear(); _inPhase.Clear();
		_deltaPhase.Clear(); _instantPeriod.Clear(); _cyclePeriod.Clear();
		_validPhaseCount = 0;
		_pendingOrder = null;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetCycle();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetCycle();
		StartProtection(new Unit(TakeProfit, UnitTypes.Absolute), new Unit(StopLoss, UnitTypes.Absolute));
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private double UpdateCycle(double price)
	{
		var a = (double)Alpha;
		var n = _prices.Count;
		_prices.Add(price);

		var smooth = n >= 3
			? (price + 2 * _prices[n - 1] + 2 * _prices[n - 2] + _prices[n - 3]) / 6.0
			: price;
		_smooth.Add(smooth);

		double cycle;
		if (n < 2)
			cycle = 0;
		else if (n < 7)
			cycle = (price - 2 * _prices[n - 1] + _prices[n - 2]) / 4.0;
		else
			cycle = Math.Pow(1 - 0.5 * a, 2) * (smooth - 2 * _smooth[n - 1] + _smooth[n - 2])
				+ 2 * (1 - a) * _cycle[n - 1] - Math.Pow(1 - a, 2) * _cycle[n - 2];
		_cycle.Add(cycle);

		var previousInstant = n == 0 ? 0.0 : _instantPeriod[n - 1];
		var q = n >= 6
			? (0.0962 * cycle + 0.5769 * _cycle[n - 2] - 0.5769 * _cycle[n - 4] - 0.0962 * _cycle[n - 6])
				* (0.5 + 0.08 * previousInstant)
			: 0.0;
		var i = n >= 3 ? _cycle[n - 3] : 0.0;
		double delta = 0;
		if (n >= 7 && q != 0.0 && _quadrature[n - 1] != 0.0)
		{
			var oldQ = _quadrature[n - 1];
			var oldI = _inPhase[n - 1];
			var denominator = 1.0 + i * oldI / (q * oldQ);
			if (denominator != 0.0)
			{
				delta = ((i / q) - (oldI / oldQ)) / denominator;
				_validPhaseCount++;
			}
		}

		delta = Math.Clamp(delta, 0.1, 0.9);
		_quadrature.Add(q);
		_inPhase.Add(i);
		_deltaPhase.Add(delta);

		double instant = 0, period = 0;
		if (n >= 7)
		{
			var recent = new double[5];
			for (var k = 0; k < 5; k++)
				recent[k] = _deltaPhase[n - 4 + k];
			Array.Sort(recent);
			var median = recent[2];
			var dominant = median == 0.0 ? 15.0 : 6.28318 / median + 0.5;
			instant = 0.33 * dominant + 0.67 * previousInstant;
			period = 0.15 * instant + 0.85 * _cyclePeriod[n - 1];
		}
		_instantPeriod.Add(instant);
		_cyclePeriod.Add(period);
		return period;
	}

	private bool HasPendingOrder => _pendingOrder is not null &&
		_pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var current = UpdateCycle((double)((candle.HighPrice + candle.LowPrice) / 2m));
		var lag = SignalBar;
		if (lag < 1 || _cyclePeriod.Count < Math.Max(15, lag + 8) ||
			_validPhaseCount < 5 || HasPendingOrder || !IsFormedAndOnlineAndAllowTrading())
			return;

		var previous = _cyclePeriod[_cyclePeriod.Count - 1 - lag];
		if (current > previous)
		{
			if (Position < 0m && SellPosClose)
				_pendingOrder = BuyMarket(Math.Abs(Position) + (BuyPosOpen ? Volume : 0m));
			else if (Position == 0m && BuyPosOpen)
				_pendingOrder = BuyMarket(Volume);
		}
		else if (current < previous)
		{
			if (Position > 0m && BuyPosClose)
				_pendingOrder = SellMarket(Math.Abs(Position) + (SellPosOpen ? Volume : 0m));
			else if (Position == 0m && SellPosOpen)
				_pendingOrder = SellMarket(Volume);
		}
	}
}
