using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Classic moving average crossover system converted from the MetaTrader Moving Average expert advisor.
/// </summary>
public class MovingAverageStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<int> _movingShift;
	private readonly StrategyParam<decimal> _baseVolume;

	private readonly Queue<decimal> _smaHistory = new();
	private decimal? _latestSmaValue;
	private ICandleMessage _previousCandle;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the simple moving average.
	/// </summary>
	public int MovingPeriod
	{
		get => _movingPeriod.Value;
		set => _movingPeriod.Value = value;
	}

	/// <summary>
	/// Number of completed candles used as an offset for the moving average value.
	/// </summary>
	public int MovingShift
	{
		get => _movingShift.Value;
		set => _movingShift.Value = value;
	}

	/// <summary>
	/// Base order volume used for opening new positions.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MovingAverageStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for moving average calculations", "General")
			.SetCanOptimize(true);

		_movingPeriod = Param(nameof(MovingPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Moving Average Period", "Number of candles used in the moving average", "Indicators")
			.SetCanOptimize(true);

		_movingShift = Param(nameof(MovingShift), 6)
			.SetDisplay("Moving Average Shift", "Offset applied to the moving average value in completed candles", "Indicators")
			.SetCanOptimize(true);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Default volume for new orders", "Trading")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SimpleMovingAverage
		{
			Length = MovingPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateMovingAverageBuffer(smaValue);

		var shiftedAverage = GetShiftedMovingAverage();
		if (shiftedAverage is null)
		{
			_previousCandle = candle;
			return;
		}

		if (_previousCandle is null)
		{
			_previousCandle = candle;
			return;
		}

		var previousOpen = _previousCandle.OpenPrice;
		var previousClose = _previousCandle.ClosePrice;

		// Entry logic mirrors the original expert advisor: act on the previous completed candle.
		if (Position == 0)
		{
			if (previousOpen < shiftedAverage && previousClose > shiftedAverage)
			{
				BuyMarket(GetEntryVolume());
			}
			else if (previousOpen > shiftedAverage && previousClose < shiftedAverage)
			{
				SellMarket(GetEntryVolume());
			}
		}
		else if (Position > 0)
		{
			// Close the long position when the candle crosses below the shifted moving average.
			if (previousOpen > shiftedAverage && previousClose < shiftedAverage)
			{
				SellMarket(Math.Abs(Position));
			}
		}
		else if (Position < 0)
		{
			// Close the short position when the candle crosses above the shifted moving average.
			if (previousOpen < shiftedAverage && previousClose > shiftedAverage)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		_previousCandle = candle;
	}

	private void UpdateMovingAverageBuffer(decimal smaValue)
	{
		_latestSmaValue = smaValue;
		_smaHistory.Enqueue(smaValue);

		var maxSize = Math.Max(1, MovingShift + 1);
		while (_smaHistory.Count > maxSize)
		{
			_smaHistory.Dequeue();
		}
	}

	private decimal? GetShiftedMovingAverage()
	{
		if (MovingShift <= 0)
			return _latestSmaValue;

		if (_smaHistory.Count < MovingShift + 1)
			return null;

		return _smaHistory.Peek();
	}

	private decimal GetEntryVolume()
	{
		var volume = BaseVolume;
		if (volume <= 0)
			volume = 1m;

		return volume;
	}
}

