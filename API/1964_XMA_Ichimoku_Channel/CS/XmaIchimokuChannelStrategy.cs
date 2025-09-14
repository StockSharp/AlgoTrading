namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the XMA Ichimoku channel concept.
/// Calculates upper and lower bands from recent highs and lows,
/// smoothed by a moving average, and trades on band breakouts.
/// </summary>
public class XmaIchimokuChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _upPeriod;
	private readonly StrategyParam<int> _downPeriod;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _upPercent;
	private readonly StrategyParam<decimal> _downPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private SimpleMovingAverage _sma = null!;

	private bool _isInitialized;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;

	/// <summary>
	/// Period for calculating the highest value.
	/// </summary>
	public int UpPeriod
	{
		get => _upPeriod.Value;
		set => _upPeriod.Value = value;
	}

	/// <summary>
	/// Period for calculating the lowest value.
	/// </summary>
	public int DownPeriod
	{
		get => _downPeriod.Value;
		set => _downPeriod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing moving average.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Percentage above the average for the upper band.
	/// </summary>
	public decimal UpPercent
	{
		get => _upPercent.Value;
		set => _upPercent.Value = value;
	}

	/// <summary>
	/// Percentage below the average for the lower band.
	/// </summary>
	public decimal DownPercent
	{
		get => _downPercent.Value;
		set => _downPercent.Value = value;
	}

	/// <summary>
	/// Type of candles to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="XmaIchimokuChannelStrategy"/>.
	/// </summary>
	public XmaIchimokuChannelStrategy()
	{
		_upPeriod =
			Param(nameof(UpPeriod), 3)
				.SetGreaterThanZero()
				.SetDisplay("Up Period", "Lookback for high prices", "Channel")
				.SetCanOptimize(true);

		_downPeriod =
			Param(nameof(DownPeriod), 3)
				.SetGreaterThanZero()
				.SetDisplay("Down Period", "Lookback for low prices", "Channel")
				.SetCanOptimize(true);

		_maLength = Param(nameof(MaLength), 100)
						.SetGreaterThanZero()
						.SetDisplay("MA Length", "Smoothing length", "Channel")
						.SetCanOptimize(true);

		_upPercent =
			Param(nameof(UpPercent), 1m)
				.SetDisplay("Up Percent", "Upper band offset in %", "Channel")
				.SetCanOptimize(true);

		_downPercent =
			Param(nameof(DownPercent), 1m)
				.SetDisplay("Down Percent", "Lower band offset in %", "Channel")
				.SetCanOptimize(true);

		_candleType =
			Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = UpPeriod };
		_lowest = new Lowest { Length = DownPeriod };
		_sma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawIndicator(area, _sma);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue,
							   decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var middle =
			_sma.Process((highestValue + lowestValue) / 2m).ToDecimal();

		if (!_sma.IsFormed)
			return;

		var upper = middle * (1m + UpPercent / 100m);
		var lower = middle * (1m - DownPercent / 100m);

		if (!_isInitialized)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_prevClose = candle.ClosePrice;
			_isInitialized = true;
			return;
		}

		if (_prevClose > _prevUpper)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (candle.ClosePrice <= upper && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevClose < _prevLower)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (candle.ClosePrice >= lower && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}
}
