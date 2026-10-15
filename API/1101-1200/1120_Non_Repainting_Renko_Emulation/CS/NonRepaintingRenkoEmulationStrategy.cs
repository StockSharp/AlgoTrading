using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Non-repainting Renko emulation strategy.
/// Renko bricks of BrickSize price units are built from finished candle closes only, so a brick never changes once formed.
/// When a new brick continues the direction of the previous brick the strategy enters in that direction (reversing an
/// opposite position); when a new brick reverses the direction, the open position is closed.
/// </summary>
public class NonRepaintingRenkoEmulationStrategy : Strategy
{
	private readonly StrategyParam<decimal> _brickSize;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _brickLevel;
	private int _prevDirection;

	/// <summary>
	/// Brick size in price units.
	/// </summary>
	public decimal BrickSize
	{
		get => _brickSize.Value;
		set => _brickSize.Value = value;
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
	public NonRepaintingRenkoEmulationStrategy()
	{
		_brickSize = Param(nameof(BrickSize), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Brick Size", "Brick size in price units", "Renko");

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
		_brickLevel = null;
		_prevDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_brickLevel = null;
		_prevDirection = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_brickLevel is not decimal level)
		{
			_brickLevel = close;
			return;
		}

		var bricks = (int)Math.Floor(Math.Abs(close - level) / BrickSize);
		if (bricks == 0)
			return;

		var direction = close > level ? 1 : -1;
		_brickLevel = level + direction * bricks * BrickSize;

		// With several bricks in one candle the previous brick has the same direction.
		var prevDirection = bricks > 1 ? direction : _prevDirection;
		_prevDirection = direction;

		if (prevDirection == 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (direction == prevDirection)
		{
			if (direction > 0 && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (direction < 0 && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else if (direction < 0 && Position > 0)
			SellMarket(Position);
		else if (direction > 0 && Position < 0)
			BuyMarket(-Position);
	}
}
