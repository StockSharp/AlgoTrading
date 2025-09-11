
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA20/50 crossover with EMA200 trend filter and optional RSI confirmation.
/// </summary>
public class EmaRsiSwingTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastPeriod;
	private readonly StrategyParam<int> _emaSlowPeriod;
	private readonly StrategyParam<int> _emaTrendPeriod;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<decimal> _rsiMaxLong;
	private readonly StrategyParam<decimal> _rsiMinShort;
	private readonly StrategyParam<bool> _useLong;
	private readonly StrategyParam<bool> _useShort;
	private readonly StrategyParam<bool> _requireCloseConfirm;
	private readonly StrategyParam<bool> _exitOnOpposite;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEmaFast;
	private decimal _prevEmaSlow;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EmaFastPeriod
	{
		get => _emaFastPeriod.Value;
		set => _emaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaSlowPeriod
	{
		get => _emaSlowPeriod.Value;
		set => _emaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Trend EMA period.
	/// </summary>
	public int EmaTrendPeriod
	{
		get => _emaTrendPeriod.Value;
		set => _emaTrendPeriod.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Use RSI filter.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}

	/// <summary>
	/// Maximum RSI for long entries.
	/// </summary>
	public decimal RsiMaxLong
	{
		get => _rsiMaxLong.Value;
		set => _rsiMaxLong.Value = value;
	}

	/// <summary>
	/// Minimum RSI for short entries.
	/// </summary>
	public decimal RsiMinShort
	{
		get => _rsiMinShort.Value;
		set => _rsiMinShort.Value = value;
	}

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool UseLong
	{
		get => _useLong.Value;
		set => _useLong.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool UseShort
	{
		get => _useShort.Value;
		set => _useShort.Value = value;
	}

	/// <summary>
	/// Require close above/below EMAs to confirm entry.
	/// </summary>
	public bool RequireCloseConfirm
	{
		get => _requireCloseConfirm.Value;
		set => _requireCloseConfirm.Value = value;
	}

	/// <summary>
	/// Exit on opposite EMA cross.
	/// </summary>
	public bool ExitOnOpposite
	{
		get => _exitOnOpposite.Value;
		set => _exitOnOpposite.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the strategy.
	/// </summary>
	public EmaRsiSwingTrendFilterStrategy()
	{
		_emaFastPeriod = Param(nameof(EmaFastPeriod), 20)
		.SetDisplay("EMA Fast", "Period of fast EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 5);

		_emaSlowPeriod = Param(nameof(EmaSlowPeriod), 50)
		.SetDisplay("EMA Slow", "Period of slow EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 80, 5);

		_emaTrendPeriod = Param(nameof(EmaTrendPeriod), 200)
		.SetDisplay("EMA Trend", "Period of trend EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(100, 300, 50);

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "Period for RSI calculation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 21, 7);

		_useRsiFilter = Param(nameof(UseRsiFilter), true)
		.SetDisplay("Use RSI Filter", "Enable RSI filter", "General");

		_rsiMaxLong = Param(nameof(RsiMaxLong), 70m)
		.SetDisplay("Max RSI for Long", "Maximum RSI value to allow long entry", "General");

		_rsiMinShort = Param(nameof(RsiMinShort), 30m)
		.SetDisplay("Min RSI for Short", "Minimum RSI value to allow short entry", "General");

		_useLong = Param(nameof(UseLong), true)
		.SetDisplay("Enable Longs", "Allow long trades", "General");

		_useShort = Param(nameof(UseShort), false)
		.SetDisplay("Enable Shorts", "Allow short trades", "General");

		_requireCloseConfirm = Param(nameof(RequireCloseConfirm), true)
		.SetDisplay("Require Close Confirm", "Require close above/below EMAs", "General");

		_exitOnOpposite = Param(nameof(ExitOnOpposite), true)
		.SetDisplay("Exit On Opposite", "Exit when opposite EMA cross occurs", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevEmaFast = 0;
		_prevEmaSlow = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var emaFast = new ExponentialMovingAverage { Length = EmaFastPeriod };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowPeriod };
		var emaTrend = new ExponentialMovingAverage { Length = EmaTrendPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(emaFast, emaSlow, emaTrend, rsi, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, emaFast);
		DrawIndicator(area, emaSlow);
		DrawIndicator(area, emaTrend);
		DrawIndicator(area, rsi);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal emaTrend, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_prevEmaFast == 0 && _prevEmaSlow == 0)
		{
		_prevEmaFast = emaFast;
		_prevEmaSlow = emaSlow;
		return;
		}

		var crossUp = _prevEmaFast <= _prevEmaSlow && emaFast > emaSlow;
		var crossDown = _prevEmaFast >= _prevEmaSlow && emaFast < emaSlow;

		var trendUp = candle.ClosePrice > emaTrend;
		var trendDown = candle.ClosePrice < emaTrend;

		var confirmLong = !RequireCloseConfirm || (candle.ClosePrice > emaFast && candle.ClosePrice > emaSlow);
		var confirmShort = !RequireCloseConfirm || (candle.ClosePrice < emaFast && candle.ClosePrice < emaSlow);

		var rsiOkLong = !UseRsiFilter || rsi <= RsiMaxLong;
		var rsiOkShort = !UseRsiFilter || rsi >= RsiMinShort;

		if (Position == 0)
		{
		if (UseLong && trendUp && crossUp && confirmLong && rsiOkLong)
		BuyMarket(Volume);
		else if (UseShort && trendDown && crossDown && confirmShort && rsiOkShort)
		SellMarket(Volume);
		}
		else if (ExitOnOpposite)
		{
		if (Position > 0 && crossDown)
		SellMarket(Position);
		else if (Position < 0 && crossUp)
		BuyMarket(Math.Abs(Position));
		}

		_prevEmaFast = emaFast;
		_prevEmaSlow = emaSlow;
	}
}
