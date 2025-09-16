using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ColorMetro DeMarker strategy using step levels around the DeMarker indicator.
/// Long positions are opened when the fast level crosses below the slow level.
/// Short positions are opened when the fast level crosses above the slow level.
/// </summary>
public class ColorMetroDeMarkerStrategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _stepSizeFast;
	private readonly StrategyParam<decimal> _stepSizeSlow;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableBuyOpen;
	private readonly StrategyParam<bool> _enableSellOpen;
	private readonly StrategyParam<bool> _enableBuyClose;
	private readonly StrategyParam<bool> _enableSellClose;

	private decimal _fmin;
	private decimal _fmax;
	private decimal _smin;
	private decimal _smax;
	private int _ftrend;
	private int _strend;
	private decimal _prevMPlus;
	private decimal _prevMMinus;
	private bool _isFirst;

	/// <summary>
	/// Period of the DeMarker indicator.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Fast step size for MPlus line.
	/// </summary>
	public decimal StepSizeFast
	{
		get => _stepSizeFast.Value;
		set => _stepSizeFast.Value = value;
	}

	/// <summary>
	/// Slow step size for MMinus line.
	/// </summary>
	public decimal StepSizeSlow
	{
		get => _stepSizeSlow.Value;
		set => _stepSizeSlow.Value = value;
	}

	/// <summary>
	/// Candle time frame used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableBuyOpen
	{
		get => _enableBuyOpen.Value;
		set => _enableBuyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableSellOpen
	{
		get => _enableSellOpen.Value;
		set => _enableSellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool EnableBuyClose
	{
		get => _enableBuyClose.Value;
		set => _enableBuyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool EnableSellClose
	{
		get => _enableSellClose.Value;
		set => _enableSellClose.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ColorMetroDeMarkerStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "Period of the DeMarker indicator", "Indicator")
			.SetCanOptimize(true);

		_stepSizeFast = Param(nameof(StepSizeFast), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Fast Step", "Fast step size for MPlus line", "Indicator")
			.SetCanOptimize(true);

		_stepSizeSlow = Param(nameof(StepSizeSlow), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Slow Step", "Slow step size for MMinus line", "Indicator")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");

		_enableBuyOpen = Param(nameof(EnableBuyOpen), true)
			.SetDisplay("Enable Buy Open", "Allow opening long positions", "General");

		_enableSellOpen = Param(nameof(EnableSellOpen), true)
			.SetDisplay("Enable Sell Open", "Allow opening short positions", "General");

		_enableBuyClose = Param(nameof(EnableBuyClose), true)
			.SetDisplay("Enable Buy Close", "Allow closing long positions", "General");

		_enableSellClose = Param(nameof(EnableSellClose), true)
			.SetDisplay("Enable Sell Close", "Allow closing short positions", "General");
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
		_fmin = 999999m;
		_fmax = -999999m;
		_smin = 999999m;
		_smax = -999999m;
		_ftrend = 0;
		_strend = 0;
		_prevMPlus = 0m;
		_prevMMinus = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var deMarker = new DeMarker { Length = DeMarkerPeriod };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(deMarker, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deMarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarker)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var dm = deMarker * 100m;

		var fmax0 = dm + 2m * StepSizeFast;
		var fmin0 = dm - 2m * StepSizeFast;

		if (dm > _fmax)
			_ftrend = 1;
		if (dm < _fmin)
			_ftrend = -1;

		if (_ftrend > 0 && fmin0 < _fmin)
			fmin0 = _fmin;
		if (_ftrend < 0 && fmax0 > _fmax)
			fmax0 = _fmax;

		var smax0 = dm + 2m * StepSizeSlow;
		var smin0 = dm - 2m * StepSizeSlow;

		if (dm > _smax)
			_strend = 1;
		if (dm < _smin)
			_strend = -1;

		if (_strend > 0 && smin0 < _smin)
			smin0 = _smin;
		if (_strend < 0 && smax0 > _smax)
			smax0 = _smax;

		var mPlus = _ftrend > 0 ? fmin0 + StepSizeFast : fmax0 - StepSizeFast;
		var mMinus = _strend > 0 ? smin0 + StepSizeSlow : smax0 - StepSizeSlow;

		if (!_isFirst)
		{
			if (_prevMPlus > _prevMMinus)
			{
				if (EnableBuyOpen && mPlus <= mMinus && Position <= 0)
					BuyMarket();
				if (EnableSellClose && Position < 0)
					ClosePosition();
			}
			else if (_prevMPlus < _prevMMinus)
			{
				if (EnableSellOpen && mPlus >= mMinus && Position >= 0)
					SellMarket();
				if (EnableBuyClose && Position > 0)
					ClosePosition();
			}
		}

		_prevMPlus = mPlus;
		_prevMMinus = mMinus;
		_fmin = fmin0;
		_fmax = fmax0;
		_smin = smin0;
		_smax = smax0;
		_isFirst = false;
	}
}
