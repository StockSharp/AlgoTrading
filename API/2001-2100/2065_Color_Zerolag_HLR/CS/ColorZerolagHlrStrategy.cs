using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on weighted Hi-Lo Range oscillator with zero lag smoothing.
/// </summary>
public class ColorZerolagHlrStrategy : Strategy
{
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<int> _hlrPeriod1;
	private readonly StrategyParam<int> _hlrPeriod2;
	private readonly StrategyParam<int> _hlrPeriod3;
	private readonly StrategyParam<int> _hlrPeriod4, _hlrPeriod5;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<decimal> _factor4, _factor5;
	private readonly StrategyParam<bool> _buyOpen, _sellOpen, _buyClose, _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _high1;
	private Lowest _low1;
	private Highest _high2;
	private Lowest _low2;
	private Highest _high3;
	private Lowest _low3;
	private Highest _high4, _high5;
	private Lowest _low4, _low5;
	private Order _pending;

	private decimal _smoothConst;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirst;

	public int Smoothing { get => _smoothing.Value; set => _smoothing.Value = value; }
	public int HlrPeriod1 { get => _hlrPeriod1.Value; set => _hlrPeriod1.Value = value; }
	public int HlrPeriod2 { get => _hlrPeriod2.Value; set => _hlrPeriod2.Value = value; }
	public int HlrPeriod3 { get => _hlrPeriod3.Value; set => _hlrPeriod3.Value = value; }
	public int HlrPeriod4 { get => _hlrPeriod4.Value; set => _hlrPeriod4.Value = value; }
	public int HlrPeriod5 { get => _hlrPeriod5.Value; set => _hlrPeriod5.Value = value; }
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }
	public decimal Factor3 { get => _factor3.Value; set => _factor3.Value = value; }
	public decimal Factor4 { get => _factor4.Value; set => _factor4.Value = value; }
	public decimal Factor5 { get => _factor5.Value; set => _factor5.Value = value; }
	public bool BuyPosOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellPosOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyPosClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellPosClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZerolagHlrStrategy()
	{
		_smoothing = Param(nameof(Smoothing), 15)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "EMA smoothing factor", "Indicator");

		_factor1 = Param(nameof(Factor1), 0.2m)
			.SetDisplay("Factor 1", "Weight for HLR period 1", "Indicator");
		_hlrPeriod1 = Param(nameof(HlrPeriod1), 8)
			.SetGreaterThanZero()
			.SetDisplay("HLR Period 1", "Lookback for HLR 1", "Indicator");

		_factor2 = Param(nameof(Factor2), 0.35m)
			.SetDisplay("Factor 2", "Weight for HLR period 2", "Indicator");
		_hlrPeriod2 = Param(nameof(HlrPeriod2), 21)
			.SetGreaterThanZero()
			.SetDisplay("HLR Period 2", "Lookback for HLR 2", "Indicator");

		_factor3 = Param(nameof(Factor3), 0.45m)
			.SetDisplay("Factor 3", "Weight for HLR period 3", "Indicator");
		_hlrPeriod3 = Param(nameof(HlrPeriod3), 34)
			.SetGreaterThanZero()
			.SetDisplay("HLR Period 3", "Lookback for HLR 3", "Indicator");

		_factor4 = Param(nameof(Factor4), 0.1m);
		_factor5 = Param(nameof(Factor5), 0.1m);
		_hlrPeriod4 = Param(nameof(HlrPeriod4), 55).SetGreaterThanZero();
		_hlrPeriod5 = Param(nameof(HlrPeriod5), 89).SetGreaterThanZero();
		_buyOpen = Param(nameof(BuyPosOpen), true);
		_sellOpen = Param(nameof(SellPosOpen), true);
		_buyClose = Param(nameof(BuyPosClose), true);
		_sellClose = Param(nameof(SellPosClose), true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_isFirst = true;
		_prevFast = default;
		_prevSlow = default;
		_smoothConst = default;
		_high1 = default;
		_low1 = default;
		_high2 = default;
		_low2 = default;
		_high3 = default;
		_low3 = default;
		_high4 = _high5 = default;
		_low4 = _low5 = default;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_smoothConst = (Smoothing - 1m) / Smoothing;
		_isFirst = true;
		_pending = null;

		_high1 = new Highest { Length = HlrPeriod1 };
		_low1 = new Lowest { Length = HlrPeriod1 };
		_high2 = new Highest { Length = HlrPeriod2 };
		_low2 = new Lowest { Length = HlrPeriod2 };
		_high3 = new Highest { Length = HlrPeriod3 };
		_low3 = new Lowest { Length = HlrPeriod3 };
		_high4 = new Highest { Length = HlrPeriod4 };
		_low4 = new Lowest { Length = HlrPeriod4 };
		_high5 = new Highest { Length = HlrPeriod5 };
		_low5 = new Lowest { Length = HlrPeriod5 };

		Indicators.Add(_high1);
		Indicators.Add(_low1);
		Indicators.Add(_high2);
		Indicators.Add(_low2);
		Indicators.Add(_high3);
		Indicators.Add(_low3);
		Indicators.Add(_high4);
		Indicators.Add(_low4);
		Indicators.Add(_high5);
		Indicators.Add(_low5);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var h1 = _high1.Process(candle);
		var l1 = _low1.Process(candle);
		var h2 = _high2.Process(candle);
		var l2 = _low2.Process(candle);
		var h3 = _high3.Process(candle);
		var l3 = _low3.Process(candle);
		var h4 = _high4.Process(candle);
		var l4 = _low4.Process(candle);
		var h5 = _high5.Process(candle);
		var l5 = _low5.Process(candle);

		if (!h1.IsFormed || !l1.IsFormed || !h2.IsFormed || !l2.IsFormed || !h3.IsFormed || !l3.IsFormed || !h4.IsFormed || !l4.IsFormed || !h5.IsFormed || !l5.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var high1 = h1.ToDecimal();
		var low1 = l1.ToDecimal();
		var high2 = h2.ToDecimal();
		var low2 = l2.ToDecimal();
		var high3 = h3.ToDecimal();
		var low3 = l3.ToDecimal();
		var high4 = h4.ToDecimal();
		var low4 = l4.ToDecimal();
		var high5 = h5.ToDecimal();
		var low5 = l5.ToDecimal();

		var mid = (candle.HighPrice + candle.LowPrice) / 2m;

		var hlr1 = high1 - low1 == 0 ? 0m : 100m * (mid - low1) / (high1 - low1);
		var hlr2 = high2 - low2 == 0 ? 0m : 100m * (mid - low2) / (high2 - low2);
		var hlr3 = high3 - low3 == 0 ? 0m : 100m * (mid - low3) / (high3 - low3);
		var hlr4 = high4 == low4 ? 0m : 100m * (mid - low4) / (high4 - low4);
		var hlr5 = high5 == low5 ? 0m : 100m * (mid - low5) / (high5 - low5);

		var fast = Factor1 * hlr1 + Factor2 * hlr2 + Factor3 * hlr3 + Factor4 * hlr4 + Factor5 * hlr5;
		var slow = fast / Smoothing + _prevSlow * _smoothConst;

		if (_isFirst)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}

		// Cross signals
		if (_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed)
		{
			if (_prevFast > _prevSlow && fast < slow)
			{
				if (Position < 0m && SellPosClose) _pending = BuyMarket(-Position + (BuyPosOpen ? Volume : 0m));
				else if (Position == 0m && BuyPosOpen) _pending = BuyMarket(Volume);
			}
			else if (_prevFast < _prevSlow && fast > slow)
			{
				if (Position > 0m && BuyPosClose) _pending = SellMarket(Position + (SellPosOpen ? Volume : 0m));
				else if (Position == 0m && SellPosOpen) _pending = SellMarket(Volume);
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
