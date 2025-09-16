using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on weighted Hi-Lo Range oscillator with zero lag smoothing.
/// </summary>
public class ColorZerolagHlrStrategy : Strategy
{
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _hlrPeriod1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<int> _hlrPeriod2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<int> _hlrPeriod3;
	private readonly StrategyParam<decimal> _factor4;
	private readonly StrategyParam<int> _hlrPeriod4;
	private readonly StrategyParam<decimal> _factor5;
	private readonly StrategyParam<int> _hlrPeriod5;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private Highest _high1 = null!;
	private Lowest _low1 = null!;
	private Highest _high2 = null!;
	private Lowest _low2 = null!;
	private Highest _high3 = null!;
	private Lowest _low3 = null!;
	private Highest _high4 = null!;
	private Lowest _low4 = null!;
	private Highest _high5 = null!;
	private Lowest _low5 = null!;

	private decimal _smoothConst;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirst = true;

	/// <summary>
	/// EMA smoothing factor.
	/// </summary>
	public int Smoothing { get => _smoothing.Value; set => _smoothing.Value = value; }

	/// <summary>
	/// Weight for HLR period 1.
	/// </summary>
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }

	/// <summary>
	/// Lookback for HLR 1.
	/// </summary>
	public int HlrPeriod1 { get => _hlrPeriod1.Value; set => _hlrPeriod1.Value = value; }

	/// <summary>
	/// Weight for HLR period 2.
	/// </summary>
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }

	/// <summary>
	/// Lookback for HLR 2.
	/// </summary>
	public int HlrPeriod2 { get => _hlrPeriod2.Value; set => _hlrPeriod2.Value = value; }

	/// <summary>
	/// Weight for HLR period 3.
	/// </summary>
	public decimal Factor3 { get => _factor3.Value; set => _factor3.Value = value; }

	/// <summary>
	/// Lookback for HLR 3.
	/// </summary>
	public int HlrPeriod3 { get => _hlrPeriod3.Value; set => _hlrPeriod3.Value = value; }

	/// <summary>
	/// Weight for HLR period 4.
	/// </summary>
	public decimal Factor4 { get => _factor4.Value; set => _factor4.Value = value; }

	/// <summary>
	/// Lookback for HLR 4.
	/// </summary>
	public int HlrPeriod4 { get => _hlrPeriod4.Value; set => _hlrPeriod4.Value = value; }

	/// <summary>
	/// Weight for HLR period 5.
	/// </summary>
	public decimal Factor5 { get => _factor5.Value; set => _factor5.Value = value; }

	/// <summary>
	/// Lookback for HLR 5.
	/// </summary>
	public int HlrPeriod5 { get => _hlrPeriod5.Value; set => _hlrPeriod5.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool BuyPosOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool SellPosOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose { get => _buyClose.Value; set => _buyClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref=\"ColorZerolagHlrStrategy\"/> class.
	/// </summary>
	public ColorZerolagHlrStrategy()
	{
		_smoothing = Param(nameof(Smoothing), 15)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "EMA smoothing factor", "Indicator");

		_factor1 = Param(nameof(Factor1), 0.05m)
			.SetDisplay("Factor 1", "Weight for HLR period 1", "Indicator");
		_hlrPeriod1 = Param(nameof(HlrPeriod1), 8)
			.SetGreaterThanZero()
			.SetDisplay("HLR Period 1", "Lookback for HLR 1", "Indicator");

		_factor2 = Param(nameof(Factor2), 0.10m)
			.SetDisplay("Factor 2", "Weight for HLR period 2", "Indicator");
		_hlrPeriod2 = Param(nameof(HlrPeriod2), 21)
			.SetGreaterThanZero()
			.SetDisplay("HLR Period 2", "Lookback for HLR 2", "Indicator");

		_factor3 = Param(nameof(Factor3), 0.16m)
			.SetDisplay("Factor 3", "Weight for HLR period 3", "Indicator");
		_hlrPeriod3 = Param(nameof(HlrPeriod3), 34)
			.SetGreaterThanZero()
			.SetDisplay("HLR Period 3", "Lookback for HLR 3", "Indicator");

		_factor4 = Param(nameof(Factor4), 0.26m)
			.SetDisplay("Factor 4", "Weight for HLR period 4", "Indicator");
		_hlrPeriod4 = Param(nameof(HlrPeriod4), 55)
			.SetGreaterThanZero()
			.SetDisplay("HLR Period 4", "Lookback for HLR 4", "Indicator");

		_factor5 = Param(nameof(Factor5), 0.43m)
			.SetDisplay("Factor 5", "Weight for HLR period 5", "Indicator");
		_hlrPeriod5 = Param(nameof(HlrPeriod5), 89)
			.SetGreaterThanZero()
			.SetDisplay("HLR Period 5", "Lookback for HLR 5", "Indicator");

		_buyOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Allow Buy Open", "Enable long entries", "Trading");
		_sellOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Allow Sell Open", "Enable short entries", "Trading");
		_buyClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Allow Buy Close", "Allow closing longs", "Trading");
		_sellClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Allow Sell Close", "Allow closing shorts", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_smoothConst = (Smoothing - 1m) / Smoothing;

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

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_high1, _low1, _high2, _low2, _high3, _low3, _high4, _low4, _high5, _low5, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal high1, decimal low1, decimal high2, decimal low2, decimal high3, decimal low3, decimal high4, decimal low4, decimal high5, decimal low5)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var mid = (candle.HighPrice + candle.LowPrice) / 2m;

		var hlr1 = high1 - low1 == 0 ? 0m : 100m * (mid - low1) / (high1 - low1);
		var hlr2 = high2 - low2 == 0 ? 0m : 100m * (mid - low2) / (high2 - low2);
		var hlr3 = high3 - low3 == 0 ? 0m : 100m * (mid - low3) / (high3 - low3);
		var hlr4 = high4 - low4 == 0 ? 0m : 100m * (mid - low4) / (high4 - low4);
		var hlr5 = high5 - low5 == 0 ? 0m : 100m * (mid - low5) / (high5 - low5);

		var fast = Factor1 * hlr1 + Factor2 * hlr2 + Factor3 * hlr3 + Factor4 * hlr4 + Factor5 * hlr5;
		var slow = fast / Smoothing + _prevSlow * _smoothConst;

		if (_isFirst)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		if (_prevFast > _prevSlow)
		{
			if (fast < slow && BuyPosOpen)
				buyOpen = true;

			if (SellPosClose)
				sellClose = true;
		}
		else if (_prevFast < _prevSlow)
		{
			if (fast > slow && SellPosOpen)
				sellOpen = true;

			if (BuyPosClose)
				buyClose = true;
		}

		if (buyClose && Position > 0)
			SellMarket(Position);

		if (sellClose && Position < 0)
			BuyMarket(-Position);

		if (buyOpen && Position <= 0)
			BuyMarket();

		if (sellOpen && Position >= 0)
			SellMarket();

		_prevFast = fast;
		_prevSlow = slow;
	}
}
