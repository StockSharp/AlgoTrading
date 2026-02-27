using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FrAMA candle trend-following strategy.
/// Uses FrAMA indicator for trend detection and trades on direction changes.
/// </summary>
public class FramaCandleTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _framaPeriod;

	private decimal _prevFramaValue;
	private decimal _prevPrevFramaValue;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FramaPeriod
	{
		get => _framaPeriod.Value;
		set => _framaPeriod.Value = value;
	}

	public FramaCandleTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");

		_framaPeriod = Param(nameof(FramaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("FrAMA Period", "Length of the Fractal Adaptive Moving Average", "Indicator");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_prevFramaValue = 0;
		_prevPrevFramaValue = 0;

		var frama = new FractalAdaptiveMovingAverage { Length = FramaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(frama, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, frama);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal framaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFramaValue = framaValue;
			_hasPrev = true;
			return;
		}

		// Trend direction from FrAMA slope
		var rising = framaValue > _prevFramaValue;
		var falling = framaValue < _prevFramaValue;
		var wasRising = _prevFramaValue > _prevPrevFramaValue;
		var wasFalling = _prevFramaValue < _prevPrevFramaValue;

		// Buy on transition from falling to rising
		if (rising && wasFalling && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell on transition from rising to falling
		else if (falling && wasRising && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrevFramaValue = _prevFramaValue;
		_prevFramaValue = framaValue;
	}
}
