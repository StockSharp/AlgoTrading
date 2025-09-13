using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on comparison of two ZigZag levels.
/// A short position is opened when the fast ZigZag level is above the slow one.
/// A long position is opened when the fast ZigZag level is below the slow one.
/// </summary>
public class RobotDanuStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastFast;
	private decimal _lastFastHigh;
	private decimal _lastFastLow;
	private int _fastDirection;

	private decimal _lastSlow;
	private decimal _lastSlowHigh;
	private decimal _lastSlowLow;
	private int _slowDirection;

	/// <summary>
	/// Fast ZigZag lookback length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow ZigZag lookback length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RobotDanuStrategy()
	{
		_fastLength = Param(nameof(FastLength), 28)
			.SetGreaterThanZero()
			.SetDisplay("Fast ZigZag Length", "Lookback for fast ZigZag", "ZigZag")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_slowLength = Param(nameof(SlowLength), 56)
			.SetGreaterThanZero()
			.SetDisplay("Slow ZigZag Length", "Lookback for slow ZigZag", "ZigZag")
			.SetCanOptimize(true)
			.SetOptimize(20, 120, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastFast = 0m;
		_lastFastHigh = 0m;
		_lastFastLow = 0m;
		_fastDirection = 0;

		_lastSlow = 0m;
		_lastSlowHigh = 0m;
		_lastSlowLow = 0m;
		_slowDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastHigh = new Highest { Length = FastLength };
		var fastLow = new Lowest { Length = FastLength };

		var slowHigh = new Highest { Length = SlowLength };
		var slowLow = new Lowest { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastHigh, fastLow, slowHigh, slowLow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastHigh, decimal fastLow, decimal slowHigh, decimal slowLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update fast ZigZag pivot
		if (candle.HighPrice >= fastHigh && _fastDirection != 1)
		{
			_lastFast = candle.HighPrice;
			_lastFastHigh = candle.HighPrice;
			_fastDirection = 1;
		}
		else if (candle.LowPrice <= fastLow && _fastDirection != -1)
		{
			_lastFast = candle.LowPrice;
			_lastFastLow = candle.LowPrice;
			_fastDirection = -1;
		}

		// Update slow ZigZag pivot
		if (candle.HighPrice >= slowHigh && _slowDirection != 1)
		{
			_lastSlow = candle.HighPrice;
			_lastSlowHigh = candle.HighPrice;
			_slowDirection = 1;
		}
		else if (candle.LowPrice <= slowLow && _slowDirection != -1)
		{
			_lastSlow = candle.LowPrice;
			_lastSlowLow = candle.LowPrice;
			_slowDirection = -1;
		}

		// Trading logic: compare fast and slow pivots
		if (_lastFast > _lastSlow && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (_lastFast < _lastSlow && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
	}
}
