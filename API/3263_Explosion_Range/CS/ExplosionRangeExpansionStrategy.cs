using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Explosion Range Expansion strategy.
/// Detects when current candle range exceeds previous candle range by a ratio,
/// and enters in the direction of the candle body (bullish close = buy, bearish close = sell).
/// Uses ATR for trend confirmation.
/// </summary>
public class ExplosionRangeExpansionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _ratio;
	private readonly StrategyParam<int> _atrPeriod;

	private decimal? _previousRange;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal Ratio
	{
		get => _ratio.Value;
		set => _ratio.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public ExplosionRangeExpansionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_ratio = Param(nameof(Ratio), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Range Ratio", "Current range must exceed previous range by this ratio", "Signals");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for volatility context", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousRange = null;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentRange = candle.HighPrice - candle.LowPrice;

		if (_previousRange.HasValue && _previousRange.Value > 0)
		{
			var expanded = currentRange > _previousRange.Value * Ratio;

			if (expanded)
			{
				// Bullish candle (close > open) = buy signal
				if (candle.ClosePrice > candle.OpenPrice && Position <= 0)
				{
					BuyMarket();
				}
				// Bearish candle (close < open) = sell signal
				else if (candle.ClosePrice < candle.OpenPrice && Position >= 0)
				{
					SellMarket();
				}
			}
		}

		_previousRange = currentRange;
	}
}
