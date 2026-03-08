using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA scalper strategy combining fast/slow EMA trend with stochastic momentum.
/// Buys when fast EMA > slow EMA and stochastic is oversold.
/// Sells when fast EMA below slow EMA and stochastic is overbought.
/// </summary>
public class ScalperEmaSimpleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<DataType> _candleType;

	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ScalperEmaSimpleStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 10)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 30)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stochastic Oversold", "Oversold level", "Indicators");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stochastic Overbought", "Overbought level", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		var stochK = new StochasticK { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, stochK, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal stochK)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Buy: uptrend (fast > slow) + stochastic oversold
		if (fastValue > slowValue && stochK < StochOversold && Position <= 0)
		{
			BuyMarket();
		}
		// Sell: downtrend (fast < slow) + stochastic overbought
		else if (fastValue < slowValue && stochK > StochOverbought && Position >= 0)
		{
			SellMarket();
		}
	}
}
