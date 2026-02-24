using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko level breakout strategy. Emulates Renko bricks on time-based candles
/// and trades when the rounded level shifts up or down.
/// </summary>
public class RenkoLevelStrategy : Strategy
{
	private readonly StrategyParam<int> _blockSize;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _upperLevel;
	private decimal _lowerLevel;
	private bool _hasLevels;

	public int BlockSize
	{
		get => _blockSize.Value;
		set => _blockSize.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public RenkoLevelStrategy()
	{
		_blockSize = Param(nameof(BlockSize), 500)
			.SetGreaterThanZero()
			.SetDisplay("Block Size", "Renko block size in price steps", "Renko");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var brickSize = GetBrickSize();
		if (brickSize <= 0m)
			return;

		var close = candle.ClosePrice;

		if (!_hasLevels)
		{
			InitializeLevels(close, brickSize);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var previousUpper = _upperLevel;
		var moved = false;

		if (close < _lowerLevel)
		{
			var (round, _, ceil) = CalculateLevels(close, brickSize);
			if (Math.Abs(round - _lowerLevel) > brickSize * 0.01m)
			{
				_lowerLevel = round;
				_upperLevel = ceil;
				moved = true;
			}
		}
		else if (close > _upperLevel)
		{
			var (round, floor, _) = CalculateLevels(close, brickSize);
			if (Math.Abs(round - _upperLevel) > brickSize * 0.01m)
			{
				_lowerLevel = floor;
				_upperLevel = round;
				moved = true;
			}
		}

		if (!moved)
			return;

		if (_upperLevel > previousUpper && Position <= 0)
			BuyMarket();
		else if (_upperLevel < previousUpper && Position >= 0)
			SellMarket();
	}

	private void InitializeLevels(decimal price, decimal brickSize)
	{
		var (round, floor, _) = CalculateLevels(price, brickSize);
		_upperLevel = round;
		_lowerLevel = floor;
		_hasLevels = true;
	}

	private (decimal round, decimal floor, decimal ceil) CalculateLevels(decimal price, decimal brickSize)
	{
		var ratio = price / brickSize;
		var rounded = Math.Round(ratio, 0, MidpointRounding.AwayFromZero);
		var priceRound = rounded * brickSize;
		var priceFloor = priceRound - brickSize;
		var priceCeil = priceRound + brickSize;
		return (priceRound, priceFloor, priceCeil);
	}

	private decimal GetBrickSize()
	{
		var step = Security?.PriceStep ?? 0.01m;
		return step * BlockSize;
	}
}
