using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple SMA strategy that trades when three moving averages are properly aligned.
/// Enters long when fast > middle > slow, enters short when fast &lt; middle &lt; slow.
/// </summary>
public class TripleSmaSpreadStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _middlePeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private int _prevSignal;

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

	public int MiddlePeriod
	{
		get => _middlePeriod.Value;
		set => _middlePeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public TripleSmaSpreadStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast SMA period", "Indicators");

		_middlePeriod = Param(nameof(MiddlePeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Middle Period", "Middle SMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 29)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow SMA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSignal = 0;

		var fastSma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowSma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var signal = 0;
		if (fast > slow && close > fast)
			signal = 1;
		else if (fast < slow && close < fast)
			signal = -1;

		if (signal == _prevSignal)
			return;

		var oldSignal = _prevSignal;
		_prevSignal = signal;

		if (signal == 1 && oldSignal <= 0)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (signal == -1 && oldSignal >= 0)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}
	}
}
