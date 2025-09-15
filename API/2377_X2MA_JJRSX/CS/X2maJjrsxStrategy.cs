using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines dual moving average trend filter with RSI entries.
/// </summary>
public class X2maJjrsxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<bool> _useLong;
	private readonly StrategyParam<bool> _useShort;

	private readonly SimpleMovingAverage _fastMa = new();
	private readonly SimpleMovingAverage _slowMa = new();
	private readonly RelativeStrengthIndex _rsi = new();

	private int _trend;
	private decimal _prevRsi;

	/// <summary>
	/// Constructor.
	/// </summary>
	public X2maJjrsxStrategy()
	{
		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Trend Candle Type", "Timeframe for trend moving averages", "General");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Signal Candle Type", "Timeframe for entry signals", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Period", "Length of fast moving average", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Period", "Length of slow moving average", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Length of RSI filter", "Indicators");

		_overbought = Param(nameof(Overbought), 70m)
		.SetDisplay("Overbought", "RSI overbought threshold", "Risk");

		_oversold = Param(nameof(Oversold), 30m)
		.SetDisplay("Oversold", "RSI oversold threshold", "Risk");

		_useLong = Param(nameof(UseLong), true)
		.SetDisplay("Enable Long", "Allow long trades", "General");

		_useShort = Param(nameof(UseShort), true)
		.SetDisplay("Enable Short", "Allow short trades", "General");
	}

	/// <summary>
	/// Candle type for trend calculation.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Candle type for entry signals.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
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

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, TrendCandleType), (Security, SignalCandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa.Length = FastMaPeriod;
		_slowMa.Length = SlowMaPeriod;
		_rsi.Length = RsiPeriod;
		_prevRsi = 50m;

		var trendSub = SubscribeCandles(TrendCandleType);
		trendSub.Bind(_fastMa, _slowMa, ProcessTrend).Start();

		var signalSub = SubscribeCandles(SignalCandleType);
		signalSub.Bind(_rsi, ProcessSignal).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, signalSub);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrend(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_trend = fastMa > slowMa ? 1 : fastMa < slowMa ? -1 : _trend;
	}

	private void ProcessSignal(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (UseLong && _trend > 0 && Position <= 0 && _prevRsi < Oversold && rsi >= Oversold)
		BuyMarket();

		if (UseShort && _trend < 0 && Position >= 0 && _prevRsi > Overbought && rsi <= Overbought)
		SellMarket();

		if (Position > 0 && (rsi >= Overbought || _trend < 0))
		SellMarket(Position);

		if (Position < 0 && (rsi <= Oversold || _trend > 0))
		BuyMarket(-Position);

		_prevRsi = rsi;
	}
}
