using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tenkan/Kijun cross alert strategy converted from the MetaTrader expert advisor.
/// Generates log notifications whenever the Ichimoku Tenkan-sen crosses the Kijun-sen during the active session.
/// </summary>
public class TenKijunCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _lastHour;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku = null!;
	private decimal? _prevTenkan;
	private decimal? _prevKijun;

	/// <summary>
	/// Inclusive hour that marks the start of the active session.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Inclusive hour that marks the end of the active session.
	/// </summary>
	public int LastHour
	{
		get => _lastHour.Value;
		set => _lastHour.Value = value;
	}

	/// <summary>
	/// Ichimoku Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Ichimoku Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Ichimoku Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the Ichimoku indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TenKijunCrossStrategy"/>.
	/// </summary>
	public TenKijunCrossStrategy()
	{
		_startHour = Param(nameof(StartHour), 0)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Inclusive start of the active trading window", "Session");

		_lastHour = Param(nameof(LastHour), 20)
			.SetRange(0, 23)
			.SetDisplay("Last Hour", "Inclusive end of the active trading window", "Session");

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetRange(3, 18)
			.SetDisplay("Tenkan Period", "Ichimoku Tenkan-sen lookback", "Ichimoku")
			.SetCanOptimize(true);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetRange(10, 52)
			.SetDisplay("Kijun Period", "Ichimoku Kijun-sen lookback", "Ichimoku")
			.SetCanOptimize(true);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetRange(26, 104)
			.SetDisplay("Senkou Span B Period", "Ichimoku Senkou Span B lookback", "Ichimoku")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for the Ichimoku calculation", "General");
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
		_prevTenkan = null;
		_prevKijun = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradingHours(candle.OpenTime))
			return;

		var ichimoku = (IchimokuValue)ichimokuValue;

		if (ichimoku.Tenkan is not decimal tenkan)
			return;

		if (ichimoku.Kijun is not decimal kijun)
			return;

		if (_prevTenkan is null || _prevKijun is null)
		{
			_prevTenkan = tenkan;
			_prevKijun = kijun;
			return;
		}

		var bullishCross = _prevTenkan <= _prevKijun && tenkan > kijun;
		var bearishCross = _prevTenkan >= _prevKijun && tenkan < kijun;

		if (bullishCross)
		{
			AddInfoLog($"Ichimoku Tenkan crossed above Kijun at {candle.CloseTime:yyyy-MM-dd HH:mm}. Close price: {candle.ClosePrice:0.#####}.");
		}
		else if (bearishCross)
		{
			AddInfoLog($"Ichimoku Tenkan crossed below Kijun at {candle.CloseTime:yyyy-MM-dd HH:mm}. Close price: {candle.ClosePrice:0.#####}.");
		}

		_prevTenkan = tenkan;
		_prevKijun = kijun;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;
		var start = StartHour;
		var end = LastHour;

		if (start <= end)
			return hour >= start && hour <= end;

		return hour >= start || hour <= end;
	}
}
