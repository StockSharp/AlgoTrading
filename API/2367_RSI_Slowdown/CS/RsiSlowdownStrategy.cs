using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI slowdown strategy.
/// Opens long when RSI reaches the upper level and slows down.
/// Opens short when RSI reaches the lower level and slows down.
/// </summary>
public class RsiSlowdownStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _levelMax;
	private readonly StrategyParam<decimal> _levelMin;
	private readonly StrategyParam<bool> _seekSlowdown;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousRsi;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI level.
	/// </summary>
	public decimal LevelMax
	{
		get => _levelMax.Value;
		set => _levelMax.Value = value;
	}

	/// <summary>
	/// Lower RSI level.
	/// </summary>
	public decimal LevelMin
	{
		get => _levelMin.Value;
		set => _levelMin.Value = value;
	}

	/// <summary>
	/// Enable slowdown condition.
	/// </summary>
	public bool SeekSlowdown
	{
		get => _seekSlowdown.Value;
		set => _seekSlowdown.Value = value;
	}

	/// <summary>
	/// The type of candles to use for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiSlowdownStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(2, 14, 1);

		_levelMax = Param(nameof(LevelMax), 90m)
			.SetDisplay("Upper Level", "Overbought RSI level", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(50m, 100m, 5m);

		_levelMin = Param(nameof(LevelMin), 10m)
			.SetDisplay("Lower Level", "Oversold RSI level", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(0m, 50m, 5m);

		_seekSlowdown = Param(nameof(SeekSlowdown), true)
			.SetDisplay("Seek Slowdown", "Check RSI change below 1", "RSI");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the strategy", "General");

		_previousRsi = decimal.MinValue;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_previousRsi == decimal.MinValue)
		{
		_previousRsi = rsiValue;
		return;
		}

		var isSlowdown = !SeekSlowdown || Math.Abs(_previousRsi - rsiValue) < 1m;

		if (isSlowdown)
		{
		if (rsiValue >= LevelMax)
		{
		if (Position < 0)
		BuyMarket(-Position);

		if (Position <= 0)
		BuyMarket();
		}
		else if (rsiValue <= LevelMin)
		{
		if (Position > 0)
		SellMarket(Position);

		if (Position >= 0)
		SellMarket();
		}
		}

		_previousRsi = rsiValue;
	}
}
