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
/// Strategy that trades based on percentage change momentum of candles.
/// Simplified from the original multi-pair currency strength approach to single-security.
/// </summary>
public class CurrencyStrengthV11Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _differenceThreshold;

	private decimal? _prevChange;
	private decimal _entryPrice;

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum percentage change to trigger trades.
	/// </summary>
	public decimal DifferenceThreshold
	{
		get => _differenceThreshold.Value;
		set => _differenceThreshold.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CurrencyStrengthV11Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strength calculation", "General");

		_differenceThreshold = Param(nameof(DifferenceThreshold), 0.1m)
			.SetDisplay("Threshold", "Minimum percentage change to trigger trade", "Parameters")
			.SetOptimize(0.05m, 1m, 0.05m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevChange = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		var change = candle.OpenPrice != 0m
			? (candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice * 100m
			: 0m;

		if (_prevChange == null)
		{
			_prevChange = change;
			return;
		}

		var momentum = change - _prevChange.Value;

		if (momentum > DifferenceThreshold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (momentum < -DifferenceThreshold && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}

		_prevChange = change;
	}
}
