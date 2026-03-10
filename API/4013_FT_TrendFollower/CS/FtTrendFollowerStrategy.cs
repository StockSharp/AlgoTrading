using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using EMA crossover with MACD confirmation.
/// Buys when fast EMA crosses above slow EMA and MACD histogram is positive.
/// Sells when fast EMA crosses below slow EMA and MACD histogram is negative.
/// </summary>
public class FtTrendFollowerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevMacd;
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

	public FtTrendFollowerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0;
		_prevSlow = 0;
		_prevMacd = 0;
		_isReady = false;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_prevMacd = 0;
		_isReady = false;

		var fast = new EMA { Length = FastPeriod };
		var slow = new EMA { Length = SlowPeriod };
		var macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, macd, OnProcess)
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

	private void OnProcess(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			_prevMacd = macdVal;
			_isReady = true;
			return;
		}

		// Buy: fast crosses above slow + MACD positive
		if (_prevFast <= _prevSlow && fastVal > slowVal && macdVal > 0)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Sell: fast crosses below slow + MACD negative
		else if (_prevFast >= _prevSlow && fastVal < slowVal && macdVal < 0)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
		_prevMacd = macdVal;
	}
}
