using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the zero-lag momentum OSMA indicator.
/// </summary>
public class ColorZerolagMomentumOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _smoothing1;
	private readonly StrategyParam<int> _smoothing2;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<decimal> _factor4;
	private readonly StrategyParam<decimal> _factor5;
	private readonly StrategyParam<int> _period1;
	private readonly StrategyParam<int> _period2;
	private readonly StrategyParam<int> _period3;
	private readonly StrategyParam<int> _period4;
	private readonly StrategyParam<int> _period5;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private Momentum _mom1;
	private Momentum _mom2;
	private Momentum _mom3;
	private Momentum _mom4;
	private Momentum _mom5;
	private decimal _prevSlow;
	private decimal _prevOsma;
	private decimal? _osma1;
	private decimal? _osma2;
	private decimal? _osma3;
	private bool _initialized;

	public ColorZerolagMomentumOsmaStrategy()
	{
		_smoothing1 = Param(nameof(Smoothing1), 15)
			.SetDisplay("Smoothing 1");
		_smoothing2 = Param(nameof(Smoothing2), 15)
			.SetDisplay("Smoothing 2");
		_factor1 = Param<decimal>(nameof(Factor1), 0.43m)
			.SetDisplay("Factor 1");
		_factor2 = Param<decimal>(nameof(Factor2), 0.26m)
			.SetDisplay("Factor 2");
		_factor3 = Param<decimal>(nameof(Factor3), 0.16m)
			.SetDisplay("Factor 3");
		_factor4 = Param<decimal>(nameof(Factor4), 0.10m)
			.SetDisplay("Factor 4");
		_factor5 = Param<decimal>(nameof(Factor5), 0.05m)
			.SetDisplay("Factor 5");
		_period1 = Param(nameof(MomentumPeriod1), 8)
			.SetDisplay("Momentum Period 1");
		_period2 = Param(nameof(MomentumPeriod2), 21)
			.SetDisplay("Momentum Period 2");
		_period3 = Param(nameof(MomentumPeriod3), 34)
			.SetDisplay("Momentum Period 3");
		_period4 = Param(nameof(MomentumPeriod4), 55)
			.SetDisplay("Momentum Period 4");
		_period5 = Param(nameof(MomentumPeriod5), 89)
			.SetDisplay("Momentum Period 5");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type");
		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Enable Buy Open");
		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Enable Sell Open");
		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Enable Buy Close");
		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Enable Sell Close");
	}

	public int Smoothing1 { get => _smoothing1.Value; set => _smoothing1.Value = value; }
	public int Smoothing2 { get => _smoothing2.Value; set => _smoothing2.Value = value; }
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }
	public decimal Factor3 { get => _factor3.Value; set => _factor3.Value = value; }
	public decimal Factor4 { get => _factor4.Value; set => _factor4.Value = value; }
	public decimal Factor5 { get => _factor5.Value; set => _factor5.Value = value; }
	public int MomentumPeriod1 { get => _period1.Value; set => _period1.Value = value; }
	public int MomentumPeriod2 { get => _period2.Value; set => _period2.Value = value; }
	public int MomentumPeriod3 { get => _period3.Value; set => _period3.Value = value; }
	public int MomentumPeriod4 { get => _period4.Value; set => _period4.Value = value; }
	public int MomentumPeriod5 { get => _period5.Value; set => _period5.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_mom1 = new Momentum { Length = MomentumPeriod1 };
		_mom2 = new Momentum { Length = MomentumPeriod2 };
		_mom3 = new Momentum { Length = MomentumPeriod3 };
		_mom4 = new Momentum { Length = MomentumPeriod4 };
		_mom5 = new Momentum { Length = MomentumPeriod5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_mom1, _mom2, _mom3, _mom4, _mom5, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal m1, decimal m2, decimal m3, decimal m4, decimal m5)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_mom1.IsFormed || !_mom2.IsFormed || !_mom3.IsFormed || !_mom4.IsFormed || !_mom5.IsFormed)
		return;

		var fast = Factor1 * m1 + Factor2 * m2 + Factor3 * m3 + Factor4 * m4 + Factor5 * m5;

		if (!_initialized)
		{
		_prevSlow = fast / Smoothing1;
		_prevOsma = (fast - _prevSlow) / Smoothing2;
		_initialized = true;
		}

		var slow = fast / Smoothing1 + _prevSlow * ((Smoothing1 - 1m) / Smoothing1);
		var osma = (fast - slow) / Smoothing2 + _prevOsma * ((Smoothing2 - 1m) / Smoothing2);

		if (_osma2.HasValue && _osma3.HasValue && _osma1.HasValue)
		{
		if (_osma2 < _osma3)
		{
		if (SellClose && Position < 0)
		BuyMarket(-Position);
		if (BuyOpen && _osma1 > _osma2 && Position <= 0)
		BuyMarket();
		}
		else if (_osma2 > _osma3)
		{
		if (BuyClose && Position > 0)
		SellMarket(Position);
		if (SellOpen && _osma1 < _osma2 && Position >= 0)
		SellMarket();
		}
		}

		_osma3 = _osma2;
		_osma2 = _osma1;
		_osma1 = osma;
		_prevSlow = slow;
		_prevOsma = osma;
	}
}
