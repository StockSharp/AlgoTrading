using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Linear Regression Channel strategy: uses WMA crossover with momentum confirmation.
/// Buys when fast WMA crosses above slow WMA with rising momentum,
/// sells when fast WMA crosses below slow WMA with falling momentum.
/// </summary>
public class LinearRegressionChannelFibStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;

	private decimal? _prevFastMa;
	private decimal? _prevSlowMa;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	public LinearRegressionChannelFibStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Fast weighted MA length", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Slow weighted MA length", "Indicators");

		_momentumLength = Param(nameof(MomentumLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum", "Momentum lookback", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFastMa = null;
		_prevSlowMa = null;

		var fastWma = new WeightedMovingAverage { Length = FastMaLength };
		var slowWma = new WeightedMovingAverage { Length = SlowMaLength };
		var momentum = new Momentum { Length = MomentumLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastWma, slowWma, momentum, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullMomentum = momentum > 100m;
		var bearMomentum = momentum < 100m;

		if (_prevFastMa.HasValue && _prevSlowMa.HasValue)
		{
			// Buy: fast crosses above slow + bullish momentum
			if (_prevFastMa.Value <= _prevSlowMa.Value && fastMa > slowMa && bullMomentum && Position <= 0)
			{
				BuyMarket();
			}
			// Sell: fast crosses below slow + bearish momentum
			else if (_prevFastMa.Value >= _prevSlowMa.Value && fastMa < slowMa && bearMomentum && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevFastMa = fastMa;
		_prevSlowMa = slowMa;
	}
}
