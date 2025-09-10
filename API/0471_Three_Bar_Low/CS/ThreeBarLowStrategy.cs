using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 3-Bar Low Strategy - buys when price breaks below the previous three-bar low and exits on a seven-bar high.
/// </summary>
public class ThreeBarLowStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _lowestLength;
	private readonly StrategyParam<int> _highestLength;
	private readonly StrategyParam<bool> _useEmaFilter;

	private ExponentialMovingAverage _ema;
	private Lowest _lowest;
	private Highest _highest;

	private decimal _prevLowest;
	private decimal _prevHighest;
	private bool _isInitialized;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA period for optional filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Lookback length for lowest close.
	/// </summary>
	public int LowestLength
	{
		get => _lowestLength.Value;
		set => _lowestLength.Value = value;
	}

	/// <summary>
	/// Lookback length for highest close.
	/// </summary>
	public int HighestLength
	{
		get => _highestLength.Value;
		set => _highestLength.Value = value;
	}

	/// <summary>
	/// Enable EMA filter.
	/// </summary>
	public bool UseEmaFilter
	{
		get => _useEmaFilter.Value;
		set => _useEmaFilter.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ThreeBarLowStrategy"/>.
	/// </summary>
	public ThreeBarLowStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

		_lowestLength = Param(nameof(LowestLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lowest Length", "Lookback for lowest close", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 5, 1);

		_highestLength = Param(nameof(HighestLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Highest Length", "Lookback for highest close", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 10, 1);

		_useEmaFilter = Param(nameof(UseEmaFilter), false)
			.SetDisplay("Use EMA Filter", "Require price above EMA to enter", "Filters");
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

		_prevLowest = default;
		_prevHighest = default;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = MaPeriod };
		_lowest = new Lowest { Length = LowestLength };
		_highest = new Highest { Length = HighestLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_lowest, _highest, _ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _lowest);
			DrawIndicator(area, _highest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lowestValue, decimal highestValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_lowest.IsFormed || !_highest.IsFormed || (UseEmaFilter && !_ema.IsFormed))
			return;

		if (!_isInitialized)
		{
			_prevLowest = lowestValue;
			_prevHighest = highestValue;
			_isInitialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longCondition = candle.ClosePrice < _prevLowest;

		if (UseEmaFilter)
			longCondition &= candle.ClosePrice > emaValue;

		if (longCondition && Position <= 0)
			RegisterOrder(CreateOrder(Sides.Buy, candle.ClosePrice, Volume));

		if (Position > 0 && candle.ClosePrice > _prevHighest)
			RegisterOrder(CreateOrder(Sides.Sell, candle.ClosePrice, Math.Abs(Position)));

		_prevLowest = lowestValue;
		_prevHighest = highestValue;
	}
}
