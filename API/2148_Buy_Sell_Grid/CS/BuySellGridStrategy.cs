using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy using EMA mean-reversion.
/// Buys when price drops below EMA by a threshold, sells when it rises above.
/// </summary>
public class BuySellGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gridStepPct;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>
	/// Grid step as percentage from EMA.
	/// </summary>
	public decimal GridStepPct
	{
		get => _gridStepPct.Value;
		set => _gridStepPct.Value = value;
	}

	/// <summary>
	/// EMA period for mean reference.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used to trigger strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BuySellGridStrategy"/>.
	/// </summary>
	public BuySellGridStrategy()
	{
		_gridStepPct = Param(nameof(GridStepPct), 0.3m)
			.SetDisplay("Grid Step %", "Distance from EMA for grid entry", "General")
			.SetGreaterThanZero();

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetDisplay("EMA Period", "EMA period for grid center", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for processing", "General");
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

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var lowerGrid = emaValue * (1m - GridStepPct / 100m);
		var upperGrid = emaValue * (1m + GridStepPct / 100m);

		if (Position == 0)
		{
			if (close <= lowerGrid)
			{
				BuyMarket();
				_entryPrice = close;
			}
			else if (close >= upperGrid)
			{
				SellMarket();
				_entryPrice = close;
			}
		}
		else if (Position > 0)
		{
			// Take profit at EMA or above
			if (close >= emaValue)
			{
				SellMarket();
			}
			// Add on further dip
			else if (close <= _entryPrice * (1m - GridStepPct / 100m))
			{
				BuyMarket();
				_entryPrice = close;
			}
		}
		else if (Position < 0)
		{
			// Take profit at EMA or below
			if (close <= emaValue)
			{
				BuyMarket();
			}
			// Add on further rally
			else if (close >= _entryPrice * (1m + GridStepPct / 100m))
			{
				SellMarket();
				_entryPrice = close;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
	}
}
