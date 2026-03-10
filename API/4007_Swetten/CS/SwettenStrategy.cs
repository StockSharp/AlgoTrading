using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Swetten strategy: multi-SMA spread crossover system.
/// Uses fast SMA (5) and slow SMA (50) to generate trend signals.
/// Buys when fast crosses above slow, sells when fast crosses below slow.
/// </summary>
public class SwettenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isReady;

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

	public SwettenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");

		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 34)
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0;
		_prevSlow = 0;
		_isReady = false;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_isReady = false;

		var fast = new SMA { Length = FastPeriod };
		var slow = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, OnProcess)
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

	private void OnProcess(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			_isReady = true;
			return;
		}

		// Fast crosses above slow = buy
		if (_prevFast <= _prevSlow && fastVal > slowVal)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Fast crosses below slow = sell
		else if (_prevFast >= _prevSlow && fastVal < slowVal)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
