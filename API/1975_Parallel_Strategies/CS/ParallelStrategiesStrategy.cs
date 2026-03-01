using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Heikin Ashi trend reversals with Donchian Channel breakouts and MACD confirmation.
/// </summary>
public class ParallelStrategiesStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private int? _prevTrend;

	// Heikin Ashi state
	private decimal _haOpen;
	private decimal _haClose;
	private bool _haInitialized;

	/// <summary>
	/// Donchian channel period for breakout detection.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Candle type for subscriptions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ParallelStrategiesStrategy()
	{
		_donchianPeriod = Param("DonchianPeriod", 5)
			.SetDisplay("Donchian Period", "Lookback for breakout calculation", "Indicators");
		_macdFast = Param("MacdFast", 12)
			.SetDisplay("MACD Fast", "Fast EMA period", "Indicators");
		_macdSlow = Param("MacdSlow", 26)
			.SetDisplay("MACD Slow", "Slow EMA period", "Indicators");
		_macdSignal = Param("MacdSignal", 9)
			.SetDisplay("MACD Signal", "Signal line period", "Indicators");
		_candleType = Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "Common");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpper = null;
		_prevLower = null;
		_prevTrend = null;
		_haInitialized = false;

		var donchian = new DonchianChannels { Length = DonchianPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal(
			new MovingAverageConvergenceDivergence(
				new ExponentialMovingAverage { Length = MacdSlow },
				new ExponentialMovingAverage { Length = MacdFast }),
			new ExponentialMovingAverage { Length = MacdSignal });

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var dc = (IDonchianChannelsValue)donchianValue;
		var macdV = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
			return;

		if (macdV.Macd is not decimal macdLine || macdV.Signal is not decimal signalLine)
			return;

		// Compute Heikin Ashi manually
		var haCloseNew = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		decimal haOpenNew;
		if (!_haInitialized)
		{
			haOpenNew = (candle.OpenPrice + candle.ClosePrice) / 2m;
			_haInitialized = true;
		}
		else
		{
			haOpenNew = (_haOpen + _haClose) / 2m;
		}

		_haOpen = haOpenNew;
		_haClose = haCloseNew;

		var trend = haOpenNew < haCloseNew ? 1 : -1;

		if (_prevUpper is decimal prevHigh && _prevLower is decimal prevLow && _prevTrend is int prevTrend)
		{
			if (trend > 0 && prevTrend < 0 && candle.ClosePrice > prevHigh && macdLine > signalLine && Position <= 0)
				BuyMarket();
			else if (trend < 0 && prevTrend > 0 && candle.ClosePrice < prevLow && macdLine < signalLine && Position >= 0)
				SellMarket();
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevTrend = trend;
	}
}
