using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dematus strategy using DEMA crossover for trend detection.
/// </summary>
public class DematusStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public DematusStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast DEMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow DEMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = null;
		_prevSlow = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fast = new DoubleExponentialMovingAverage { Length = FastPeriod };
		var slow = new DoubleExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		if (_prevFast == null || _prevSlow == null)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var prevAbove = _prevFast.Value > _prevSlow.Value;
		var currAbove = fastVal > slowVal;

		_prevFast = fastVal;
		_prevSlow = slowVal;

		if (!prevAbove && currAbove)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (prevAbove && !currAbove)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}
	}
}
