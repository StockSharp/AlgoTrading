namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Three Bar Low Strategy.
/// A long opens when the close falls below the lowest close of the previous LowestLength candles and, with UseEmaFilter,
/// the close is above the MaPeriod EMA. The long closes when the close rises above the highest close of the previous
/// HighestLength candles.
/// </summary>
public class ThreeBarLowStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _lowestLength;
	private readonly StrategyParam<int> _highestLength;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<DataType> _candleType;

	private Lowest _lowestClose;
	private Highest _highestClose;
	private decimal? _prevLowest;
	private decimal? _prevHighest;

	/// <summary>
	/// EMA period of the filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Previous candles of the lowest close.
	/// </summary>
	public int LowestLength
	{
		get => _lowestLength.Value;
		set => _lowestLength.Value = value;
	}

	/// <summary>
	/// Previous candles of the highest close.
	/// </summary>
	public int HighestLength
	{
		get => _highestLength.Value;
		set => _highestLength.Value = value;
	}

	/// <summary>
	/// Require the close above the EMA for entries.
	/// </summary>
	public bool UseEmaFilter
	{
		get => _useEmaFilter.Value;
		set => _useEmaFilter.Value = value;
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
	public ThreeBarLowStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "EMA period of the filter", "Indicators");

		_lowestLength = Param(nameof(LowestLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lowest Length", "Previous candles of the lowest close", "Indicators");

		_highestLength = Param(nameof(HighestLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Highest Length", "Previous candles of the highest close", "Indicators");

		_useEmaFilter = Param(nameof(UseEmaFilter), false)
			.SetDisplay("Use EMA Filter", "Require the close above the EMA for entries", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevLowest = null;
		_prevHighest = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevLowest = null;
		_prevHighest = null;

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		_lowestClose = new Lowest { Length = LowestLength };
		_highestClose = new Highest { Length = HighestLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Both levels are measured on the candles before this one.
		var lowest = _prevLowest;
		var highest = _prevHighest;

		var currentLowest = _lowestClose.Process(close, candle.OpenTime, true).ToDecimal();
		var currentHighest = _highestClose.Process(close, candle.OpenTime, true).ToDecimal();

		if (_lowestClose.IsFormed)
			_prevLowest = currentLowest;

		if (_highestClose.IsFormed)
			_prevHighest = currentHighest;

		if (lowest is not decimal lowLevel || highest is not decimal highLevel)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (close > highLevel)
				SellMarket(Position);

			return;
		}

		var emaOk = !UseEmaFilter || (emaValue.IsFormed && close > emaValue.GetValue<decimal>());

		if (Position == 0 && close < lowLevel && emaOk)
			BuyMarket(Volume);
	}
}
