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
/// Break-even management strategy that enters on SMA crossover and moves
/// the exit level to break-even once price moves a configurable distance in favor.
/// Simplified from the MetaTrader "Breakeven v3" utility.
/// </summary>
public class BreakevenV3Strategy : Strategy
{
	private readonly StrategyParam<int> _deltaPoints;
	private readonly StrategyParam<int> _activationPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;
	private decimal _entryPrice;
	private decimal _breakEvenPrice;
	private bool _breakEvenActivated;
	private decimal _pointValue;

	/// <summary>
	/// Extra offset from break-even price in points.
	/// </summary>
	public int DeltaPoints
	{
		get => _deltaPoints.Value;
		set => _deltaPoints.Value = value;
	}

	/// <summary>
	/// Distance in points price must move in favor before break-even activates.
	/// </summary>
	public int ActivationPoints
	{
		get => _activationPoints.Value;
		set => _activationPoints.Value = value;
	}

	/// <summary>
	/// Candle type for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BreakevenV3Strategy()
	{
		_deltaPoints = Param(nameof(DeltaPoints), 100)
			.SetNotNegative()
			.SetDisplay("Delta (points)", "Offset from entry for break-even stop", "General")
			.SetOptimize(10, 300, 10);

		_activationPoints = Param(nameof(ActivationPoints), 200)
			.SetNotNegative()
			.SetDisplay("Activation (points)", "Distance price must move before break-even activates", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for signals", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_fastMa = null;
		_slowMa = null;
		_entryPrice = 0m;
		_breakEvenPrice = 0m;
		_breakEvenActivated = false;
		_pointValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pointValue = Security?.PriceStep ?? 1m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		_fastMa = new SimpleMovingAverage { Length = 10 };
		_slowMa = new SimpleMovingAverage { Length = 30 };

		SubscribeCandles(CandleType)
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		var price = candle.ClosePrice;

		// Manage break-even for open position
		if (Position != 0 && _entryPrice > 0m)
		{
			var activationDistance = ActivationPoints * _pointValue;
			var deltaOffset = DeltaPoints * _pointValue;

			if (Position > 0)
			{
				// Activate break-even when price moves sufficiently in our favor
				if (!_breakEvenActivated && activationDistance > 0m && price >= _entryPrice + activationDistance)
				{
					_breakEvenActivated = true;
					_breakEvenPrice = _entryPrice + deltaOffset;
				}

				// Check break-even exit
				if (_breakEvenActivated && price <= _breakEvenPrice)
				{
					SellMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
			}
			else if (Position < 0)
			{
				if (!_breakEvenActivated && activationDistance > 0m && price <= _entryPrice - activationDistance)
				{
					_breakEvenActivated = true;
					_breakEvenPrice = _entryPrice - deltaOffset;
				}

				if (_breakEvenActivated && price >= _breakEvenPrice)
				{
					BuyMarket(Math.Abs(Position));
					ResetPosition();
					return;
				}
			}
		}

		// Entry: MA crossover
		if (Position == 0)
		{
			if (fastValue > slowValue)
			{
				BuyMarket();
				_entryPrice = price;
				_breakEvenActivated = false;
			}
			else if (fastValue < slowValue)
			{
				SellMarket();
				_entryPrice = price;
				_breakEvenActivated = false;
			}
		}
	}

	private void ResetPosition()
	{
		_entryPrice = 0m;
		_breakEvenPrice = 0m;
		_breakEvenActivated = false;
	}
}
