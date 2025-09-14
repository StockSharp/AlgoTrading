using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Zerolag RSI OSMA indicator.
/// </summary>
public class ColorZerolagRsiOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _smoothing1;
	private readonly StrategyParam<int> _smoothing2;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<decimal> _factor4;
	private readonly StrategyParam<decimal> _factor5;
	private readonly StrategyParam<int> _rsiPeriod1;
	private readonly StrategyParam<int> _rsiPeriod2;
	private readonly StrategyParam<int> _rsiPeriod3;
	private readonly StrategyParam<int> _rsiPeriod4;
	private readonly StrategyParam<int> _rsiPeriod5;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _smoothConst1;	// coefficient for slow trend smoothing
	private decimal _smoothConst2;	// coefficient for OSMA smoothing
	private decimal? _slowTrend;	// previous slow trend value
	private decimal? _osmaPrev;	// previous OSMA value
	private decimal? _value1;	// last OSMA value
	private decimal? _value2;	// OSMA two bars ago

	public int Smoothing1 { get => _smoothing1.Value; set => _smoothing1.Value = value; }
	public int Smoothing2 { get => _smoothing2.Value; set => _smoothing2.Value = value; }
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }
	public decimal Factor3 { get => _factor3.Value; set => _factor3.Value = value; }
	public decimal Factor4 { get => _factor4.Value; set => _factor4.Value = value; }
	public decimal Factor5 { get => _factor5.Value; set => _factor5.Value = value; }
	public int RsiPeriod1 { get => _rsiPeriod1.Value; set => _rsiPeriod1.Value = value; }
	public int RsiPeriod2 { get => _rsiPeriod2.Value; set => _rsiPeriod2.Value = value; }
	public int RsiPeriod3 { get => _rsiPeriod3.Value; set => _rsiPeriod3.Value = value; }
	public int RsiPeriod4 { get => _rsiPeriod4.Value; set => _rsiPeriod4.Value = value; }
	public int RsiPeriod5 { get => _rsiPeriod5.Value; set => _rsiPeriod5.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZerolagRsiOsmaStrategy()
	{
		_smoothing1 = Param(nameof(Smoothing1), 15)
			.SetDisplay("Smoothing 1", "First smoothing length", "Indicator");

		_smoothing2 = Param(nameof(Smoothing2), 7)
			.SetDisplay("Smoothing 2", "Second smoothing length", "Indicator");

		_factor1 = Param(nameof(Factor1), 0.05m)
			.SetDisplay("Factor 1", "Weight for RSI #1", "Indicator");

		_factor2 = Param(nameof(Factor2), 0.10m)
			.SetDisplay("Factor 2", "Weight for RSI #2", "Indicator");

		_factor3 = Param(nameof(Factor3), 0.16m)
			.SetDisplay("Factor 3", "Weight for RSI #3", "Indicator");

		_factor4 = Param(nameof(Factor4), 0.26m)
			.SetDisplay("Factor 4", "Weight for RSI #4", "Indicator");

		_factor5 = Param(nameof(Factor5), 0.43m)
			.SetDisplay("Factor 5", "Weight for RSI #5", "Indicator");

		_rsiPeriod1 = Param(nameof(RsiPeriod1), 8)
			.SetDisplay("RSI Period 1", "First RSI length", "Indicator");

		_rsiPeriod2 = Param(nameof(RsiPeriod2), 21)
			.SetDisplay("RSI Period 2", "Second RSI length", "Indicator");

		_rsiPeriod3 = Param(nameof(RsiPeriod3), 34)
			.SetDisplay("RSI Period 3", "Third RSI length", "Indicator");

		_rsiPeriod4 = Param(nameof(RsiPeriod4), 55)
			.SetDisplay("RSI Period 4", "Fourth RSI length", "Indicator");

		_rsiPeriod5 = Param(nameof(RsiPeriod5), 89)
			.SetDisplay("RSI Period 5", "Fifth RSI length", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Unused bar offset", "Indicator");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Buy", "Enable opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Sell", "Enable opening short positions", "Trading");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Long", "Close longs on downward signal", "Trading");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Short", "Close shorts on upward signal", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_smoothConst1 = (Smoothing1 - 1m) / Smoothing1;
		_smoothConst2 = (Smoothing2 - 1m) / Smoothing2;

		var rsi1 = new RelativeStrengthIndex { Length = RsiPeriod1 };
		var rsi2 = new RelativeStrengthIndex { Length = RsiPeriod2 };
		var rsi3 = new RelativeStrengthIndex { Length = RsiPeriod3 };
		var rsi4 = new RelativeStrengthIndex { Length = RsiPeriod4 };
		var rsi5 = new RelativeStrengthIndex { Length = RsiPeriod5 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi1, rsi2, rsi3, rsi4, rsi5, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi1, decimal rsi2, decimal rsi3, decimal rsi4, decimal rsi5)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastTrend = Factor1 * rsi1 + Factor2 * rsi2 + Factor3 * rsi3 + Factor4 * rsi4 + Factor5 * rsi5;	// weighted sum of RSI values

		if (_slowTrend is null)
		{
			_slowTrend = fastTrend / Smoothing1;	// initial slow trend
			_osmaPrev = (fastTrend - _slowTrend.Value) / Smoothing2;	// initial OSMA
			_value1 = _osmaPrev;
			_value2 = _osmaPrev;
			return;
		}

		_slowTrend = fastTrend / Smoothing1 + _slowTrend.Value * _smoothConst1;	// update slow trend
		var osma = (fastTrend - _slowTrend.Value) / Smoothing2 + _osmaPrev!.Value * _smoothConst2;	// calculate OSMA

		if (_value1.HasValue && _value2.HasValue)
		{
			var buyOpen = BuyOpen && _value1 < _value2 && osma > _value1;
			var sellOpen = SellOpen && _value1 > _value2 && osma < _value1;
			var buyClose = BuyClose && _value1 > _value2;
			var sellClose = SellClose && _value1 < _value2;

			if (sellClose && Position < 0)
				BuyMarket();

			if (buyClose && Position > 0)
				SellMarket();

			if (buyOpen && Position <= 0)
				BuyMarket();

			if (sellOpen && Position >= 0)
				SellMarket();
		}

		_osmaPrev = osma;
		_value2 = _value1;
		_value1 = osma;
	}
}
