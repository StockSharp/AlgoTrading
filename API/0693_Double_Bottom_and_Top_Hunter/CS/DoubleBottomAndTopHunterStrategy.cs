using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Searches for double bottom and double top patterns and trades accordingly.
/// </summary>
public class DoubleBottomAndTopHunterStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevLowAtLowest;
	private decimal? _currentLowAtLowest;
	private decimal? _prevHighAtHighest;
	private decimal? _currentHighAtHighest;
	private decimal _prevHighLength;
	private decimal _prevLowLength;

	/// <summary>
	/// Period for lowest and highest calculations.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Lookback period to confirm double patterns.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
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
	/// Initializes a new instance of the <see cref="DoubleBottomAndTopHunterStrategy"/> class.
	/// </summary>
	public DoubleBottomAndTopHunterStrategy()
	{
		_length = Param(nameof(Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback for extremes", "General")
			.SetCanOptimize(true);

		_lookback = Param(nameof(Lookback), 100)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Period to confirm doubles", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevLowAtLowest = _currentLowAtLowest = null;
		_prevHighAtHighest = _currentHighAtHighest = null;
		_prevHighLength = _prevLowLength = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var lowestLength = new Lowest { Length = Length };
		var highestLength = new Highest { Length = Length };
		var lowestLookback = new Lowest { Length = Lookback };
		var highestLookback = new Highest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lowestLength, highestLength, lowestLookback, highestLookback, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lowLength, decimal highLength, decimal lowLookback, decimal highLookback)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var doubleBottom = candle.LowPrice == lowLength && lowLookback == lowLength && _prevLowAtLowest.HasValue && candle.LowPrice == _prevLowAtLowest.Value;
		var doubleTop = candle.HighPrice == highLength && highLookback == highLength && _prevHighAtHighest.HasValue && candle.HighPrice == _prevHighAtHighest.Value;

		var closeLongCondition = highLength > _prevHighLength && candle.LowPrice < _prevLowLength;
		var closeShortCondition = lowLength < _prevLowLength && candle.HighPrice > _prevHighLength;

		if (doubleBottom && Position <= 0)
			BuyMarket();

		if (doubleTop && Position >= 0)
			SellMarket();

		if (Position > 0 && closeLongCondition)
			SellMarket();

		if (Position < 0 && closeShortCondition)
			BuyMarket();

		if (candle.LowPrice == lowLength)
		{
			_prevLowAtLowest = _currentLowAtLowest;
			_currentLowAtLowest = candle.LowPrice;
		}

		if (candle.HighPrice == highLength)
		{
			_prevHighAtHighest = _currentHighAtHighest;
			_currentHighAtHighest = candle.HighPrice;
		}

		_prevHighLength = highLength;
		_prevLowLength = lowLength;
	}
}

