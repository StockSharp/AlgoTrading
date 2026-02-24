using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Conditional Position Opener" MetaTrader expert.
/// Uses Momentum indicator to conditionally open long or short positions.
/// Opens long when momentum is positive, short when negative.
/// </summary>
public class ConditionalPositionOpenerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momentumPeriod;

	private Momentum _momentum;
	private decimal? _prevMomentum;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public ConditionalPositionOpenerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal generation", "General");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum indicator period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMomentum = null;
		_momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_momentum.IsFormed)
		{
			_prevMomentum = momentumValue;
			return;
		}

		if (_prevMomentum is null)
		{
			_prevMomentum = momentumValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Cross above 100 (positive momentum)
		var crossUp = _prevMomentum.Value <= 100 && momentumValue > 100;
		// Cross below 100 (negative momentum)
		var crossDown = _prevMomentum.Value >= 100 && momentumValue < 100;

		if (crossUp)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevMomentum = momentumValue;
	}
}
