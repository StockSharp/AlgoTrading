using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko level breakout strategy converted from the MetaTrader expert.
/// Trades when the rounded Renko level shifts up or down and optionally reverses signals.
/// </summary>
public class RenkoLevelStrategy : Strategy
{
	private readonly StrategyParam<int> _blockSize;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<bool> _allowIncrease;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _upperLevel;
	private decimal _lowerLevel;
	private bool _hasLevels;

	/// <summary>
	/// Renko block size expressed in pips.
	/// </summary>
	public int BlockSize
	{
		get => _blockSize.Value;
		set => _blockSize.Value = value;
	}

	/// <summary>
	/// Inverts the direction of generated trade signals.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
	}

	/// <summary>
	/// Allows adding to an existing position when true.
	/// </summary>
	public bool AllowIncrease
	{
		get => _allowIncrease.Value;
		set => _allowIncrease.Value = value;
	}

	/// <summary>
	/// Source candle type used to emulate Renko levels.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RenkoLevelStrategy"/> class.
	/// </summary>
	public RenkoLevelStrategy()
	{
		_blockSize = Param(nameof(BlockSize), 30)
			.SetGreaterThanZero()
			.SetDisplay("Block Size", "Renko block size in pips", "Renko")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse", "Invert trading signals", "General");

		_allowIncrease = Param(nameof(AllowIncrease), false)
			.SetDisplay("Allow Increase", "Add to positions instead of waiting for flat", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used to drive the Renko logic", "General");
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

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
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

		var previousUpper = _upperLevel;
		var moved = false;

		if (close < _lowerLevel)
		{
			var (round, _, ceil) = CalculateLevels(close, brickSize);
			if (!AreEqual(round, _lowerLevel))
			{
				_lowerLevel = round;
				_upperLevel = ceil;
				moved = true;
			}
		}
		else if (close > _upperLevel)
		{
			var (round, floor, _) = CalculateLevels(close, brickSize);
			if (!AreEqual(round, _upperLevel))
			{
				_lowerLevel = floor;
				_upperLevel = round;
				moved = true;
			}
		}

		if (!moved)
			return;

		if (_upperLevel > previousUpper)
		{
			if (!Reverse)
			{
				EnterLong();
			}
			else
			{
				EnterShort();
			}
		}
		else if (_upperLevel < previousUpper)
		{
			if (!Reverse)
			{
				EnterShort();
			}
			else
			{
				EnterLong();
			}
		}
	}

	private void EnterLong()
	{
		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position) + Volume);
			return;
		}

		if (!AllowIncrease && Position > 0)
			return;

		BuyMarket(Volume);
	}

	private void EnterShort()
	{
		if (Position > 0)
		{
			SellMarket(Math.Abs(Position) + Volume);
			return;
		}

		if (!AllowIncrease && Position < 0)
			return;

		SellMarket(Volume);
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
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 0;
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;
		return step * adjust * BlockSize;
	}

	private bool AreEqual(decimal first, decimal second)
	{
		var tolerance = (Security?.PriceStep ?? 0.0001m) / 10m;
		return Math.Abs(first - second) <= tolerance;
	}
}
