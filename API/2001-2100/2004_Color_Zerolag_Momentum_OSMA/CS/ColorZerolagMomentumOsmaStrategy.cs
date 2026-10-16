using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>Five weighted Momentum indicators, two OSMA smoothing stages and the documented t-2/t-3 signal.</summary>
public class ColorZerolagMomentumOsmaStrategy : Strategy
{
	private readonly StrategyParam<int>[] _periods = new StrategyParam<int>[5];
	private readonly StrategyParam<decimal>[] _factors = new StrategyParam<decimal>[5];
	private readonly StrategyParam<int> _smoothing1, _smoothing2;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen, _sellOpen, _buyClose, _sellClose;

	private readonly Queue<decimal> _closes = new();
	private decimal? _slow, _smoothed, _last, _prev2, _prev3;
	private Order _pending;

	public int MomentumPeriod1 { get => _periods[0].Value; set => _periods[0].Value = value; }
	public int MomentumPeriod2 { get => _periods[1].Value; set => _periods[1].Value = value; }
	public int MomentumPeriod3 { get => _periods[2].Value; set => _periods[2].Value = value; }
	public int MomentumPeriod4 { get => _periods[3].Value; set => _periods[3].Value = value; }
	public int MomentumPeriod5 { get => _periods[4].Value; set => _periods[4].Value = value; }
	public decimal Factor1 { get => _factors[0].Value; set => _factors[0].Value = value; }
	public decimal Factor2 { get => _factors[1].Value; set => _factors[1].Value = value; }
	public decimal Factor3 { get => _factors[2].Value; set => _factors[2].Value = value; }
	public decimal Factor4 { get => _factors[3].Value; set => _factors[3].Value = value; }
	public decimal Factor5 { get => _factors[4].Value; set => _factors[4].Value = value; }
	public int Smoothing1 { get => _smoothing1.Value; set => _smoothing1.Value = value; }
	public int Smoothing2 { get => _smoothing2.Value; set => _smoothing2.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	public ColorZerolagMomentumOsmaStrategy()
	{
		var periods = new[] { 5, 8, 13, 21, 34 };
		for (var i = 0; i < 5; i++)
		{
			_periods[i] = Param("MomentumPeriod" + (i + 1), periods[i]).SetGreaterThanZero()
				.SetDisplay("Momentum period " + (i + 1), "Price difference lookback", "Indicator");
			_factors[i] = Param("Factor" + (i + 1), 1m)
				.SetDisplay("Factor " + (i + 1), "Component weight", "Indicator");
		}
		_smoothing1 = Param(nameof(Smoothing1), 9).SetGreaterThanZero()
			.SetDisplay("Smoothing 1", "Slow trend EMA", "Indicator");
		_smoothing2 = Param(nameof(Smoothing2), 5).SetGreaterThanZero()
			.SetDisplay("Smoothing 2", "OSMA EMA", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Signal candles", "General");
		_buyOpen = Param(nameof(BuyOpen), true).SetDisplay("Buy Open", "Enable long entries", "Trading");
		_sellOpen = Param(nameof(SellOpen), true).SetDisplay("Sell Open", "Enable short entries", "Trading");
		_buyClose = Param(nameof(BuyClose), true).SetDisplay("Buy Close", "Enable long exits", "Trading");
		_sellClose = Param(nameof(SellClose), true).SetDisplay("Sell Close", "Enable short exits", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	private void ResetHistory()
	{
		_closes.Clear();
		_slow = _smoothed = _last = _prev2 = _prev3 = null;
		_pending = null;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetHistory();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetHistory();
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var max = _periods.Max(p => p.Value);
		_closes.Enqueue(candle.ClosePrice);
		if (_closes.Count > max + 1)
			_closes.Dequeue();
		if (_closes.Count < max + 1)
			return;

		var closes = _closes.ToArray();
		var fast = 0m;
		for (var i = 0; i < 5; i++)
		{
			// Matches StockSharp Momentum: close[t] - close[t - period].
			var period = _periods[i].Value;
			fast += (candle.ClosePrice - closes[closes.Length - period - 1]) * _factors[i].Value;
		}
		var slow = _slow is decimal oldSlow
			? oldSlow + 2m / (Smoothing1 + 1m) * (fast - oldSlow)
			: fast;
		var rawOsma = fast - slow;
		var osma = _smoothed is decimal oldOsma
			? oldOsma + 2m / (Smoothing2 + 1m) * (rawOsma - oldOsma)
			: rawOsma;

		// The README refers to t-2 against t-3, then t against t-2.
		var buy = _prev3 is decimal third && _prev2 is decimal second
			&& second < third && osma > second;
		var sell = _prev3 is decimal thirdDown && _prev2 is decimal secondDown
			&& secondDown > thirdDown && osma < secondDown;
		_prev3 = _prev2;
		_prev2 = _last;
		_last = _smoothed = osma;
		_slow = slow;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed)))
			return;
		if (buy)
		{
			if (Position < 0m && SellClose)
				_pending = BuyMarket(-Position + (BuyOpen ? Volume : 0m));
			else if (Position == 0m && BuyOpen)
				_pending = BuyMarket(Volume);
		}
		else if (sell)
		{
			if (Position > 0m && BuyClose)
				_pending = SellMarket(Position + (SellOpen ? Volume : 0m));
			else if (Position == 0m && SellOpen)
				_pending = SellMarket(Volume);
		}
	}
}
