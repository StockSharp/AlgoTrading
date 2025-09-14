using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on color changes of the Fibo Candles indicator.
/// The indicator colors candles according to Fibonacci ratios and trend direction.
/// A change from bearish to bullish color triggers a long entry and closes short positions.
/// A change from bullish to bearish color triggers a short entry and closes long positions.
/// </summary>
public class FiboCandlesTrendStrategy : Strategy
{
	public enum FiboLevel
	{
		Level1 = 1,
		Level2,
		Level3,
		Level4,
		Level5
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<FiboLevel> _fiboLevel;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;

	private Highest _highest;
	private Lowest _lowest;
	private int _trend;
	private int? _previousColor;
	private decimal _levelMultiplier;

	/// <summary>
	/// Type and timeframe of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period for high and low calculations.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Fibonacci level used by the indicator logic.
	/// </summary>
	public FiboLevel Level
	{
		get => _fiboLevel.Value;
		set => _fiboLevel.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FiboCandlesTrendStrategy"/>.
	/// </summary>
	public FiboCandlesTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type and timeframe of candles", "General");

		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Lookback period for high/low", "FiboCandles")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_fiboLevel = Param(nameof(Level), FiboLevel.Level1)
			.SetDisplay("Fibo Level", "Fibonacci ratio level", "FiboCandles");

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(500, 2000, 500);

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1000, 4000, 500);
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
		_highest = null;
		_lowest = null;
		_trend = 0;
		_previousColor = null;
		_levelMultiplier = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = Period };
		_lowest = new Lowest { Length = Period };
		_levelMultiplier = GetLevelMultiplier(Level);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Point),
			new Unit(StopLoss, UnitTypes.Point),
			false);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var range = highest - lowest;
		var trend = _trend;
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		if (open > close)
		{
			if (!(trend < 0 && range * _levelMultiplier < close - lowest))
				trend = 1;
			else
				trend = -1;
		}
		else
		{
			if (!(trend > 0 && range * _levelMultiplier < highest - close))
				trend = -1;
			else
				trend = 1;
		}

		var color = trend == 1 ? 1 : 0;

		if (_previousColor.HasValue)
		{
			if (color == 1 && _previousColor.Value == 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (color == 0 && _previousColor.Value == 1)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
		}

		_previousColor = color;
		_trend = trend;
	}

	private static decimal GetLevelMultiplier(FiboLevel level)
	{
		return level switch
		{
			FiboLevel.Level1 => 0.236m,
			FiboLevel.Level2 => 0.382m,
			FiboLevel.Level3 => 0.500m,
			FiboLevel.Level4 => 0.618m,
			FiboLevel.Level5 => 0.762m,
			_ => 0.236m
		};
	}
}

