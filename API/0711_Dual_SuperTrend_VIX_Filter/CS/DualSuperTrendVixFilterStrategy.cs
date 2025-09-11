
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual SuperTrend with VIX filter.
/// </summary>
public class DualSuperTrendVixFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stLength1;
	private readonly StrategyParam<decimal> _stMultiplier1;
	private readonly StrategyParam<int> _stLength2;
	private readonly StrategyParam<decimal> _stMultiplier2;
	private readonly StrategyParam<bool> _useVixFilter;
	private readonly StrategyParam<Security> _vixSecurity;
	private readonly StrategyParam<int> _vixLookback;
	private readonly StrategyParam<int> _vixTrendPeriod;
	private readonly StrategyParam<decimal> _stdDevMultiplier;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;

	private SuperTrend _st1;
	private SuperTrend _st2;
	private SimpleMovingAverage _vixSma;
	private StandardDeviation _vixStd;
	private ExponentialMovingAverage _vixEma;

	private decimal _vixClose;
	private decimal _vixMean;
	private decimal _vixStdValue;
	private decimal _vixTrend;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int StLength1
	{
		get => _stLength1.Value;
		set => _stLength1.Value = value;
	}

	public decimal StMultiplier1
	{
		get => _stMultiplier1.Value;
		set => _stMultiplier1.Value = value;
	}

	public int StLength2
	{
		get => _stLength2.Value;
		set => _stLength2.Value = value;
	}

	public decimal StMultiplier2
	{
		get => _stMultiplier2.Value;
		set => _stMultiplier2.Value = value;
	}

	public bool UseVixFilter
	{
		get => _useVixFilter.Value;
		set => _useVixFilter.Value = value;
	}

	public Security VixSecurity
	{
		get => _vixSecurity.Value;
		set => _vixSecurity.Value = value;
	}

	public int VixLookback
	{
		get => _vixLookback.Value;
		set => _vixLookback.Value = value;
	}

	public int VixTrendPeriod
	{
		get => _vixTrendPeriod.Value;
		set => _vixTrendPeriod.Value = value;
	}

	public decimal StdDevMultiplier
	{
		get => _stdDevMultiplier.Value;
		set => _stdDevMultiplier.Value = value;
	}

	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	public DualSuperTrendVixFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stLength1 = Param(nameof(StLength1), 13)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend 1 Length", "ATR period for first SuperTrend", "SuperTrend");

		_stMultiplier1 = Param(nameof(StMultiplier1), 3.5m)
			.SetDisplay("SuperTrend 1 Mult", "ATR multiplier for first SuperTrend", "SuperTrend");

		_stLength2 = Param(nameof(StLength2), 8)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend 2 Length", "ATR period for second SuperTrend", "SuperTrend");

		_stMultiplier2 = Param(nameof(StMultiplier2), 5m)
			.SetDisplay("SuperTrend 2 Mult", "ATR multiplier for second SuperTrend", "SuperTrend");

		_useVixFilter = Param(nameof(UseVixFilter), true)
			.SetDisplay("Use VIX Filter", "Enable VIX volatility filter", "VIX");

		_vixSecurity = Param<Security>(nameof(VixSecurity))
			.SetDisplay("VIX Security", "Security representing VIX index", "VIX")
			.SetRequired();

		_vixLookback = Param(nameof(VixLookback), 252)
			.SetGreaterThanZero()
			.SetDisplay("VIX Lookback", "Period for VIX mean and deviation", "VIX");

		_vixTrendPeriod = Param(nameof(VixTrendPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("VIX Trend Period", "EMA period for VIX trend", "VIX");

		_stdDevMultiplier = Param(nameof(StdDevMultiplier), 1m)
			.SetDisplay("StdDev Mult", "Standard deviation multiplier for short filter", "VIX");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long positions", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short positions", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> UseVixFilter ? [(Security, CandleType), (VixSecurity, CandleType)] : [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_vixClose = _vixMean = _vixStdValue = _vixTrend = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_st1 = new SuperTrend { Length = StLength1, Multiplier = StMultiplier1 };
		_st2 = new SuperTrend { Length = StLength2, Multiplier = StMultiplier2 };

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.BindEx(_st1, _st2, ProcessMain)
			.Start();

		if (UseVixFilter)
		{
			_vixSma = new SimpleMovingAverage { Length = VixLookback };
			_vixStd = new StandardDeviation { Length = VixLookback };
			_vixEma = new ExponentialMovingAverage { Length = VixTrendPeriod };

			var vixSubscription = SubscribeCandles(CandleType, security: VixSecurity);
			vixSubscription
				.Bind(_vixSma, _vixStd, _vixEma, ProcessVix)
				.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _st1);
			DrawIndicator(area, _st2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessVix(ICandleMessage candle, decimal mean, decimal std, decimal trend)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_vixClose = candle.ClosePrice;
		_vixMean = mean;
		_vixStdValue = std;
		_vixTrend = trend;
	}

	private void ProcessMain(ICandleMessage candle, IIndicatorValue st1Value, IIndicatorValue st2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var st1 = (SuperTrendIndicatorValue)st1Value;
		var st2 = (SuperTrendIndicatorValue)st2Value;

		var longFilter = !UseVixFilter || _vixClose > _vixMean;
		var shortFilter = !UseVixFilter || (_vixClose > _vixMean + _vixStdValue * StdDevMultiplier && _vixClose > _vixTrend);

		var longCondition = st1.IsUpTrend && st2.IsUpTrend && longFilter;
		var shortCondition = st1.IsDownTrend && st2.IsDownTrend && shortFilter;

		var exitLong = st1.IsDownTrend || st2.IsDownTrend;
		var exitShort = st1.IsUpTrend || st2.IsUpTrend;

		if (EnableLong && longCondition && Position <= 0)
			BuyMarket();

		if (EnableShort && shortCondition && Position >= 0)
			SellMarket();

		if (Position > 0 && exitLong)
			SellMarket();

		if (Position < 0 && exitShort)
			BuyMarket();
	}
}
