using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FiboChannel Line strategy: WMA crossover with linear regression slope filter.
/// Buys on bullish WMA cross when slope is positive, sells on bearish cross when slope is negative.
/// </summary>
public class FiboChannelLineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _slopePeriod;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _prevLr;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int SlopePeriod
	{
		get => _slopePeriod.Value;
		set => _slopePeriod.Value = value;
	}

	public FiboChannelLineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast weighted MA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow weighted MA period", "Indicators");

		_slopePeriod = Param(nameof(SlopePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slope Period", "Linear regression period for slope", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;
		_prevLr = null;

		var fastWma = new WeightedMovingAverage { Length = FastMaPeriod };
		var slowWma = new WeightedMovingAverage { Length = SlowMaPeriod };
		var lr = new LinearReg { Length = SlopePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastWma, slowWma, lr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastWma);
			DrawIndicator(area, slowWma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal lrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var slope = _prevLr.HasValue ? lrValue - _prevLr.Value : 0m;

		if (_prevFast.HasValue && _prevSlow.HasValue)
		{
			if (_prevFast.Value <= _prevSlow.Value && fast > slow && slope > 0 && Position <= 0)
			{
				BuyMarket();
			}
			else if (_prevFast.Value >= _prevSlow.Value && fast < slow && slope < 0 && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevLr = lrValue;
	}
}
