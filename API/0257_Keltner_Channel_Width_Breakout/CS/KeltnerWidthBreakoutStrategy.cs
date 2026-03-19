using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Keltner Channel width breakouts.
/// When Keltner Channel width increases significantly above its average,
/// it enters position in the direction determined by price movement.
/// </summary>
public class KeltnerWidthBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _widthThreshold;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// EMA period for Keltner Channel.
	/// </summary>
	public int EMAPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for Keltner Channel.
	/// </summary>
	public int ATRPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for Keltner Channel.
	/// </summary>
	public decimal ATRMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Width threshold multiplier for breakout detection.
	/// </summary>
	public decimal WidthThreshold
	{
		get => _widthThreshold.Value;
		set => _widthThreshold.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="KeltnerWidthBreakoutStrategy"/>.
	/// </summary>
	public KeltnerWidthBreakoutStrategy()
	{
		_emaPeriod = Param(nameof(EMAPeriod), 20)
			.SetDisplay("EMA Period", "Period of EMA for Keltner Channel", "Indicators");

		_atrPeriod = Param(nameof(ATRPeriod), 14)
			.SetDisplay("ATR Period", "Period of ATR for Keltner Channel", "Indicators");

		_atrMultiplier = Param(nameof(ATRMultiplier), 2.0m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators");

		_widthThreshold = Param(nameof(WidthThreshold), 1.2m)
			.SetDisplay("Width Threshold", "Threshold multiplier for width breakout detection", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EMAPeriod };
		var atr = new AverageTrueRange { Length = ATRPeriod };
		var widthAverage = new SimpleMovingAverage { Length = Math.Max(5, EMAPeriod / 2) };

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, atr, (candle, emaValue, atrValue) =>
			{
				if (candle.State != CandleStates.Finished || atrValue <= 0)
					return;

				// Keltner width = (EMA + ATR*k) - (EMA - ATR*k) = 2*ATR*k
				var width = 2m * ATRMultiplier * atrValue;
				var avgWidthValue = widthAverage.Process(new DecimalIndicatorValue(widthAverage, width, candle.ServerTime) { IsFinal = true });

				if (!widthAverage.IsFormed)
					return;

				var avgWidth = avgWidthValue.ToDecimal();
				if (avgWidth <= 0)
					return;

				// Width breakout detection
				if (width > avgWidth * WidthThreshold && Position == 0)
				{
					// Determine direction based on price relative to EMA
					if (candle.ClosePrice > emaValue)
						BuyMarket();
					else if (candle.ClosePrice < emaValue)
						SellMarket();
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
