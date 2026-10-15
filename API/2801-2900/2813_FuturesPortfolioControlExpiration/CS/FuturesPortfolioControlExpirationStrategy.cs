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
/// Monitors position and rebalances to maintain a target exposure.
/// Simplified from the multi-leg futures portfolio controller to single security.
/// </summary>
public class FuturesPortfolioControlExpirationStrategy : Strategy
{
	private readonly StrategyParam<int> _targetPosition;
	private readonly StrategyParam<int> _rebalancePeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private int _barCount;

	/// <summary>
	/// Target position size. Positive for long, negative for short.
	/// </summary>
	public int TargetPosition
	{
		get => _targetPosition.Value;
		set => _targetPosition.Value = value;
	}

	/// <summary>
	/// Number of bars between rebalance checks.
	/// </summary>
	public int RebalancePeriod
	{
		get => _rebalancePeriod.Value;
		set => _rebalancePeriod.Value = value;
	}

	/// <summary>
	/// Candle type used as heartbeat for monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public FuturesPortfolioControlExpirationStrategy()
	{
		_targetPosition = Param(nameof(TargetPosition), 1)
			.SetDisplay("Target Position", "Desired position size (positive=long, negative=short)", "Portfolio");

		_rebalancePeriod = Param(nameof(RebalancePeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Rebalance Period", "Number of bars between rebalance checks", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for monitoring", "General");
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
		_sma = null;
		_barCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = 20 };

		SubscribeCandles(CandleType)
			.Bind(_sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		_barCount++;

		var price = candle.ClosePrice;
		var target = (decimal)TargetPosition;

		// Rebalance: ensure position matches target
		if (_barCount % RebalancePeriod == 0)
		{
			var current = Position;
			var diff = target - current;

			if (diff > 0)
				BuyMarket(Math.Abs(diff));
			else if (diff < 0)
				SellMarket(Math.Abs(diff));
		}

		// Trend reversal exit and re-entry
		if (Position > 0 && price < smaValue)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && price > smaValue)
		{
			BuyMarket(Math.Abs(Position));
		}
		else if (Position == 0)
		{
			if (target > 0 && price > smaValue)
				BuyMarket(Math.Abs(target));
			else if (target < 0 && price < smaValue)
				SellMarket(Math.Abs(target));
			else if (target > 0)
				BuyMarket(Math.Abs(target));
			else if (target < 0)
				SellMarket(Math.Abs(target));
		}
	}
}
