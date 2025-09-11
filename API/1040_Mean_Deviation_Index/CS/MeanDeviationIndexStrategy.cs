using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean Deviation Index strategy.
/// Opens long when MDX exceeds the level and short when it drops below the negative level.
/// </summary>
public class MeanDeviationIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _level;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// EMA calculation period.
	/// </summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier used in deviation.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Deviation level to trigger trades.
	/// </summary>
	public decimal Level { get => _level.Value; set => _level.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MeanDeviationIndexStrategy"/> class.
	/// </summary>
	public MeanDeviationIndexStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR deviation factor", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_level = Param(nameof(Level), 0m)
			.SetDisplay("Level", "Threshold to trigger trades", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0m, 10m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var dev = candle.ClosePrice - emaValue;
		var atrVal = atrValue * AtrMultiplier;
		var mdx = dev > 0m ? Math.Max(dev - atrVal, 0m) : Math.Min(dev + atrVal, 0m);

		if (mdx > Level && Position <= 0)
		{
			BuyMarket();
		}
		else if (mdx < -Level && Position >= 0)
		{
			SellMarket();
		}
	}
}
