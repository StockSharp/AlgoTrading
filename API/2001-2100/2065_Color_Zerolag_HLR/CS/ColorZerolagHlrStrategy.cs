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
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _high1;
	private Lowest _low1;
	private Highest _high2;
	private Lowest _low2;
	private Highest _high3;
	private Lowest _low3;

	private decimal _smoothConst;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirst;

	public int Smoothing { get => _smoothing.Value; set => _smoothing.Value = value; }
	public int HlrPeriod1 { get => _hlrPeriod1.Value; set => _hlrPeriod1.Value = value; }
	public int HlrPeriod2 { get => _hlrPeriod2.Value; set => _hlrPeriod2.Value = value; }
	public int HlrPeriod3 { get => _hlrPeriod3.Value; set => _hlrPeriod3.Value = value; }
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }
	public decimal Factor3 { get => _factor3.Value; set => _factor3.Value = value; }
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
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_smoothConst = (Smoothing - 1m) / Smoothing;
		_isFirst = true;

		_high1 = new Highest { Length = HlrPeriod1 };
		_low1 = new Lowest { Length = HlrPeriod1 };
		_high2 = new Highest { Length = HlrPeriod2 };
		_low2 = new Lowest { Length = HlrPeriod2 };
		_high3 = new Highest { Length = HlrPeriod3 };
		_low3 = new Lowest { Length = HlrPeriod3 };

		Indicators.Add(_high1);
		Indicators.Add(_low1);
		Indicators.Add(_high2);
		Indicators.Add(_low2);
		Indicators.Add(_high3);
		Indicators.Add(_low3);

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

		if (!h1.IsFormed || !l1.IsFormed || !h2.IsFormed || !l2.IsFormed || !h3.IsFormed || !l3.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var high1 = h1.ToDecimal();
		var low1 = l1.ToDecimal();
		var high2 = h2.ToDecimal();
		var low2 = l2.ToDecimal();
		var high3 = h3.ToDecimal();
		var low3 = l3.ToDecimal();

		var mid = (candle.HighPrice + candle.LowPrice) / 2m;

		var hlr1 = high1 - low1 == 0 ? 0m : 100m * (mid - low1) / (high1 - low1);
		var hlr2 = high2 - low2 == 0 ? 0m : 100m * (mid - low2) / (high2 - low2);
		var hlr3 = high3 - low3 == 0 ? 0m : 100m * (mid - low3) / (high3 - low3);

		var fast = Factor1 * hlr1 + Factor2 * hlr2 + Factor3 * hlr3;
		var slow = fast / Smoothing + _prevSlow * _smoothConst;

		if (_isFirst)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}

		// Cross signals
		if (_prevFast > _prevSlow && fast < slow && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prevFast < _prevSlow && fast > slow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
