using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover with Dow Theory trend filter.
/// Trades long when fast EMA is above slow EMA and price breaks last swing high.
/// Trades short when fast EMA is below slow EMA and price breaks last swing low.
/// </summary>
public class EmaDowTheoryStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _swingLength;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _lastSwingHigh;
	private decimal _lastSwingLow;
	private int _trend;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Swing detection length.
	/// </summary>
	public int SwingLength
	{
		get => _swingLength.Value;
		set => _swingLength.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EmaDowTheoryStrategy"/>.
	/// </summary>
	public EmaDowTheoryStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_fastLength = Param(nameof(FastLength), 47)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_slowLength = Param(nameof(SlowLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_swingLength = Param(nameof(SwingLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Swing Length", "Bars for swing detection", "Dow Theory")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);
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
		_lastSwingHigh = 0m;
		_lastSwingLow = 0m;
		_trend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };
		_highest = new Highest { Length = SwingLength };
		_lowest = new Lowest { Length = SwingLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_fastEma, _slowEma, _highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new());
	}

	private void ProcessCandle(
		ICandleMessage candle,
		decimal fastValue,
		decimal slowValue,
		decimal highestValue,
		decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_highest.IsFormed && candle.High == highestValue)
			_lastSwingHigh = candle.High;

		if (_lowest.IsFormed && candle.Low == lowestValue)
			_lastSwingLow = candle.Low;

		if (_lastSwingHigh != 0m && candle.ClosePrice > _lastSwingHigh)
			_trend = 1;
		else if (_lastSwingLow != 0m && candle.ClosePrice < _lastSwingLow)
			_trend = -1;

		var goLong = fastValue >= slowValue;
		var goShort = fastValue < slowValue;

		if (goLong && _trend == 1 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (goShort && _trend == -1 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
