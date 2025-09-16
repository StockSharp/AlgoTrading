using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades breakouts of Fibonacci retracement levels derived from ZigZag swings.
/// The strategy builds Fibonacci levels between consecutive pivot points and enters when price crosses the 50% level.
/// </summary>
public class ZZFiboTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _breakout;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPivot;
	private decimal _currPivot;
	private int _direction;
	private decimal _level38;
	private decimal _level50;
	private decimal _level61;

	/// <summary>
	/// Minimum distance beyond level to confirm breakout.
	/// </summary>
	public decimal Breakout
	{
		get => _breakout.Value;
		set => _breakout.Value = value;
	}

	/// <summary>
	/// Absolute stop loss distance.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Number of bars to search for pivots.
	/// </summary>
	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
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
	/// Initializes a new instance of the <see cref="ZZFiboTraderStrategy"/> class.
	/// </summary>
	public ZZFiboTraderStrategy()
	{
		_breakout = Param(nameof(Breakout), 5m)
			.SetDisplay("Breakout", "Minimum distance beyond level to confirm breakout", "General")
			.SetGreaterOrEqualZero();

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Absolute stop loss distance", "Risk")
			.SetGreaterOrEqualZero();

		_zigZagDepth = Param(nameof(ZigZagDepth), 12)
			.SetDisplay("ZigZag Depth", "Number of bars to search for pivots", "ZigZag")
			.SetGreaterThanZero();

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
		_prevPivot = 0m;
		_currPivot = 0m;
		_direction = 0;
		_level38 = _level50 = _level61 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var highest = new Highest { Length = ZigZagDepth };
		var lowest = new Lowest { Length = ZigZagDepth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}

		if (StopLoss > 0)
			StartProtection(stopLoss: new Unit(StopLoss, UnitTypes.Absolute));
		else
			StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update pivots when new extremes appear
		if (candle.HighPrice >= highest && candle.HighPrice != _currPivot)
		{
			_prevPivot = _currPivot;
			_currPivot = candle.HighPrice;
			UpdateLevels();
		}
		else if (candle.LowPrice <= lowest && candle.LowPrice != _currPivot)
		{
			_prevPivot = _currPivot;
			_currPivot = candle.LowPrice;
			UpdateLevels();
		}

		if (_direction == 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		// Enter when price crosses 50% level in direction of trend with breakout confirmation
		if (_direction == 1 && Position <= 0 &&
			candle.ClosePrice > _level50 + Breakout * Security.PriceStep)
		{
			BuyMarket();
		}
		else if (_direction == -1 && Position >= 0 &&
			candle.ClosePrice < _level50 - Breakout * Security.PriceStep)
		{
			SellMarket();
		}
	}

	private void UpdateLevels()
	{
		if (_prevPivot == 0m || _currPivot == 0m)
			return;

		_direction = _currPivot > _prevPivot ? 1 : -1;

		var high = _direction == 1 ? _currPivot : _prevPivot;
		var low = _direction == 1 ? _prevPivot : _currPivot;

		var range = high - low;
		_level38 = high - range * 0.382m;
		_level50 = high - range * 0.5m;
		_level61 = high - range * 0.618m;
	}
}
