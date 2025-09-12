using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean reversion strategy with incremental entries around a moving average.
/// </summary>
public class MeanReversionWithIncrementalEntryStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _initialPercent;
	private readonly StrategyParam<decimal> _percentStep;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _lastBuyPrice;
	private decimal? _lastSellPrice;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Percent difference from MA for the first entry.
	/// </summary>
	public decimal InitialPercent
	{
		get => _initialPercent.Value;
		set => _initialPercent.Value = value;
	}

	/// <summary>
	/// Percent step for additional entries.
	/// </summary>
	public decimal PercentStep
	{
		get => _percentStep.Value;
		set => _percentStep.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public MeanReversionWithIncrementalEntryStrategy()
	{
		_maLength = Param(nameof(MaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_initialPercent = Param(nameof(InitialPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Percent", "Percent from MA for first entry", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_percentStep = Param(nameof(PercentStep), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Percent Step", "Additional order percent step", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastBuyPrice = null;
		_lastSellPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SMA { Length = MaLength };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var low = candle.LowPrice;
		var high = candle.HighPrice;
		var close = candle.ClosePrice;

		if (low < maValue)
		{
			if (_lastBuyPrice is null)
			{
				if (PricePercentDiff(low, maValue) >= InitialPercent)
				{
					BuyMarket();
					_lastBuyPrice = low;
				}
			}
			else if (low < _lastBuyPrice && PricePercentDiff(low, _lastBuyPrice.Value) >= PercentStep)
			{
				BuyMarket();
				_lastBuyPrice = low;
			}
		}

		if (high > maValue)
		{
			if (_lastSellPrice is null)
			{
				if (PricePercentDiff(high, maValue) >= InitialPercent)
				{
					SellMarket();
					_lastSellPrice = high;
				}
			}
			else if (high > _lastSellPrice && PricePercentDiff(high, _lastSellPrice.Value) >= PercentStep)
			{
				SellMarket();
				_lastSellPrice = high;
			}
		}

		if (close >= maValue && Position > 0)
		{
			SellMarket(Position);
			_lastBuyPrice = null;
		}
		else if (close <= maValue && Position < 0)
		{
			BuyMarket(-Position);
			_lastSellPrice = null;
		}

		if (Position == 0)
		{
			_lastBuyPrice = null;
			_lastSellPrice = null;
		}
	}

	private static decimal PricePercentDiff(decimal price1, decimal price2)
	{
		return Math.Abs(price1 - price2) / price2 * 100m;
	}
}
