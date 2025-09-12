using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on smoothed Z-Score cross with momentum filter.
/// Enters long position when short-term Z-Score is above long-term Z-Score
/// and exits when it falls below. Consecutive bullish/bearish candles are used
/// as additional filters.
/// </summary>
public class PriceStatisticalZScoreStrategy : Strategy
{
	private readonly StrategyParam<int> _zScoreBasePeriod;
	private readonly StrategyParam<int> _shortSmoothPeriod;
	private readonly StrategyParam<int> _longSmoothPeriod;
	private readonly StrategyParam<int> _gapBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _mean;
	private StandardDeviation _stdDev;
	private SimpleMovingAverage _shortSmooth;
	private SimpleMovingAverage _longSmooth;

	private int _barIndex;
	private int _lastEntryIndex;
	private int _lastExitIndex;

	private decimal _prevClose1;
	private decimal _prevClose2;
	private decimal _prevClose3;
	private decimal _prevClose4;

	/// <summary>
	/// Base period for Z-Score calculation.
	/// </summary>
	public int ZScoreBasePeriod
	{
		get => _zScoreBasePeriod.Value;
		set => _zScoreBasePeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for short-term Z-Score.
	/// </summary>
	public int ShortSmoothPeriod
	{
		get => _shortSmoothPeriod.Value;
		set => _shortSmoothPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for long-term Z-Score.
	/// </summary>
	public int LongSmoothPeriod
	{
		get => _longSmoothPeriod.Value;
		set => _longSmoothPeriod.Value = value;
	}

	/// <summary>
	/// Minimum bars gap between identical signals.
	/// </summary>
	public int GapBars
	{
		get => _gapBars.Value;
		set => _gapBars.Value = value;
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
	/// Initializes a new instance of the <see cref="PriceStatisticalZScoreStrategy"/>.
	/// </summary>
	public PriceStatisticalZScoreStrategy()
	{
		_zScoreBasePeriod = Param(nameof(ZScoreBasePeriod), 3)
			.SetDisplay("Z-Score Base Period", "Base period for Z-Score calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_shortSmoothPeriod = Param(nameof(ShortSmoothPeriod), 3)
			.SetDisplay("Short-Term Smoothing", "Period for short-term Z-Score smoothing", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_longSmoothPeriod = Param(nameof(LongSmoothPeriod), 5)
			.SetDisplay("Long-Term Smoothing", "Period for long-term Z-Score smoothing", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2, 15, 1);

		_gapBars = Param(nameof(GapBars), 5)
			.SetDisplay("Signal Gap", "Minimum bars between identical signals", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

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

		_barIndex = 0;
		_lastEntryIndex = _lastExitIndex = -GapBars - 1;
		_prevClose1 = _prevClose2 = _prevClose3 = _prevClose4 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_mean = new SimpleMovingAverage { Length = ZScoreBasePeriod };
		_stdDev = new StandardDeviation { Length = ZScoreBasePeriod };
		_shortSmooth = new SimpleMovingAverage { Length = ShortSmoothPeriod };
		_longSmooth = new SimpleMovingAverage { Length = LongSmoothPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_mean, _stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal meanValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stdValue == 0)
			return;

		_barIndex++;

		var zRaw = (candle.ClosePrice - meanValue) / stdValue;
		var zShort = _shortSmooth.Process(zRaw, candle.ServerTime, true).ToDecimal();
		var zLong = _longSmooth.Process(zRaw, candle.ServerTime, true).ToDecimal();

		if (!_longSmooth.IsFormed)
		{
			UpdateCloses(candle.ClosePrice);
			return;
		}

		var bullish3 = candle.ClosePrice > _prevClose1 && _prevClose1 > _prevClose2 && _prevClose2 > _prevClose3 && _prevClose3 > _prevClose4;
		var bearish3 = candle.ClosePrice < _prevClose1 && _prevClose1 < _prevClose2 && _prevClose2 < _prevClose3 && _prevClose3 < _prevClose4;

		if (Position <= 0)
		{
			var longCondition = zShort > zLong;
			var gapOk = _barIndex - _lastEntryIndex > GapBars;
			if (longCondition && gapOk && !bullish3)
			{
			BuyMarket();
			_lastEntryIndex = _barIndex;
			}
		}
		else
		{
			var exitCondition = zShort < zLong;
			var gapOk = _barIndex - _lastExitIndex > GapBars;
			if (exitCondition && gapOk && !bearish3)
			{
			SellMarket(Position);
			_lastExitIndex = _barIndex;
			}
		}

		UpdateCloses(candle.ClosePrice);
	}

	private void UpdateCloses(decimal close)
	{
		_prevClose4 = _prevClose3;
		_prevClose3 = _prevClose2;
		_prevClose2 = _prevClose1;
		_prevClose1 = close;
	}
}
