namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Martingale MA Breakout strategy (simplified).
/// Enters long when price crosses above EMA, enters short when below.
/// Uses simple position flipping with market orders.
/// </summary>
public class MartingaleMaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public MartingaleMaBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");

		_maPeriod = Param(nameof(MaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for volatility filter", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, (ICandleMessage candle, decimal emaValue, decimal atrValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var distance = Math.Abs(candle.ClosePrice - emaValue);

				// Buy when price breaks above MA by at least half ATR
				if (candle.ClosePrice > emaValue && distance > atrValue && Position <= 0)
				{
					BuyMarket();
				}
				// Sell when price breaks below MA by at least half ATR
				else if (candle.ClosePrice < emaValue && distance > atrValue && Position >= 0)
				{
					SellMarket();
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}
}
