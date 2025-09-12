using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean reversion channel strategy.
/// Sells when price closes above upper band.
/// Buys when price closes below lower band.
/// Closes positions when price returns to mean line.
/// </summary>
public class ScreenerMeanReversionChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Lookback period for mean line and ATR.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// ATR multiplier for channel width.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public ScreenerMeanReversionChannelStrategy()
	{
		_length = Param(nameof(Length), 200)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for mean and ATR", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

		_multiplier = Param(nameof(Multiplier), 2.415m)
			.SetGreaterThanZero()
			.SetDisplay("Channel Multiplier", "ATR multiplier for channel", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");
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

		var mean = new SMA { Length = Length };
		var atr = new AverageTrueRange { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mean, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal meanValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upper = meanValue + atrValue * Multiplier;
		var lower = meanValue - atrValue * Multiplier;

		if (candle.ClosePrice > upper && Position >= 0)
		{
			SellMarket();
		}
		else if (candle.ClosePrice < lower && Position <= 0)
		{
			BuyMarket();
		}
		else if (Position > 0 && candle.ClosePrice >= meanValue)
		{
			SellMarket();
		}
		else if (Position < 0 && candle.ClosePrice <= meanValue)
		{
			BuyMarket();
		}
	}
}
