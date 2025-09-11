namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Template strategy converted from TradingView script.
/// </summary>
public class DefaultStrategyTemplateStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss multiplier.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Risk reward ratio.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DefaultStrategyTemplateStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 20)
						 .SetGreaterThanZero()
						 .SetDisplay("ATR Period", "ATR length for stop loss", "Risk Management")
						 .SetCanOptimize(true)
						 .SetOptimize(10, 40, 5);

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 1m)
								  .SetGreaterThanZero()
								  .SetDisplay("Stop Loss Multiplier", "ATR multiplier for stop loss", "Risk Management")
								  .SetCanOptimize(true)
								  .SetOptimize(0.5m, 2m, 0.5m);

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 1m)
							   .SetGreaterThanZero()
							   .SetDisplay("Risk Reward Ratio", "Take profit multiple of stop loss", "Risk Management")
							   .SetCanOptimize(true)
							   .SetOptimize(0.5m, 2m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new ATR { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Implement your trading logic here using candle and ATR value.
	}
}
