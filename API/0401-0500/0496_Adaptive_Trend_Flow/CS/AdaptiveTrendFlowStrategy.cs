using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Trend Flow strategy.
/// The basis is the mean of EMA(Length) and EMA(2 * Length) of the typical price; the channel adds and subtracts Sensitivity times
/// an EMA(SmoothLength) of the typical price standard deviation over Length bars. A close above the upper band turns the trend up,
/// a close below the lower band turns it down. The strategy buys when the trend turns from down to up while the close is above
/// SMA(SmaLength) and the MACD line is above its signal (each filter optional), and closes the long when the trend turns down.
/// </summary>
public class AdaptiveTrendFlowStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<decimal> _sensitivity;
	private readonly StrategyParam<bool> _useSmaFilter;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private StandardDeviation _stdDev;
	private ExponentialMovingAverage _volSmooth;
	private int _trend;

	/// <summary>
	/// Fast EMA and deviation length; the slow EMA uses twice this length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// EMA length that smooths the deviation.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Deviation multiplier of the channel.
	/// </summary>
	public decimal Sensitivity
	{
		get => _sensitivity.Value;
		set => _sensitivity.Value = value;
	}

	/// <summary>
	/// Require the close above the SMA.
	/// </summary>
	public bool UseSmaFilter
	{
		get => _useSmaFilter.Value;
		set => _useSmaFilter.Value = value;
	}

	/// <summary>
	/// SMA period of the filter.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Require the MACD line above its signal.
	/// </summary>
	public bool UseMacdFilter
	{
		get => _useMacdFilter.Value;
		set => _useMacdFilter.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
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
	/// Constructor.
	/// </summary>
	public AdaptiveTrendFlowStrategy()
	{
		_length = Param(nameof(Length), 2)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Fast EMA and deviation length; the slow EMA uses twice this length", "Trend");

		_smoothLength = Param(nameof(SmoothLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "EMA length that smooths the deviation", "Trend");

		_sensitivity = Param(nameof(Sensitivity), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Sensitivity", "Deviation multiplier of the channel", "Trend");

		_useSmaFilter = Param(nameof(UseSmaFilter), true)
			.SetDisplay("Use SMA Filter", "Require the close above the SMA", "Filters");

		_smaLength = Param(nameof(SmaLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period of the filter", "Filters");

		_useMacdFilter = Param(nameof(UseMacdFilter), true)
			.SetDisplay("Use MACD Filter", "Require the MACD line above its signal", "Filters");

		_macdFastLength = Param(nameof(MacdFastLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Length", "MACD fast EMA length", "Filters");

		_macdSlowLength = Param(nameof(MacdSlowLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Length", "MACD slow EMA length", "Filters");

		_macdSignalLength = Param(nameof(MacdSignalLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Length", "MACD signal EMA length", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_fastEma = null;
		_slowEma = null;
		_stdDev = null;
		_volSmooth = null;
		_trend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trend = 0;
		_fastEma = new ExponentialMovingAverage { Length = Length };
		_slowEma = new ExponentialMovingAverage { Length = Length * 2 };
		_stdDev = new StandardDeviation { Length = Length };
		_volSmooth = new ExponentialMovingAverage { Length = SmoothLength };

		var sma = new SimpleMovingAverage { Length = SmaLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var time = candle.ServerTime;

		var fast = _fastEma.Process(typical, time, true).ToDecimal();
		var slow = _slowEma.Process(typical, time, true).ToDecimal();
		var deviation = _stdDev.Process(typical, time, true);

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_stdDev.IsFormed)
			return;

		var smoothVol = _volSmooth.Process(deviation.ToDecimal(), time, true).ToDecimal();
		if (!_volSmooth.IsFormed)
			return;

		var basis = (fast + slow) / 2m;
		var upper = basis + smoothVol * Sensitivity;
		var lower = basis - smoothVol * Sensitivity;
		var close = candle.ClosePrice;

		var prevTrend = _trend;
		if (close > upper)
			_trend = 1;
		else if (close < lower)
			_trend = -1;

		if (!smaValue.IsFormed || !macdValue.IsFormed)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var smaOk = !UseSmaFilter || close > smaValue.ToDecimal();
		var macdOk = !UseMacdFilter || macdLine > signalLine;

		if (prevTrend == -1 && _trend == 1 && smaOk && macdOk && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (prevTrend == 1 && _trend == -1 && Position > 0)
			SellMarket(Position);
	}
}
