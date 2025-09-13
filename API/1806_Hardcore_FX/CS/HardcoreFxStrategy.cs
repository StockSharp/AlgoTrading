using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ZigZag breakout strategy with trailing stop.
/// Adapted from the MetaTrader "HardcoreFX" expert.
/// </summary>
public class HardcoreFxStrategy : Strategy
{
	private readonly StrategyParam<int> _zigzagLength;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastHigh;
	private decimal _lastLow;
	private decimal _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private int _direction;

	/// <summary>
	/// ZigZag depth used to search pivots.
	/// </summary>
	public int ZigzagLength
	{
		get => _zigzagLength.Value;
		set => _zigzagLength.Value = value;
	}

	/// <summary>
	/// Stop loss value in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit value in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Type of candles to use for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters with default values.
	/// </summary>
	public HardcoreFxStrategy()
	{
		_zigzagLength = Param(nameof(ZigzagLength), 17)
			.SetDisplay("ZigZag Length", "Lookback for pivot search", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 1400)
			.SetDisplay("Stop Loss", "Protective stop in points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 5400)
			.SetDisplay("Take Profit", "Target profit in points", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 500)
			.SetDisplay("Trailing Stop", "Trailing distance in points", "Risk");

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
		_lastHigh = 0m;
		_lastLow = 0m;
		_entryPrice = 0m;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_direction = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var highest = new Highest { Length = ZigzagLength };
		var lowest = new Lowest { Length = ZigzagLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();

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

		// Update ZigZag pivots.
		if (candle.HighPrice >= highest && _direction != 1)
		{
			_lastHigh = candle.HighPrice;
			_direction = 1;
		}
		else if (candle.LowPrice <= lowest && _direction != -1)
		{
			_lastLow = candle.LowPrice;
			_direction = -1;
		}

		var step = Security.PriceStep ?? 1m;

		// Entry signals.
		if (_lastHigh != 0m && candle.ClosePrice > _lastHigh && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_highestSinceEntry = _entryPrice;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_lastLow != 0m && candle.ClosePrice < _lastLow && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_lowestSinceEntry = _entryPrice;
			SellMarket(Volume + Math.Abs(Position));
		}

		// Manage open position.
		if (Position > 0)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);

			var stopPrice = _entryPrice - StopLoss * step;
			var takePrice = _entryPrice + TakeProfit * step;
			var trailPrice = _highestSinceEntry - TrailingStop * step;

			if (candle.LowPrice <= stopPrice || candle.HighPrice >= takePrice || candle.LowPrice <= trailPrice)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

			var stopPrice = _entryPrice + StopLoss * step;
			var takePrice = _entryPrice - TakeProfit * step;
			var trailPrice = _lowestSinceEntry + TrailingStop * step;

			if (candle.HighPrice >= stopPrice || candle.LowPrice <= takePrice || candle.HighPrice >= trailPrice)
				BuyMarket(Math.Abs(Position));
		}
	}
}
